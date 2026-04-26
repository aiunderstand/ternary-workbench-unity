# MRCS Studio

## What is MRCS Studio?

MRCS Studio is the successor to Mixed Radix Circuit Synthesizer (MRCS), the first browser-based Electronic Design
Automation (EDA) tool to design and verify binary, ternary and hybrid (mixed radix) circuits. It was launched and
presented at IEEE ISCAS 2022 as part of the PhD project of Steven Bos. Where conventional EDA tools assume binary
almost everywhere, MRCS treats radix as a first-class design parameter. MRCS featured a novel MVL circuit synthesis
algorithm with HSPICE and Verilog output targeting binary-coded ternary CMOS and multi-threshold CNTFET. The tool
was used to design REBEL-2, a novel balanced ternary CPU with RISC-V-like ISA. Several MRCS designs have been
successfully tested on a FPGA and verified on silicon using the Openlane ASIC workflow and TinyTapeout services.

MRCS Studio is built from the ground up for **mixed-radix and ternary digital logic design and verification at
Very Large Scale Integration (VLSI)**. It addresses several of the documented shortcomings of MRCS that prevented
designs from scaling beyond 10,000 transistors (VLSI). MRCS Studio is part of the **open ternary technology
stack** designed by Dr. Steven Bos, Dr. Henning Gundersen and the members of the Ternary Research Group at the
University of South-Eastern Norway. MRCS Studio runs in the browser with no installation required, and is also
available as a desktop application for power users.

## Features

### Schematic Editor

- **Native ternary primitives.** Gates, wires, tri-state buffers and buses built for three-valued logic; no binary-to-ternary workarounds.
- **Visual circuit editor with drag-and-drop components.** Design and explore circuits at the logic gate level.
- **C# Circuits.** [Power users] Design and explore circuits programmatically.
- **Command line interface (CLI).** [Power users] Operate MRCS Studio headless.
- **Ternary/mixed radix VLSI gate-level simulation.** Propagate trit values through the schematic in real-time using the SONIC engine.
- **Verilog and SPICE synthesis.** Export automated binary-coded ternary CMOS flows to FPGAs and ASICs or native ternary with emerging devices like CNTFETs to HSPICE.
- **Mixed-radix netlists.** Connect components of different radices within a single schematic.
- **Hierarchical design.** Compose subcircuits at any level of abstraction using reusable subcircuits.
- **Multi-trit bus wiring.** Multi-trit buses with distinct visual styling.
- **Design rule checking (DRC).** Improve circuit feasibility using three-level reporting (error, warning, info).
- **Standardized ternary library.** A collection of 56 standard cells and 66 subcircuits to kick-start any ternary design.
- **Cell / subcircuit search and filter.** Search the cell library by type, radix, or name.
- **Quality-of-life features.** Minimap, zoom controls, dark/light/custom color schemes, keyboard shortcuts, tutorials, auto save and more.

## Changelog — March 2026

### Bus System

- Added bus titles for better identification
- Added menu to select which port in a bus port to connect to gate pins
- Allow buses to cross with DRC warning
- Added highlight of internal wires a bus wire connects to
- Added dashed selection border around marked buses
- Added border and end indicators to buses
- Added red highlight and prohibitive cursor for invalid bus intersections
- Improved bus width label positioning for vertical buses
- Retain bus connection positions on bus resize
- Increased maximum bus width to 64, added shift-click to increase by 8
- Added shift-click range selection and ctrl multi-select in bus menu
- Fixed bus port merging issues and drawing edge cases

### Canvas & Interaction

- Added keystroke shortcuts for wire drawing (right-click removes last anchor)
- Added drag preview of basic components reflecting actual SVGs
- Lock move cursor while moving components
- Disabled context menu on right-click drag for canvas panning
- Added default browser back/forward navigation for subcircuit levels
- Save positional data to the browser for all subcircuit levels

### Design Rule Checks (DRC)

- Moved DRC checks to dedicated namespace
- Updated DRC to check affected paths only (performance improvement)

### UI & Display

- Revamped the About page
- Improved subcircuit title formatting and split titles with filter cell text
- Updated undriven port coloring on subcircuit components
- Added input length limits and adjusted bus port bracket positions

### Internal

- Moved computed properties in domain models to extension members

## Contributors

Steven Bos, Sondre Bitubekk, Ole Christian Moholth, Halvor Nybø Risto, Henning Gundersen, Vetle Bodahl, Erika Fegri, Anders Minde

## Links

- [MRCS Studio GitHub Repository](https://github.com/aiunderstand/MRCS-Studio)
- [MRCS GitHub Repository](https://github.com/aiunderstand/MixedRadixCircuitSynthesis)
- [MRCS Paper](https://nva.sikt.no/registration/0198cc7cede6-140f18d6-f79f-4020-a9cd-8c93850273cb)
- [MRCS Chapter](https://nva.sikt.no/registration/01991379db36-bdd54c2b-e4ec-4e60-8854-030cb3f08217)
- [USN Ternary Research Group Website](https://www.usn.no/english/research/our-research-centres-and-groups/technology/ternary-research/)
