// Mover - keeps the PC awake and your status active.
// Portable executable: no installation, no administrator rights.
// Lives in the system tray: double-click = pause/resume, right-click = menu with Exit.
//
// Build (Mono):  mcs -target:winexe -win32icon:Mover.ico -r:System.Windows.Forms.dll -r:System.Drawing.dll -out:Mover.exe Mover.cs
// Build (Windows, .NET Framework):  %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:Mover.ico /out:Mover.exe Mover.cs

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Mover")]
[assembly: AssemblyProduct("Mover")]
[assembly: AssemblyVersion("1.1.0.0")]

static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint esFlags);
    [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);

    public static uint IdleSeconds()
    {
        LASTINPUTINFO lii = new LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(lii);
        if (!GetLastInputInfo(ref lii)) return 0;
        return unchecked((uint)Environment.TickCount - lii.dwTime) / 1000;
    }

    public static void Nudge()
    {
        const byte VK_F15 = 0x7E;
        keybd_event(VK_F15, 0, 0, UIntPtr.Zero);
        keybd_event(VK_F15, 0, 2 /* KEYUP */, UIntPtr.Zero);
        mouse_event(0x0001 /* MOVE */, 1, 0, 0, UIntPtr.Zero);
        mouse_event(0x0001 /* MOVE */, -1, 0, 0, UIntPtr.Zero);
    }

    public static void StayAwake(bool on)
    {
        // ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED
        SetThreadExecutionState(on ? 0x80000003u : 0x80000000u);
    }
}

class TrayApp : ApplicationContext
{
    const int IdleThresholdSeconds = 60;       // minimum idle time before simulating activity
    const int CheckIntervalMs = 30000;      // how often to check
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "Mover";

    readonly NotifyIcon trayIcon = new NotifyIcon();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly ToolStripMenuItem toggleItem;
    readonly ToolStripMenuItem startupItem;
    readonly Icon activeIcon = CreateIcon(Color.FromArgb(46, 160, 67));
    readonly Icon pausedIcon = CreateIcon(Color.FromArgb(140, 140, 140));
    bool active = true;

    public TrayApp()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripMenuItem titleItem = new ToolStripMenuItem("Mover");
        titleItem.Enabled = false;
        menu.Items.Add(titleItem);
        menu.Items.Add(new ToolStripSeparator());
        toggleItem = new ToolStripMenuItem("Pause", null, delegate { Toggle(); });
        menu.Items.Add(toggleItem);
        startupItem = new ToolStripMenuItem("Start with Windows", null, delegate { ToggleStartup(); });
        startupItem.Checked = IsStartupEnabled();
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, delegate { Exit(); }));

        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += delegate { Toggle(); };
        trayIcon.Visible = true;

        timer.Interval = CheckIntervalMs;
        timer.Tick += delegate
        {
            if (active && Native.IdleSeconds() >= IdleThresholdSeconds) Native.Nudge();
        };
        timer.Start();

        UpdateState();
        trayIcon.ShowBalloonTip(2000, "Mover", "Running. Right-click the icon for options.", ToolTipIcon.None);
    }

    void UpdateState()
    {
        Native.StayAwake(active);
        trayIcon.Icon = active ? activeIcon : pausedIcon;
        trayIcon.Text = active ? "Mover: running" : "Mover: paused";
        toggleItem.Text = active ? "Pause" : "Resume";
    }

    void Toggle()
    {
        active = !active;
        UpdateState();
    }

    static bool IsStartupEnabled()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
            return k != null && k.GetValue(RunName) != null;
    }

    void ToggleStartup()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (IsStartupEnabled()) k.DeleteValue(RunName, false);
                else k.SetValue(RunName, "\"" + Application.ExecutablePath + "\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not change the startup setting:\n" + ex.Message, "Mover");
        }
        startupItem.Checked = IsStartupEnabled();
    }

    void Exit()
    {
        timer.Stop();
        Native.StayAwake(false);
        trayIcon.Visible = false;
        trayIcon.Dispose();
        ExitThread();
    }

    static Icon CreateIcon(Color color)
    {
        using (Bitmap bmp = new Bitmap(32, 32))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (SolidBrush b = new SolidBrush(color)) g.FillEllipse(b, 2, 2, 28, 28);
            using (Pen p = new Pen(Color.White, 4)) g.DrawLines(p, new Point[] { new Point(9, 16), new Point(14, 21), new Point(23, 11) });
            IntPtr h = bmp.GetHicon();
            Icon tmp = Icon.FromHandle(h);
            Icon copy = (Icon)tmp.Clone();
            tmp.Dispose();
            try { Native.DestroyIcon(h); } catch (EntryPointNotFoundException) { }
            return copy;
        }
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        bool createdNew;
        using (Mutex m = new Mutex(true, @"Local\Mover_Mutex", out createdNew))
        {
            if (!createdNew)
            {
                MessageBox.Show("Mover is already running (look in the system tray, next to the clock).", "Mover");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}
