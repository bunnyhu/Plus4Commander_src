![GitHub Release](https://img.shields.io/github/v/release/bunnyhu/Plus4Commander)
![GitHub Release Date](https://img.shields.io/github/release-date/bunnyhu/Plus4Commander)

# Plus/4 Commander

Desktop file manager and viewer for Commodore Plus/4 (and 16) disk images, tape images, and standalone files.
![App Screenshot](https://github.com/bunnyhu/Plus4Commander/blob/main/_doc/preview.jpg)

## Key Features

- **Image & File Support**: Browse, create, and manage `.D64` disk images, `.T64` tape images, and standalone `.PRG`, `.SEQ`, `.USR`, and `.REL` files.
- **File Operations**: Copy, rename, delete, and inject files between your PC and virtual images, create new disk/tape images, and edit disk/tape headers.
- **Built-in Viewers**:
  - **BASIC Detokenizer**: List Commodore BASIC 3.5 programs with optional CBM font rendering.
  - **6502/7501 Disassembler**: Inspect machine code binaries with full mnemonic and addressing-mode decoding.
  - **Graphics & Font Viewer**: Render HiRes and Multicolor screen bitmaps (Botticelli, MultiBotticelli, Raw TED palette modes) and 8×8 character sets.
  - **SEQ / Text & Hex Viewer**: Inspect sequential databases, PETSCII text, and raw binary dumps.
- **Yape Emulator Integration**: Mount disk/tape images or quick-run programs directly in Yape.
- **Clipboard Transfer**: Copy text and graphics from the preview panel to the clipboard.
- **Documentation**: Full usage guide available in [`MANUAL.md`](MANUAL.md) (Magyarul: [`MANUAL_HU.md`](MANUAL_HU.md)).

## Requirements

- Windows 10 or later
- .NET 9 / .NET 10 Desktop Runtime
- [Yape Emulator](http://yape.plus4.net/) (mirror: [homeserver.hu](http://yape.homeserver.hu/)) — optional, for running programs directly