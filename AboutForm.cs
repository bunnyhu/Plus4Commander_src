using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Reflection;

namespace Plus4Commander
{
    public partial class AboutForm : Form
    {
        private const string RegistrySubKey = @"Software\Plus4Commander";
        private const string RegistryYapePathValue = "YapePath";

        public AboutForm(Icon? appIcon = null)
        {
            InitializeComponent();

            if (appIcon != null)
            {
                this.Icon = appIcon;
//                pbLogo.Image = appIcon.ToBitmap();
            }

            textBox1.Text = LoadYapePathFromRegistry();

            btnYapeExe.Click += BtnYapeExe_Click;
            linkLabel1.LinkClicked += LinkLabel1_LinkClicked;
            this.FormClosing += AboutForm_FormClosing;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
            this.lblVersion.Text = $"Version: {versionText}";

        }

        private void BtnYapeExe_Click(object? sender, EventArgs e)
        {
            openFileDialog1.Title = "Select YAPE Emulator Executable";
            openFileDialog1.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            openFileDialog1.FileName = "yape.exe";

            if (!string.IsNullOrWhiteSpace(textBox1.Text) && File.Exists(textBox1.Text))
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBox1.Text);
                openFileDialog1.FileName = Path.GetFileName(textBox1.Text);
            }

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                SaveYapePathToRegistry(textBox1.Text);
            }
        }

        private void AboutForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveYapePathToRegistry(textBox1.Text.Trim());
        }

        private void LinkLabel1_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = linkLabel1.Text,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        public static string LoadYapePathFromRegistry()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistrySubKey))
                {
                    return key?.GetValue(RegistryYapePathValue) as string ?? string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void SaveYapePathToRegistry(string path)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistrySubKey))
                {
                    key.SetValue(RegistryYapePathValue, path, RegistryValueKind.String);
                }
            }
            catch
            {
            }
        }

        private void lblDescription_Click(object sender, EventArgs e)
        {
        }
    }
}