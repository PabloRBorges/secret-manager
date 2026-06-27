using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Backup criptografado do cofre para um pendrive (ou pasta escolhida).</summary>
public sealed class BackupForm : Form
{
    private readonly VaultStore _store;
    private readonly AppSettings _settings;

    private readonly ComboBox _drives = new() { Width = 290, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f) };
    private readonly CheckBox _auto = new() { Text = "  Fazer backup automático a cada alteração", AutoSize = true, Font = Theme.Base, ForeColor = Theme.Text, FlatStyle = FlatStyle.Flat };

    private string? _customPath;

    public BackupForm(VaultStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;

        Text = "Backup para pendrive";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(480, 320);
        Theme.ApplyForm(this);

        Controls.Add(Theme.Header("Backup para pendrive", "Cópia do cofre, também criptografada"));

        _auto.Checked = _settings.BackupOnSave;

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 0), BackColor = Theme.Surface };
        Controls.Add(body);
        body.BringToFront();

        var info = new Label
        {
            Text = "O backup é gravado JÁ CRIPTOGRAFADO (AES-256-GCM), com a mesma senha mestra.\n"
                 + "Mesmo que o pendrive seja perdido, o conteúdo permanece protegido.",
            AutoSize = false,
            Width = 420,
            Height = 44,
            Location = new Point(2, 0),
            ForeColor = Theme.TextMuted,
            Font = Theme.Small,
        };
        body.Controls.Add(info);

        body.Controls.Add(new Label { Text = "Destino", AutoSize = true, Font = Theme.Bold, ForeColor = Theme.Text, Location = new Point(2, 56) });

        _drives.Location = new Point(2, 80);
        body.Controls.Add(_drives);

        var refresh = new AccentButton("Atualizar", ButtonKind.Ghost) { Width = 90, Height = 32, Location = new Point(300, 80) };
        var browse = new AccentButton("Outra pasta", ButtonKind.Ghost) { Width = 100, Height = 32, Location = new Point(394 - 6, 80) };
        refresh.Click += (_, _) => LoadDrives();
        browse.Click += OnBrowse;
        body.Controls.Add(refresh);
        body.Controls.Add(browse);

        _auto.Location = new Point(2, 128);
        body.Controls.Add(_auto);

        var ok = new AccentButton("Fazer backup", ButtonKind.Primary) { Width = 140 };
        var close = new AccentButton("Fechar", ButtonKind.Secondary) { Width = 100, DialogResult = DialogResult.Cancel };
        ok.Click += OnBackup;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(24, 12, 24, 0), BackColor = Theme.Surface };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(close);
        Controls.Add(buttons);
        buttons.BringToFront();

        CancelButton = close;
        LoadDrives();
    }

    private void LoadDrives()
    {
        _customPath = null;
        _drives.Items.Clear();
        var drives = RemovableDrives.List();
        foreach (var d in drives)
            _drives.Items.Add(d);

        if (_drives.Items.Count > 0)
            _drives.SelectedIndex = 0;
        else
        {
            _drives.Items.Add("Nenhum pendrive detectado — use “Outra pasta”");
            _drives.SelectedIndex = 0;
        }
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Escolha a pasta de destino do backup" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _customPath = dlg.SelectedPath;
            _drives.Items.Clear();
            _drives.Items.Add(_customPath);
            _drives.SelectedIndex = 0;
        }
    }

    private string? ResolveDestination()
    {
        if (_customPath is not null) return _customPath;
        return _drives.SelectedItem is RemovableDrive rd ? rd.RootPath : null;
    }

    private void OnBackup(object? sender, EventArgs e)
    {
        var dest = ResolveDestination();
        if (string.IsNullOrEmpty(dest))
        {
            MessageBox.Show(this, "Selecione um destino válido.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var path = _store.Backup(dest);
            _settings.BackupOnSave = _auto.Checked;
            _settings.BackupDrivePath = dest;
            _settings.Save();
            MessageBox.Show(this, "Backup criado:\n" + path, "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha no backup: " + ex.Message, "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
