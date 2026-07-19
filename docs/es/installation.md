# Instalación

SGG PerfMeter se distribuye como un paquete Unity llamado `com.sungeargames.perfmeter`. La versión pública actual de npm es `2026.7.19-1`; Git UPM y la copia local siguen disponibles.

## Requisitos

- Unity `6000.4+` para uso en runtime con soporte.
- URP `17.4+` con Render Graph path o HDRP `17.4+` con Custom Pass integration.
- Soporte de UI Toolkit en runtime.
- Frame Timing Stats activado antes de depender de FrameTimingManager en builds.

Los metadatos del paquete mantienen Unity `2022.3` como base segura de importación para comprobaciones de importación y compilación. El objetivo runtime con soporte actual es Unity `6000.4+` con URP `17.4+` Render Graph o HDRP `17.4+` Custom Pass integration.

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
    "com.sungeargames.perfmeter": "2026.7.19-1"
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
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.7.19-1"
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
