using System.Runtime.InteropServices;
using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>
/// Ciclo de vida do app no system tray (ao lado do relogio do Windows).
/// Mantem o cofre, a janela principal e o auto-lock por inatividade.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly VaultStore _store = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly System.Windows.Forms.Timer _idleTimer;

    private MainForm? _mainForm;
    private Icon _lockedIcon = TrayIconFactory.CreateLockIcon(locked: true);
    private Icon _unlockedIcon = TrayIconFactory.CreateLockIcon(locked: false);

    public TrayApplicationContext()
    {
        StartupRegistration.Apply(_settings.StartWithWindows);

        _tray = new NotifyIcon
        {
            Icon = _lockedIcon,
            Text = "Secret Manager (travado)",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        _tray.DoubleClick += (_, _) => OnOpen();

        _idleTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
        _idleTimer.Tick += CheckIdle;
        _idleTimer.Start();

        // Mostra a janela de destravar/criar logo na inicializacao.
        BeginInvokeWhenReady(() => EnsureUnlocked());
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => OnOpen());
        menu.Items.Add("Travar agora", null, (_, _) => LockVault());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => ExitApp());
        return menu;
    }

    private static void BeginInvokeWhenReady(Action action)
    {
        // Garante que rodamos no message loop ja iniciado.
        var t = new System.Windows.Forms.Timer { Interval = 1 };
        t.Tick += (s, _) => { t.Stop(); t.Dispose(); action(); };
        t.Start();
    }

    /// <summary>Garante que o cofre esteja destravado; abre o prompt se necessario.</summary>
    private bool EnsureUnlocked()
    {
        if (_store.IsUnlocked) return true;

        using var unlock = new UnlockForm(_store);
        var result = unlock.ShowDialog();
        if (result != DialogResult.OK || !_store.IsUnlocked)
            return false;

        SetUnlockedState(true);
        return true;
    }

    private void OnOpen()
    {
        if (!EnsureUnlocked()) return;

        if (_mainForm is null || _mainForm.IsDisposed)
        {
            _mainForm = new MainForm(_store, _settings);
            _mainForm.LockRequested += (_, _) => LockVault();
        }
        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.BringToFront();
        _mainForm.Activate();
    }

    private void LockVault()
    {
        _store.Lock();
        if (_mainForm is { IsDisposed: false })
            _mainForm.Hide();
        SetUnlockedState(false);
    }

    private void SetUnlockedState(bool unlocked)
    {
        _tray.Icon = unlocked ? _unlockedIcon : _lockedIcon;
        _tray.Text = unlocked ? "Secret Manager (destravado)" : "Secret Manager (travado)";
    }

    private void CheckIdle(object? sender, EventArgs e)
    {
        if (!_store.IsUnlocked) return;
        var minutes = _settings.AutoLockMinutes;
        if (minutes <= 0) return; // 0 = nunca trava sozinho

        if (GetIdleSeconds() >= minutes * 60)
            LockVault();
    }

    private void ExitApp()
    {
        _idleTimer.Stop();
        _store.Lock();
        _tray.Visible = false;
        _tray.Dispose();
        ExitThread();
    }

    // --- Inatividade do usuario via Win32 ---

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    private static double GetIdleSeconds()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info)) return 0;
        var idleMs = (uint)Environment.TickCount - info.dwTime;
        return idleMs / 1000.0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _idleTimer.Dispose();
            _store.Dispose();
            _tray.Dispose();
            _lockedIcon.Dispose();
            _unlockedIcon.Dispose();
            _mainForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
