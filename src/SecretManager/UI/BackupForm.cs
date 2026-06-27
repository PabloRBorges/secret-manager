using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Backup criptografado do cofre para um pendrive (ou pasta escolhida).</summary>
public sealed class BackupForm : Form
{
    private readonly VaultStore _store;
    private readonly AppSettings _settings;

    private readonly ComboBox _drives = new() { Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _refresh = new() { Text = "Atualizar", Width = 90 };
    private readonly Button _browse = new() { Text = "Outra pasta...", Width = 110 };
    private readonly CheckBox _auto = new() { Text = "Fazer backup automatico a cada alteracao", AutoSize = true };

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
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        _auto.Checked = _settings.BackupOnSave;
        _refresh.Click += (_, _) => LoadDrives();
        _browse.Click += OnBrowse;

        var info = new Label
        {
            Text = "O backup e gravado JA CRIPTOGRAFADO (AES-256-GCM), com a mesma senha mestra.\n"
                 + "Mesmo que o pendrive seja perdido, o conteudo permanece protegido.",
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            ForeColor = Color.DimGray,
        };

        var drivePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        drivePanel.Controls.Add(_drives);
        drivePanel.Controls.Add(_refresh);
        drivePanel.Controls.Add(_browse);

        var ok = new Button { Text = "Fazer backup", Width = 120 };
        var close = new Button { Text = "Fechar", DialogResult = DialogResult.Cancel, Width = 90 };
        ok.Click += OnBackup;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(close);
        buttons.Controls.Add(ok);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(info);
        layout.Controls.Add(new Label { Text = "Destino:", AutoSize = true, Margin = new Padding(0, 10, 0, 0) });
        layout.Controls.Add(drivePanel);
        layout.Controls.Add(_auto);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

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
            _drives.Items.Add("Nenhum pendrive detectado — use 'Outra pasta...'");
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
            MessageBox.Show(this, "Selecione um destino valido.", "Backup",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var path = _store.Backup(dest);
            _settings.BackupOnSave = _auto.Checked;
            _settings.BackupDrivePath = dest;
            _settings.Save();
            MessageBox.Show(this, "Backup criado:\n" + path, "Backup",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha no backup: " + ex.Message, "Backup",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
