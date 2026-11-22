# Plus4Commander — User Guide

**Plus4 Commander** is a Commodore Plus/4 (and 16) disk image (`.d64`), tape image (`.t64`), and file browser utility that combines classic file manager logic with built-in Plus/4 analysis and viewing tools. It provides instant previews of Commodore files (`.PRG`, `.SEQ`, `.USR`, `.REL`), automatically detecting their content type.

The application requires no installation, runs as a standalone executable, and includes the embedded Commodore character set.

---

## 1. User Interface and Navigation

The window is divided into two main sections: the **left file list** (PC directories and virtual disk/tape images) and the **right analysis/preview panel**.

### Top Navigation and Action Bar
* **Drive Selector (`C:\`, `D:\`, ...) and Root Button (`\`):** Quickly switch between drives; the program remembers the last used folder for each drive.
* **Address Bar:** Editable path field that navigates immediately upon input. Pasting the full path of a `.d64` or `.t64` file opens its contents directly.
* **Export (`F5`):** Exports the selected file from a disk/tape image (`.d64` / `.t64`) to your PC.
* **Mount+Load (`F6`):** Inside a virtual image, mounts the image and loads the selected file into the YAPE emulator. In a PC folder with an image file selected, attaches and autostarts it in YAPE.
* **Quick Run (`F7`):** Immediately launches the selected BASIC program in the YAPE emulator.
* **About (`F1`):** Displays application information and allows you to configure the YAPE emulator path.
* **Exit:** Closes the application.

### Left File Browser and Disk Header
* **Header:** In PC mode, displays the drive letter, volume label, and file system. Inside a D64/T64 image, displays the original Commodore disk or tape title, `Disk ID`, and DOS version.
* **File List:**
  * Selecting a `.d64` or `.t64` file in a PC folder previews its full directory (`DIRECTORY`), file count, and block information.
  * Step up one level from a folder or image using `[..]` or `Backspace`.
* **Footer:** In PC mode, shows free/total space and item counts. For D64 images, it displays free/total blocks (`BLOCKS FREE`); for T64 images, it shows used blocks and file count.

---

## 2. File Operations and Context Menu (Right-Click)

Right-clicking on the left file list opens the context menu for file and image management:

* **Copy (`Ctrl+C`) and Paste (`Ctrl+V`):** Copy Commodore files via the clipboard—between PC folders and disk/tape images, or even across two separate **Plus4 Commander** instances (e.g., from one `.d64` to another). If a file with the same name already exists at the destination, you will be prompted to confirm overwriting.
* **Rename File (`F2`):** Works both in PC folders and inside D64/T64 images (up to 16 characters for Commodore filenames).
* **Rename Disk/Tape Label:** Edit the internal label either from inside a D64/T64 image or by selecting the image in a PC folder (D64: max 16 characters + 2-character Disk ID; T64: max 24 characters).
* **Delete File from Image (`Del`):** Deletes the selected file from the opened `.d64` or `.t64` image after confirmation and frees its blocks.
* **Create Empty D64 / T64:** In a PC folder, creates a new empty disk or tape image with a custom label (and Disk ID for D64).

### Preview Pane Clipboard Support
* **Text Views (`Basic`, `Asm`, `HEX`, `Text`):** Right-click inside the text preview to select and copy source code or dumps to the clipboard.
* **Graphical Views (`Image`, `Font`):** Right-click on the rendered image (or click it and press `Ctrl+C`) to copy it to the clipboard at native resolution (`Copy Image`) or crisp 2× nearest-neighbor scaling (`Copy Image (2x)`).

---

## 3. View Mode Buttons and Special Controls

The toolbar above the right-hand preview pane lets you override automatic detection at any time:

| Button | Function |
| :--- | :--- |
| **Auto** | Restores automatic content detection (also triggered with `Space`). |
| **HEX** | Hexadecimal and ASCII dump formatted in 16-byte rows. |
| **Basic** | Detokenizes Commodore BASIC V3.5 programs using original Commodore graphics characters. |
| **Asm** | 6502 / 7501 machine code disassembler (address, raw bytes, instructions). |
| **Text** | Text view (`SEQ`, `USR`, and numbered record-by-record view for `REL` files). |
| **Image** | Renders Plus/4 graphics screens (320×200). |
| **Font** | Displays custom character sets (256 8×8 characters) in a 16×16 grid. |
| **Pal: Auto / Bott / Mult / Raw** | Selects the background/border palette source in Image view (auto-detect, classic Botticelli `$1BE8/$1BE9`, nibble-swapped `MULT`, or raw defaults). |
| **Color / HiRes** | Toggles between Color (Multicolor) and high-resolution monochrome (HiRes) modes in Image view. |
| **◄ Font / Font ►** | Shifts the starting byte offset by ±1 byte in Font view for files with non-standard headers. |

---

## 4. Automatic Preview (`Auto` Mode)

When a Commodore file is selected, the program automatically chooses the most suitable view:

1. **Text / Database File (`Text`):** Used for `SEQ`, `USR`, and `REL` files (for `REL` files inside a D64, the record length is detected and records are split into separate lines).
2. **Character Set (`Font`):** Files around 2 KB (2048–2060 bytes).
3. **BASIC V3.5 Program (`Basic`):** Standard (`$1001`) or graphics-mode (`$4001`) BASIC programs.
4. **Graphics Image (`Image`):** Screen and bitmap files between 7000 and 12500 bytes.
5. **Machine Code (`Asm`):** All other binary files.

### Fast Preview for Large Files
To keep browsing responsive, **HEX** view displays the first 1024 bytes by default, and **Asm** view displays the first 400 instructions. For larger files, a **Show All / Show Full Disasm** button appears at the bottom of the preview pane (you can also double-click the preview text to load the entire file).

---

## 5. Keyboard Shortcuts

| Key | Action |
| :--- | :--- |
| **`↑` / `↓`** | Navigate through files and folders |
| **`Enter`** | Open a folder or `.d64` / `.t64` image |
| **`Backspace`** | Go up to the parent folder or exit the virtual image |
| **`←` / `→`** | Cycle through preview modes (`Auto`, `HEX`, `Basic`, `Asm`, `Text`, `Image`, `Font`) |
| **`Space`** | Reset to automatic (`Auto`) preview mode |
| **`Ctrl+C`** | Copy selected Commodore file (or preview image when focused) to clipboard |
| **`Ctrl+V`** | Paste Commodore file from clipboard (to a PC folder or virtual image) |
| **`F2`** | Rename selected file |
| **`Del`** | Delete selected file from D64/T64 image |
| **`F5`** | Export selected file from D64/T64 image to PC |
| **`F6`** | Mount image and load file in YAPE |
| **`F7`** | Quick-run BASIC file in YAPE |
| **`F1`** | Open the `About` window |
| **`Esc`** | Cancel editing in the top address bar |

---

* **Source Code & Download:** [https://github.com/bunnyhu/Plus4Commander](https://github.com/bunnyhu/Plus4Commander)