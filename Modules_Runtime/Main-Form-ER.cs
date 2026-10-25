using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modules_Runtime_ER
{
    public partial class Main_Form_ER : Form
    {
        // and here Dll area noob XD
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmd);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private const int SW_HIDE = 0, SW_RESTORE = 9;

       

        private static readonly string[] GUARD_PROCESS =
{
    "CustomCertificationServer",
    "CustomCertification",
    "Custom Certification",
    "2 - CustomCertificationServer",
    "2 - Custom Certification"
};

        private bool IsGuardRunning() =>
            Process.GetProcesses()
                .Any(p => GUARD_PROCESS.Any(name =>
                    p.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase)));



        private readonly string _iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules.ini");

        private const int MAX_LOG_LINES = 500;
        private int _logLineCount = 0;
        private bool _ledBlink = false;
        private PerformanceCounter _cpuCounter;

        private class ModuleInfo
        {
            public string Name;
            public string ProcessName;
            public string DefaultExe;
            public string ExePath;
            public bool AutoRestart;
            public int StartDelay;
            public bool _wasRunning;
            public bool _intentionallyClosed;
        }

        private ModuleInfo[] _modules;

        private Panel[] _ledIndicator;
        private Label[] _lblResources;
        private Button[] _btnToggle;
        private Button[] _btnRestart;
        private Button[] _btnBrowse;
        private Button[] _btnHideShow;
        private CheckBox[] _chkAutoRestart;

        // hna kman SMC & Certification Start/Stop + Browse onlyy
        private Panel[] _ledSimple;
        private Button[] _btnToggleSimple;
        private Button[] _btnBrowseSimple;
        private ModuleInfo[] _modulesSimple;

        private HashSet<string> _hiddenModules = new HashSet<string>();

        private static readonly Color LED_OFF = Color.FromArgb(60, 60, 60);
        private static readonly Color LED_RUNNING = Color.FromArgb(0, 220, 80);
        private static readonly Color LED_BLINK = Color.FromArgb(0, 120, 40);
        private static readonly Color LED_STOPPED = Color.FromArgb(220, 40, 40);
        private static readonly Color LED_UNCONFIGURED = Color.FromArgb(192, 192, 0);
   
        public Main_Form_ER()
        {
            _modules = new ModuleInfo[]
            {
   
    new ModuleInfo { Name="Global Manager",  ProcessName="GlobalManager",   DefaultExe="GlobalManager.exe",   StartDelay=5000  },
    new ModuleInfo { Name="Download Server", ProcessName="DownloadServer",  DefaultExe="DownloadServer.exe",  StartDelay=5000  },
    new ModuleInfo { Name="Gateway Server",  ProcessName="GatewayServer",   DefaultExe="GatewayServer.exe",   StartDelay=5000  },
    new ModuleInfo { Name="Machine Manager", ProcessName="MachineManager",  DefaultExe="MachineManager.exe",  StartDelay=5000  },
    new ModuleInfo { Name="Farm Manager",    ProcessName="FarmManager",     DefaultExe="FarmManager.exe",     StartDelay=5000  },
    new ModuleInfo { Name="Agent Server",    ProcessName="AgentServer",     DefaultExe="AgentServer.exe",     StartDelay=5000  },
    new ModuleInfo { Name="Shard Manager",   ProcessName="SR_ShardManager", DefaultExe="SR_ShardManager.exe", StartDelay=7000 },
    new ModuleInfo { Name="GameServer",      ProcessName="SR_GameServer",   DefaultExe="SR_GameServer.exe",   StartDelay=15000 },
            };

            _modulesSimple = new ModuleInfo[]
            {
    new ModuleInfo { Name="SMC",           ProcessName="smc_independent",           DefaultExe="smc_independent.exe",           StartDelay=1000 },
    new ModuleInfo { Name="Certification", ProcessName="CustomCertificationServer", DefaultExe="CustomCertificationServer.exe", StartDelay=1000 },
            };

            LoadIni();
            InitializeComponent();
            LinkDesignerControls();

            this.FormClosing += Main_Form_ER_FormClosing;

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch { _cpuCounter = null; }

            Log("Welcome to Modules Runtime, Vsro Server Files");
            Log("Follow us on Facebook & Subscribe on YouTube = @MrExamofficial", Color.Green);

        }

        private void LinkDesignerControls()
        {
            _ledIndicator = new Panel[] { led0, led1, led2, led3, led4, led5, led6, led7 };
            _lblResources = new Label[] { lblRes0, lblRes1, lblRes2, lblRes3, lblRes4, lblRes5, lblRes6, lblRes7 };
            _btnToggle = new Button[] { btnToggle0, btnToggle1, btnToggle2, btnToggle3, btnToggle4, btnToggle5, btnToggle6, btnToggle7, };
            _btnRestart = new Button[] { btnRestart0, btnRestart1, btnRestart2, btnRestart3, btnRestart4, btnRestart5, btnRestart6, btnRestart7 };
            _btnBrowse = new Button[] { btnBrowse0, btnBrowse1, btnBrowse2, btnBrowse3, btnBrowse4, btnBrowse5, btnBrowse6, btnBrowse7 };
            _btnHideShow = new Button[] { btnHS0, btnHS1, btnHS2, btnHS3, btnHS4, btnHS5, btnHS6, btnHS7 };
            _chkAutoRestart = new CheckBox[] { chkAuto0, chkAuto1, chkAuto2, chkAuto3, chkAuto4, chkAuto5, chkAuto6, chkAuto7 };

            // here SMC & Certification LED + Toggle + Browse only tmm ya bro
            _ledSimple = new Panel[] { led8, led9 };
            _btnToggleSimple = new Button[] { btnToggle8, btnToggle9 };
            _btnBrowseSimple = new Button[] { btnBrowse8, btnBrowse9 };

            for (int i = 0; i < _modules.Length; i++)
            {

                
                 _btnToggle[i].Tag = i;
                _btnRestart[i].Tag = i;
                _btnBrowse[i].Tag = i;
                _btnHideShow[i].Tag = i;
                _chkAutoRestart[i].Tag = i;

                _btnToggle[i].Click += BtnToggle_Click;
                _btnRestart[i].Click += BtnRestart_Click;
                _btnBrowse[i].Click += BtnBrowse_Click;
                _btnHideShow[i].Click += BtnHideShow_Click;
                _chkAutoRestart[i].CheckedChanged += ChkAutoRestart_CheckedChanged;

                _ledIndicator[i].Paint += LedPaint;

                _chkAutoRestart[i].Checked = _modules[i].AutoRestart;
                toolTip1.SetToolTip(_btnBrowse[i], string.IsNullOrEmpty(_modules[i].ExePath) ? "Click to set exe path" : _modules[i].ExePath);
            }

            for (int i = 0; i < _modulesSimple.Length; i++)
            {
                _btnToggleSimple[i].Tag = i;
                _btnBrowseSimple[i].Tag = i;
                _btnToggleSimple[i].Click += BtnToggleSimple_Click;
                _btnBrowseSimple[i].Click += BtnBrowseSimple_Click;
                _ledSimple[i].Paint += LedPaint;
                toolTip1.SetToolTip(_btnBrowseSimple[i], string.IsNullOrEmpty(_modulesSimple[i].ExePath) ? "Click to set exe path" : _modulesSimple[i].ExePath);
            }
        }

        private void LedPaint(object ps, PaintEventArgs pe)
        {
            pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(((Panel)ps).BackColor))
                pe.Graphics.FillEllipse(brush, 0, 0, ((Panel)ps).Width - 1, ((Panel)ps).Height - 1);
            using (var hi = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                pe.Graphics.FillEllipse(hi, 2, 1, 5, 4);
        }

        private void Main_Form_ER_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) e.Cancel = true;
            }
        }

        private void LoadIni()
        {
            if (!File.Exists(_iniPath)) return;
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(_iniPath))
            {
                int eq = line.IndexOf('=');
                if (eq > 0) dict[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
            foreach (var m in _modules)
            {
                if (dict.TryGetValue(m.ProcessName, out string v) && v.Length > 0) m.ExePath = v;
                if (dict.TryGetValue(m.ProcessName + "_AutoRestart", out string auto)) m.AutoRestart = auto == "1";
            }
            foreach (var m in _modulesSimple)
            {
                if (dict.TryGetValue(m.ProcessName, out string v) && v.Length > 0) m.ExePath = v;
            }
        }

        private void SaveIni()
        {
            try
            {
                var lines = new List<string>();
                foreach (var m in _modules)
                {
                    lines.Add(m.ProcessName + "=" + (m.ExePath ?? ""));
                    lines.Add(m.ProcessName + "_AutoRestart=" + (m.AutoRestart ? "1" : "0"));
                }
                foreach (var m in _modulesSimple)
                {
                    lines.Add(m.ProcessName + "=" + (m.ExePath ?? ""));
                }
                File.WriteAllLines(_iniPath, lines.ToArray());
            }
            catch (Exception ex) { Log("ERROR saving ini: " + ex.Message); }
        }

        private void Log(string msg)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
            if (rtbLog.InvokeRequired) rtbLog.Invoke(new Action(() => AppendLog(line)));
            else AppendLog(line);
        }
        private void Log(string msg, Color color)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;

            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => AppendLogColor(line, color)));
            }
            else
            {
                AppendLogColor(line, color);
            }
        }

        private void AppendLogColor(string line, Color color)
        {
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = color; 
            rtbLog.AppendText(line + Environment.NewLine);
            rtbLog.SelectionColor = rtbLog.ForeColor; 
            rtbLog.ScrollToCaret();
        }

        private void AppendLog(string line)
        {
            if (_logLineCount >= MAX_LOG_LINES)
            {
                string text = rtbLog.Text;
                int firstNewline = text.IndexOf('\n');
                if (firstNewline >= 0) rtbLog.Text = text.Substring(firstNewline + 1);
                else rtbLog.Clear();
                _logLineCount--;
            }
            rtbLog.AppendText(line + "\n");
            _logLineCount++;
            rtbLog.ScrollToCaret();
        }


        private void SetControlsEnabled(bool guardOk)
        {

            for (int i = 0; i < _modules.Length; i++)
            {
                _chkAutoRestart[i].Enabled = guardOk;
                if (!guardOk)
                {
                    _btnToggle[i].Enabled   = false;
                    _btnRestart[i].Enabled  = false;
                    _btnHideShow[i].Enabled = false;
                    _chkAutoRestart[i].Checked = false;
                    chkGuardKillAll.Enabled = false;
                }
            }
        }

        private string ResolveExePath(ModuleInfo m) => (!string.IsNullOrEmpty(m.ExePath) && File.Exists(m.ExePath)) ? m.ExePath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, m.DefaultExe);
        private bool IsRunning(ModuleInfo m) => Process.GetProcessesByName(m.ProcessName).Length > 0;

        private bool IsConfigured(ModuleInfo m) => !string.IsNullOrEmpty(m.ExePath);

        private void StartModule(ModuleInfo m)
        {
            string path = ResolveExePath(m);
            try
            {
                ProcessStartInfo psi;
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".bat" || ext == ".cmd")
                {
                    
                    psi = new ProcessStartInfo("cmd.exe", "/c \"" + path + "\"")
                    {
                        WorkingDirectory  = Path.GetDirectoryName(path),
                        UseShellExecute   = true,
                        CreateNoWindow    = false
                    };
                }
                else
                {
                    psi = new ProcessStartInfo(path)
                    {
                        WorkingDirectory = Path.GetDirectoryName(path)
                    };
                }
                Process.Start(psi);
                Log("Started: " + m.Name);
            }
            catch (Exception ex) { Log("ERROR starting " + m.Name + ": " + ex.Message); }
        }

        private async Task KillModuleAsync(ModuleInfo m)
        {
            m._intentionallyClosed = true;
            foreach (var p in Process.GetProcessesByName(m.ProcessName)) try { p.Kill(); } catch { }
            Log("Closed: " + m.Name);
            await Task.Delay(1500);
        }

        private async Task StartModuleAsync(ModuleInfo m)
        {
            string path = ResolveExePath(m);
            if (!File.Exists(path)) { Log("SKIP (not found): " + m.Name); return; }
            StartModule(m);
            await Task.Delay(m.StartDelay);
        }

        private string GetResourceInfo(ModuleInfo m)
        {
            var procs = Process.GetProcessesByName(m.ProcessName);
            if (procs.Length == 0) return "0 MB";
            try
            {
                long totalMem = 0;
                foreach (var p in procs)
                {
                    p.Refresh(); 
                    totalMem += p.WorkingSet64;
                }
                return (totalMem / 1024 / 1024).ToString() + " MB";
            }
            catch
            {
                return "0 MB"; 
            }
        }

        private List<IntPtr> GetProcessWindows(int pid)
        {
            var result = new List<IntPtr>();
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                GetWindowThreadProcessId(hWnd, out uint wPid);
                if ((int)wPid == pid && IsWindow(hWnd)) result.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return result;
        }

        private void HideModuleWindows(ModuleInfo m)
        {
            foreach (var p in Process.GetProcessesByName(m.ProcessName))
                foreach (var hWnd in GetProcessWindows(p.Id)) ShowWindow(hWnd, SW_HIDE);
        }

        private void ShowModuleWindows(ModuleInfo m)
        {
            foreach (var p in Process.GetProcessesByName(m.ProcessName))
                foreach (var hWnd in GetProcessWindows(p.Id)) { ShowWindow(hWnd, SW_RESTORE); SetForegroundWindow(hWnd); }
        }

        private void timerStatus_Tick(object sender, EventArgs e)
        {
            bool guardOk = IsGuardRunning();
            SetControlsEnabled(guardOk);

            lblGuard.Text = guardOk ? "Custom Certification: Running" : "Custom Certification: Not Running";
            lblGuard.ForeColor = guardOk ? Color.Green : Color.Red;

            _ledBlink = !_ledBlink;
            int runningCount = 0, totalConfigured = 0;

            for (int i = 0; i < _modules.Length; i++)
            {
                var m = _modules[i];
                bool configured = IsConfigured(m);
                bool running = configured && IsRunning(m);

                if (configured) totalConfigured++;
                if (running) runningCount++;

                _ledIndicator[i].BackColor = !configured ? LED_UNCONFIGURED : (running ? (_ledBlink ? LED_RUNNING : LED_BLINK) : LED_STOPPED);

                if (running)
                {
                    string sizeInfo = GetResourceInfo(m);

                    if (!string.IsNullOrEmpty(sizeInfo))
                    {
                        _lblResources[i].AutoSize = true;
                        _lblResources[i].Text = sizeInfo;
                        _lblResources[i].BringToFront();
                    }
                }
                else
                {
                    _lblResources[i].Text = "";
                }

                if (guardOk)
                {
                    _btnToggle[i].Enabled   = configured;
                    _btnToggle[i].Text      = running ? "Close" : "Start";
                    _btnRestart[i].Enabled  = running;
                    _btnHideShow[i].Enabled = running;
                    chkGuardKillAll.Enabled = true;
                }

                if (m._wasRunning && !running && !m._intentionallyClosed && m.AutoRestart)
                {
                    Log("" + m.Name + " crashed — auto-restarting...");
                    StartModule(m);
                }

                if (!running) m._intentionallyClosed = false;
                m._wasRunning = running;
            }

            this.Text = string.Format("Modules Runtime vsro v1.0 | {0}/{1} Running", runningCount, totalConfigured);
            for (int i = 0; i < _modulesSimple.Length; i++)
            {
                var m = _modulesSimple[i];
                bool configured = !string.IsNullOrEmpty(m.ExePath);
                bool running = configured && IsRunning(m);
                _ledSimple[i].BackColor = !configured ? LED_UNCONFIGURED : (running ? (_ledBlink ? LED_RUNNING : LED_BLINK) : LED_STOPPED);
                _btnToggleSimple[i].Text = running ? "Close" : "Start";
                _btnToggleSimple[i].Enabled = configured;
                _btnBrowseSimple[i].Enabled = true;
            }

            UpdateSysInfo();
            if (chkGuardKillAll.Checked && !guardOk)
            {
                Log($"CertModule stopped/crashed! Emergency closing all modules...");
                btnCloseAll_Click(null, null);
                chkGuardKillAll.Checked = false;
            }
        }

        private void UpdateSysInfo()
        {
            try
            {
                var ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
                long totalMB = (long)(ci.TotalPhysicalMemory / 1024 / 1024);
                long availMB = (long)(ci.AvailablePhysicalMemory / 1024 / 1024);
                long usedMB = totalMB - availMB;
                int ramPct = totalMB > 0 ? (int)(usedMB * 100 / totalMB) : 0;

                lblSysRam.Text = string.Format("RAM: {0} MB / {1} MB ({2}%)", usedMB, totalMB, ramPct);
                pbRam.Value = Math.Min(ramPct, 100);

                if (_cpuCounter != null)
                {
                    int cpuPct = (int)_cpuCounter.NextValue();
                    lblSysCpu.Text = string.Format("CPU: {0}%", cpuPct);
                    pbCpu.Value = Math.Min(cpuPct, 100);
                }
            }
            catch { }
        }

        private async void btnStartAll_Click(object s, EventArgs e)
        {
            if (s != null)
            {
                if (MessageBox.Show("Are you sure you want to start All modules?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }
            btnStartAll.Enabled = false;

            try
            {
                for (int i = 0; i < _modules.Length; i++)
                {
                    if (!IsGuardRunning())
                    {
                        Log("Start All cancelled during execution (Custom Certification crashed or closed)");
                        break;
                    }

                    var m = _modules[i];

                    if (!IsConfigured(m)) continue;

                    if (IsRunning(m)) continue;
                    await StartModuleAsync(m);

                    if (m.ProcessName.Equals("AgentServer", StringComparison.OrdinalIgnoreCase))
                    {
                        var smcModule = _modulesSimple.FirstOrDefault(x => x.ProcessName.Equals("smc_independent", StringComparison.OrdinalIgnoreCase));
                        if (smcModule != null && !IsRunning(smcModule) && !string.IsNullOrEmpty(smcModule.ExePath))
                        {
                            Log("Agent Server started. Launching SMC automatically...");
                            StartModule(smcModule);
                            await Task.Delay(smcModule.StartDelay);
                        }
                    }
                }

                Log("Start All finished.");
            }
            catch (Exception ex)
            {
                Log($"Error in Start All: {ex.Message}");
            }
            finally
            {
                btnStartAll.Enabled = true;
            }
        }
        // i leave it here just if you need to use it mate
        private async Task CloseModuleByNameAsync(string processName)
        {
            foreach (var m in _modules)
            {
                if (string.Equals(m.ProcessName, processName, StringComparison.OrdinalIgnoreCase) && IsRunning(m))
                {
                    await KillModuleAsync(m);
                    break;
                }
            }
        }

        private async void btnCloseAll_Click(object s, EventArgs e)
        {
            if (s != null)
            {
                if (MessageBox.Show("Are you sure you want to Shutdown for ALL modules?.",
                    "Confirm Safe Close All", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }

            btnCloseAll.Enabled = false;

            try
            {
                Log("Starting Shutdown...");
                await CloseModuleSafe("GatewayServer");
                await CloseModuleSafe("AgentServer");
                await Task.Delay(5000);
                await CloseModuleSafe("SR_GameServer");
                await CloseModuleSafe("SR_ShardManager");
                await CloseModuleSafe("FarmManager");
                await CloseModuleSafe("MachineManager");
                await CloseModuleSafe("DownloadServer");
                await CloseModuleSafe("GlobalManager");
                foreach (var m in _modulesSimple)
                {
                    if (!IsRunning(m))
                    {
                        continue;
                    }

                    await KillModuleAsync(m);
                }

                Log("Shutdown completed successfully!");
            }
            catch (Exception ex)
            {
                Log($"Error during Shutdown: {ex.Message}");
            }
            finally
            {
                btnCloseAll.Enabled = true;
            }
        }
        private async Task CloseModuleSafe(string processName)
        {
            var m = _modules.FirstOrDefault(x =>
                x.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

            if (m == null)
            {
                return;
            }

            if (!IsRunning(m))
            {
                return;
            }

            await KillModuleAsync(m);
        }
        private void btnHideAll_Click(object s, EventArgs e)
        {
            for (int i = 0; i < _modules.Length; i++)
            {
                if (!IsRunning(_modules[i])) continue;
                HideModuleWindows(_modules[i]);
                _hiddenModules.Add(_modules[i].ProcessName);
                _btnHideShow[i].Text = "Show";
            }
        }

        private void btnShowAll_Click(object s, EventArgs e)
        {
            for (int i = 0; i < _modules.Length; i++)
            {
                ShowModuleWindows(_modules[i]);
                _hiddenModules.Remove(_modules[i].ProcessName);
                _btnHideShow[i].Text = "Hide";
            }
        }

        private async void BtnToggle_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            var m = _modules[idx];
            if (IsRunning(m))
            {
                if (MessageBox.Show("Close " + m.Name + "?", "Confirm Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
                await KillModuleAsync(m);
            }
            else StartModule(m);
        }

        private async void BtnToggleSimple_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            var m = _modulesSimple[idx];
            if (IsRunning(m))
            {
                if (MessageBox.Show("Close " + m.Name + "?", "Confirm Close", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) return;
                await KillModuleAsync(m);
            }
            else StartModule(m);
        }

        private async void BtnRestart_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            _btnRestart[idx].Enabled = false;
            try { await KillModuleAsync(_modules[idx]); StartModule(_modules[idx]); }
            finally { _btnRestart[idx].Enabled = true; }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            var m = _modules[idx];
            using (var dlg = new OpenFileDialog { Title = "Select exe for " + m.Name, Filter = "Executable files (*.exe)|*.exe", FileName = m.DefaultExe })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (!Path.GetFileName(dlg.FileName).Equals(m.DefaultExe, StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("You must select \"" + m.DefaultExe + "\"", "Wrong file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    m.ExePath = dlg.FileName;
                    SaveIni();
                    toolTip1.SetToolTip(_btnBrowse[idx], m.ExePath);
                }
            }
        }

        private void BtnBrowseSimple_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            var m = _modulesSimple[idx];
            using (var dlg = new OpenFileDialog
            {
                Title    = "Select file for " + m.Name,
                Filter   = "Executable/Batch files (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd",
                FileName = m.DefaultExe
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    m.ExePath = dlg.FileName;
                    SaveIni();
                    toolTip1.SetToolTip(_btnBrowseSimple[idx], m.ExePath);
                }
            }
        }

        private void BtnHideShow_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            var m = _modules[idx];
            if (_hiddenModules.Contains(m.ProcessName))
            {
                ShowModuleWindows(m); _hiddenModules.Remove(m.ProcessName); _btnHideShow[idx].Text = "Hide";
            }
            else
            {
                HideModuleWindows(m); _hiddenModules.Add(m.ProcessName); _btnHideShow[idx].Text = "Show";
            }
        }

        private void ontopch_CheckedChanged(object sender, EventArgs e)
        {
            if (ontopch.Checked)
                TopMost = true;
            else 
                TopMost = false;
        }

        private void utubsub_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/@MrExamofficial");
        }

        private void blogsub_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.blogger.com/follow.g?blogID=9211519412361164505");
        }

        private void facesub_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/MrExamofficial/");
        }

        private void ChkAutoRestart_CheckedChanged(object sender, EventArgs e)
        {
            int idx = (int)((CheckBox)sender).Tag;
            _modules[idx].AutoRestart = ((CheckBox)sender).Checked;
            SaveIni();
        }
    }
}