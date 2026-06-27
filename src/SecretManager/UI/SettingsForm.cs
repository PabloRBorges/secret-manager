using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Preferencias: auto-lock, limpeza de clipboard, iniciar com Windows, trocar senha mestra.</summary>
public sealed class SettingsForm : Form
{
    private readonly VaultStore _store;
    private readonly AppSettings _settings;

    private readonly NumericUpDown _autoLock = new() { Minimum = 0, Maximum = 120, Width = 70, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
    private readonly NumericUpDown _clipClear = new() { Minimum = 5, Maximum = 600, Width = 70, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
    private readonly CheckBox _startup = new() { Text = "  Iniciar com o Windows (minimizado no tray)", AutoSize = true, Font = Theme.Base, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat };

    public SettingsForm(VaultStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;

        Text = "Configurações";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 320);
        Theme.ApplyForm(this);

        Controls.Add(Theme.Header("Configurações", "Segurança e comportamento do app"));

        _autoLock.Value = Math.Clamp(settings.AutoLockMinutes, 0, 120);
        _clipClear.Value = Math.Clamp(settings.ClipboardClearSeconds, 5, 600);
        _startup.Checked = settings.StartWithWindows;

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 0), BackColor = Theme.Surface };
        Controls.Add(body);
        body.BringToFront();

        body.Controls.Add(Row("Travar após inatividade (min, 0 = nunca)", _autoLock, 4));
        body.Controls.Add(Row("Limpar área de transferência após (s)", _clipClear, 52));
        _startup.Location = new Point(2, 100);
        body.Controls.Add(_startup);

        var changePw = new AccentButton("Trocar senha mestra", ButtonKind.Secondary) { Width = 190, Height = 38, Location = new Point(2, 140) };
        changePw.Click += OnChangeMasterPassword;
        body.Controls.Add(changePw);

        var ok = new AccentButton("Salvar", ButtonKind.Primary) { Width = 120 };
        var cancel = new AccentButton("Cancelar", ButtonKind.Secondary) { Width = 110, DialogResult = DialogResult.Cancel };
        ok.Click += OnSave;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(24, 12, 24, 0), BackColor = Theme.Surface };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        Controls.Add(buttons);
        buttons.BringToFront();

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Panel Row(string label, Control control, int top)
    {
        var p = new Panel { Location = new Point(2, top), Width = 388, Height = 40, BackColor = Color.Transparent };
        p.Controls.Add(new Label { Text = label, AutoSize = true, Font = Theme.Base, ForeColor = Theme.Text, Location = new Point(0, 8) });
        control.Location = new Point(316, 4);
        p.Controls.Add(control);
        return p;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        _settings.AutoLockMinutes = (int)_autoLock.Value;
        _settings.ClipboardClearSeconds = (int)_clipClear.Value;
        _settings.StartWithWindows = _startup.Checked;
        _settings.Save();
        StartupRegistration.Apply(_startup.Checked);
        DialogResult = DialogResult.OK;
    }

    private void OnChangeMasterPassword(object? sender, EventArgs e)
    {
        using var dlg = new ChangePasswordForm(_store);
        dlg.ShowDialog(this);
    }
}
