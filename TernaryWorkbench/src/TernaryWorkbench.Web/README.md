# TernaryWorkbench.Web

Blazor WebAssembly single-page application providing interactive UIs for all Ternary Workbench tools.

## Tech Stack

- **Blazor WebAssembly** (.NET 10) — runs entirely in the browser via WebAssembly
- **MudBlazor 9** — Material Design component library
- **Markdig 0.40** — Markdown rendering for ISA references and encoding specs

## Running Locally

```bash
# Dev server with hot reload
dotnet watch --project TernaryWorkbench/src/TernaryWorkbench.Web

# Build for production
dotnet publish TernaryWorkbench/src/TernaryWorkbench.Web -c Release
```

The dev server starts at `https://localhost:5001` by default.

## Pages & Routes

| Route | Page | Description |
|-------|------|-------------|
| `/` | Home | Landing page |
| `/radix-converter` | RadixConverter | Convert numbers between numeral systems |
| `/rebel-assembler` | RebelAssembler | REBEL-2/2v2/6 assembly and disassembly |
| `/chart-string-converter` | CharTStringConverter | charT_u8 / charTC_u8 encoding and decoding |
| `/mrcs-studio` | MrcsStudio | MRCS Studio info and link |

## Documentation Rendering

The `MarkdownViewer` component ([Components/MarkdownViewer.razor](Components/MarkdownViewer.razor)) fetches `.md` files from `wwwroot/docs/` at runtime and renders them with Markdig. The `docs/*.md` files from the repo root are copied into `wwwroot/docs/` by MSBuild during the build — edit the source files in `docs/` at the repo root, not the copies in `wwwroot/docs/`.

## Key Files

- `_Imports.razor` — global using directives
- `Layout/MainLayout.razor` — app shell, navigation drawer
- `Layout/Footer.razor` — git commit hash and date footer
- `Components/MarkdownViewer.razor` — shared markdown rendering component
- `Tools/*/` — one folder per tool page
- `wwwroot/` — static assets (CSS, JS, images, fonts, generated docs)
