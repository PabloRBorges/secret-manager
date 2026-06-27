using SecretManager.Models;
using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Janela principal: lista, busca, CRUD de credenciais e backup para pendrive.</summary>
public sealed class MainForm : Form
{
    private readonly VaultStore _store;
    private readonly AppSettings _settings;

    private const string AllGroupsLabel = "Todos os grupos";

    private readonly UiTextBox _search = new() { Width = 240 };
    private readonly ComboBox _groupFilter = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9.5f),
        Width = 180,
    };
    private bool _suppressFilter;
    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        Dock = DockStyle.Fill,
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
        BorderStyle = BorderStyle.None,
        OwnerDraw = true,
        BackColor = Theme.Surface,
    };

    private int _hoverIndex = -1;
    private System.Windows.Forms.Timer? _clipboardTimer;
    private string? _clipboardGuard;
    private readonly ToolStripStatusLabel _statusLabel = new("Pronto.");

    /// <summary>Disparado quando o usuario pede para travar o cofre pela janela.</summary>
    public event EventHandler? LockRequested;

    public MainForm(VaultStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;

        Text = "Secret Manager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(940, 560);
        MinimumSize = new Size(760, 440);
        ShowInTaskbar = true;
        Theme.ApplyForm(this);

        BuildUi();
        ReloadList();
    }

    private void BuildUi()
    {
        // --- Lista (preenche o centro) ---
        _list.Columns.Add("Título", 220);
        _list.Columns.Add("Usuário", 190);
        _list.Columns.Add("URL", 220);
        _list.Columns.Add("Atualizado", 90);
        _list.Font = new Font("Segoe UI", 9.75f);
        _list.DoubleClick += (_, _) => CopySelected(pw: true);
        _list.KeyDown += OnListKeyDown;
        _list.DrawColumnHeader += OnDrawHeader;
        _list.DrawItem += (_, e) => e.DrawDefault = false;
        _list.DrawSubItem += OnDrawSubItem;
        _list.MouseMove += OnListMouseMove;
        _list.MouseLeave += (_, _) => { _hoverIndex = -1; _list.Invalidate(); };

        var listHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), BackColor = Theme.AppBackground };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(1) };
        card.Controls.Add(_list);
        listHost.Controls.Add(card);

        // --- Barra de acoes ---
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.AppBackground, Padding = new Padding(16, 11, 16, 0) };

        var rightActions = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Theme.AppBackground, AutoSize = true };
        rightActions.Controls.Add(MakeButton("Backup", ButtonKind.Ghost, () => DoBackup(), 84));
        rightActions.Controls.Add(MakeButton("Config", ButtonKind.Ghost, () => OpenSettings(), 78));
        rightActions.Controls.Add(MakeButton("🔒 Travar", ButtonKind.Ghost, () => LockRequested?.Invoke(this, EventArgs.Empty), 96));

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Theme.AppBackground };
        actions.Controls.Add(MakeButton("+ Nova", ButtonKind.Primary, () => AddEntry(), 92));
        actions.Controls.Add(MakeButton("Editar", ButtonKind.Secondary, () => EditSelected(), 80));
        actions.Controls.Add(MakeButton("Excluir", ButtonKind.Secondary, () => DeleteSelected(), 80));
        actions.Controls.Add(MakeButton("Copiar senha", ButtonKind.Secondary, () => CopySelected(true), 120));
        actions.Controls.Add(MakeButton("Copiar usuário", ButtonKind.Secondary, () => CopySelected(false), 138));

        toolbar.Controls.Add(actions);
        toolbar.Controls.Add(rightActions);

        // --- Header com busca embutida ---
        var header = Theme.Header("Secret Manager", "Suas senhas, criptografadas e à mão", height: 76);
        _search.PlaceholderText = "🔎  Buscar...";
        _search.Width = 230;
        _search.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _search.Location = new Point(header.Width - 230 - 20, 19);
        _search.TextChanged += (_, _) => ReloadList();
        header.Controls.Add(_search);

        // Filtro por grupo, a esquerda da busca.
        _groupFilter.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _groupFilter.SelectedIndexChanged += (_, _) => { if (!_suppressFilter) ReloadList(); };
        header.Controls.Add(_groupFilter);

        void PositionHeaderControls()
        {
            _search.Location = new Point(header.Width - _search.Width - 20, 19);
            _groupFilter.Location = new Point(_search.Left - _groupFilter.Width - 10, 21);
        }
        PositionHeaderControls();
        header.Resize += (_, _) => PositionHeaderControls();

        // --- Status ---
        var status = new StatusStrip { BackColor = Theme.Surface, SizingGrip = false };
        _statusLabel.ForeColor = Theme.TextMuted;
        status.Items.Add(_statusLabel);

        // Ordem de docking (ultimo Fill por cima dos Top/Bottom)
        Controls.Add(listHost);
        Controls.Add(toolbar);
        Controls.Add(status);
        Controls.Add(header);
    }

    private AccentButton MakeButton(string text, ButtonKind kind, Action onClick, int width)
    {
        var b = new AccentButton(text, kind) { Width = width, Height = 38, Margin = new Padding(0, 0, 8, 0) };
        b.Click += (_, _) => onClick();
        return b;
    }

    // ---------- Owner draw ----------

    private void OnDrawHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var bg = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(bg, e.Bounds);
        var rect = e.Bounds; rect.X += 8;
        TextRenderer.DrawText(e.Graphics, e.Header?.Text, new Font("Segoe UI Semibold", 8.5f), rect,
            Theme.TextMuted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        using var pen = new Pen(Theme.Border);
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void OnDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        bool selected = e.Item!.Selected;
        bool hover = e.ItemIndex == _hoverIndex;

        if (e.ColumnIndex == 0)
        {
            var rowRect = new Rectangle(0, e.Bounds.Y, _list.ClientSize.Width, e.Bounds.Height);
            Color back = selected ? Theme.RowSelected : hover ? Theme.RowHover : (e.ItemIndex % 2 == 1 ? Theme.RowAlt : Theme.Surface);
            using var b = new SolidBrush(back);
            e.Graphics.FillRectangle(b, rowRect);
            if (selected)
            {
                using var accent = new SolidBrush(Theme.Accent);
                e.Graphics.FillRectangle(accent, new Rectangle(0, e.Bounds.Y, 3, e.Bounds.Height));
            }
        }

        var textRect = e.Bounds; textRect.X += 8; textRect.Width -= 12;
        var font = e.ColumnIndex == 0 ? new Font("Segoe UI Semibold", 9.75f) : new Font("Segoe UI", 9.5f);
        var color = e.ColumnIndex == 0 ? Theme.Text : Theme.TextMuted;
        TextRenderer.DrawText(e.Graphics, e.SubItem?.Text, font, textRect, color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        font.Dispose();
    }

    private void OnListMouseMove(object? sender, MouseEventArgs e)
    {
        var item = _list.GetItemAt(e.X, e.Y);
        int idx = item?.Index ?? -1;
        if (idx != _hoverIndex) { _hoverIndex = idx; _list.Invalidate(); }
    }

    // ---------- Dados ----------

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) { CopySelected(pw: true); e.Handled = true; }
        else if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
    }

    private void ReloadList()
    {
        if (_store.Current is null) return;

        RefreshGroupFilter();
        var filter = SelectedFilterGroup(); // null = todos

        var entries = _store.Current.Search(_search.Text)
            .Where(e => filter is null || string.Equals(Vault.DisplayGroup(e.Group), filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Groups.Clear();

        // Cria os grupos em ordem: reais (alfabetica) e "Sem grupo" por ultimo.
        var displays = entries.Select(e => Vault.DisplayGroup(e.Group))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d == Vault.NoGroupLabel ? 1 : 0)
            .ThenBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groupMap = new Dictionary<string, ListViewGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in displays)
        {
            var g = new ListViewGroup(d) { HeaderAlignment = HorizontalAlignment.Left };
            groupMap[d] = g;
            _list.Groups.Add(g);
        }

        foreach (var entry in entries)
        {
            var item = new ListViewItem(entry.Title) { Tag = entry };
            item.SubItems.Add(entry.Username);
            item.SubItems.Add(entry.Url);
            item.SubItems.Add(entry.UpdatedAt.LocalDateTime.ToString("dd/MM/yy"));
            item.Group = groupMap[Vault.DisplayGroup(entry.Group)];
            _list.Items.Add(item);
        }

        _list.ShowGroups = _list.Groups.Count > 0;
        _list.EndUpdate();

        var n = _list.Items.Count;
        _statusLabel.Text = n == 0 ? "Nenhuma credencial. Clique em “+ Nova”." : $"{n} credencial(is).";
    }

    /// <summary>Repopula o filtro de grupos preservando a selecao atual.</summary>
    private void RefreshGroupFilter()
    {
        var desired = new List<string> { AllGroupsLabel };
        desired.AddRange(_store.Current!.Groups());
        if (_store.Current.Entries.Any(e => string.IsNullOrWhiteSpace(e.Group)))
            desired.Add(Vault.NoGroupLabel);

        var current = _groupFilter.Items.Cast<string>().ToList();
        if (current.SequenceEqual(desired)) return;

        var prev = _groupFilter.SelectedItem as string;
        _suppressFilter = true;
        _groupFilter.Items.Clear();
        _groupFilter.Items.AddRange(desired.Cast<object>().ToArray());
        var idx = prev is not null ? desired.IndexOf(prev) : 0;
        _groupFilter.SelectedIndex = idx >= 0 ? idx : 0;
        _suppressFilter = false;
    }

    private string? SelectedFilterGroup() =>
        _groupFilter.SelectedItem is string s && s != AllGroupsLabel ? s : null;

    private VaultEntry? Selected =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as VaultEntry : null;

    private void AddEntry()
    {
        using var form = new EntryForm(null, _store.Current!.Groups());
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _store.Current!.Entries.Add(form.Entry);
            PersistAndRefresh();
        }
    }

    private void EditSelected()
    {
        var sel = Selected;
        if (sel is null) { Toast("Selecione uma credencial."); return; }
        using var form = new EntryForm(sel, _store.Current!.Groups());
        if (form.ShowDialog(this) == DialogResult.OK)
            PersistAndRefresh();
    }

    private void DeleteSelected()
    {
        var sel = Selected;
        if (sel is null) { Toast("Selecione uma credencial."); return; }
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
        if (sel is null) { Toast("Selecione uma credencial."); return; }
        var value = pw ? sel.Password : sel.Username;
        if (string.IsNullOrEmpty(value)) return;

        try
        {
            Clipboard.SetText(value);
            if (pw)
            {
                ScheduleClipboardClear(value);
                Toast($"Senha copiada — limpa em {Math.Max(5, _settings.ClipboardClearSeconds)}s.");
            }
            else Toast("Usuário copiado.");
        }
        catch { /* clipboard ocupado por outro processo */ }
    }

    private void Toast(string msg) => _statusLabel.Text = msg;

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
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }
}
