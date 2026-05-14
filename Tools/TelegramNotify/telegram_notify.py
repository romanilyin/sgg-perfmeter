#!/usr/bin/env python3

import argparse
import json
import mimetypes
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
ENV_PATH = SCRIPT_DIR / ".env"

TELEGRAM_API_BASE = "https://api.telegram.org/bot"
MAX_MESSAGE_LENGTH = 4096
MAX_CAPTION_LENGTH = 1024
MAX_PHOTO_BYTES = 10 * 1024 * 1024
MAX_DOCUMENT_BYTES = 50 * 1024 * 1024


def load_env(path):
	if not path.exists():
		return

	for raw_line in path.read_text(encoding="utf-8").splitlines():
		line = raw_line.strip()
		if not line or line.startswith("#") or "=" not in line:
			continue

		key, value = line.split("=", 1)
		key = key.strip()
		value = value.strip().strip('"').strip("'")

		if key and key not in os.environ:
			os.environ[key] = value


def get_token():
	token = os.environ.get("TELEGRAM_BOT_TOKEN", "").strip()
	if not token:
		raise RuntimeError("Не задан TELEGRAM_BOT_TOKEN")

	return token


def resolve_recipient(name_or_id):
	value = name_or_id.strip()
	if not value:
		return None

	if value.lstrip("-").isdigit():
		return value

	env_key = f"TELEGRAM_RECIPIENT_{value.upper()}"
	recipient_id = os.environ.get(env_key, "").strip()
	if not recipient_id:
		raise RuntimeError(f"Не найден получатель '{value}'. Ожидалась переменная {env_key}")

	return recipient_id


def resolve_recipients(raw_recipients):
	if raw_recipients:
		values = raw_recipients
	else:
		defaults = os.environ.get("TELEGRAM_DEFAULT_RECIPIENTS", "").strip()
		values = [item.strip() for item in defaults.split(",") if item.strip()]

	recipients = []
	for item in values:
		recipient = resolve_recipient(item)
		if recipient is not None:
			recipients.append(recipient)

	if not recipients:
		raise RuntimeError("Не заданы получатели. Используй --to или TELEGRAM_DEFAULT_RECIPIENTS")

	return recipients


def request_json(token, method, data):
	url = f"{TELEGRAM_API_BASE}{token}/{method}"
	encoded_data = urllib.parse.urlencode(data).encode("utf-8")
	request = urllib.request.Request(url, data=encoded_data, method="POST")

	with urllib.request.urlopen(request, timeout=60) as response:
		payload = response.read().decode("utf-8")

	result = json.loads(payload)
	if not result.get("ok"):
		raise RuntimeError(f"Telegram API error: {result}")

	return result


def request_multipart(token, method, fields, file_field, file_path):
	boundary = "----RoadToIthakaTelegramBoundary"
	url = f"{TELEGRAM_API_BASE}{token}/{method}"

	file_name = file_path.name
	content_type = mimetypes.guess_type(file_name)[0] or "application/octet-stream"
	body = bytearray()

	for key, value in fields.items():
		body.extend(f"--{boundary}\r\n".encode("utf-8"))
		body.extend(f'Content-Disposition: form-data; name="{key}"\r\n\r\n'.encode("utf-8"))
		body.extend(str(value).encode("utf-8"))
		body.extend(b"\r\n")

	body.extend(f"--{boundary}\r\n".encode("utf-8"))
	body.extend(
		f'Content-Disposition: form-data; name="{file_field}"; filename="{file_name}"\r\n'.encode("utf-8")
	)
	body.extend(f"Content-Type: {content_type}\r\n\r\n".encode("utf-8"))
	body.extend(file_path.read_bytes())
	body.extend(b"\r\n")
	body.extend(f"--{boundary}--\r\n".encode("utf-8"))

	request = urllib.request.Request(url, data=bytes(body), method="POST")
	request.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
	request.add_header("Content-Length", str(len(body)))

	with urllib.request.urlopen(request, timeout=120) as response:
		payload = response.read().decode("utf-8")

	result = json.loads(payload)
	if not result.get("ok"):
		raise RuntimeError(f"Telegram API error: {result}")

	return result


def validate_text_length(text, limit, label):
	if text and len(text) > limit:
		raise RuntimeError(f"{label} длиннее лимита Telegram: {len(text)} > {limit}")


def validate_file_size(file_path, limit, label):
	file_size = file_path.stat().st_size
	if file_size > limit:
		raise RuntimeError(f"{label} больше лимита Telegram: {file_size} > {limit} bytes")


def send_message(token, chat_id, text):
	validate_text_length(text, MAX_MESSAGE_LENGTH, "Сообщение")
	return request_json(
		token,
		"sendMessage",
		{
			"chat_id": chat_id,
			"text": text,
			"disable_web_page_preview": "true",
		},
	)


def send_photo(token, chat_id, photo_path, caption):
	validate_text_length(caption, MAX_CAPTION_LENGTH, "Caption")
	validate_file_size(photo_path, MAX_PHOTO_BYTES, "Фото")
	return request_multipart(
		token,
		"sendPhoto",
		{
			"chat_id": chat_id,
			"caption": caption or "",
		},
		"photo",
		photo_path,
	)


def send_document(token, chat_id, document_path, caption):
	validate_text_length(caption, MAX_CAPTION_LENGTH, "Caption")
	validate_file_size(document_path, MAX_DOCUMENT_BYTES, "Документ")
	return request_multipart(
		token,
		"sendDocument",
		{
			"chat_id": chat_id,
			"caption": caption or "",
		},
		"document",
		document_path,
	)


def existing_file(path):
	file_path = Path(path).resolve()
	if not file_path.exists():
		raise RuntimeError(f"Файл не найден: {file_path}")

	if not file_path.is_file():
		raise RuntimeError(f"Путь не является файлом: {file_path}")

	return file_path


def parse_args():
	parser = argparse.ArgumentParser(description="Send Road to Ithaka pipeline notifications to Telegram.")
	parser.add_argument("--to", action="append", help="Получатель: alias из env или chat_id. Можно указать несколько раз.")
	parser.add_argument("--message", help="Текст сообщения.")
	parser.add_argument("--photo", help="Путь к скриншоту/изображению.")
	parser.add_argument("--document", help="Путь к файлу/артефакту.")
	parser.add_argument("--caption", default="", help="Подпись для фото или файла.")
	return parser.parse_args()


def main():
	load_env(ENV_PATH)
	args = parse_args()
	selected_actions = sum(1 for value in [args.message, args.photo, args.document] if value)

	if selected_actions != 1:
		raise RuntimeError("Нужно указать ровно одно действие: --message, --photo или --document")

	token = get_token()
	recipients = resolve_recipients(args.to)
	photo_path = existing_file(args.photo) if args.photo else None
	document_path = existing_file(args.document) if args.document else None

	for chat_id in recipients:
		if args.message:
			send_message(token, chat_id, args.message)
		elif photo_path:
			send_photo(token, chat_id, photo_path, args.caption)
		elif document_path:
			send_document(token, chat_id, document_path, args.caption)

	print(f"Telegram notification sent to {len(recipients)} recipient(s)")


if __name__ == "__main__":
	try:
		main()
	except urllib.error.HTTPError as error:
		details = error.read().decode("utf-8", errors="replace")
		print(f"HTTP error {error.code}: {details}", file=sys.stderr)
		sys.exit(1)
	except Exception as error:
		print(f"Telegram notification failed: {error}", file=sys.stderr)
		sys.exit(1)
