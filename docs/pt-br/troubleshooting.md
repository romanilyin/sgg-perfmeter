# Solucao De Problemas

Use este checklist quando PerfMeter nao mostrar os dados esperados.

## Overlay Nao Aparece

- Abra `SGG/Perfmeter/Setup` e confirme que a visibilidade do overlay esta ativada.
- Confirme que o modo de coleta e `Overlay`, nao `Background` ou `Stopped`.
- Se estiver usando setup sem codigo, confirme que o arquivo de configuracoes existe em `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Se estiver usando bootstrap manual, confirme que `PerformanceMeter.EnsureRunning()` e chamado depois do carregamento da cena.
- Entre em Play Mode; chamadas de API em Edit Mode sao seguras, mas nao criam um overlay runtime.

## Frame Timing Ou GPU Timing Esta Ausente

- Ative Player Settings -> Rendering -> Frame Timing Stats.
- Prefira Vulkan no Android quando GPU frame timing for importante.
- Trate OpenGL/OpenGLES como modo degradado para GPU timing.
- Verifique `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` e `Warning` antes de presumir que um counter existe.

## Medicao De Overdraw Nao Avanca

- Em URP, instale `PerfMeterRenderGraphFeature` no URP renderer ativo.
- Em HDRP, overdraw e heatmap nao sao suportados by design; use core diagnostics.
- Confirme que a camera ativa usa o renderer que contem a feature.
- Confirme que o backend alvo suporta fragment UAV/storage buffers, compute shaders e async GPU readback.
- Use `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` para uma janela de medicao limitada.
- Se o alvo nao for suportado, PerfMeter reporta `OverdrawState.Unsupported` em vez de agendar o pass.

## Exportacao De Sessao Falha

- Exporte para um caminho local ao projeto.
- Nao sobrescreva uma exportacao existente a menos que seu workflow remova explicitamente o arquivo primeiro.
- Mantenha `MaxSamples` limitado para execucoes longas.
- Use warm-up em frames/segundos para evitar spikes de inicializacao nos resumos.

## Alerts Estao Ruidosos Demais

- Ajuste thresholds e janelas de frames consecutivos nas configuracoes JSON.
- Aumente os cooldowns de avisos do Editor.
- Desative logs de aviso do Editor quando callbacks ou logs estruturados forem suficientes.

## Dados Parecem Diferentes Entre Devices

Isso e esperado. GPU timings, profiler counters, informacoes de display, async readback e suporte a overdraw variam por graphics API, plataforma, versao do Unity e device. Use snapshots de device e avisos em sessoes exportadas para explicar diferencas.
