using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Re-criptografa o cofre com uma nova senha mestra.</summary>
public sealed class ChangePasswordForm : Form
{
    private readonly VaultStore _store;
    private readonly TextBox _new = new() { Width = 280, UseSystemPasswordChar = true };
    private readonly TextBox _confirm = new() { Width = 280, UseSystemPasswordChar = true };

    public ChangePasswordForm(VaultStore store)
    {
        _store = store;

        Text = "Trocar senha mestra";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Nova senha:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_new, 1, 0);
        layout.Controls.Add(new Label { Text = "Confirmar:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_confirm, 1, 1);

        var ok = new Button { Text = "Trocar", DialogResult = DialogResult.OK, Width = 100 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100 };
        ok.Click += OnOk;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        layout.SetColumnSpan(buttons, 2);
        layout.Controls.Add(buttons, 0, 2);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        if (_new.Text.Length < 8)
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, "Use ao menos 8 caracteres.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_new.Text != _confirm.Text)
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, "As senhas nao conferem.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            _store.ChangeMasterPassword(_new.Text);
            MessageBox.Show(this, "Senha mestra alterada.", "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            DialogResult = DialogResult.None;
            MessageBox.Show(this, "Falha: " + ex.Message, "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
