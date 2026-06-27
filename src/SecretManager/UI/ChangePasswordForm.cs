using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Re-criptografa o cofre com uma nova senha mestra.</summary>
public sealed class ChangePasswordForm : Form
{
    private readonly VaultStore _store;
    private readonly UiTextBox _new = new(password: true);
    private readonly UiTextBox _confirm = new(password: true);

    public ChangePasswordForm(VaultStore store)
    {
        _store = store;

        Text = "Trocar senha mestra";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 280);
        Theme.ApplyForm(this);

        var header = Theme.Header("Trocar senha mestra", "O cofre será re-criptografado");

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 0), BackColor = Theme.Surface };

        body.Controls.Add(Lbl("Nova senha", 0));
        _new.Width = 372; _new.Location = new Point(0, 22);
        body.Controls.Add(_new);

        int t = 22 + _new.Height + 14;
        body.Controls.Add(Lbl("Confirmar senha", t));
        _confirm.Width = 372; _confirm.Location = new Point(0, t + 22);
        body.Controls.Add(_confirm);

        var ok = new AccentButton("Trocar senha", ButtonKind.Primary) { Width = 140 };
        var cancel = new AccentButton("Cancelar", ButtonKind.Secondary) { Width = 110, DialogResult = DialogResult.Cancel };
        ok.Click += OnOk;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(24, 12, 24, 0), BackColor = Theme.Surface };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(body);
        Controls.Add(header);
        Controls.Add(buttons);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Label Lbl(string text, int y) => new()
    {
        Text = text, AutoSize = true, Font = Theme.Bold, ForeColor = Theme.Text, Location = new Point(2, y),
    };

    private void OnOk(object? sender, EventArgs e)
    {
        if (_new.Text.Length < 8)
        {
            MessageBox.Show(this, "Use ao menos 8 caracteres.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_new.Text != _confirm.Text)
        {
            MessageBox.Show(this, "As senhas não conferem.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _store.ChangeMasterPassword(_new.Text);
            MessageBox.Show(this, "Senha mestra alterada.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha: " + ex.Message, "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
