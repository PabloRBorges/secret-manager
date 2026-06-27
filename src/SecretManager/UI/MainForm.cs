using SecretManager.Models;
using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Janela principal: lista, busca, CRUD de credenciais e backup para pendrive.</summary>
public sealed class MainForm : Form
{
    private readonly VaultStore _store;
    private readonly AppSettings _settings;

    private readonly TextBox _search = new() { Width = 240, PlaceholderText = "Buscar..." };
    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        Dock = DockStyle.Fill,
        HideSelection = false,
    };

    private System.Windows.Forms.Timer? _clipboardTimer;
    private string? _clipboardGuard;

    /// <summary>Disparado quando o usuario pede para travar o cofre pela janela.</summary>
    public event EventHandler? LockRequested;

    public MainForm(VaultStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;

        Text = "Secret Manager";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 720;
        Height = 460;
        MinimumSize = new Size(560, 360);
        ShowInTaskbar = true;

        BuildUi();
        ReloadList();
    }

    private void BuildUi()
    {
        var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        toolbar.Items.Add(NewButton("Nova", (_, _) => AddEntry()));
        toolbar.Items.Add(NewButton("Editar", (_, _) => EditSelected()));
        toolbar.Items.Add(NewButton("Excluir", (_, _) => DeleteSelected()));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(NewButton("Copiar usuario", (_, _) => CopySelected(pw: false)));
        toolbar.Items.Add(NewButton("Copiar senha", (_, _) => CopySelected(pw: true)));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(NewButton("Backup pendrive", (_, _) => DoBackup()));
        toolbar.Items.Add(NewButton("Config", (_, _) => OpenSettings()));
        toolbar.Items.Add(NewButton("Travar", (_, _) => LockRequested?.Invoke(this, EventArgs.Empty)));

        var searchPanel = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
        var host = new ToolStripControlHost(_search);
        searchPanel.Items.Add(new ToolStripLabel("Filtro: "));
        searchPanel.Items.Add(host);
        _search.TextChanged += (_, _) => ReloadList();

        _list.Columns.Add("Titulo", 200);
        _list.Columns.Add("Usuario", 180);
        _list.Columns.Add("URL", 220);
        _list.Columns.Add("Atualizado", 90);
        _list.DoubleClick += (_, _) => CopySelected(pw: true);
        _list.KeyDown += OnListKeyDown;

        var top = new ToolStripContainer { Dock = DockStyle.Fill };
        top.TopToolStripPanel.Controls.Add(searchPanel);
        top.TopToolStripPanel.Controls.Add(toolbar);
        top.ContentPanel.Controls.Add(_list);

        var status = new StatusStrip();
        status.Items.Add(new ToolStripStatusLabel(
            "Enter/duplo-clique copia a senha. A area de transferencia e limpada automaticamente."));

        Controls.Add(top);
        Controls.Add(status);
    }

    private static ToolStripButton NewButton(string text, EventHandler onClick)
    {
        var b = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        b.Click += onClick;
        return b;
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) { CopySelected(pw: true); e.Handled = true; }
        else if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
    }

    private void ReloadList()
    {
        if (_store.Current is null) return;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var entry in _store.Current.Search(_search.Text))
        {
            var item = new ListViewItem(entry.Title) { Tag = entry };
            item.SubItems.Add(entry.Username);
            item.SubItems.Add(entry.Url);
            item.SubItems.Add(entry.UpdatedAt.LocalDateTime.ToString("dd/MM/yy"));
            _list.Items.Add(item);
        }
        _list.EndUpdate();
    }

    private VaultEntry? Selected =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as VaultEntry : null;

    private void AddEntry()
    {
        using var form = new EntryForm(null);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _store.Current!.Entries.Add(form.Entry);
            PersistAndRefresh();
        }
    }

    private void EditSelected()
    {
        var sel = Selected;
        if (sel is null) return;
        using var form = new EntryForm(sel);
        if (form.ShowDialog(this) == DialogResult.OK)
            PersistAndRefresh();
    }

    private void DeleteSelected()
    {
        var sel = Selected;
        if (sel is null) return;
        if (MessageBox.Show(this, $"Excluir \"{sel.Title}\"?", "Secret Manager",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _store.Current!.Entries.RemoveAll(x => x.Id == sel.Id);
            PersistAndRefresh();
        }
    }

    private void PersistAndRefresh()
    {
        try
        {
            _store.Save();
            if (_settings.BackupOnSave && !string.IsNullOrEmpty(_settings.BackupDrivePath)
                && Directory.Exists(_settings.BackupDrivePath))
            {
                try { _store.Backup(_settings.BackupDrivePath!); } catch { /* pendrive ausente */ }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha ao gravar o cofre: " + ex.Message, "Secret Manager",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        ReloadList();
    }

    private void CopySelected(bool pw)
    {
        var sel = Selected;
        if (sel is null) return;
        var value = pw ? sel.Password : sel.Username;
        if (string.IsNullOrEmpty(value)) return;

        try
        {
            Clipboard.SetText(value);
            if (pw) ScheduleClipboardClear(value);
        }
        catch { /* clipboard ocupado por outro processo */ }
    }

    private void ScheduleClipboardClear(string value)
    {
        _clipboardGuard = value;
        _clipboardTimer?.Stop();
        _clipboardTimer?.Dispose();
        _clipboardTimer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(5, _settings.ClipboardClearSeconds) * 1000,
        };
        _clipboardTimer.Tick += (_, _) =>
        {
            _clipboardTimer!.Stop();
            try
            {
                // So limpa se ainda for o nosso valor (nao apaga o que o usuario copiou depois).
                if (Clipboard.ContainsText() && Clipboard.GetText() == _clipboardGuard)
                    Clipboard.Clear();
            }
            catch { /* ignore */ }
            _clipboardGuard = null;
        };
        _clipboardTimer.Start();
    }

    private void DoBackup()
    {
        using var form = new BackupForm(_store, _settings);
        form.ShowDialog(this);
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_store, _settings);
        form.ShowDialog(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Fechar a janela apenas a esconde; o app continua no tray.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }
}
