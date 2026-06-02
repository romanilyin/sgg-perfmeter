# Instalacao

SGG PerfMeter atualmente e distribuido como um pacote Unity chamado `com.sungeargames.perfmeter`. Um caminho de instalacao via npm nao esta disponivel para a release `2026.6.5-1`.

## Requisitos

- Unity `6000.4+` para uso runtime suportado.
- URP `17.4+` com caminho Render Graph.
- Suporte runtime a UI Toolkit.
- Frame Timing Stats ativado antes de depender de FrameTimingManager em builds.

Os metadados do pacote mantem Unity `2022.3` como piso de seguranca para importacao e verificacoes de compilacao. O alvo runtime suportado atual e Unity `6000.4+` com URP `17.4+` Render Graph.

## Instalacao Por Git UPM

O pacote fica dentro deste repositorio:

```text
Assets/Scripts/SGG.PerfMeter
```

Adicione ao `Packages/manifest.json` do seu projeto Unity:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Se o seu ambiente usa SSH para dependencias Git:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Fixe uma tag ou commit para instalacoes reproduziveis:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.6.5-1"
  }
}
```

## Instalacao Por Copia Local

Copie esta pasta para o seu projeto Unity:

```text
Assets/Scripts/SGG.PerfMeter
```

Isso e util para desenvolvimento local do pacote ou quando dependencias Git nao sao desejadas.

## Setup Inicial Do Projeto

Abra:

```text
SGG/Perfmeter/Setup
```

Depois execute o setup recomendado:

1. Ative Frame Timing Stats.
2. Instale `PerfMeterRenderGraphFeature` nos URP renderer assets ativos editaveis.
3. Salve as configuracoes JSON em `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` para setup sem codigo, ou copie o snippet de inicializacao.
4. Entre em Play Mode e verifique o overlay.

## Samples

Importe os samples do pacote pelo painel de detalhes do Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
