# Janela de Setup

Abra a janela do Editor em `SGG/Perfmeter/Setup`.

## Comportamento atual

- **Setup** e **Presets** exibem as configurações persistentes do projeto PerfMeter e os dados dos presets do overlay: linhas de schema/versão, compatibilidade `legacy` e metadados reservados, todas somente leitura, além da composição de widgets e dos valores numéricos normalizados ao perder o foco.
- **Runtime** exibe, somente para leitura, diagnósticos de sessão, memória, estado gráfico, integração de renderização e GRD/BRG, incluindo a capacidade/o estado das integrações opcionais. Os estados `Unavailable`, `unknown` e sem amostra continuam explícitos. `Measure Overdraw (project default)` usa o valor sentinel padrão do projeto.
- As ações incluem `Session Analysis`, `Profile Analyzer` e `Refresh`. `Start Session` e `Stop Session` ficam disponíveis somente no Play Mode. Abrir ou atualizar o Setup nunca inicia a coleta runtime.
- Os parâmetros de solicitação de memory snapshot e de trace/prewarm do graphics-state são entradas exclusivas de runtime, não configurações do projeto.

## Screenshots de referência

> Os screenshots abaixo são anteriores ao P3.5. Eles são mantidos apenas como referência visual e não constituem evidência atual da UX de Setup concluída.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-pt-br-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-pt-br-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-pt-br-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-pt-br-debug.png)
