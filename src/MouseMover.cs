// Mouse Mover - mantiene lo stato di Teams "Disponibile".
// Eseguibile portatile: nessuna installazione, nessun permesso di amministratore.
// Vive nella tray: doppio clic = pausa/riprendi, tasto destro = menu con Esci.
//
// Compilazione (Mono):  mcs -target:winexe -win32icon:MouseMover.ico -r:System.Windows.Forms.dll -r:System.Drawing.dll -out:MouseMover.exe MouseMover.cs
// Compilazione (Windows, .NET Framework):  %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /win32icon:MouseMover.ico /out:MouseMover.exe MouseMover.cs

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Mouse Mover")]
[assembly: AssemblyProduct("Mouse Mover")]
[assembly: AssemblyVersion("1.0.0.0")]

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
    const int SogliaSecondi = 60;       // inattivita' minima prima di simulare attivita'
    const int ControlloMs = 30000;      // ogni quanto controllare
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "MouseMover";

    readonly NotifyIcon icona = new NotifyIcon();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly ToolStripMenuItem voceToggle;
    readonly ToolStripMenuItem voceAvvio;
    readonly Icon iconaAttiva = CreaIcona(Color.FromArgb(46, 160, 67));
    readonly Icon iconaPausa = CreaIcona(Color.FromArgb(140, 140, 140));
    bool attivo = true;

    public TrayApp()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripMenuItem titolo = new ToolStripMenuItem("Mouse Mover");
        titolo.Enabled = false;
        menu.Items.Add(titolo);
        menu.Items.Add(new ToolStripSeparator());
        voceToggle = new ToolStripMenuItem("Metti in pausa", null, delegate { Cambia(); });
        menu.Items.Add(voceToggle);
        voceAvvio = new ToolStripMenuItem("Avvia con Windows", null, delegate { CambiaAvvio(); });
        voceAvvio.Checked = AvvioAttivo();
        menu.Items.Add(voceAvvio);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Esci", null, delegate { Esci(); }));

        icona.ContextMenuStrip = menu;
        icona.DoubleClick += delegate { Cambia(); };
        icona.Visible = true;

        timer.Interval = ControlloMs;
        timer.Tick += delegate
        {
            if (attivo && Native.IdleSeconds() >= SogliaSecondi) Native.Nudge();
        };
        timer.Start();

        Aggiorna();
        icona.ShowBalloonTip(2000, "Mouse Mover", "In funzione. Tasto destro sull'icona per le opzioni.", ToolTipIcon.None);
    }

    void Aggiorna()
    {
        Native.StayAwake(attivo);
        icona.Icon = attivo ? iconaAttiva : iconaPausa;
        icona.Text = attivo ? "Mouse Mover: in funzione" : "Mouse Mover: in pausa";
        voceToggle.Text = attivo ? "Metti in pausa" : "Riprendi";
    }

    void Cambia()
    {
        attivo = !attivo;
        Aggiorna();
    }

    static bool AvvioAttivo()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
            return k != null && k.GetValue(RunName) != null;
    }

    void CambiaAvvio()
    {
        try
        {
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (AvvioAttivo()) k.DeleteValue(RunName, false);
                else k.SetValue(RunName, "\"" + Application.ExecutablePath + "\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Impossibile modificare l'avvio automatico:\n" + ex.Message, "Mouse Mover");
        }
        voceAvvio.Checked = AvvioAttivo();
    }

    void Esci()
    {
        timer.Stop();
        Native.StayAwake(false);
        icona.Visible = false;
        icona.Dispose();
        ExitThread();
    }

    static Icon CreaIcona(Color colore)
    {
        using (Bitmap bmp = new Bitmap(32, 32))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (SolidBrush b = new SolidBrush(colore)) g.FillEllipse(b, 2, 2, 28, 28);
            using (Pen p = new Pen(Color.White, 4)) g.DrawLines(p, new Point[] { new Point(9, 16), new Point(14, 21), new Point(23, 11) });
            IntPtr h = bmp.GetHicon();
            Icon tmp = Icon.FromHandle(h);
            Icon copia = (Icon)tmp.Clone();
            tmp.Dispose();
            try { Native.DestroyIcon(h); } catch (EntryPointNotFoundException) { }
            return copia;
        }
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        bool nuovo;
        using (Mutex m = new Mutex(true, @"Local\MouseMover_Mutex", out nuovo))
        {
            if (!nuovo)
            {
                MessageBox.Show("Mouse Mover e' gia' in esecuzione (guarda nella tray, vicino all'orologio).", "Mouse Mover");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}
