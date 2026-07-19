# Installation

SGG PerfMeter est distribue comme package Unity nomme `com.sungeargames.perfmeter`. La version npm publique actuelle est `2026.7.19-1`; Git UPM et la copie locale restent disponibles.

## Exigences

- Unity `6000.4+` pour l'utilisation runtime prise en charge.
- URP `17.4+` avec Render Graph path ou HDRP `17.4+` avec Custom Pass integration.
- Prise en charge runtime de UI Toolkit.
- Frame Timing Stats active avant de s'appuyer sur FrameTimingManager dans les builds.

Les metadonnees du package conservent Unity `2022.3` comme plancher de securite pour l'import et les verifications de compilation. La cible runtime actuellement prise en charge est Unity `6000.4+` avec URP `17.4+` Render Graph ou HDRP `17.4+` Custom Pass integration.

## Installation Avec npm Scoped Registry

Ajoutez le npm registry comme Unity Package Manager scoped registry dans le `Packages/manifest.json` de votre projet Unity:

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

Si votre manifest contient deja `scopedRegistries`, ajoutez l'entree `npmjs` au tableau existant.

## Installation Git UPM

Le package se trouve dans ce depot:

```text
Assets/Scripts/SGG.PerfMeter
```

Ajoutez-le au fichier `Packages/manifest.json` de votre projet Unity:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Si votre environnement utilise SSH pour les dependances Git:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "git+ssh://git@github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter"
  }
}
```

Epinglez un tag ou un commit pour des installations reproductibles:

```json
{
  "dependencies": {
    "com.sungeargames.perfmeter": "https://github.com/romanilyin/sgg-perfmeter.git?path=/Assets/Scripts/SGG.PerfMeter#2026.7.19-1"
  }
}
```

## Installation Par Copie Locale

Copiez ce dossier dans votre projet Unity:

```text
Assets/Scripts/SGG.PerfMeter
```

C'est utile pour le developpement local du package ou lorsque les dependances Git ne sont pas souhaitees.

## Configuration Initiale Du Projet

Ouvrez:

```text
SGG/Perfmeter/Setup
```

Puis executez la configuration recommandee:

1. Activer Frame Timing Stats.
2. Installer `PerfMeterRenderGraphFeature` dans les assets de renderer URP actifs et modifiables. Les projets HDRP ignorent les changements du renderer URP; le package HDRP Custom Pass est enregistre au runtime lorsque HDRP `17.4+` est installe.
3. Enregistrer les reglages JSON dans `Assets/Resources/SGG.PerfMeter/perfmeter-settings.json` pour une configuration sans code, ou copier l'extrait d'initialisation.
4. Entrer en Play Mode et verifier l'overlay.

## Samples

Importez les samples du package depuis le panneau de details du Package Manager:

- `Bootstrap and Zero-Code Settings`
- `Runtime Workflows`
- `Editor and MCP Automation`
