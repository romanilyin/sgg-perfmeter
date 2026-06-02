# Solución De Problemas

Usa esta checklist cuando PerfMeter no muestre los datos esperados.

## El Overlay No Aparece

- Abre `SGG/Perfmeter/Setup` y confirma que la visibilidad del overlay está activada.
- Confirma que el modo de recolección es `Overlay`, no `Background` ni `Stopped`.
- Si usas setup sin código, confirma que el archivo de configuración existe en `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json`.
- Si usas bootstrap manual, confirma que se llama a `PerformanceMeter.EnsureRunning()` después de cargar la escena.
- Entra en Play Mode; las llamadas API en Edit Mode son seguras pero no crean un overlay runtime.

## Falta Frame Timing O GPU Timing

- Activa Player Settings -> Rendering -> Frame Timing Stats.
- Prefiere Vulkan en Android cuando el GPU frame timing importa.
- Trata OpenGL/OpenGLES como modo degradado para GPU timing.
- Comprueba `PerfMeterStatusSnapshot.AvailableCounters`, `UnavailableCounters` y `Warning` antes de asumir que existe un contador.

## La Medición De Overdraw No Avanza

- Instala `PerfMeterRenderGraphFeature` en el URP renderer activo.
- Confirma que la camera activa usa el renderer que contiene la feature.
- Confirma que el backend objetivo soporta fragment UAV/storage buffers, compute shaders y async GPU readback.
- Usa `PerformanceMeter.RequestOverdrawMeasurement(frameCount)` para una ventana de medición acotada.
- Si el target no es compatible, PerfMeter informa `OverdrawState.Unsupported` en vez de programar el pass.

## Falla La Exportación De Sesión

- Exporta a una ruta local del proyecto.
- No sobrescribas una exportación existente salvo que tu flujo la elimine explícitamente antes.
- Mantén `MaxSamples` acotado para ejecuciones largas.
- Usa frames/segundos de warm-up para evitar spikes de arranque en los resúmenes.

## Las Alertas Son Demasiado Ruidosas

- Ajusta umbrales y ventanas de frames consecutivos en la configuración JSON.
- Aumenta los cooldowns de advertencias del Editor.
- Desactiva los logs de advertencia del Editor cuando callbacks o logs estructurados sean suficientes.

## Los Datos Se Ven Diferentes Entre Dispositivos

Esto es esperado. GPU timings, profiler counters, información de display, async readback y soporte de overdraw varían por graphics API, plataforma, versión de Unity y dispositivo. Usa snapshots de device y warnings en sesiones exportadas para explicar diferencias.
