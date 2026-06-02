---
name: telegram-notify
description: Отправка уведомлений, скриншотов и файлов в Telegram из пайплайна Road to Ithaka
---

# Telegram Notify

Используй этот skill, когда нужно отправить пользователю уведомление, скриншот, лог или артефакт через Telegram.

## Расположение

Все файлы находятся выше `Assets/`:

- `Tools/TelegramNotify/telegram_notify.py`
- `Tools/TelegramNotify/.env`
- `Tools/TelegramNotify/.env.example`

## Получатели

Получатели задаются в `Tools/TelegramNotify/.env`.

```dotenv
TELEGRAM_RECIPIENT_STINGER=CHANGE_ME
TELEGRAM_DEFAULT_RECIPIENTS=stinger
```

Можно отправлять default-получателям:

```bash
python3 Tools/TelegramNotify/telegram_notify.py --message "Unity compile passed"
```

Можно явно указать alias или chat id:

```bash
python3 Tools/TelegramNotify/telegram_notify.py --to stinger --message "Готово"
python3 Tools/TelegramNotify/telegram_notify.py --to CHANGE_ME --message "Готово"
```

## Скриншот

Сначала сохрани PNG/JPG через пайплайн или Unity MCP, затем отправь файл:

```bash
python3 Tools/TelegramNotify/telegram_notify.py \
	--photo "Assets/Screenshots/result.png" \
	--caption "Скрин проверки"
```

## Файл

```bash
python3 Tools/TelegramNotify/telegram_notify.py \
	--document "Logs/compile.log" \
	--caption "Лог компиляции"
```

## Ограничения Telegram

- Сообщение через `sendMessage`: до `4096` символов.
- Caption для `sendPhoto` и `sendDocument`: до `1024` символов.
- Фото через `sendPhoto`: до `10 MB`; безопаснее держать скрины до `4096px` по длинной стороне.
- Документ через `sendDocument`: до `50 MB`.
- Скрипт читает файл в память целиком, поэтому для файлов около `50 MB` нужен сопоставимый запас RAM.
- Для больших билдов, profiler captures, архивов и дампов отправляй ссылку на CI artifact/cloud storage, а не сам файл.

## Правила безопасности

- Не хардкодить Telegram token в скрипты, C# код, markdown или CI logs.
- Не коммитить `Tools/TelegramNotify/.env`.
- В CI использовать secret-переменные окружения вместо локального `.env`.
- Если токен попал в чат, логи или репозиторий, перевыпустить токен через BotFather.
