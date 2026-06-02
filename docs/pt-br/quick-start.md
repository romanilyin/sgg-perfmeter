# Inicio Rapido

Este caminho coloca um overlay visivel em execucao sem escrever codigo.

## 1. Abra A Janela De Setup

No Unity, abra:

```text
SGG/Perfmeter/Setup
```

## 2. Execute O Setup Recomendado

Use a janela de setup para:

- ativar Frame Timing Stats;
- instalar `PerfMeterRenderGraphFeature` nos URP renderers ativos editaveis;
- criar configuracoes JSON padrao pertencentes ao projeto;
- configurar visibilidade do overlay, canto, target FPS, visual preset e modo de coleta.

O arquivo de configuracoes sem codigo e salvo em:

```text
Assets/Resources/SGG.PerfMeter/perfmeter-settings.json
```

Quando `enabled` e `autoStart` sao true, PerfMeter inicia a partir deste JSON em runtime.

## 3. Entre Em Play Mode

O overlay padrao deve aparecer no canto selecionado. Se ele nao aparecer:

- confirme que o arquivo JSON de configuracoes existe no caminho Resources;
- confirme que o overlay esta visivel na janela de setup;
- confirme que o modo de coleta runtime e `Overlay`;
- confirme que o URP renderer ativo tem `PerfMeterRenderGraphFeature` ao testar diagnosticos de Render Graph ou overdraw.

## Criterios De Conclusao

Voce terminou quando:

- o overlay aparece no canto selecionado;
- FPS e timing de CPU atualizam no intervalo de refresh configurado;
- `PerformanceMeter.GetStatus().CollectionMode` reporta `Overlay`.

## Bootstrap Manual Opcional

Use codigo quando quiser controle explicito de inicializacao:

```csharp
using SGG.PerfMeter;
using UnityEngine;

public static class PerfMeterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartPerfMeter()
    {
        PerformanceMeter.EnsureRunning();
        PerformanceMeter.SetTargetFps(PerfMeterTargetFps.Fps60);
        PerformanceMeter.SetOverlayCorner(PerfMeterOverlayCorner.TopRight);
        PerformanceMeter.SetOverlayLayout(PerfMeterOverlayLayout.MetricBars);
        PerformanceMeter.SetOverlayVisible(true);
    }
}
```

## Primeiras Leituras Uteis

```csharp
PerfMeterStatusSnapshot status = PerformanceMeter.GetStatus();
PerfMeterMetricsSnapshot metrics = PerformanceMeter.GetLatestMetrics();
PerfMeterDeviceSnapshot device = PerformanceMeter.GetDeviceInfo();
PerfMeterCameraSnapshot camera = PerformanceMeter.GetCameraSnapshot();
```
