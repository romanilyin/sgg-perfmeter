# Controlli Per Contributor

Usa il controllo piu leggero adatto alla modifica. I controlli Unity compile e Test Runner sono costosi, quindi sono previsti per modifiche di comportamento runtime/editor, non per ogni modifica solo documentazione.

## Solo Documentazione O Metadati

```bash
git diff --check
```

Verifica anche i link interessati e mantieni sincronizzate le lingue coinvolte quando piu versioni sono interessate.

## Modifiche Al Codice Runtime O Editor

Esegui un controllo di compilazione Unity per il progetto target e includi il comando nella pull request. Quando i test sono rilevanti, esegui controlli Test Runner EditMode e/o PlayMode.

Per gate di release riservati ai maintainer o smoke test su dispositivo, usa la checklist corrente dei maintainer del progetto e menziona comando o ambiente nella pull request.

## Prima Di Aprire Una Pull Request

- Controlla `git status` e metti in stage solo i file previsti.
- Non committare stato Unity generato come `Library/`, `Logs/`, `Temp/`, `Obj/` o output di build locali.
- Non committare segreti, file `.env`, dump di dispositivi, log privati o screenshot non correlati.
- Se cambia il comportamento del runtime profiler, aggiorna test e documentazione utente nella stessa PR.

## CI Delle Prestazioni

`.github/workflows/performance-ci.yml` esegue l'intera suite di correttezza EditMode e i test delle prestazioni isolati con Unity `6000.4.12f1` e `6000.5.6f1` per pull request dello stesso repository, push su `main` ed esecuzioni manuali. Le pull request provenienti da fork vengono ignorate perche GitHub non espone i secret della licenza Unity. La CI inietta `com.unity.test-framework.performance` `3.5.0` solo nel checkout effimero; il package non mantiene una dipendenza obbligatoria. Le soglie versionate si trovano in `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`; la CI pubblica XML NUnit grezzo, XML JUnit convertito, JSON delle prestazioni e log.

Lo stesso workflow esegue un job PlayMode lifecycle completo e separato su entrambe le versioni Unity quando `PERFMETER_UNITY_CI_ENABLED` e `true` e sono configurate credenziali Unity compatibili con GameCI. Le modifiche sotto `Tests/PlayMode/**` attivano il workflow; credenziali disabilitate e PR da fork producono un job esplicitamente ignorato.
