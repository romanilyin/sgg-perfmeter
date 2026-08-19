# SGG PerfMeter

**Diagnostico runtime leve e profiling legivel por agentes para Unity 6 URP+HDRP (FPS meter).**

[English](../../README.md) | [Русский](../ru/README.md) | [Deutsch](../de/README.md) | [Español](../es/README.md) | [Français](../fr/README.md) | [Italiano](../it/README.md) | [日本語](../ja/README.md) | [한국어](../ko/README.md) | [Português (Brasil)](./README.md) | [简体中文](../zh-cn/README.md)

[Instalacao](./installation.md) | [Inicio rapido](./quick-start.md) | [Workflows](./workflows.md) | [API](./api.md) | [MCP](./mcp.md) | [Comparacao](./comparison.md) | [Limitacoes](./limitations.md) | [Solucao de problemas](./troubleshooting.md)

![SGG PerfMeter landing screenshot](../assets/screenshots/presets/preset-default-landing.png)

SGG PerfMeter identifica gargalos de frame, compara alteracoes de performance, grava sessoes reproduziveis e fornece dados estruturados de profiling para ferramentas e AI agents.

## Por Que Ajuda

- Ver o contexto de gargalos diretamente durante o jogo.
- Alternar entre presets, graficos, barras de metrica, layouts compactos e linhas de custom metrics.
- Gravar sessoes de profiling reproduziveis com warm-up, contexto de cena, resumo dos piores frames e exportacao JSON/CSV.
- Coordenar RenderDoc/PIX pelo caminho generico da Unity ou usar opcionalmente o bridge RenderDoc distribuido separadamente e verificado por SHA-256 para artefatos `.rdc` autenticados no Editor Windows x64 com D3D11, D3D12 ou Vulkan. O pacote UPM continua sem binarios e nao instala o RenderDoc.
- Usar alerts, logs estruturados, callbacks e cooldowns de avisos do Editor sem observar o overlay continuamente.
- Fornecer dados estruturados a ferramentas e agents para comparacoes, testes A/B e busca de hotspots.

## O Que E Medido

- Estado runtime de Unity `6000.4+` / URP `17.4+` Render Graph e HDRP `17.4+` Custom Pass.
- Timing de CPU/GPU via FrameTimingManager: CPU frame, main thread, render thread, present wait e GPU frame time quando disponivel.
- ProfilerRecorder render counters: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory e GPU memory quando disponiveis.
- Classificacao de gargalo para GPU, CPU main, CPU render, present/VSync, balanced ou unknown.
- Medicao opt-in de overdraw e overdraw heatmap visual por URP Render Graph; em HDRP overdraw/heatmap nao sao suportados, enquanto core diagnostics continuam disponiveis.
- Snapshots de device, URP/HDRP camera, render integration, status, metrics, alerts, sessions e custom metrics para codigo e automacao MCP.

## Telemetria de plataforma opcional

O PerfMeter pode coletar sinais opcionais de thermal e Adaptive Performance por meio de um provider. A core assembly nao tem hard dependency em `com.unity.adaptiveperformance`; a assembly opcional `SGG.PerfMeter.AdaptivePerformance` e ativada para Unity `6000.4+` quando `com.unity.adaptiveperformance` esta na versao `5.1.0+`.

- **API do provider**: `PerformanceMeter.RegisterPlatformTelemetryProvider(...)`, `UnregisterPlatformTelemetryProvider(...)` e `GetPlatformTelemetry()` permitem no maximo um provider ativo e retornam o `PerfMeterPlatformTelemetrySnapshot` imutavel com ID/versao do provider, tempos de sample/mudanca, thermal warning, temperature level/trend, CPU/GPU performance levels, adaptive bottleneck e disponibilidade por campo. Um segundo provider diferente e rejeitado.
- **Coleta e exports**: o core usa uma cadencia limitada de 0,25 segundo e um snapshot em cache com metadados de last attempt/result, idade do last success e freshness. Um capture boundary forca uma tentativa sem ocultar `Unavailable`. O JSON/CSV da sessao e os capture samples preservam o snapshot e a procedencia do provider. O comando MCP `perfmeter.platform.telemetry` expoe o snapshot estruturado atual.
- **Profiler e alerts**: `SGG.PerfMeter.Thermal.Sample` e `SGG.PerfMeter.Thermal.Available` expoem o marker de coleta termica e o counter de disponibilidade. O alert padrao `thermal.throttling` usa a metrica `ThermalWarningLevel` quando um nivel de throttling iminente ou ativo esta disponivel.
- **Indisponibilidade e explicita**: providers e campos nao suportados permanecem unavailable; o JSON serializa valores numericos indisponiveis como `null` e o CSV usa campos vazios, com flags de disponibilidade em vez de zeros falsos. A validacao do package Adaptive Performance real e dos dispositivos alvo continua sendo um gate da release matrix/release candidate; este README nao afirma nenhum resultado em dispositivo.

## Inicio Rapido

1. Instale o pacote Unity pelo npm registry ou por Git UPM.
2. Abra `SGG/Perfmeter/Setup` no Unity.
3. Execute a configuracao recomendada, entre em Play Mode e confirme que o overlay aparece.

```json
{
  "scopedRegistries": [
    {
      "name": "npmjs",
      "url": "https://registry.npmjs.org",
      "scopes": [
        "com.sungeargames"
      ]
    }
  ],
  "dependencies": {
    "com.sungeargames.perfmeter": "2026.8.19-1"
  }
}
```

## Documentacao

- [Instalacao](./installation.md)
- [Inicio rapido](./quick-start.md)
- [Workflows](./workflows.md)
- [API](./api.md)
- [MCP e automacao de agentes](./mcp.md)
- [Visual Presets](./presets.md)
- [Widgets implementados](./widgets.md)
- [Screenshots](./screenshots.md)
- [Screenshots da janela de setup](./setup-window-screenshots.md)
- [Limitacoes](./limitations.md)
- [Solucao de problemas](./troubleshooting.md)
- [Comparacao](./comparison.md)
- [Brand Usage Policy](./brand.md)

## Licenca

O pacote e licenciado sob **Stinger Royalty-Free EULA 1.0**.

- Texto russo autoritativo da licenca: [LICENSE.ru.md](../../LICENSE.ru.md)
- Traducao auxiliar em ingles: [LICENSE.md](../../LICENSE.md)
- Avisos: [NOTICE.md](../../NOTICE.md) e [NOTICE.ru.md](../../NOTICE.ru.md)
- Brand usage policy: [brand.md](./brand.md)
