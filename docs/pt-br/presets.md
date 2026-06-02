# Visual Presets

Visual presets sao arquivos JSON do projeto que definem layout do overlay, estilo, widgets ativados e ordem dos widgets. Eles sao criados na aba `Presets` de `SGG/Perfmeter/Setup` e gravados em JSON dentro de Resources para builds, entao o runtime nao depende de `AssetDatabase`.

Os screenshots abaixo sao capturas fullscreen da cena capture-lab depois de 1000 frames de warmup. O texto do overlay runtime nao e localizado, entao as documentacoes localizadas usam as mesmas imagens de preset.

## Default

Preset padrao de diagnosticos sem codigo. Ele usa o layout `MetricBars`, entao o bloco de texto inferior e renderizado como barras de metrica compactas em vez de um bloco de texto simples.

![Default preset](../assets/screenshots/presets/preset-default.png)

## FPS Only

Preset de FPS em linha unica com FPS atual, FPS medio, 1% low, 0.1% low e tempo de render-thread. Valores da familia FPS usam cores de acordo com o target FPS selecionado.

![FPS Only preset](../assets/screenshots/presets/preset-fps-only.png)

## Compact Timing

Preset compacto de timing com FPS, cards de timing CPU/GPU e barras de budget CPU/GPU.

![Compact Timing preset](../assets/screenshots/presets/preset-compact-timing.png)

## Classic Cards

Preset focado em cards para FPS, CPU, GPU, frame spikes, rendering e memory sem graficos.

![Classic Cards preset](../assets/screenshots/presets/preset-classic-cards.png)

## Graphs

Preset focado em timing com graficos de historico de CPU e GPU, alem de cards centrais de FPS/timing.

![Graphs preset](../assets/screenshots/presets/preset-graphs.png)

## Full Diagnostics

Preset diagnostico amplo com todos os principais widgets de alto nivel do PerfMeter ativados.

![Full Diagnostics preset](../assets/screenshots/presets/preset-full-diagnostics.png)

## Escala De Cores De FPS

O preset `FPS Only` colore valores da familia FPS em relacao ao target FPS selecionado:

| Ratio Para O Target | Cor |
| --- | --- |
| `> 2.0x` | Azul |
| `>= 1.0x` | Verde |
| `0.75x` to `< 1.0x` | Amarelo |
| `0.25x` to `< 0.75x` | Laranja |
| `< 0.25x` | Vermelho |
