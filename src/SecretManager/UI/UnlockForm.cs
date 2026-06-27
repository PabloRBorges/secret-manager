using System.Security.Cryptography;
using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Prompt da senha mestra: cria o cofre no primeiro uso, ou destrava o existente.</summary>
public sealed class UnlockForm : Form
{
    private readonly VaultStore _store;
    private readonly bool _isCreate;

    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Width = 300 };
    private readonly TextBox _confirm = new() { UseSystemPasswordChar = true, Width = 300 };
    private readonly Label _strength = new() { AutoSize = true, ForeColor = Color.Gray };
    private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, Width = 100 };
    private readonly Button _cancel = new() { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100 };

    public UnlockForm(VaultStore store)
    {
        _store = store;
        _isCreate = !store.VaultExists;

        Text = _isCreate ? "Criar cofre — defina a senha mestra" : "Destravar cofre";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16);
        AcceptButton = _ok;
        CancelButton = _cancel;
        TopMost = true;

        var layout = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Senha mestra:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_password, 1, 0);

        int row = 1;
        if (_isCreate)
        {
            layout.Controls.Add(new Label { Text = "Confirmar:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            layout.Controls.Add(_confirm, 1, row);
            row++;
            layout.Controls.Add(_strength, 1, row);
            row++;

            var warn = new Label
            {
                Text = "Esta senha NAO pode ser recuperada. Se esquecer, o cofre fica inacessivel.",
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                ForeColor = Color.Firebrick,
            };
            layout.SetColumnSpan(warn, 2);
            layout.Controls.Add(warn, 0, row);
            row++;

            _password.TextChanged += (_, _) => UpdateStrength();
        }

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(_cancel);
        buttons.Controls.Add(_ok);
        layout.SetColumnSpan(buttons, 2);
        layout.Controls.Add(buttons, 0, row);

        Controls.Add(layout);

        _ok.Click += OnOk;
    }

    private void UpdateStrength()
    {
        var p = _password.Text;
        var (text, color) = ScorePassword(p);
        _strength.Text = $"Forca: {text}";
        _strength.ForeColor = color;
    }

    private static (string, Color) ScorePassword(string p)
    {
        if (p.Length == 0) return ("—", Color.Gray);
        int score = 0;
        if (p.Length >= 8) score++;
        if (p.Length >= 12) score++;
        if (p.Length >= 16) score++;
        if (p.Any(char.IsUpper) && p.Any(char.IsLower)) score++;
        if (p.Any(char.IsDigit)) score++;
        if (p.Any(c => !char.IsLetterOrDigit(c))) score++;
        return score switch
        {
            <= 2 => ("fraca", Color.Firebrick),
            <= 4 => ("media", Color.DarkOrange),
            _ => ("forte", Color.SeaGreen),
        };
    }

    private void OnOk(object? sender, EventArgs e)
    {
        var pw = _password.Text;
        if (string.IsNullOrEmpty(pw))
        {
            Fail("Informe a senha mestra.");
            return;
        }

        try
        {
            if (_isCreate)
            {
                if (pw.Length < 8)
                {
                    Fail("Use ao menos 8 caracteres na senha mestra.");
                    return;
                }
                if (pw != _confirm.Text)
                {
                    Fail("As senhas nao conferem.");
                    return;
                }
                _store.Create(pw);
            }
            else
            {
                _store.Unlock(pw);
            }
        }
        catch (CryptographicException)
        {
            Fail("Senha mestra incorreta.");
        }
        catch (Exception ex)
        {
            Fail("Falha ao abrir o cofre: " + ex.Message);
        }
    }

    private void Fail(string msg)
    {
        DialogResult = DialogResult.None;
        MessageBox.Show(this, msg, "Secret Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        _password.SelectAll();
        _password.Focus();
    }
}
