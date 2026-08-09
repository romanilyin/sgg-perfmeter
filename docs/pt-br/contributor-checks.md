# Verificacoes Para Contribuidores

Use a verificacao mais leve que corresponda a alteracao. Compilacao Unity e verificacoes do Test Runner sao caras, entao sao esperadas para alteracoes de comportamento runtime/editor, nao para toda edicao apenas de documentacao.

## Apenas Documentacao Ou Metadados

```bash
git diff --check
```

Tambem verifique links afetados e mantenha os idiomas envolvidos em sincronia quando varias versoes forem afetadas.

## Alteracoes De Codigo Runtime Ou Editor

Execute uma verificacao de compilacao Unity para o projeto alvo e inclua o comando no pull request. Quando testes forem relevantes, execute verificacoes EditMode e/ou PlayMode Test Runner.

Para gates de release ou smoke tests em device exclusivos de maintainers, use o checklist atual do maintainer do projeto e mencione o comando ou ambiente no pull request.

## Antes De Abrir Um Pull Request

- Verifique `git status` e faca stage apenas dos arquivos pretendidos.
- Nao commite estado Unity gerado, como `Library/`, `Logs/`, `Temp/`, `Obj/` ou saidas de build locais.
- Nao commite secrets, arquivos `.env`, dumps de device, logs privados ou screenshots nao relacionados.
- Se o comportamento do profiler runtime mudar, atualize testes e docs de usuario no mesmo PR.

## CI De Performance

`.github/workflows/performance-ci.yml` executa toda a suite de correcao EditMode e os testes de performance isolados no Unity `6000.4.12f1` e `6000.5.6f1` para pull requests do mesmo repositorio, pushes para `main` e execucoes manuais. Pull requests de forks sao ignorados porque o GitHub nao expoe os secrets da licenca Unity. A CI injeta `com.unity.test-framework.performance` `3.5.0` somente no checkout efemero; o pacote nao mantem dependencia obrigatoria. Os limites versionados ficam em `Assets/Scripts/SGG.PerfMeter/Tests/Performance/performance-baselines.json`; a CI publica XML NUnit bruto, XML JUnit convertido, JSON de performance e logs.

O mesmo workflow executa um job PlayMode lifecycle completo e separado nas duas versoes Unity quando `PERFMETER_UNITY_CI_ENABLED` e `true` e credenciais Unity compativeis com GameCI estao configuradas. Alteracoes em `Tests/PlayMode/**` ativam o workflow; credenciais desativadas e PRs de forks produzem um job explicitamente ignorado.
