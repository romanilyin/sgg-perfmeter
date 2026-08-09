# Verifications Contributeur

Utilisez la verification la plus legere qui correspond au changement. La compilation Unity et les verifications Test Runner sont couteuses; elles sont donc attendues pour les changements de comportement runtime/editor, pas pour chaque modification de documentation seule.

## Documentation Ou Metadonnees Seulement

```bash
git diff --check
```

Verifiez aussi les liens affectes et gardez les langues concernees synchronisees lorsque plusieurs versions sont touchees.

## Changements De Code Runtime Ou Editor

Executez une verification de compilation Unity pour le projet cible et incluez la commande dans la pull request. Lorsque les tests sont pertinents, executez les verifications Test Runner EditMode et/ou PlayMode.

Pour les portes de release reservees aux mainteneurs ou les smoke tests sur appareil, utilisez la checklist actuelle du mainteneur du projet et mentionnez la commande ou l'environnement dans la pull request.

## Avant D'ouvrir Une Pull Request

- Verifiez `git status` et stagez uniquement les fichiers prevus.
- Ne commitez pas l'etat Unity genere comme `Library/`, `Logs/`, `Temp/`, `Obj/` ou les sorties de build locales.
- Ne commitez pas de secrets, fichiers `.env`, dumps d'appareil, logs prives ou captures d'ecran sans rapport.
- Si le comportement du profiler runtime change, mettez a jour les tests et la documentation utilisateur dans la meme PR.

## CI De Performance

`.github/workflows/performance-ci.yml` execute toute la suite de correction EditMode et les tests de performance isoles avec Unity `6000.4.12f1` et `6000.5.6f1` pour les pull requests du meme depot, les pushes vers `main` et les lancements manuels. Les pull requests issues de forks sont ignorees, car GitHub n'expose pas les secrets de licence Unity. La CI injecte `com.unity.test-framework.performance` `3.5.0` uniquement dans son checkout ephemere; le package ne conserve aucune dependance obligatoire. Les seuils versionnes se trouvent dans `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`; la CI publie le XML NUnit brut, le XML JUnit converti, le JSON de performance et les logs.

Le meme workflow execute un job PlayMode lifecycle complet et separe sur les deux versions Unity lorsque `PERFMETER_UNITY_CI_ENABLED` vaut `true` et que des credentials Unity compatibles GameCI sont configures. Les changements sous `Tests/PlayMode/**` declenchent le workflow; les credentials desactives et les PR de forks produisent un job explicitement ignore.
