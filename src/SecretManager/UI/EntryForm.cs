using SecretManager.Crypto;
using SecretManager.Models;

namespace SecretManager.UI;

/// <summary>Cria ou edita uma credencial.</summary>
public sealed class EntryForm : Form
{
    public VaultEntry Entry { get; }

    private readonly TextBox _title = new() { Width = 320 };
    private readonly TextBox _username = new() { Width = 320 };
    private readonly TextBox _password = new() { Width = 250, UseSystemPasswordChar = true };
    private readonly TextBox _url = new() { Width = 320 };
    private readonly TextBox _notes = new() { Width = 320, Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _show = new() { Text = "Mostrar", AutoSize = true };
    private readonly Button _gen = new() { Text = "Gerar", Width = 64 };

    public EntryForm(VaultEntry? existing)
    {
        Entry = existing ?? new VaultEntry();

        Text = existing is null ? "Nova credencial" : "Editar credencial";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        _title.Text = Entry.Title;
        _username.Text = Entry.Username;
        _password.Text = Entry.Password;
        _url.Text = Entry.Url;
        _notes.Text = Entry.Notes;

        _show.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_show.Checked;
        _gen.Click += (_, _) => { _password.Text = PasswordGenerator.Generate(); };

        var pwPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = Padding.Empty };
        pwPanel.Controls.Add(_password);
        pwPanel.Controls.Add(_gen);
        pwPanel.Controls.Add(_show);

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        AddRow(layout, "Titulo:", _title, 0);
        AddRow(layout, "Usuario:", _username, 1);
        AddRow(layout, "Senha:", pwPanel, 2);
        AddRow(layout, "URL:", _url, 3);
        AddRow(layout, "Notas:", _notes, 4);

        var ok = new Button { Text = "Salvar", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100 };
        ok.Click += OnSave;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.SetColumnSpan(buttons, 2);
        layout.Controls.Add(buttons, 0, 5);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control, int row)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, "Informe um titulo.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Entry.Title = _title.Text.Trim();
        Entry.Username = _username.Text;
        Entry.Password = _password.Text;
        Entry.Url = _url.Text.Trim();
        Entry.Notes = _notes.Text;
        Entry.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
