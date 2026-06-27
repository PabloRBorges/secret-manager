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
    private readonly AccentButton _show = new("Mostrar", ButtonKind.Ghost) { Width = 84, Height = 38 };
    private readonly AccentButton _gen = new("Gerar", ButtonKind.Secondary) { Width = 84, Height = 38 };

    public EntryForm(VaultEntry? existing)
    {
        Entry = existing ?? new VaultEntry();

        Text = existing is null ? "Nova credencial" : "Editar credencial";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 540);
        Theme.ApplyForm(this);

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

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 0), BackColor = Theme.Surface };

        int y = 0;
        y = AddField(body, "Título", _title, y);
        y = AddField(body, "Usuário", _username, y);

        // Linha da senha: campo + Gerar + Mostrar
        body.Controls.Add(MakeLabel("Senha", y));
        _password.Width = 412 - 2 * 88;
        _password.Location = new Point(0, y + 22);
        body.Controls.Add(_password);
        _gen.Location = new Point(_password.Right + 8, y + 22);
        _show.Location = new Point(_gen.Right + 4, y + 22);
        body.Controls.Add(_gen);
        body.Controls.Add(_show);
        y = y + 22 + _password.Height + 14;

        y = AddField(body, "URL", _url, y);
        y = AddField(body, "Notas", _notes, y);

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

    private static Label MakeLabel(string text, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Bold,
        ForeColor = Theme.Text,
        Location = new Point(2, y),
    };

    private static int AddField(Panel body, string label, UiTextBox field, int y)
    {
        body.Controls.Add(MakeLabel(label, y));
        field.Width = 412;
        field.Location = new Point(0, y + 22);
        body.Controls.Add(field);
        return y + 22 + field.Height + 14;
    }

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
        Entry.Notes = _notes.Text;
        Entry.UpdatedAt = DateTimeOffset.UtcNow;
        DialogResult = DialogResult.OK;
    }
}
