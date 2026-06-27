using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Preferencias: auto-lock, limpeza de clipboard, iniciar com Windows, trocar senha mestra.</summary>
public sealed class SettingsForm : Form
{
    private readonly VaultStore _store;
    private readonly AppSettings _settings;

    private readonly NumericUpDown _autoLock = new() { Minimum = 0, Maximum = 120, Width = 80 };
    private readonly NumericUpDown _clipClear = new() { Minimum = 5, Maximum = 600, Width = 80 };
    private readonly CheckBox _startup = new() { Text = "Iniciar com o Windows (no tray)", AutoSize = true };

    public SettingsForm(VaultStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;

        Text = "Configuracoes";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        _autoLock.Value = Math.Clamp(settings.AutoLockMinutes, 0, 120);
        _clipClear.Value = Math.Clamp(settings.ClipboardClearSeconds, 5, 600);
        _startup.Checked = settings.StartWithWindows;

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Travar apos inatividade (min, 0 = nunca):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 0);
        layout.Controls.Add(_autoLock, 1, 0);
        layout.Controls.Add(new Label { Text = "Limpar area de transferencia apos (s):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 1);
        layout.Controls.Add(_clipClear, 1, 1);
        layout.SetColumnSpan(_startup, 2);
        layout.Controls.Add(_startup, 0, 2);

        var changePw = new Button { Text = "Trocar senha mestra...", AutoSize = true };
        changePw.Click += OnChangeMasterPassword;
        layout.SetColumnSpan(changePw, 2);
        layout.Controls.Add(changePw, 0, 3);

        var ok = new Button { Text = "Salvar", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100 };
        ok.Click += OnSave;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.SetColumnSpan(buttons, 2);
        layout.Controls.Add(buttons, 0, 4);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.AutoLockMinutes = (int)_autoLock.Value;
        _settings.ClipboardClearSeconds = (int)_clipClear.Value;
        _settings.StartWithWindows = _startup.Checked;
        _settings.Save();
        StartupRegistration.Apply(_startup.Checked);
    }

    private void OnChangeMasterPassword(object? sender, EventArgs e)
    {
        using var dlg = new ChangePasswordForm(_store);
        dlg.ShowDialog(this);
    }
}
