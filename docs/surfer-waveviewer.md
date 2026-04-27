# Surfer Ternary Waveviewer

Surfer Ternary Waveviewer is a browser-based waveform viewer for balanced ternary hardware designs, built on top of [Surfer](https://gitlab.com/surfer-project/surfer) with ternary-specific extensions developed at the University of South-Eastern Norway (USN).

## Features

- **GHW file support** — loads GHDL-generated waveform files from balanced ternary VHDL simulations
- **Ternary signal types** — natively displays `btern_ulogic`, `btern_logic`, `btern_ulogic_vector`, `btern_logic_vector`, `kleene`, and `kleene_vector` types
- **Three-rail coloring** — signals are color-coded by trit value: High (+), Mid (0/unknown), Low (−)
- **Balanced Ternary Translator plugin** — a `.wasm` translator plugin decodes GHDL's binary ordinal encoding into human-readable ternary literals (+, 0, −, U, X, Z, W, L, M, H, D)
- **WebAssembly** — runs entirely in the browser, no installation required

## Using the Web App

1. Click the image above to open the waveviewer in a new tab.
2. Use **File → Open** to load a `.ghw` waveform file from your local machine.
3. Signal types from balanced ternary VHDL libraries are automatically decoded.

## Translator Plugin (Desktop)

For the desktop version of Surfer, a precompiled translator plugin is available:

- **Download**: [`btern_translator.wasm`](https://aiunderstand.github.io/surfer-ternary-waveviewer/btern_translator.wasm)
- Place the `.wasm` file in your Surfer translator directory (`~/.local/share/surfer/translators/` on Linux/macOS).
- Surfer will automatically discover and load it on next start.

## Example Waveforms

Click any link below to open the waveviewer in a new tab and load the file automatically.

| Example | Description | Open |
|---|---|---|
| `tvhdl_surfer_test.ghw` | All supported ternary signal types (`btern_ulogic`, `btern_ulogic_vector`, `kleene`, `kleene_vector`) | [Open in Surfer](https://aiunderstand.github.io/surfer-ternary-waveviewer/?load_url=https://aiunderstand.github.io/surfer-ternary-waveviewer/tvhdl_surfer_test.ghw) |
| `ternary-data-flip-flop.ghw` | Ternary data flip-flop simulation | [Open in Surfer](https://aiunderstand.github.io/surfer-ternary-waveviewer/?load_url=https://aiunderstand.github.io/surfer-ternary-waveviewer/ternary-data-flip-flop.ghw) |
| `ternary-full-adder.ghw` | Ternary full adder simulation | [Open in Surfer](https://aiunderstand.github.io/surfer-ternary-waveviewer/?load_url=https://aiunderstand.github.io/surfer-ternary-waveviewer/ternary-full-adder.ghw) |

You can also download any file and open it manually via **File → Open**.

## Source Code

| Repository | Description |
|---|---|
| [surfer-ternary-waveviewer](https://github.com/aiunderstand/surfer-ternary-waveviewer) | Main repo: surfer-fork + wellen-fork + translator plugin |
| [surfer-fork](https://gitlab.com/anders.minde/surfer-fork) | Surfer fork with ternary GHW support |
| [wellen-fork](https://github.com/anesh1234/wellen-fork) | Wellen fork with balanced ternary GHW decoding |
| [Balanced-Ternary-Surfer-Translator](https://github.com/anesh1234/Balanced-Ternary-Surfer-Translator) | Extism WASM translator plugin |
