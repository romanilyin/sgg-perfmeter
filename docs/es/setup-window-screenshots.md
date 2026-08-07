# Ventana de Setup

Abre la ventana del Editor desde `SGG/Perfmeter/Setup`.

## Comportamiento actual

- **Setup** y **Presets** muestran los ajustes persistentes del proyecto PerfMeter y los datos de los presets del overlay: filas de esquema/versión, compatibilidad `legacy` y metadatos reservados, todas de solo lectura, además de la composición de widgets y los valores numéricos normalizados al perder el foco.
- **Runtime** muestra en modo de solo lectura los diagnósticos de sesión, memoria, estado gráfico, integración de renderizado y GRD/BRG, además de la capacidad/estado de las integraciones opcionales. Los estados `Unavailable`, `unknown` y sin muestra se mantienen explícitos. `Measure Overdraw (project default)` usa el sentinel predeterminado del proyecto.
- Las acciones incluyen `Session Analysis`, `Profile Analyzer` y `Refresh`. `Start Session` y `Stop Session` solo están disponibles en Play Mode. Abrir o actualizar Setup nunca inicia la recopilación runtime.
- Los parámetros de solicitud de memory snapshot y de trace/prewarm de graphics-state son entradas exclusivas de runtime, no ajustes del proyecto.

## Screenshots de referencia

> Los screenshots siguientes son anteriores a P3.5. Se conservan solo como referencia visual y no son evidencia actual del Setup UX completado.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-es-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-es-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-es-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-es-debug.png)
