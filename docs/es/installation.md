# Instalación

SGG PerfMeter se distribuye como un paquete Unity llamado `com.sungeargames.perfmeter`. La versión pública actual de npm es `2026.8.9-1`; Git UPM y la copia local siguen disponibles.

## Requisitos

- Unity `6000.4+` para uso en runtime con soporte.
- URP `17.4+` con Render Graph path o HDRP `17.4+` con Custom Pass integration.
- Soporte de UI Toolkit en runtime.
- Frame Timing Stats activado antes de depender de FrameTimingManager en builds.
- La captura nativa opcional de RenderDoc solo admite el Editor Unity Windows x64 con Direct3D 11, Direct3D 12 o Vulkan; Development Player, Linux nativo, IL2CPP, mobile y macOS nativo no están soportados.
- El paquete UPM sigue sin binarios y nunca instala RenderDoc. FTUE solo puede descargar o instalar localmente el bridge publicado por separado y fijado por longitud, SHA-256 y contrato PE AMD64; después se requiere reiniciar el Editor.

Los metadatos del paquete mantienen Unity `2022.3` como base segura de importación para comprobaciones de importación y compilación. El objetivo runtime con soporte actual es Unity `6000.4+` con URP `17.4+` Render Graph o HDRP `17.4+` Custom Pass integration.

Son niveles de compatibilidad separados: `ImportCompatible` no promete comportamiento runtime con soporte; `CoreRuntimeCompatible` requiere Unity `6000.4+` pero no un pipeline específico; `RenderIntegrationCompatible` requiere además URP/HDRP activo `17.4+` y el adapter de PerfMeter. Consúltalos con `PerfMeterSetupActions.GetCompatibilityStatus()` o MCP `perfmeter.compatibility.status`; configuration readiness se informa aparte.

## Instalación Con npm Scoped Registry

Agrega el npm registry como Unity Package Manager scoped registry en el `Packages/manifest.json` de tu proyecto Unity:

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
    "com.sungeargames.perfmeter": "2026.8.9-1"
  }
}
```

Si tu manifest ya tiene `scopedRegistries`, agrega la entrada `npmjs` al array existente.

## Instalación Git UPM

El paquete vive dentro de este repositorio:

```text
Assets/Scripts/SGG.PerfMeter
```

Añádelo al `Packages/manifest.json` de tu proyecto Unity:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Si tu entorno usa SSH para dependencias Git:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Fija un tag o commit para instalaciones repetibles:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.8.9-1"
  }
}
```

## Instalación Como Copia Local

Copia esta carpeta dentro de tu proyecto Unity:

```text
Assets/Scripts/SGG.PerfMeter
```

Esto es útil para desarrollo local del paquete o cuando no se desean dependencias Git.

## Configuración Inicial Del Proyecto

Abre:

```text
SGG/Perfmeter/Setup
```

La pestaña de configuración inicial controla en tiempo real los requisitos obligatorios. Instala u omite cada integración marcada claramente como opcional; la pestaña se oculta al resolver todos los pasos y vuelve a aparecer si una comprobación obligatoria deja de cumplirse.

Después ejecuta la configuración recomendada:

1. Activa Frame Timing Stats.
2. Instala `PerfMeterRenderGraphFeature` en los URP renderer assets activos editables. Los proyectos HDRP omiten cambios del URP renderer; el package HDRP Custom Pass se registra en runtime cuando HDRP `17.4+` está instalado.
3. Guarda la configuración JSON en `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` para setup sin código, o copia el snippet de inicialización.
4. Entra en Play Mode y verifica el overlay.

## Samples

Importa los samples del paquete desde el panel de detalles de Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
