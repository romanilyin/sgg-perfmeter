# Contributor Checks

Nutze die leichteste Pruefung, die zur Aenderung passt. Unity compile und Test Runner checks sind teuer und werden fuer Runtime/Editor-Verhaltensaenderungen erwartet, nicht fuer jede reine Dokumentationsaenderung.

## Nur Dokumentation Oder Metadaten

```bash
git diff --check
```

Pruefe ausserdem betroffene Links und halte Sprachversionen synchron, wenn mehrere Zielgruppen betroffen sind.

## Runtime- Oder Editor-Code

Fuehre einen Unity compile check fuer das Zielprojekt aus und nenne den Befehl im Pull Request. Wenn Tests relevant sind, fuehre EditMode und/oder PlayMode Test Runner checks aus.

## Vor Dem Pull Request

- Pruefe `git status` und stage nur die beabsichtigten Dateien.
- Committe keinen generierten Unity-Zustand wie `Library/`, `Logs/`, `Temp/`, `Obj/` oder lokale Build-Ausgaben.
- Committe keine Secrets, `.env`-Dateien, Geraete-Dumps, privaten Logs oder unzusammenhaengenden Screenshots.
- Wenn sich Runtime-Profiler-Verhalten aendert, aktualisiere Tests und user-facing docs im selben PR.
