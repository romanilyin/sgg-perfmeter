# Setup-Fenster

Öffne das Editorfenster über `SGG/Perfmeter/Setup`.

## Aktuelles Verhalten

- **Setup** und **Presets** zeigen die persistierten PerfMeter-Projekteinstellungen und Overlay-Presetdaten: schreibgeschützte Schema-/Versionszeilen, `legacy`-Kompatibilitätszeilen und Zeilen für reservierte Metadaten, Widget-Zusammensetzung und numerische Werte, die beim Fokusverlust normalisiert werden.
- **Runtime** zeigt schreibgeschützte Sitzungs-, Speicher-, Grafikstatus-, Render-Integrations- und GRD/BRG-Diagnosen einschließlich Fähigkeiten und Status optionaler Integrationen. `Unavailable`, `unknown` und „kein Sample“ bleiben ausdrücklich sichtbar. `Measure Overdraw (project default)` verwendet den Projekt-Sentinel.
- Die Aktionen `Session Analysis`, `Profile Analyzer` und `Refresh` sind verfügbar. `Start Session` und `Stop Session` gibt es nur im Play Mode. Das Öffnen oder Aktualisieren von Setup startet niemals die Runtime-Sammlung.
- Anfrageparameter für Memory Snapshot sowie Graphics-State-Trace/Prewarm sind reine Runtime-Eingaben und keine Projekteinstellungen.

## Referenz-Screenshots

> Die folgenden Screenshots stammen aus der Zeit vor P3.5. Sie dienen nur als visuelle Referenz und sind kein aktueller Nachweis für die fertige Setup-UX.

### Setup

![Setup tab](../assets/screenshots/setup-window/setup-window-de-setup.png)

### Presets

![Presets tab](../assets/screenshots/setup-window/setup-window-de-presets.png)

### Runtime

![Runtime tab](../assets/screenshots/setup-window/setup-window-de-runtime.png)

### Debug

![Debug tab](../assets/screenshots/setup-window/setup-window-de-debug.png)
