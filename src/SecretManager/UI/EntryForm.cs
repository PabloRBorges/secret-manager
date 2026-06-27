using SecretManager.Crypto;
using SecretManager.Models;

namespace SecretManager.UI;

/// <summary>Cria ou edita uma credencial.</summary>
public sealed class EntryForm : Form
{
    public VaultEntry Entry { get; }

    private readonly UiTextBox _title = new();
    private readonly UiTextBox _username = new();
    private readonly UiTextBox _password = new(password: true);
    private readonly UiTextBox _url = new();
    private readonly UiTextBox _notes = new(multiline: true);
    private readonly ComboBox _group = new()
    {
        DropDownStyle = ComboBoxStyle.DropDown,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10f),
    };
    private readonly AccentButton _show = new("Mostrar", ButtonKind.Ghost) { Width = 84, Height = 38 };
    private readonly AccentButton _gen = new("Gerar", ButtonKind.Secondary) { Width = 84, Height = 38 };

    public EntryForm(VaultEntry? existing, IEnumerable<string>? existingGroups = null)
    {
        Entry = existing ?? new VaultEntry();

        Text = existing is null ? "Nova credencial" : "Editar credencial";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 650);
        Theme.ApplyForm(this);

        if (existingGroups is not null)
            _group.Items.AddRange(existingGroups.Cast<object>().ToArray());
        _group.Text = Entry.Group;

        var header = Theme.Header(
            existing is null ? "Nova credencial" : "Editar credencial",
            "Os dados são salvos criptografados no cofre");

        _title.Text = Entry.Title;
        _username.Text = Entry.Username;
        _password.Text = Entry.Password;
        _url.Text = Entry.Url;
        _notes.Text = Entry.Notes;

        _show.Click += (_, _) =>
        {
            _password.UseSystemPasswordChar = !_password.UseSystemPasswordChar;
            _show.Text = _password.UseSystemPasswordChar ? "Mostrar" : "Ocultar";
        };
        _gen.Click += (_, _) => { _password.UseSystemPasswordChar = false; _show.Text = "Ocultar"; _password.Text = PasswordGenerator.Generate(); };

        // Layout por TableLayoutPanel: margens/larguras corretas em qualquer DPI.
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 12), BackColor = Theme.Surface };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Theme.Surface,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void AddRow(Control c, int topGap)
        {
            c.Margin = new Padding(0, topGap, 0, 0);
            c.Dock = DockStyle.Top;
            table.Controls.Add(c);
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddRow(MakeLabel("Título"), 0);
        AddRow(_title, 6);
        AddRow(MakeLabel("Usuário"), 12);
        AddRow(_username, 6);

        AddRow(MakeLabel("Senha"), 12);
        // Linha da senha: campo (estica) + Gerar + Mostrar
        var pwRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            ColumnCount = 3,
            Margin = new Padding(0, 6, 0, 0),
            BackColor = Theme.Surface,
        };
        pwRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pwRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pwRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _password.Dock = DockStyle.Fill;
        _password.Margin = new Padding(0);
        _gen.Margin = new Padding(8, 0, 0, 0);
        _show.Margin = new Padding(4, 0, 0, 0);
        pwRow.Controls.Add(_password, 0, 0);
        pwRow.Controls.Add(_gen, 1, 0);
        pwRow.Controls.Add(_show, 2, 0);
        table.Controls.Add(pwRow);
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddRow(MakeLabel("URL"), 12);
        AddRow(_url, 6);
        AddRow(MakeLabel("Grupo (opcional)"), 12);
        AddRow(_group, 6);
        AddRow(MakeLabel("Notas"), 12);
        AddRow(_notes, 6);

        body.Controls.Add(table);

        var ok = new AccentButton("Salvar", ButtonKind.Primary) { Width = 130 };
        var cancel = new AccentButton("Cancelar", ButtonKind.Secondary) { Width = 110, DialogResult = DialogResult.Cancel };
        ok.Click += OnSave;

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 62,
            Padding = new Padding(24, 12, 24, 0),
            BackColor = Theme.Surface,
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(body);
        Controls.Add(header);
        Controls.Add(buttons);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Bold,
        ForeColor = Theme.Text,
    };

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, "Informe um título.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Entry.Title = _title.Text.Trim();
        Entry.Username = _username.Text;
        Entry.Password = _password.Text;
        Entry.Url = _url.Text.Trim();
        Entry.Group = _group.Text.Trim();
        Entry.Notes = _notes.Text;
        Entry.UpdatedAt = DateTimeOffset.UtcNow;
        DialogResult = DialogResult.OK;
    }
}
