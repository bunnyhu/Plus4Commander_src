using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Plus4Commander
{
    public partial class Form1 : Form
    {
        private const string RegistrySubKey = @"Software\Plus4Commander";
        private const string RegistryLastPathValue = "LastPcPath";
        private const string CbmClipboardFormat = "Plus4Commander.CbmFile.v1";

        private sealed class CbmClipboardPayload
        {
            public string Name { get; set; } = string.Empty;
            public byte[]? RawNameBytes { get; set; }
            public string Type { get; set; } = "PRG";
            public int RecordLength { get; set; } = 0;
            public byte[] RawData { get; set; } = Array.Empty<byte>();
        }

        private string currentPcPath = @"C:\";
        private bool isInsideVirtualImage = false;
        private string activeImageFilePath = string.Empty;
        private string lastSelectedPcEntry = string.Empty;

        private readonly Dictionary<string, string> driveLastPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool suppressDriveChangeEvent = false;
        private bool suppressAddressTextChanged = false;

        private List<DiskFileItem> currentFileList = new List<DiskFileItem>();
        private DiskFileItem? currentSelectedFile = null;
        private Plus4ContentType currentDetectedType = Plus4ContentType.UnknownBinary;
        private string currentSelectedPcFilePath = string.Empty;

        private string forcedViewMode = "AUTO";
        private int fontByteShift = 0;
        private bool isMulticolorMode = true;
        private ImagePaletteMode imagePaletteMode = ImagePaletteMode.Auto;
        private bool showFullPreviewContent = false;

        private readonly Color D64BackColor = Color.FromArgb(62, 49, 162);
        private readonly Color D64ForeColor = Color.FromArgb(165, 165, 255);
        private readonly Color D64HeaderBackColor = Color.FromArgb(44, 34, 125);

        private PrivateFontCollection? customCbmFonts;
        private FontFamily? cbmListFontFamily;

        private ContextMenuStrip ctxListMenu = null!;
        private ToolStripMenuItem miCopyCbmFile = null!;
        private ToolStripMenuItem miPasteCbmFile = null!;
        private ToolStripMenuItem miRenameFile = null!;
        private ToolStripMenuItem miRenameImageHeader = null!;
        private ToolStripMenuItem miDeleteVirtualFile = null!;
        private ToolStripSeparator miSepCreateImage = null!;
        private ToolStripMenuItem miNewD64 = null!;
        private ToolStripMenuItem miNewT64 = null!;

        private ContextMenuStrip ctxPreviewImageMenu = null!;
        private ToolStripMenuItem miCopyPreviewImage1x = null!;
        private ToolStripMenuItem miCopyPreviewImage2x = null!;

        private enum ListItemKind
        {
            ParentDirectory,
            Directory,
            D64Image,
            T64Image,
            PrgFile,
            SeqFile,
            UsrFile,
            RelFile,
            OtherFile
        }

        private readonly Bitmap icoDirParent = Properties.Resources.icoDirParent;
        private readonly Bitmap icoDir = Properties.Resources.icoDir;
        private readonly Bitmap icoD64 = Properties.Resources.icoD64;
        private readonly Bitmap icoT64 = Properties.Resources.icoT64;
        private readonly Bitmap icoPRG = Properties.Resources.icoPRG;
        private readonly Bitmap icoSEQ = Properties.Resources.icoSEQ;
        private readonly Bitmap icoUSR = Properties.Resources.icoUSR;
        private readonly Bitmap icoREL = Properties.Resources.icoREL;
        private readonly Bitmap icoOther = Properties.Resources.icoOther;
        private readonly Bitmap icoCopy = Properties.Resources.icoCopy;
        private readonly Bitmap icoPaste = Properties.Resources.icoPaste;
        private readonly Bitmap icoDelete = Properties.Resources.icoDelete;
        private readonly Bitmap icoRenameVol = Properties.Resources.icoRenameVol;

        public Form1()
        {
            InitializeComponent();
            foreach (var bmp in new[] { icoDirParent, icoDir, icoD64, icoT64, icoPRG, icoSEQ, icoUSR, icoREL, icoOther, icoCopy, icoPaste, icoDelete, icoRenameVol })
            {
                bmp.MakeTransparent(Color.Magenta);
            }
            LoadEmbeddedCbmFont();
            InitializeFileListContextMenu();
            InitializePreviewImageContextMenu();
            BindEvents();
            UpdateActiveViewButtonHighlight();

            string defaultPath = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory) ?? @"C:\";
            string savedPath = LoadLastPathFromRegistry();
            currentPcPath = (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                ? savedPath
                : defaultPath;

            LoadPcDirectory(currentPcPath);
        }

        private void InitializePreviewImageContextMenu()
        {
            ctxPreviewImageMenu = new ContextMenuStrip();

            miCopyPreviewImage1x = new ToolStripMenuItem("Copy Image (1:1 size)", icoCopy, (s, e) => CopyPreviewImageToClipboard(1))
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };

            miCopyPreviewImage2x = new ToolStripMenuItem("Copy Image (2x size)", icoCopy, (s, e) => CopyPreviewImageToClipboard(2));

            ctxPreviewImageMenu.Items.AddRange(new ToolStripItem[]
            {
                miCopyPreviewImage1x,
                miCopyPreviewImage2x
            });

            ctxPreviewImageMenu.Opening += (s, e) =>
            {
                bool hasImage = pbView.Visible && pbView.Image != null;
                miCopyPreviewImage1x.Enabled = hasImage;
                miCopyPreviewImage2x.Enabled = hasImage;
            };

            pbView.ContextMenuStrip = ctxPreviewImageMenu;
            pbView.MouseDown += (s, e) => pbView.Focus();
        }

        private void CopyPreviewImageToClipboard(int scale = 1)
        {
            if (!pbView.Visible || pbView.Image == null) return;

            try
            {
                Image src = pbView.Image;
                if (scale <= 1)
                {
                    using (Bitmap copy = new Bitmap(src))
                    {
                        Clipboard.SetImage(copy);
                    }
                }
                else
                {
                    int w = src.Width * scale;
                    int h = src.Height * scale;
                    using (Bitmap scaled = new Bitmap(w, h))
                    {
                        using (Graphics g = Graphics.FromImage(scaled))
                        {
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.Half;
                            g.DrawImage(src, new Rectangle(0, 0, w, h));
                        }
                        Clipboard.SetImage(scaled);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy image to clipboard:\r\n{ex.Message}", "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsSelectedPcFile(out string fileName)
        {
            fileName = string.Empty;
            if (isInsideVirtualImage) return false;

            int index = listBoxFiles.SelectedIndex;
            if (index <= 0 || listBoxFiles.SelectedItem == null) return false;

            string selectedLine = listBoxFiles.SelectedItem.ToString() ?? string.Empty;
            int pipeIdx = selectedLine.IndexOf('|');
            if (pipeIdx <= 0) return false;

            fileName = selectedLine.Substring(0, pipeIdx).Trim();
            return !string.IsNullOrEmpty(fileName) && File.Exists(Path.Combine(currentPcPath, fileName));
        }

        private string? GetTargetImagePathForHeaderRename()
        {
            if (isInsideVirtualImage)
            {
                return (!string.IsNullOrEmpty(activeImageFilePath) && File.Exists(activeImageFilePath))
                    ? activeImageFilePath
                    : null;
            }

            if (IsSelectedPcFile(out string pcFile))
            {
                string ext = Path.GetExtension(pcFile).ToUpperInvariant();
                if (ext == ".D64" || ext == ".T64")
                {
                    return Path.Combine(currentPcPath, pcFile);
                }
            }

            return null;
        }

        private void InitializeFileListContextMenu()
        {
            ctxListMenu = new ContextMenuStrip();

            miCopyCbmFile = new ToolStripMenuItem("Copy Commodore File", icoCopy, (s, e) => CopySelectedCbmFileToClipboard())
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };

            miPasteCbmFile = new ToolStripMenuItem("Paste Commodore File", icoPaste, (s, e) => PasteCbmFileFromClipboard())
            {
                ShortcutKeyDisplayString = "Ctrl+V"
            };

            miRenameFile = new ToolStripMenuItem("Rename File...", null, (s, e) => RenameSelectedFile())
            {
                ShortcutKeyDisplayString = "F2"
            };

            miRenameImageHeader = new ToolStripMenuItem("Rename Disk/Tape Label...", icoRenameVol, (s, e) => RenameVirtualImageHeader());

            miDeleteVirtualFile = new ToolStripMenuItem("Delete File from Image", icoDelete, (s, e) => DeleteSelectedVirtualFile())
            {
                ShortcutKeyDisplayString = "Del"
            };

            miSepCreateImage = new ToolStripSeparator();
            miNewD64 = new ToolStripMenuItem("Create Empty D64 Disk Image...", icoD64, (s, e) => CreateNewImageInPcFolder(isT64: false));
            miNewT64 = new ToolStripMenuItem("Create Empty T64 Tape Image...", icoT64, (s, e) => CreateNewImageInPcFolder(isT64: true));

            ctxListMenu.Items.AddRange(new ToolStripItem[]
            {
                miCopyCbmFile,
                miPasteCbmFile,
                miRenameFile,
                miRenameImageHeader,
                miDeleteVirtualFile,
                miSepCreateImage,
                miNewD64,
                miNewT64
            });

            ctxListMenu.Opening += (s, e) =>
            {
                bool hasCbmFileSelected = currentSelectedFile != null;
                bool hasClipboardCbm = Clipboard.ContainsData(CbmClipboardFormat);
                bool canRename = isInsideVirtualImage
                    ? hasCbmFileSelected
                    : IsSelectedPcFile(out _);

                string? targetImagePath = GetTargetImagePathForHeaderRename();
                bool canRenameHeader = !string.IsNullOrEmpty(targetImagePath);

                miCopyCbmFile.Visible = true;
                miCopyCbmFile.Enabled = hasCbmFileSelected;

                miPasteCbmFile.Visible = true;
                miPasteCbmFile.Enabled = hasClipboardCbm;

                miRenameFile.Visible = true;
                miRenameFile.Enabled = canRename;

                miRenameImageHeader.Visible = canRenameHeader;
                if (canRenameHeader)
                {
                    bool isT64 = Path.GetExtension(targetImagePath!).Equals(".T64", StringComparison.OrdinalIgnoreCase);
                    miRenameImageHeader.Text = isT64 ? "Rename T64 Tape Label..." : "Rename D64 Disk Label...";
                }

                miDeleteVirtualFile.Visible = isInsideVirtualImage;
                miDeleteVirtualFile.Enabled = isInsideVirtualImage && hasCbmFileSelected;

                miSepCreateImage.Visible = !isInsideVirtualImage;
                miNewD64.Visible = !isInsideVirtualImage;
                miNewT64.Visible = !isInsideVirtualImage;
            };

            listBoxFiles.ContextMenuStrip = ctxListMenu;
        }

        private void RenameVirtualImageHeader()
        {
            string? imagePath = GetTargetImagePathForHeaderRename();
            if (string.IsNullOrEmpty(imagePath)) return;

            bool isT64 = Path.GetExtension(imagePath).Equals(".T64", StringComparison.OrdinalIgnoreCase);
            DiskHeaderInfo header;

            try
            {
                header = isT64
                    ? Plus4Parser.ReadT64Header(imagePath)
                    : Plus4Parser.ReadD64Header(imagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not read image header:\r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int maxLabelLen = isT64 ? 24 : 16;

            using (Form dlg = new Form())
            {
                dlg.Text = isT64 ? "Rename T64 Tape Label" : "Rename D64 Disk Label";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(380, isT64 ? 125 : 175);

                Label lblLabel = new Label
                {
                    Text = isT64 ? $"Tape label (max {maxLabelLen} chars):" : $"Disk label (max {maxLabelLen} chars):",
                    Left = 14,
                    Top = 14,
                    Width = 250
                };
                Label lblCount = new Label
                {
                    Left = 284,
                    Top = 14,
                    Width = 80,
                    TextAlign = ContentAlignment.TopRight,
                    ForeColor = Color.DimGray
                };
                TextBox txtLabel = new TextBox
                {
                    Left = 14,
                    Top = 38,
                    Width = 350,
                    MaxLength = maxLabelLen,
                    CharacterCasing = CharacterCasing.Upper,
                    Text = header.DiskName.Length > maxLabelLen ? header.DiskName.Substring(0, maxLabelLen) : header.DiskName
                };
                lblCount.Text = $"{txtLabel.Text.Length} / {maxLabelLen}";
                txtLabel.TextChanged += (s, e) => lblCount.Text = $"{txtLabel.Text.Length} / {maxLabelLen}";

                TextBox? txtDiskId = null;
                if (!isT64)
                {
                    Label lblId = new Label { Text = "Disk ID (max 2 chars):", Left = 14, Top = 74, Width = 180 };
                    txtDiskId = new TextBox
                    {
                        Left = 14,
                        Top = 96,
                        Width = 80,
                        MaxLength = 2,
                        CharacterCasing = CharacterCasing.Upper,
                        Text = string.IsNullOrWhiteSpace(header.DiskId) ? "01" : header.DiskId
                    };
                    dlg.Controls.Add(lblId);
                    dlg.Controls.Add(txtDiskId);
                }

                int btnTop = isT64 ? 80 : 132;
                Button btnOk = new Button { Text = "Save", DialogResult = DialogResult.OK, Left = 200, Top = btnTop, Width = 78, Height = 28 };
                Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 286, Top = btnTop, Width = 78, Height = 28 };

                dlg.Controls.AddRange(new Control[] { lblLabel, lblCount, txtLabel, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                dlg.Shown += (s, e) =>
                {
                    txtLabel.Focus();
                    txtLabel.SelectAll();
                };

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    string newLabel = txtLabel.Text.Trim();
                    if (isT64)
                    {
                        Plus4Parser.RenameT64Header(imagePath, newLabel);
                    }
                    else
                    {
                        string newId = txtDiskId?.Text.Trim() ?? header.DiskId;
                        Plus4Parser.RenameD64Header(imagePath, newLabel, newId);
                    }

                    if (isInsideVirtualImage)
                    {
                        OpenVirtualImage(imagePath, currentSelectedFile?.Name);
                    }
                    else
                    {
                        RefreshFileContentView();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to rename image label:\r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    listBoxFiles.Focus();
                }
            }
        }

        private void RenameSelectedFile()
        {
            if (isInsideVirtualImage)
            {
                if (currentSelectedFile == null || string.IsNullOrEmpty(activeImageFilePath))
                    return;

                int listIndex = listBoxFiles.SelectedIndex;
                int fileIndex = listIndex - 1;
                if (fileIndex < 0 || fileIndex >= currentFileList.Count) return;

                string oldName = currentSelectedFile.Name;
                const int maxCbmLen = 16;

                string? newName = ShowRenameDialog(
                    title: "Rename Commodore File",
                    prompt: $"New file name (max {maxCbmLen} chars):",
                    initialValue: oldName,
                    maxLength: maxCbmLen,
                    forceUppercase: true,
                    selectWithoutExtension: false
                );

                if (string.IsNullOrWhiteSpace(newName)) return;
                newName = newName.Trim().ToUpperInvariant();
                if (newName.Length > maxCbmLen) newName = newName.Substring(0, maxCbmLen);
                if (newName.Equals(oldName, StringComparison.Ordinal)) return;

                bool duplicateExists = currentFileList
                    .Where((f, idx) => idx != fileIndex)
                    .Any(f => f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

                if (duplicateExists)
                {
                    MessageBox.Show(
                        $"A file named '{newName}' already exists in this image.",
                        "Rename Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                try
                {
                    string ext = Path.GetExtension(activeImageFilePath).ToUpperInvariant();
                    if (ext == ".T64")
                    {
                        Plus4Parser.RenameFileInT64(activeImageFilePath, fileIndex, newName);
                    }
                    else
                    {
                        Plus4Parser.RenameFileInD64(activeImageFilePath, fileIndex, newName);
                    }

                    OpenVirtualImage(activeImageFilePath, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to rename file:\r\n{ex.Message}", "Rename Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    listBoxFiles.Focus();
                }
            }
            else
            {
                if (!IsSelectedPcFile(out string oldFileName)) return;

                string oldFullPath = Path.Combine(currentPcPath, oldFileName);
                int maxPcFileLen = Math.Clamp(250 - currentPcPath.Length, 16, 200);

                string? newFileName = ShowRenameDialog(
                    title: "Rename PC File",
                    prompt: $"New file name (max {maxPcFileLen} chars):",
                    initialValue: oldFileName,
                    maxLength: maxPcFileLen,
                    forceUppercase: false,
                    selectWithoutExtension: true
                );

                if (string.IsNullOrWhiteSpace(newFileName)) return;
                newFileName = newFileName.Trim();
                if (newFileName.Equals(oldFileName, StringComparison.Ordinal)) return;

                if (newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    MessageBox.Show(
                        "The file name contains invalid characters.",
                        "Invalid File Name",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                string newFullPath = Path.Combine(currentPcPath, newFileName);
                bool isCaseOnlyChange = oldFullPath.Equals(newFullPath, StringComparison.OrdinalIgnoreCase);

                if (!isCaseOnlyChange && File.Exists(newFullPath))
                {
                    MessageBox.Show(
                        $"A file named '{newFileName}' already exists in this folder.",
                        "Rename Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                try
                {
                    if (isCaseOnlyChange)
                    {
                        string tempPath = Path.Combine(currentPcPath, $".p4c_tmp_{Guid.NewGuid():N}");
                        File.Move(oldFullPath, tempPath);
                        File.Move(tempPath, newFullPath);
                    }
                    else
                    {
                        File.Move(oldFullPath, newFullPath);
                    }

                    LoadPcDirectory(currentPcPath, newFileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to rename file:\r\n{ex.Message}", "Rename Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    listBoxFiles.Focus();
                }
            }
        }

        private string? ShowRenameDialog(
            string title,
            string prompt,
            string initialValue,
            int maxLength,
            bool forceUppercase,
            bool selectWithoutExtension)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(380, 125);

                Label lblPrompt = new Label { Text = prompt, Left = 14, Top = 14, Width = 260 };
                Label lblCount = new Label
                {
                    Left = 284,
                    Top = 14,
                    Width = 80,
                    TextAlign = ContentAlignment.TopRight,
                    ForeColor = Color.DimGray
                };

                TextBox txtName = new TextBox
                {
                    Left = 14,
                    Top = 38,
                    Width = 350,
                    MaxLength = maxLength,
                    CharacterCasing = forceUppercase ? CharacterCasing.Upper : CharacterCasing.Normal,
                    Text = initialValue.Length > maxLength ? initialValue.Substring(0, maxLength) : initialValue
                };

                lblCount.Text = $"{txtName.Text.Length} / {maxLength}";
                txtName.TextChanged += (s, e) => lblCount.Text = $"{txtName.Text.Length} / {maxLength}";

                Button btnOk = new Button { Text = "Rename", DialogResult = DialogResult.OK, Left = 200, Top = 80, Width = 78, Height = 28 };
                Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 286, Top = 80, Width = 78, Height = 28 };

                dlg.Controls.AddRange(new Control[] { lblPrompt, lblCount, txtName, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                dlg.Shown += (s, e) =>
                {
                    txtName.Focus();
                    if (selectWithoutExtension)
                    {
                        int dotIdx = txtName.Text.LastIndexOf('.');
                        if (dotIdx > 0)
                        {
                            txtName.Select(0, dotIdx);
                            return;
                        }
                    }
                    txtName.SelectAll();
                };

                return dlg.ShowDialog(this) == DialogResult.OK ? txtName.Text : null;
            }
        }

        private void CopySelectedCbmFileToClipboard()
        {
            if (currentSelectedFile == null) return;

            try
            {
                string cleanName = isInsideVirtualImage
                    ? currentSelectedFile.Name
                    : Path.GetFileNameWithoutExtension(currentSelectedFile.Name).ToUpperInvariant();

                if (cleanName.Length > 16) cleanName = cleanName.Substring(0, 16);

                var payload = new CbmClipboardPayload
                {
                    Name = cleanName,
                    RawNameBytes = (isInsideVirtualImage && currentSelectedFile.RawNameBytes.Length == 16)
                        ? currentSelectedFile.RawNameBytes
                        : null,
                    Type = currentSelectedFile.Type.Replace("*", "").Trim().ToUpperInvariant(),
                    RecordLength = currentSelectedFile.RecordLength,
                    RawData = currentSelectedFile.RawData
                };

                string json = JsonSerializer.Serialize(payload);
                DataObject dataObj = new DataObject();
                dataObj.SetData(CbmClipboardFormat, json);
                Clipboard.SetDataObject(dataObj, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy file to clipboard:\r\n{ex.Message}", "Clipboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PasteCbmFileFromClipboard()
        {
            if (!Clipboard.ContainsData(CbmClipboardFormat)) return;

            try
            {
                if (!Clipboard.TryGetData(CbmClipboardFormat, out string? json) || string.IsNullOrWhiteSpace(json))
                    return;

                CbmClipboardPayload? item = JsonSerializer.Deserialize<CbmClipboardPayload>(json);
                if (item == null || item.RawData == null) return;

                string cleanType = string.IsNullOrWhiteSpace(item.Type)
                    ? "PRG"
                    : item.Type.Replace("*", "").Trim().ToUpperInvariant();

                if (!isInsideVirtualImage)
                {
                    string safeName = string.IsNullOrWhiteSpace(item.Name) ? "pasted_file" : item.Name.Trim();
                    foreach (char invalidChar in Path.GetInvalidFileNameChars())
                    {
                        safeName = safeName.Replace(invalidChar, '_');
                    }

                    string fileName = $"{safeName}.{cleanType.ToLowerInvariant()}";
                    string targetPath = Path.Combine(currentPcPath, fileName);

                    if (File.Exists(targetPath))
                    {
                        DialogResult dr = MessageBox.Show(
                            $"The file '{fileName}' already exists in this folder.\r\nDo you want to overwrite it?",
                            "Confirm Overwrite",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );
                        if (dr != DialogResult.Yes) return;
                    }

                    File.WriteAllBytes(targetPath, item.RawData);
                    LoadPcDirectory(currentPcPath, fileName);
                }
                else
                {
                    if (string.IsNullOrEmpty(activeImageFilePath) || !File.Exists(activeImageFilePath)) return;

                    string cbmName = (item.Name ?? "UNNAMED").Trim().ToUpperInvariant();
                    if (cbmName.Length > 16) cbmName = cbmName.Substring(0, 16);

                    bool exists = currentFileList.Any(f => f.Name.Equals(cbmName, StringComparison.OrdinalIgnoreCase));
                    if (exists)
                    {
                        DialogResult dr = MessageBox.Show(
                            $"A file named '{cbmName}' already exists inside '{Path.GetFileName(activeImageFilePath)}'.\r\nDo you want to overwrite it?",
                            "Confirm Overwrite",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );
                        if (dr != DialogResult.Yes) return;
                    }

                    string ext = Path.GetExtension(activeImageFilePath).ToUpperInvariant();
                    if (ext == ".T64")
                    {
                        Plus4Parser.AddFileToT64(
                            activeImageFilePath,
                            cbmName,
                            item.RawNameBytes,
                            cleanType,
                            item.RawData,
                            overwriteExisting: true
                        );
                    }
                    else
                    {
                        Plus4Parser.AddFileToD64(
                            activeImageFilePath,
                            cbmName,
                            item.RawNameBytes,
                            cleanType,
                            item.RawData,
                            item.RecordLength,
                            overwriteExisting: true
                        );
                    }

                    OpenVirtualImage(activeImageFilePath, cbmName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to paste file:\r\n{ex.Message}", "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listBoxFiles.Focus();
            }
        }

        private void DeleteSelectedVirtualFile()
        {
            if (!isInsideVirtualImage || currentSelectedFile == null || string.IsNullOrEmpty(activeImageFilePath))
                return;

            int listIndex = listBoxFiles.SelectedIndex;
            int fileIndex = listIndex - 1;
            if (fileIndex < 0 || fileIndex >= currentFileList.Count) return;

            DialogResult dr = MessageBox.Show(
                $"Are you sure you want to delete '{currentSelectedFile.Name}' ({currentSelectedFile.Type}) from '{Path.GetFileName(activeImageFilePath)}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (dr != DialogResult.Yes) return;

            try
            {
                string ext = Path.GetExtension(activeImageFilePath).ToUpperInvariant();
                if (ext == ".T64")
                {
                    Plus4Parser.DeleteFileFromT64(activeImageFilePath, fileIndex);
                }
                else
                {
                    Plus4Parser.DeleteFileFromD64(activeImageFilePath, fileIndex);
                }

                OpenVirtualImage(activeImageFilePath);
                if (listBoxFiles.Items.Count > 0)
                {
                    listBoxFiles.SelectedIndex = Math.Min(listIndex, listBoxFiles.Items.Count - 1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete file:\r\n{ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listBoxFiles.Focus();
            }
        }

        private void CreateNewImageInPcFolder(bool isT64)
        {
            if (isInsideVirtualImage) return;

            int maxLabelLen = isT64 ? 24 : 16;
            string ext = isT64 ? ".t64" : ".d64";
            string title = isT64 ? "Create Empty T64 Tape Image" : "Create Empty D64 Disk Image";

            using (Form dlg = new Form())
            {
                dlg.Text = title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ClientSize = new Size(380, isT64 ? 175 : 220);

                Label lblFile = new Label { Text = "File name:", Left = 14, Top = 16, Width = 100 };
                TextBox txtFile = new TextBox
                {
                    Left = 14,
                    Top = 38,
                    Width = 350,
                    Text = isT64 ? "new_tape.t64" : "new_disk.d64"
                };

                Label lblLabel = new Label
                {
                    Text = isT64 ? $"Tape label (max {maxLabelLen} chars):" : $"Disk label (max {maxLabelLen} chars):",
                    Left = 14,
                    Top = 72,
                    Width = 250
                };
                Label lblCount = new Label
                {
                    Text = $"0 / {maxLabelLen}",
                    Left = 284,
                    Top = 72,
                    Width = 80,
                    TextAlign = ContentAlignment.TopRight,
                    ForeColor = Color.DimGray
                };
                TextBox txtLabel = new TextBox
                {
                    Left = 14,
                    Top = 94,
                    Width = 350,
                    MaxLength = maxLabelLen,
                    CharacterCasing = CharacterCasing.Upper,
                    Text = isT64 ? "PLUS4 TAPE" : "PLUS4 DISK"
                };
                lblCount.Text = $"{txtLabel.Text.Length} / {maxLabelLen}";
                txtLabel.TextChanged += (s, e) => lblCount.Text = $"{txtLabel.Text.Length} / {maxLabelLen}";

                TextBox? txtDiskId = null;
                if (!isT64)
                {
                    Label lblId = new Label { Text = "Disk ID (max 2 chars):", Left = 14, Top = 128, Width = 180 };
                    txtDiskId = new TextBox
                    {
                        Left = 14,
                        Top = 150,
                        Width = 80,
                        MaxLength = 2,
                        CharacterCasing = CharacterCasing.Upper,
                        Text = "01"
                    };
                    dlg.Controls.Add(lblId);
                    dlg.Controls.Add(txtDiskId);
                }

                int btnTop = isT64 ? 132 : 178;
                Button btnOk = new Button { Text = "Create", DialogResult = DialogResult.OK, Left = 200, Top = btnTop, Width = 78, Height = 28 };
                Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 286, Top = btnTop, Width = 78, Height = 28 };

                dlg.Controls.AddRange(new Control[] { lblFile, txtFile, lblLabel, lblCount, txtLabel, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string fileName = txtFile.Text.Trim();
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.Show("Please enter a valid file name.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (char invalidChar in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(invalidChar, '_');
                }

                if (!fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ext;
                }

                string fullPath = Path.Combine(currentPcPath, fileName);
                if (File.Exists(fullPath))
                {
                    DialogResult dr = MessageBox.Show(
                        $"The file '{fileName}' already exists.\r\nDo you want to overwrite it?",
                        "Confirm Overwrite",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );
                    if (dr != DialogResult.Yes) return;
                }

                try
                {
                    string labelText = txtLabel.Text.Trim();
                    if (isT64)
                    {
                        Plus4Parser.CreateEmptyT64(fullPath, labelText);
                    }
                    else
                    {
                        string diskId = txtDiskId?.Text.Trim() ?? "01";
                        if (string.IsNullOrEmpty(diskId)) diskId = "01";
                        Plus4Parser.CreateEmptyD64(fullPath, labelText, diskId);
                    }

                    LoadPcDirectory(currentPcPath, fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create image file:\r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            listBoxFiles.Focus();
        }

        private void LoadEmbeddedCbmFont()
        {
            try
            {
                byte[] fontData = Properties.Resources.fontC64ProMono;
                IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
                try
                {
                    Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
                    customCbmFonts = new PrivateFontCollection();
                    customCbmFonts.AddMemoryFont(fontPtr, fontData.Length);
                    if (customCbmFonts.Families.Length > 0)
                    {
                        cbmListFontFamily = customCbmFonts.Families[0];
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(fontPtr);
                }
            }
            catch
            {
                cbmListFontFamily = null;
            }
        }

        private string FormatPetsciiForCbmFont(byte[] rawNameBytes, int padWidth = 17)
        {
            StringBuilder sb = new StringBuilder();
            int charCount = 0;

            foreach (byte b in rawNameBytes)
            {
                if (b == 0xA0 || b == 0x00) break;

                if (cbmListFontFamily != null)
                {
                    sb.Append(char.ConvertFromUtf32(0xE000 + b));
                }
                else
                {
                    char c = (b >= 0x20 && b <= 0x5E) ? (char)b : (b >= 0xC1 && b <= 0xDA ? (char)(b - 0x80) : '?');
                    sb.Append(c);
                }
                charCount++;
            }

            while (charCount < padWidth)
            {
                sb.Append(' ');
                charCount++;
            }

            return sb.ToString();
        }

        private static string FormatPcFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";

            double kb = bytes / 1024.0;
            if (kb < 1024)
                return $"{kb:0.#} KB";

            double mb = kb / 1024.0;
            if (mb < 1024)
                return $"{mb:0.##} MB";

            double gb = mb / 1024.0;
            return $"{gb:0.##} GB";
        }

        private string LoadLastPathFromRegistry()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistrySubKey))
                {
                    return key?.GetValue(RegistryLastPathValue) as string ?? string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private void SaveLastPathToRegistry(string path)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistrySubKey))
                {
                    key.SetValue(RegistryLastPathValue, path, RegistryValueKind.String);
                }
            }
            catch
            {
            }
        }

        private void BindEvents()
        {
            toolCurrentLocation.TextChanged += (s, e) =>
            {
                if (suppressAddressTextChanged) return;
                TryNavigateFromAddressBar(toolCurrentLocation.Text, isPasteOrAuto: true);
            };

            if (toolCurrentLocation.Control is TextBox toolTxt)
            {
                toolTxt.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        TryNavigateFromAddressBar(toolCurrentLocation.Text, isPasteOrAuto: false);
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        e.SuppressKeyPress = true;
                        RestoreAddressBarText();
                        listBoxFiles.Focus();
                    }
                };
            }

            btnShowAllContent.Click += (s, e) =>
            {
                showFullPreviewContent = true;
                RefreshFileContentView();
            };

            textBoxInfo.DoubleClick += (s, e) =>
            {
                if (panelLoadMoreBar.Visible)
                {
                    showFullPreviewContent = true;
                    RefreshFileContentView();
                }
            };

            btnViewAuto.Click += (s, e) => ChangeManualView("AUTO");
            btnViewBasic.Click += (s, e) => ChangeManualView("BASIC");
            btnViewFont.Click += (s, e) => ChangeManualView("FONT");
            btnViewImage.Click += (s, e) => ChangeManualView("IMAGE");
            btnViewHex.Click += (s, e) => ChangeManualView("HEX");
            btnViewDisasm.Click += (s, e) => ChangeManualView("DISASM");
            btnViewSeq.Click += (s, e) => ChangeManualView("SEQ");

            btnImagePalette.Click += (s, e) =>
            {
                imagePaletteMode = imagePaletteMode switch
                {
                    ImagePaletteMode.Auto => ImagePaletteMode.Botticelli,
                    ImagePaletteMode.Botticelli => ImagePaletteMode.MultiB,
                    ImagePaletteMode.MultiB => ImagePaletteMode.Raw,
                    _ => ImagePaletteMode.Auto
                };
                btnImagePalette.Text = imagePaletteMode switch
                {
                    ImagePaletteMode.Botticelli => "Pal: Bott",
                    ImagePaletteMode.MultiB => "Pal: Mult",
                    ImagePaletteMode.Raw => "Pal: Raw",
                    _ => "Pal: Auto"
                };
                RefreshFileContentView();
            };

            btnToggleMulti.Click += (s, e) =>
            {
                isMulticolorMode = !isMulticolorMode;
                btnToggleMulti.Text = isMulticolorMode ? "Color" : "HiRes";
                RefreshFileContentView();
            };

            btnShiftMinus.Click += (s, e) => { fontByteShift--; RefreshFileContentView(); };
            btnShiftPlus.Click += (s, e) => { fontByteShift++; RefreshFileContentView(); };

            listBoxFiles.DrawItem += ListBoxFiles_DrawItem;
            listBoxFiles.DoubleClick += (s, e) => ActivateSelectedListItem();
            listBoxFiles.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    int idx = listBoxFiles.IndexFromPoint(e.Location);
                    if (idx >= 0 && idx < listBoxFiles.Items.Count)
                    {
                        listBoxFiles.SelectedIndex = idx;
                    }
                }
            };
            listBoxFiles.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ActivateSelectedListItem();
                }
                else if (e.KeyCode == Keys.Back)
                {
                    e.SuppressKeyPress = true;
                    NavigateUpOneLevel();
                }
            };

            toolButtonRoot.Click += (s, e) =>
            {
                isInsideVirtualImage = false;
                string root = Path.GetPathRoot(currentPcPath) ?? @"C:\";
                LoadPcDirectory(root);
            };

            toolBtnExportFile.Click += (s, e) => ExportSelectedVirtualFile();
            toolBtnYapeMountRun.Click += (s, e) => YapeMountAndRun();
            toolBtnYapeQRun.Click += (s, e) => YapeQuickRun();
            toolBtnAbout.Click += (s, e) => ShowAboutDialog();
            toolBtnExit.Click += (s, e) => Application.Exit();

            RefreshDriveList();
            toolComboDrives.SelectedIndexChanged += (s, e) =>
            {
                if (suppressDriveChangeEvent || toolComboDrives.SelectedItem == null) return;

                string? driveLetter = toolComboDrives.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    if (driveLastPaths.TryGetValue(driveLetter, out string? rememberedPath) && Directory.Exists(rememberedPath))
                    {
                        LoadPcDirectory(rememberedPath);
                    }
                    else
                    {
                        LoadPcDirectory(driveLetter);
                    }
                }
            };
        }

        private string? EnsureYapePathConfigured()
        {
            string yapePath = AboutForm.LoadYapePathFromRegistry();
            if (!string.IsNullOrWhiteSpace(yapePath) && File.Exists(yapePath))
            {
                return yapePath;
            }

            MessageBox.Show(
                "YAPE emulator path is not configured or the file was not found.\r\nPlease select yape.exe in the About window.",
                "YAPE Not Configured",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            ShowAboutDialog();

            yapePath = AboutForm.LoadYapePathFromRegistry();
            return (!string.IsNullOrWhiteSpace(yapePath) && File.Exists(yapePath)) ? yapePath : null;
        }

        private void CloseRunningYapeInstances()
        {
            foreach (var proc in Process.GetProcessesByName("yape"))
            {
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.CloseMainWindow();
                        if (!proc.WaitForExit(1000))
                        {
                            proc.Kill();
                            proc.WaitForExit(500);
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }

        private void YapeMountAndRun()
        {
            if (!isInsideVirtualImage)
            {
                if (string.IsNullOrEmpty(currentSelectedPcFilePath) || !File.Exists(currentSelectedPcFilePath))
                    return;

                string pcExt = Path.GetExtension(currentSelectedPcFilePath).ToUpperInvariant();
                if (pcExt != ".D64" && pcExt != ".T64")
                    return;

                string? yapeExe = EnsureYapePathConfigured();
                if (yapeExe == null) return;

                try
                {
                    CloseRunningYapeInstances();

                    var psi = new ProcessStartInfo
                    {
                        FileName = yapeExe,
                        Arguments = $"\"{currentSelectedPcFilePath}\"",
                        WorkingDirectory = Path.GetDirectoryName(yapeExe) ?? string.Empty,
                        UseShellExecute = false
                    };

                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to launch YAPE:\r\n{ex.Message}", "YAPE Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            if (currentSelectedFile == null || string.IsNullOrEmpty(activeImageFilePath))
                return;

            string? yapePath = EnsureYapePathConfigured();
            if (yapePath == null) return;

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Plus4Commander");
                Directory.CreateDirectory(tempDir);
                string cmdFilePath = Path.Combine(tempDir, "yape_cmd.txt");

                string cbmName = currentSelectedFile.Name.ToUpperInvariant();
                string bootPadding = " ";
                string commandText;

                if (currentDetectedType == Plus4ContentType.BasicProgram)
                {
                    commandText = $"{bootPadding}DLOAD\"{cbmName}\"\n";
                }
                else
                {
                    commandText = $"{bootPadding}LOAD\"{cbmName}\",8,1\n";
                }

                File.WriteAllText(cmdFilePath, commandText, Encoding.ASCII);

                CloseRunningYapeInstances();

                string ext = Path.GetExtension(activeImageFilePath).ToUpperInvariant();
                string mountArg = ext == ".T64"
                    ? $"/TAPE:\"{activeImageFilePath}\""
                    : $"/DISK8:\"{activeImageFilePath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = yapePath,
                    Arguments = $"{mountArg} /LOAD:\"{cmdFilePath}\"",
                    WorkingDirectory = Path.GetDirectoryName(yapePath) ?? string.Empty,
                    UseShellExecute = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch YAPE:\r\n{ex.Message}", "YAPE Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void YapeQuickRun()
        {
            if (currentSelectedFile == null || currentDetectedType != Plus4ContentType.BasicProgram)
                return;

            string? yapePath = EnsureYapePathConfigured();
            if (yapePath == null) return;

            try
            {
                string targetPrgPath;

                if (!isInsideVirtualImage)
                {
                    if (string.IsNullOrEmpty(currentSelectedPcFilePath) || !File.Exists(currentSelectedPcFilePath))
                        return;

                    targetPrgPath = currentSelectedPcFilePath;
                }
                else
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "Plus4Commander");
                    Directory.CreateDirectory(tempDir);

                    string safeName = currentSelectedFile.Name;
                    foreach (char invalidChar in Path.GetInvalidFileNameChars())
                    {
                        safeName = safeName.Replace(invalidChar, '_');
                    }
                    if (string.IsNullOrWhiteSpace(safeName)) safeName = "quickrun";

                    targetPrgPath = Path.Combine(tempDir, $"{safeName}.prg");
                    File.WriteAllBytes(targetPrgPath, currentSelectedFile.RawData);
                }

                CloseRunningYapeInstances();

                var psi = new ProcessStartInfo
                {
                    FileName = yapePath,
                    Arguments = $"\"{targetPrgPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(yapePath) ?? string.Empty,
                    UseShellExecute = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch YAPE Quick Run:\r\n{ex.Message}", "YAPE Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetAddressBarText(string text, Color backColor, Color foreColor)
        {
            suppressAddressTextChanged = true;
            toolCurrentLocation.BackColor = backColor;
            toolCurrentLocation.ForeColor = foreColor;
            toolCurrentLocation.Text = text;
            suppressAddressTextChanged = false;
        }

        private void RestoreAddressBarText()
        {
            if (isInsideVirtualImage && !string.IsNullOrEmpty(activeImageFilePath))
            {
                SetAddressBarText(
                    activeImageFilePath,
                    Color.FromArgb(238, 240, 255),
                    Color.FromArgb(62, 49, 162)
                );
            }
            else
            {
                SetAddressBarText(
                    currentPcPath,
                    Color.White,
                    Color.Black
                );
            }
        }

        private void TryNavigateFromAddressBar(string rawInput, bool isPasteOrAuto)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return;

            string candidate = rawInput.Trim().Trim('"');

            if (File.Exists(candidate))
            {
                string ext = Path.GetExtension(candidate).ToUpperInvariant();
                if (ext == ".D64" || ext == ".T64")
                {
                    string parentDir = Path.GetDirectoryName(candidate) ?? (Path.GetPathRoot(candidate) ?? @"C:\");
                    string fileName = Path.GetFileName(candidate);

                    currentPcPath = parentDir;
                    SaveLastPathToRegistry(currentPcPath);
                    string root = Path.GetPathRoot(currentPcPath) ?? @"C:\";
                    driveLastPaths[root] = currentPcPath;

                    suppressDriveChangeEvent = true;
                    for (int i = 0; i < toolComboDrives.Items.Count; i++)
                    {
                        if (toolComboDrives.Items[i]?.ToString()?.Equals(root, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            toolComboDrives.SelectedIndex = i;
                            break;
                        }
                    }
                    suppressDriveChangeEvent = false;

                    lastSelectedPcEntry = fileName;
                    activeImageFilePath = candidate;
                    OpenVirtualImage(activeImageFilePath);
                    listBoxFiles.Focus();
                    return;
                }
            }

            if (Directory.Exists(candidate))
            {
                if (!isPasteOrAuto || !candidate.Equals(currentPcPath, StringComparison.OrdinalIgnoreCase))
                {
                    LoadPcDirectory(candidate);
                    listBoxFiles.Focus();
                }
            }
        }

        private void ShowAboutDialog()
        {
            using (AboutForm aboutForm = new AboutForm(this.Icon))
            {
                aboutForm.ShowDialog(this);
            }

            listBoxFiles.Focus();
        }

        private void ExportSelectedVirtualFile()
        {
            if (!isInsideVirtualImage) return;

            int index = listBoxFiles.SelectedIndex;
            if (index <= 0 || (index - 1) >= currentFileList.Count) return;

            DiskFileItem selectedFile = currentFileList[index - 1];

            string safeName = selectedFile.Name;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "exported_file";

            string cleanType = selectedFile.Type.Replace("*", "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(cleanType)) cleanType = "prg";

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Export file from disk/tape image to PC";
                sfd.InitialDirectory = currentPcPath;
                sfd.FileName = $"{safeName}.{cleanType}";
                sfd.Filter = $"Commodore {cleanType.ToUpperInvariant()} File (*.{cleanType})|*.{cleanType}|All Files (*.*)|*.*";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, selectedFile.RawData);
                        MessageBox.Show(
                            $"Successfully exported '{selectedFile.Name}' ({selectedFile.SizeInBytes} bytes) to:\r\n{sfd.FileName}",
                            "Export Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export file:\r\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            listBoxFiles.Focus();
        }

        private void StepViewMode(int direction)
        {
            var viewButtons = new (Button Btn, string Mode)[]
            {
                (btnViewAuto, "AUTO"),
                (btnViewHex, "HEX"),
                (btnViewBasic, "BASIC"),
                (btnViewDisasm, "DISASM"),
                (btnViewSeq, "SEQ"),
                (btnViewImage, "IMAGE"),
                (btnViewFont, "FONT")
            };

            var orderedModes = viewButtons
                .OrderBy(x => x.Btn.Left)
                .Select(x => x.Mode)
                .ToList();

            int currentIndex = orderedModes.IndexOf(forcedViewMode);
            if (currentIndex < 0) currentIndex = 0;

            int nextIndex = (currentIndex + direction + orderedModes.Count) % orderedModes.Count;
            ChangeManualView(orderedModes[nextIndex]);
        }

        private void ListBoxFiles_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= listBoxFiles.Items.Count) return;

            string rawText = listBoxFiles.Items[e.Index]?.ToString() ?? string.Empty;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            string leftText = rawText;
            string rightText = string.Empty;
            int pipeIdx = rawText.IndexOf('|');
            if (pipeIdx >= 0)
            {
                leftText = rawText.Substring(0, pipeIdx);
                rightText = rawText.Substring(pipeIdx + 1);
            }

            ListItemKind kind;

            if (leftText == "[..]")
            {
                kind = ListItemKind.ParentDirectory;
            }
            else if (leftText.StartsWith("[") && leftText.EndsWith("]"))
            {
                kind = ListItemKind.Directory;
            }
            else if (isInsideVirtualImage)
            {
                string trimmed = leftText.TrimEnd();
                if (trimmed.EndsWith(" PRG") || trimmed.EndsWith(" PRG*"))
                    kind = ListItemKind.PrgFile;
                else if (trimmed.EndsWith(" SEQ") || trimmed.EndsWith(" SEQ*"))
                    kind = ListItemKind.SeqFile;
                else if (trimmed.EndsWith(" USR") || trimmed.EndsWith(" USR*"))
                    kind = ListItemKind.UsrFile;
                else if (trimmed.EndsWith(" REL") || trimmed.EndsWith(" REL*"))
                    kind = ListItemKind.RelFile;
                else
                    kind = ListItemKind.OtherFile;
            }
            else
            {
                string ext = Path.GetExtension(leftText).ToUpperInvariant();
                if (ext == ".D64")
                    kind = ListItemKind.D64Image;
                else if (ext == ".T64")
                    kind = ListItemKind.T64Image;
                else if (ext == ".PRG")
                    kind = ListItemKind.PrgFile;
                else if (ext == ".SEQ")
                    kind = ListItemKind.SeqFile;
                else if (ext == ".USR")
                    kind = ListItemKind.UsrFile;
                else if (ext == ".REL")
                    kind = ListItemKind.RelFile;
                else
                    kind = ListItemKind.OtherFile;
            }

            Color backColor;
            Color textColor;
            FontStyle fontStyle;
            Font baseFont = e.Font ?? listBoxFiles.Font;
            FontFamily activeFamily = baseFont.FontFamily;
            float activeSize = baseFont.Size;

            if (isInsideVirtualImage)
            {
                fontStyle = FontStyle.Regular;

                if (cbmListFontFamily != null)
                {
                    activeFamily = cbmListFontFamily;
                    activeSize = 7.5F;
                }

                if (isSelected)
                {
                    backColor = SystemColors.Highlight;
                    textColor = SystemColors.HighlightText;
                }
                else
                {
                    backColor = D64BackColor;
                    textColor = leftText.EndsWith(" PRG") || leftText.EndsWith(" PRG*")
                        ? Color.Cyan
                        : D64ForeColor;
                }
            }
            else
            {
                fontStyle = (kind == ListItemKind.Directory || kind == ListItemKind.ParentDirectory)
                    ? FontStyle.Bold
                    : FontStyle.Regular;

                if (isSelected)
                {
                    backColor = SystemColors.Highlight;
                    textColor = SystemColors.HighlightText;
                }
                else
                {
                    backColor = Color.White;
                    textColor = kind switch
                    {
                        ListItemKind.ParentDirectory or ListItemKind.Directory => Color.Black,
                        ListItemKind.D64Image or ListItemKind.T64Image => Color.Indigo,
                        ListItemKind.PrgFile or ListItemKind.SeqFile or ListItemKind.UsrFile or ListItemKind.RelFile => Color.DarkBlue,
                        _ => Color.DimGray
                    };
                }
            }

            using (SolidBrush bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            int iconSize = Math.Min(16, e.Bounds.Height - 2);
            int iconX = e.Bounds.Left + 4;
            int iconY = e.Bounds.Top + (e.Bounds.Height - iconSize) / 2;
            DrawFileIcon(e.Graphics, kind, new Rectangle(iconX, iconY, iconSize, iconSize));

            int textLeft = iconX + iconSize + 6;
            using (Font drawFont = new Font(activeFamily, activeSize, fontStyle))
            {
                int rightColWidth = string.IsNullOrEmpty(rightText)
                    ? 0
                    : TextRenderer.MeasureText(e.Graphics, rightText, drawFont).Width + 8;

                Rectangle leftRect = new Rectangle(
                    textLeft,
                    e.Bounds.Top,
                    Math.Max(10, e.Bounds.Width - textLeft - rightColWidth),
                    e.Bounds.Height
                );

                TextRenderer.DrawText(
                    e.Graphics,
                    leftText,
                    drawFont,
                    leftRect,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
                );

                if (!string.IsNullOrEmpty(rightText))
                {
                    Rectangle rightRect = new Rectangle(
                        e.Bounds.Right - rightColWidth - 4,
                        e.Bounds.Top,
                        rightColWidth,
                        e.Bounds.Height
                    );

                    TextRenderer.DrawText(
                        e.Graphics,
                        rightText,
                        drawFont,
                        rightRect,
                        textColor,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
                    );
                }
            }

            e.DrawFocusRectangle();
        }

        private void DrawFileIcon(Graphics g, ListItemKind kind, Rectangle rect)
        {
            Bitmap icon = kind switch
            {
                ListItemKind.ParentDirectory => icoDirParent,
                ListItemKind.Directory => icoDir,
                ListItemKind.D64Image => icoD64,
                ListItemKind.T64Image => icoT64,
                ListItemKind.PrgFile => icoPRG,
                ListItemKind.SeqFile => icoSEQ,
                ListItemKind.UsrFile => icoUSR,
                ListItemKind.RelFile => icoREL,
                _ => icoOther
            };

            g.DrawImage(icon, rect);
        }

        private void UpdateActiveViewButtonHighlight()
        {
            var modeButtons = new Dictionary<string, Button>
            {
                { "AUTO", btnViewAuto },
                { "BASIC", btnViewBasic },
                { "FONT", btnViewFont },
                { "IMAGE", btnViewImage },
                { "HEX", btnViewHex },
                { "DISASM", btnViewDisasm },
                { "SEQ", btnViewSeq }
            };

            foreach (var kvp in modeButtons)
            {
                bool isActive = kvp.Key == forcedViewMode;
                kvp.Value.BackColor = isActive ? D64BackColor : SystemColors.Control;
                kvp.Value.ForeColor = isActive ? Color.White : Color.Black;
                kvp.Value.Font = new Font(kvp.Value.Font, isActive ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private void RefreshDriveList()
        {
            suppressDriveChangeEvent = true;
            toolComboDrives.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady) toolComboDrives.Items.Add(drive.Name);
            }
            if (toolComboDrives.Items.Count > 0) toolComboDrives.SelectedIndex = 0;
            suppressDriveChangeEvent = false;
        }

        private void SetDefaultPreviewFont()
        {
            if (textBoxInfo.Font.FontFamily.Name != "Consolas")
            {
                textBoxInfo.Font = new Font("Consolas", 10F, FontStyle.Regular);
            }
        }

        private void ClearPreviewPanel(bool isPcMode = false)
        {
            SetDefaultPreviewFont();
            panelLoadMoreBar.Visible = false;
            currentSelectedFile = null;
            currentDetectedType = Plus4ContentType.UnknownBinary;
            currentSelectedPcFilePath = string.Empty;

            if (isPcMode)
            {
                textBoxFileInfo.Text = " PC Directory Browser";
                textBoxInfo.Text = " Preview is only shown for Commodore files (.PRG, .SEQ, .USR, .REL) or inside .D64 / .T64 images.";
            }
            else
            {
                textBoxFileInfo.Text = " File Info: (None selected)";
                textBoxInfo.Text = " Select a file from the list on the left.";
            }
            pbView.Image = null;
            pbView.Visible = false;
            textBoxInfo.Visible = true;
            toolBtnExportFile.Enabled = false;
            toolBtnYapeMountRun.Enabled = false;
            toolBtnYapeQRun.Enabled = false;
        }

        private void UpdatePcHeaderAndFooter(string root, int dirCount, int fileCount)
        {
            lblDiskHeader.BackColor = Color.FromArgb(40, 40, 52);
            lblDiskHeader.ForeColor = Color.White;
            lblDiskHeader.Font = new Font("Consolas", 9.5F, FontStyle.Bold);

            lblDiskFooter.BackColor = Color.FromArgb(40, 40, 52);
            lblDiskFooter.ForeColor = Color.LightGray;
            lblDiskFooter.Font = new Font("Consolas", 9F, FontStyle.Bold);

            try
            {
                DriveInfo di = new DriveInfo(root);
                if (di.IsReady)
                {
                    string volLabel = string.IsNullOrWhiteSpace(di.VolumeLabel) ? "Local Disk" : di.VolumeLabel;
                    lblDiskHeader.Text = $" {di.Name} [{volLabel}] ({di.DriveFormat})";
                    lblDiskFooter.Text = $" {FormatPcFileSize(di.AvailableFreeSpace)} / {FormatPcFileSize(di.TotalSize)} free | {dirCount} dirs, {fileCount} files";
                    return;
                }
            }
            catch
            {
            }

            lblDiskHeader.Text = $" {root}";
            lblDiskFooter.Text = $" {dirCount} dirs, {fileCount} files";
        }

        private void UpdateVirtualImageHeaderAndFooter(DiskHeaderInfo header, int fileCount)
        {
            lblDiskHeader.BackColor = D64HeaderBackColor;
            lblDiskHeader.ForeColor = Color.White;

            lblDiskFooter.BackColor = D64HeaderBackColor;
            lblDiskFooter.ForeColor = Color.Cyan;

            if (cbmListFontFamily != null)
            {
                lblDiskHeader.Font = new Font(cbmListFontFamily, 7.5F, FontStyle.Regular);
                lblDiskFooter.Font = new Font(cbmListFontFamily, 7.5F, FontStyle.Regular);
            }
            else
            {
                lblDiskHeader.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
                lblDiskFooter.Font = new Font("Consolas", 9F, FontStyle.Bold);
            }

            string formattedName = header.RawDiskNameBytes.Length > 0
                ? FormatPetsciiForCbmFont(header.RawDiskNameBytes, 16)
                : header.DiskName.PadRight(16);

            if (header.IsT64)
            {
                int totalUsedBlocks = currentFileList.Sum(f => f.BlockSize);
                lblDiskHeader.Text = $" 0 \"{formattedName}\"";
                lblDiskFooter.Text = $" {totalUsedBlocks} BLOCKS USED | {fileCount} FILES";
            }
            else
            {
                lblDiskHeader.Text = $" 0 \"{formattedName}\" {header.DiskId} {header.DosType}";
                lblDiskFooter.Text = $" {header.FreeBlocks} / {header.TotalBlocks} BLOCKS FREE.";
            }
        }

        private void LoadPcDirectory(string path, string? itemToSelect = null)
        {
            try
            {
                isInsideVirtualImage = false;
                listBoxFiles.BackColor = Color.White;
                listBoxFiles.ForeColor = Color.Black;

                currentPcPath = path;
                SaveLastPathToRegistry(currentPcPath);

                string root = Path.GetPathRoot(path) ?? "C:\\";
                driveLastPaths[root] = currentPcPath;

                SetAddressBarText(
                    currentPcPath,
                    Color.White,
                    Color.Black
                );

                currentFileList.Clear();
                listBoxFiles.Items.Clear();
                ClearPreviewPanel(isPcMode: true);

                suppressDriveChangeEvent = true;
                for (int i = 0; i < toolComboDrives.Items.Count; i++)
                {
                    if (toolComboDrives.Items[i]?.ToString()?.Equals(root, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        toolComboDrives.SelectedIndex = i;
                        break;
                    }
                }
                suppressDriveChangeEvent = false;

                listBoxFiles.Items.Add("[..]");

                DirectoryInfo dir = new DirectoryInfo(path);
                var directories = dir.GetDirectories();
                var files = dir.GetFiles();

                foreach (var d in directories)
                {
                    listBoxFiles.Items.Add($"[{d.Name}]");
                }

                foreach (var f in files)
                {
                    listBoxFiles.Items.Add($"{f.Name}|{FormatPcFileSize(f.Length)}");
                }

                UpdatePcHeaderAndFooter(root, directories.Length, files.Length);

                int targetIndex = -1;

                if (!string.IsNullOrEmpty(itemToSelect))
                {
                    for (int i = 0; i < listBoxFiles.Items.Count; i++)
                    {
                        string entry = listBoxFiles.Items[i]?.ToString() ?? string.Empty;
                        string entryName = entry.Contains('|') ? entry.Split('|')[0].Trim() : entry;

                        if (entryName.Equals(itemToSelect, StringComparison.OrdinalIgnoreCase))
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }

                if (targetIndex < 0)
                {
                    targetIndex = listBoxFiles.Items.Count > 1 ? 1 : 0;
                }

                if (listBoxFiles.Items.Count > 0)
                {
                    listBoxFiles.SelectedIndex = targetIndex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open directory:\r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void NavigateUpOneLevel()
        {
            if (isInsideVirtualImage)
            {
                string imageFileName = Path.GetFileName(activeImageFilePath);
                LoadPcDirectory(currentPcPath, imageFileName);
            }
            else
            {
                DirectoryInfo currentDir = new DirectoryInfo(currentPcPath);
                DirectoryInfo? parent = currentDir.Parent;
                if (parent != null)
                {
                    LoadPcDirectory(parent.FullName, $"[{currentDir.Name}]");
                }
                else
                {
                    LoadPcDirectory(Path.GetPathRoot(currentPcPath) ?? @"C:\");
                }
            }
        }

        private void ActivateSelectedListItem()
        {
            int index = listBoxFiles.SelectedIndex;
            if (index < 0) return;

            string selectedText = listBoxFiles.SelectedItem?.ToString() ?? string.Empty;

            if (!isInsideVirtualImage)
            {
                if (index == 0)
                {
                    NavigateUpOneLevel();
                }
                else if (selectedText.StartsWith("[") && selectedText.EndsWith("]"))
                {
                    string dirName = selectedText.Substring(1, selectedText.Length - 2);
                    LoadPcDirectory(Path.Combine(currentPcPath, dirName));
                }
                else if (selectedText.Contains("|"))
                {
                    string fileName = selectedText.Split('|')[0].Trim();
                    string ext = Path.GetExtension(fileName).ToUpperInvariant();
                    if (ext == ".D64" || ext == ".T64")
                    {
                        lastSelectedPcEntry = fileName;
                        activeImageFilePath = Path.Combine(currentPcPath, fileName);
                        OpenVirtualImage(activeImageFilePath);
                    }
                }
            }
            else if (index == 0)
            {
                NavigateUpOneLevel();
            }
        }

        private void OpenVirtualImage(string imagePath, string? selectCbmName = null)
        {
            try
            {
                string ext = Path.GetExtension(imagePath).ToUpperInvariant();
                bool isT64 = (ext == ".T64");

                DiskHeaderInfo headerInfo = isT64
                    ? Plus4Parser.ReadT64Header(imagePath)
                    : Plus4Parser.ReadD64Header(imagePath);

                currentFileList = isT64
                    ? Plus4Parser.ReadT64File(imagePath)
                    : Plus4Parser.ReadD64File(imagePath);

                isInsideVirtualImage = true;

                listBoxFiles.BackColor = D64BackColor;
                listBoxFiles.ForeColor = D64ForeColor;

                listBoxFiles.Items.Clear();
                ClearPreviewPanel(isPcMode: false);

                string imageName = Path.GetFileName(imagePath);
                SetAddressBarText(
                    imagePath,
                    Color.FromArgb(238, 240, 255),
                    Color.FromArgb(62, 49, 162)
                );

                UpdateVirtualImageHeaderAndFooter(headerInfo, currentFileList.Count);

                listBoxFiles.Items.Add("[..]");
                int targetIndex = -1;
                for (int i = 0; i < currentFileList.Count; i++)
                {
                    var file = currentFileList[i];
                    string paddedName = FormatPetsciiForCbmFont(file.RawNameBytes, 17);
                    listBoxFiles.Items.Add($"{paddedName}  {file.Type}|{file.BlockSize} BLK");

                    if (!string.IsNullOrEmpty(selectCbmName) &&
                        file.Name.Equals(selectCbmName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetIndex = i + 1;
                    }
                }

                if (targetIndex > 0)
                {
                    listBoxFiles.SelectedIndex = targetIndex;
                }
                else if (listBoxFiles.Items.Count > 1)
                {
                    listBoxFiles.SelectedIndex = 1;
                }
                else if (listBoxFiles.Items.Count > 0)
                {
                    listBoxFiles.SelectedIndex = 0;
                    textBoxInfo.Text = $"Successfully opened {imageName} (0 files).";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading image file:\r\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListBoxFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            fontByteShift = 0;
            showFullPreviewContent = false;
            RefreshFileContentView();
        }

        private void ChangeManualView(string mode)
        {
            forcedViewMode = mode;
            showFullPreviewContent = false;
            UpdateActiveViewButtonHighlight();
            RefreshFileContentView();
        }

        private void RefreshFileContentView()
        {
            int index = listBoxFiles.SelectedIndex;
            DiskFileItem? file = null;
            panelLoadMoreBar.Visible = false;
            currentSelectedFile = null;
            currentDetectedType = Plus4ContentType.UnknownBinary;
            currentSelectedPcFilePath = string.Empty;

            if (!isInsideVirtualImage)
            {
                toolBtnExportFile.Enabled = false;
                toolBtnYapeMountRun.Enabled = false;
                toolBtnYapeQRun.Enabled = false;

                if (index <= 0 || listBoxFiles.SelectedItem == null)
                {
                    ClearPreviewPanel(isPcMode: true);
                    return;
                }

                string selectedLine = listBoxFiles.SelectedItem.ToString() ?? string.Empty;
                int pipeIdx = selectedLine.IndexOf('|');
                if (pipeIdx <= 0)
                {
                    ClearPreviewPanel(isPcMode: true);
                    return;
                }

                string fileName = selectedLine.Substring(0, pipeIdx).Trim();
                string fullPath = Path.Combine(currentPcPath, fileName);
                string ext = Path.GetExtension(fullPath).ToUpperInvariant();

                if ((ext == ".D64" || ext == ".T64") && File.Exists(fullPath))
                {
                    currentSelectedPcFilePath = fullPath;
                    toolBtnYapeMountRun.Enabled = true;
                    ShowVirtualImagePreview(fullPath, ext == ".T64");
                    return;
                }

                bool isSupportedCbmFile = ext == ".PRG" || ext == ".SEQ" || ext == ".USR" || ext == ".REL";
                if (!isSupportedCbmFile || !File.Exists(fullPath))
                {
                    ClearPreviewPanel(isPcMode: true);
                    return;
                }

                file = Plus4Parser.LoadStandalonePcFile(fullPath);
                currentSelectedPcFilePath = fullPath;
            }
            else
            {
                if (index <= 0 || (index - 1) >= currentFileList.Count)
                {
                    ClearPreviewPanel(isPcMode: false);
                    return;
                }

                file = currentFileList[index - 1];
                toolBtnExportFile.Enabled = true;
                toolBtnYapeMountRun.Enabled = true;
            }

            var detectedType = FileClassifier.Identify(file);
            currentSelectedFile = file;
            currentDetectedType = detectedType;
            toolBtnYapeQRun.Enabled = (detectedType == Plus4ContentType.BasicProgram);

            StringBuilder infoSb = new StringBuilder();
            infoSb.AppendLine($" Filename:    {file.Name} ({file.Type})");
            infoSb.AppendLine($" Size:        {file.SizeInBytes} bytes ({file.BlockSize} blocks)");
            infoSb.AppendLine($" Start Addr:  ${file.StartAddress:X4} ({file.StartAddress})");
            infoSb.AppendLine($" End Addr:    ${file.EndAddress:X4} ({file.EndAddress})");
            if (file.RecordLength > 0)
            {
                infoSb.AppendLine($" Record Len:  {file.RecordLength} bytes");
            }
            infoSb.AppendLine($" Detected:    {GetContentTypeDescription(detectedType)} [Mode: {forcedViewMode}]");
            textBoxFileInfo.Text = infoSb.ToString();

            pbView.Visible = false;
            textBoxInfo.Visible = true;

            string activeView = forcedViewMode;
            if (activeView == "AUTO")
            {
                if (detectedType == Plus4ContentType.BasicProgram) activeView = "BASIC";
                else if (detectedType == Plus4ContentType.FontCharset) activeView = "FONT";
                else if (detectedType == Plus4ContentType.ScreenOrBitmap) activeView = "IMAGE";
                else if (detectedType == Plus4ContentType.SeqDatabaseOrText) activeView = "SEQ";
                else if (detectedType == Plus4ContentType.MachineCode) activeView = "DISASM";
                else activeView = "HEX";
            }

            StringBuilder contentSb = new StringBuilder();
            switch (activeView)
            {
                case "BASIC":
                    if (cbmListFontFamily != null)
                    {
                        textBoxInfo.Font = new Font(cbmListFontFamily, 8.5F, FontStyle.Regular);
                    }
                    else
                    {
                        SetDefaultPreviewFont();
                    }

                    contentSb.AppendLine("--- PLUS/4 BASIC V3.5 SOURCE CODE ---");
                    contentSb.AppendLine();
                    contentSb.AppendLine(Plus4Parser.DetokenizeBasic(file.RawData, useCbmPua: cbmListFontFamily != null));
                    textBoxInfo.Text = contentSb.ToString();
                    break;

                case "FONT":
                    SetDefaultPreviewFont();
                    pbView.Image = Plus4Parser.RenderFontBitmap(file.RawData, fontByteShift);
                    pbView.Visible = true;
                    textBoxInfo.Visible = false;
                    break;

                case "IMAGE":
                    SetDefaultPreviewFont();
                    pbView.Image = Plus4Parser.RenderScreenBitmap(file.RawData, isMulticolorMode, imagePaletteMode);
                    pbView.Visible = true;
                    textBoxInfo.Visible = false;
                    break;

                case "DISASM":
                    SetDefaultPreviewFont();
                    int maxInstr = showFullPreviewContent ? int.MaxValue : 400;
                    string disasmText = Plus4Parser.Disassemble6502(file.RawData, maxInstr);

                    contentSb.AppendLine("--- 6502 / 7501 ASSEMBLY DISASSEMBLY ---");
                    contentSb.AppendLine();
                    contentSb.AppendLine(disasmText);
                    textBoxInfo.Text = contentSb.ToString();

                    if (!showFullPreviewContent && disasmText.Contains("more bytes hidden"))
                    {
                        lblTruncatedHint.Text = "Truncated at 400 instructions — click to load full disassembly";
                        btnShowAllContent.Text = "Show Full Disasm";
                        panelLoadMoreBar.Visible = true;
                    }
                    break;

                case "HEX":
                    SetDefaultPreviewFont();
                    int maxHexBytes = showFullPreviewContent ? file.RawData.Length : 1024;
                    contentSb.AppendLine("--- MACHINE CODE / BINARY HEX DUMP ---");
                    contentSb.AppendLine();
                    contentSb.AppendLine(GetHexDump(file.RawData, maxHexBytes));
                    textBoxInfo.Text = contentSb.ToString();

                    if (!showFullPreviewContent && file.RawData.Length > 1024)
                    {
                        int hiddenBytes = file.RawData.Length - 1024;
                        lblTruncatedHint.Text = $"{hiddenBytes} more bytes hidden — click to show entire file";
                        btnShowAllContent.Text = $"Show All ({file.RawData.Length} B)";
                        panelLoadMoreBar.Visible = true;
                    }
                    break;

                case "SEQ":
                default:
                    SetDefaultPreviewFont();
                    if (file.RecordLength > 0)
                    {
                        contentSb.AppendLine($"--- RELATIVE FILE (RECORD LENGTH: {file.RecordLength} BYTES) ---");
                        contentSb.AppendLine();
                        contentSb.AppendLine(GetRelText(file.RawData, file.RecordLength));
                    }
                    else
                    {
                        contentSb.AppendLine("--- SEQUENTIAL / TEXT CONTENT ---");
                        contentSb.AppendLine();
                        contentSb.AppendLine(GetSeqText(file.RawData));
                    }
                    textBoxInfo.Text = contentSb.ToString();
                    break;
            }
            listBoxFiles.Focus();
        }

        private void ShowVirtualImagePreview(string imagePath, bool isT64)
        {
            try
            {
                DiskHeaderInfo header = isT64
                    ? Plus4Parser.ReadT64Header(imagePath)
                    : Plus4Parser.ReadD64Header(imagePath);

                List<DiskFileItem> files = isT64
                    ? Plus4Parser.ReadT64File(imagePath)
                    : Plus4Parser.ReadD64File(imagePath);

                int usedBlocks = files.Sum(f => f.BlockSize);
                long fileBytes = new FileInfo(imagePath).Length;

                StringBuilder infoSb = new StringBuilder();
                infoSb.AppendLine($" Image File:  {Path.GetFileName(imagePath)} ({FormatPcFileSize(fileBytes)})");
                if (isT64)
                {
                    infoSb.AppendLine($" Tape Title:  \"{header.DiskName}\"");
                    infoSb.AppendLine($" Files:       {files.Count} files");
                    infoSb.AppendLine($" Blocks:      {usedBlocks} blocks used");
                    infoSb.AppendLine($" Hint:        Press [Enter] to browse inside tape image");
                }
                else
                {
                    infoSb.AppendLine($" Disk Header: 0 \"{header.DiskName}\" {header.DiskId} {header.DosType}");
                    infoSb.AppendLine($" Files:       {files.Count} files");
                    infoSb.AppendLine($" Blocks:      {header.FreeBlocks} free / {header.TotalBlocks - header.FreeBlocks} used ({header.TotalBlocks} total)");
                    infoSb.AppendLine($" Hint:        Press [Enter] to browse inside disk image");
                }
                textBoxFileInfo.Text = infoSb.ToString();

                pbView.Visible = false;
                textBoxInfo.Visible = true;

                if (cbmListFontFamily != null)
                {
                    textBoxInfo.Font = new Font(cbmListFontFamily, 8.5F, FontStyle.Regular);
                }
                else
                {
                    SetDefaultPreviewFont();
                }

                string formattedDiskName = header.RawDiskNameBytes.Length > 0
                    ? FormatPetsciiForCbmFont(header.RawDiskNameBytes, 16)
                    : header.DiskName.PadRight(16);

                StringBuilder dirSb = new StringBuilder();
                if (isT64)
                {
                    dirSb.AppendLine($"0 \"{formattedDiskName}\" T64");
                }
                else
                {
                    dirSb.AppendLine($"0 \"{formattedDiskName}\" {header.DiskId} {header.DosType}");
                }

                foreach (var item in files)
                {
                    string blockCol = item.BlockSize.ToString().PadRight(5);
                    string nameCol = item.RawNameBytes.Length > 0
                        ? FormatPetsciiForCbmFont(item.RawNameBytes, 16)
                        : item.Name.PadRight(16);
                    dirSb.AppendLine($"{blockCol}\"{nameCol}\" {item.Type}");
                }

                if (isT64)
                {
                    dirSb.AppendLine($"{usedBlocks} BLOCKS USED.");
                }
                else
                {
                    dirSb.AppendLine($"{header.FreeBlocks} BLOCKS FREE.");
                }

                textBoxInfo.Text = dirSb.ToString();
            }
            catch (Exception ex)
            {
                textBoxFileInfo.Text = $" Image File:  {Path.GetFileName(imagePath)}";
                SetDefaultPreviewFont();
                textBoxInfo.Text = $"Could not read directory preview:\r\n{ex.Message}";
            }

            listBoxFiles.Focus();
        }

        private string GetContentTypeDescription(Plus4ContentType type)
        {
            return type switch
            {
                Plus4ContentType.BasicProgram => "BASIC V3.5 Program",
                Plus4ContentType.FontCharset => "Custom Charset / Font",
                Plus4ContentType.ScreenOrBitmap => "Plus/4 Screen Memory / Graphics",
                Plus4ContentType.MachineCode => "Machine Code / Binary Program",
                Plus4ContentType.SeqDatabaseOrText => "SEQ / USR / REL Text",
                _ => "Unknown"
            };
        }

        private string GetHexDump(byte[] data, int maxBytes)
        {
            StringBuilder sb = new StringBuilder();
            int limit = Math.Min(data.Length, maxBytes);
            for (int i = 0; i < limit; i += 16)
            {
                sb.Append($"${i:X4}: ");
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < limit) sb.Append($"{data[i + j]:X2} ");
                    else sb.Append("   ");
                }
                sb.Append(" | ");
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < limit)
                    {
                        byte b = data[i + j];
                        char c = (b >= 32 && b <= 126) ? (char)b : '.';
                        sb.Append(c);
                    }
                }
                sb.AppendLine();
            }
            if (data.Length > maxBytes) sb.AppendLine($"\r\n[... {data.Length - maxBytes} more bytes hidden ...]");
            return sb.ToString();
        }

        private string GetSeqText(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in data)
            {
                if (b == 13 || b == 10) sb.AppendLine();
                else if (b >= 32 && b <= 126) sb.Append((char)b);
                else sb.Append($"[{b:X2}]");
            }
            return sb.ToString();
        }

        private string GetRelText(byte[] data, int recordLength)
        {
            StringBuilder sb = new StringBuilder();
            int recordNum = 1;
            for (int offset = 0; offset < data.Length; offset += recordLength)
            {
                sb.Append($"#{recordNum:D4}: ");
                int len = Math.Min(recordLength, data.Length - offset);
                for (int i = 0; i < len; i++)
                {
                    byte b = data[offset + i];
                    if (b == 0x00 || b == 0xFF) continue;
                    if (b == 13 || b == 10) sb.Append(' ');
                    else if (b >= 32 && b <= 126) sb.Append((char)b);
                    else sb.Append($"[{b:X2}]");
                }
                sb.AppendLine();
                recordNum++;
            }
            return sb.ToString();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (toolComboDrives.ComboBox?.DroppedDown == true)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (toolCurrentLocation.Control?.Focused == true)
            {
                if (keyData == Keys.F1)
                {
                    ShowAboutDialog();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == Keys.F1)
            {
                ShowAboutDialog();
                return true;
            }

            if (keyData == Keys.F2)
            {
                if (listBoxFiles.Focused && (isInsideVirtualImage ? currentSelectedFile != null : IsSelectedPcFile(out _)))
                {
                    RenameSelectedFile();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == (Keys.Control | Keys.C))
            {
                if (pbView.Focused && pbView.Visible && pbView.Image != null)
                {
                    CopyPreviewImageToClipboard(1);
                    return true;
                }
                if (listBoxFiles.Focused && currentSelectedFile != null)
                {
                    CopySelectedCbmFileToClipboard();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == (Keys.Control | Keys.V))
            {
                if (listBoxFiles.Focused && Clipboard.ContainsData(CbmClipboardFormat))
                {
                    PasteCbmFileFromClipboard();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == Keys.Delete)
            {
                if (listBoxFiles.Focused && isInsideVirtualImage && currentSelectedFile != null)
                {
                    DeleteSelectedVirtualFile();
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == Keys.Space)
            {
                ChangeManualView("AUTO");
                return true;
            }

            if (keyData == Keys.F5)
            {
                if (toolBtnExportFile.Enabled)
                {
                    ExportSelectedVirtualFile();
                }
                return true;
            }

            if (keyData == Keys.F6)
            {
                if (toolBtnYapeMountRun.Enabled)
                {
                    YapeMountAndRun();
                }
                return true;
            }

            if (keyData == Keys.F7)
            {
                if (toolBtnYapeQRun.Enabled)
                {
                    YapeQuickRun();
                }
                return true;
            }

            if (keyData == Keys.Left)
            {
                StepViewMode(-1);
                return true;
            }

            if (keyData == Keys.Right)
            {
                StepViewMode(1);
                return true;
            }

            if (keyData == Keys.Up || keyData == Keys.Down)
            {
                if (listBoxFiles.Items.Count > 0)
                {
                    int newIndex = listBoxFiles.SelectedIndex;
                    if (keyData == Keys.Up)
                    {
                        newIndex = (newIndex > 0) ? newIndex - 1 : 0;
                    }
                    else
                    {
                        newIndex = (newIndex < listBoxFiles.Items.Count - 1) ? newIndex + 1 : listBoxFiles.Items.Count - 1;
                    }

                    listBoxFiles.SelectedIndex = newIndex;
                    listBoxFiles.Focus();
                }
                return true;
            }

            if (keyData == Keys.Enter)
            {
                ActivateSelectedListItem();
                listBoxFiles.Focus();
                return true;
            }

            if (keyData == Keys.Back)
            {
                NavigateUpOneLevel();
                listBoxFiles.Focus();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}