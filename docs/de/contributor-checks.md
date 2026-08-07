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

## Performance CI

`.github/workflows/performance-ci.yml` fuehrt die vollstaendige EditMode-Correctness-Suite und die isolierten Performance-Tests mit Unity `6000.4.12f1` und `6000.5.6f1` fuer Pull Requests aus demselben Repository, Pushes auf `main` und manuelle Laeufe aus. Pull Requests aus Forks werden uebersprungen, weil GitHub keine Unity-Lizenz-Secrets bereitstellt. CI fuegt `com.unity.test-framework.performance` `3.5.0` nur in den temporaeren Checkout ein; das Paket behaelt keine feste Abhaengigkeit. Versionierte Grenzwerte liegen in `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`; CI laedt rohes NUnit-XML, konvertiertes JUnit-XML, Performance-JSON und Logs hoch.
