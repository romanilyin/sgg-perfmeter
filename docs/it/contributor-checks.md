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
