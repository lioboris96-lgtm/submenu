using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MalumMenuInjector
{
    public partial class MainForm : Form
    {
        private Button btnInject;
        private Button btnDownload;
        private Button btnBrowse;
        private TextBox txtGamePath;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Label lblVersion;
        private static readonly HttpClient httpClient = new();
        private const string REPO_OWNER = "lioboris96-lgtm";
        private const string REPO_NAME = "submenu";
        private const string LATEST_RELEASE_URL = $"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest";
        private const string DLL_NAME = "MalumMenu.dll";
        private const string BEPINEX_DLL = "BepInEx.dll";
        private const string DOORSTOP_DLL = "winhttp.dll";
        private const string DOORSTOP_CONFIG = "doorstop_config.ini";

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            CheckForUpdates();
        }

        private void InitializeComponent()
        {
            this.Text = "MalumMenu Injector v3.2.0";
            this.Size = new System.Drawing.Size(600, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Game path section
            var lblGamePath = new Label
            {
                Text = "Among Us Game Path:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(150, 23)
            };

            txtGamePath = new TextBox
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(400, 23),
                Text = GetDefaultGamePath()
            };

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(430, 48),
                Size = new System.Drawing.Size(75, 25)
            };
            btnBrowse.Click += BtnBrowse_Click;

            // Version label
            lblVersion = new Label
            {
                Text = "Checking for updates...",
                Location = new System.Drawing.Point(20, 90),
                Size = new System.Drawing.Size(400, 23),
                ForeColor = System.Drawing.Color.Gray
            };

            // Buttons
            btnDownload = new Button
            {
                Text = "Download & Install",
                Location = new System.Drawing.Point(20, 130),
                Size = new System.Drawing.Size(150, 35),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold)
            };
            btnDownload.Click += BtnDownload_Click;

            btnInject = new Button
            {
                Text = "Inject Only",
                Location = new System.Drawing.Point(190, 130),
                Size = new System.Drawing.Size(150, 35),
                BackColor = System.Drawing.Color.FromArgb(215, 0, 0),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold)
            };
            btnInject.Click += BtnInject_Click;

            // Status
            lblStatus = new Label
            {
                Text = "Ready to inject MalumMenu",
                Location = new System.Drawing.Point(20, 180),
                Size = new System.Drawing.Size(550, 80),
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(20, 280),
                Size = new System.Drawing.Size(550, 23),
                Style = ProgressBarStyle.Continuous
            };

            // Add controls
            this.Controls.AddRange(new Control[] { lblGamePath, txtGamePath, btnBrowse, lblVersion, btnDownload, btnInject, lblStatus, progressBar });
        }

        private async void CheckForUpdates()
        {
            try
            {
                var response = await httpClient.GetStringAsync(LATEST_RELEASE_URL);
                var releaseInfo = System.Text.Json.JsonDocument.Parse(response);
                var tagName = releaseInfo.RootElement.GetProperty("tag_name").GetString();
                
                this.Invoke((MethodInvoker)delegate
                {
                    lblVersion.Text = $"Latest: {tagName} | Current: v3.2.0";
                    if (tagName != "v3.2.0")
                    {
                        lblVersion.ForeColor = System.Drawing.Color.Orange;
                        lblVersion.Text += " (Update Available!)";
                    }
                    else
                    {
                        lblVersion.ForeColor = System.Drawing.Color.Green;
                    }
                });
            }
            catch
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lblVersion.Text = "Could not check for updates";
                    lblVersion.ForeColor = System.Drawing.Color.Red;
                });
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MalumMenu", "settings.ini");
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("GamePath="))
                        {
                            var path = line.Substring(9);
                            if (Directory.Exists(path))
                            {
                                txtGamePath.Text = path;
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If settings fail, try default path
                txtGamePath.Text = GetDefaultGamePath();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MalumMenu");
                Directory.CreateDirectory(settingsDir);
                var settingsPath = Path.Combine(settingsDir, "settings.ini");
                File.WriteAllText(settingsPath, $"GamePath={txtGamePath.Text}");
            }
            catch
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetDefaultGamePath()
        {
            string[] possiblePaths = {
                // Steam paths
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "Among Us"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "Among Us"),
                
                // Epic Games
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Epic Games", "Among Us"),
                
                // Microsoft Store
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SteamApps", "content", "app", "945360", "AC"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", "945360", "AC", "UserLocalCache"),
                
                // Common user locations
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Among Us"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Among Us")
            };

            foreach (var path in possiblePaths)
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    // Look for Among Us executable to confirm
                    var exePaths = new[] { "Among Us.exe", "Among Us.exe" };
                    foreach (var exePath in exePaths)
                    {
                        if (File.Exists(Path.Combine(path, exePath)))
                        {
                            return path;
                        }
                    }
                }
            }

            return "";
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select Among Us Game Folder";
            folderDialog.ShowNewFolderButton = true;
            
            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtGamePath.Text = folderDialog.SelectedPath;
            }
        }

        private async void BtnDownload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtGamePath.Text))
            {
                MessageBox.Show("Please select a valid Among Us game folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(txtGamePath.Text))
            {
                MessageBox.Show($"Game folder does not exist:\n{txtGamePath.Text}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnDownload.Enabled = false;
            btnInject.Enabled = false;
            progressBar.Value = 0;
            lblStatus.Text = "Downloading MalumMenu...";

            try
            {
                // Get latest release info
                var response = await httpClient.GetStringAsync(LATEST_RELEASE_URL);
                var releaseInfo = System.Text.Json.JsonDocument.Parse(response);
                var downloadUrl = releaseInfo.RootElement.GetProperty("assets").EnumerateArray()
                    .First(a => a.GetProperty("name").GetString().EndsWith("-Steam-Itch.zip"))
                    .GetProperty("browser_download_url").GetString();

                // Download release
                var tempPath = Path.GetTempFileName();
                using (var downloadStream = await httpClient.GetStreamAsync(downloadUrl))
                using (var fileStream = File.Create(tempPath))
                {
                    await downloadStream.CopyToAsync(fileStream);
                }

                progressBar.Value = 30;
                lblStatus.Text = "Extracting files...";

                // Extract to game folder
                await Task.Run(() =>
                {
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(tempPath, txtGamePath.Text, true);
                    }
                    catch
                    {
                        // Try alternative extraction method
                        var extractPath = Path.Combine(Path.GetTempPath(), "extract");
                        System.IO.Compression.ZipFile.ExtractToDirectory(tempPath, extractPath);
                        
                        // Copy files manually
                        CopyDirectory(extractPath, txtGamePath.Text);
                        Directory.Delete(extractPath, true);
                    }
                    
                    File.Delete(tempPath);
                });

                progressBar.Value = 100;
                lblStatus.Text = "MalumMenu v3.2.0 installed successfully!\n\nYou can now launch Among Us with the mod.\n\nPress DELETE in-game to open the menu.";
                lblStatus.ForeColor = System.Drawing.Color.Green;

                SaveSettings();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                btnDownload.Enabled = true;
                btnInject.Enabled = true;
            }
        }

        private void BtnInject_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtGamePath.Text))
            {
                MessageBox.Show("Please select a valid Among Us game folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var gamePath = txtGamePath.Text;
            var bepInExPath = Path.Combine(gamePath, "BepInEx", "plugins", DLL_NAME);
            var doorstopPath = Path.Combine(gamePath, DOORSTOP_DLL);
            var doorstopConfigPath = Path.Combine(gamePath, DOORSTOP_CONFIG);

            if (!File.Exists(bepInExPath))
            {
                MessageBox.Show($"MalumMenu.dll not found at:\n{bepInExPath}\n\nPlease download and install first.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Check if Doorstop is already installed
                if (!File.Exists(doorstopPath))
                {
                    var result = MessageBox.Show(
                        "Doorstop injector not found. Do you want to download and install it?\n\nThis is required for BepInEx mods to work.",
                        "Doorstop Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        DownloadDoorstop(gamePath);
                    }
                    return;
                }

                // Check doorstop config
                if (!File.Exists(doorstopConfigPath))
                {
                    CreateDoorstopConfig(doorstopConfigPath);
                }

                MessageBox.Show(
                    "MalumMenu is ready!\n\nLaunch Among Us to start using the mod.\n\nPress DELETE in-game to open the menu.",
                    "Injection Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Injection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DownloadDoorstop(string gamePath)
        {
            try
            {
                lblStatus.Text = "Downloading Doorstop...";
                progressBar.Value = 0;

                var doorstopUrl = "https://github.com/NeighTools/Doorstop/releases/download/v4.5.0/winhttp.dll";
                var doorstopPath = Path.Combine(gamePath, DOORSTOP_DLL);

                using (var response = await httpClient.GetAsync(doorstopUrl))
                using (var fileStream = File.Create(doorstopPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                CreateDoorstopConfig(Path.Combine(gamePath, DOORSTOP_CONFIG));
                
                lblStatus.Text = "Doorstop installed successfully!";
                lblStatus.ForeColor = System.Drawing.Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install Doorstop: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateDoorstopConfig(string configPath)
        {
            try
            {
                var config = @"[General]
enabled = true
target_assembly = BepInEx\core\BepInEx.Unity.IL2CPP.dll
coreclr_path = dotnet\coreclr.dll
";
                File.WriteAllText(configPath, config);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create doorstop config: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                Directory.CreateDirectory(destSubDir);
                CopyDirectory(dir, destSubDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
        }
    }

    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
