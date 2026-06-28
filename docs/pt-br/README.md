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
- Usar alerts, logs estruturados, callbacks e cooldowns de avisos do Editor sem observar o overlay continuamente.
- Fornecer dados estruturados a ferramentas e agents para comparacoes, testes A/B e busca de hotspots.

## O Que E Medido

- Estado runtime de Unity `6000.4+` / URP `17.4+` Render Graph e HDRP `17.4+` Custom Pass.
- Timing de CPU/GPU via FrameTimingManager: CPU frame, main thread, render thread, present wait e GPU frame time quando disponivel.
- ProfilerRecorder render counters: draw calls, SetPass, batches, vertices, SRP Batcher, BRG/GRD, uploads, memory e GPU memory quando disponiveis.
- Classificacao de gargalo para GPU, CPU main, CPU render, present/VSync, balanced ou unknown.
- Medicao opt-in de overdraw e overdraw heatmap visual por URP Render Graph; em HDRP overdraw/heatmap nao sao suportados, enquanto core diagnostics continuam disponiveis.
- Snapshots de device, URP/HDRP camera, render integration, status, metrics, alerts, sessions e custom metrics para codigo e automacao MCP.

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
    "com.sungeargames.perfmeter": "2026.6.28-1"
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
