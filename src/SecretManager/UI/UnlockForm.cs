using System.Security.Cryptography;
using SecretManager.Storage;

namespace SecretManager.UI;

/// <summary>Prompt da senha mestra: cria o cofre no primeiro uso, ou destrava o existente.</summary>
public sealed class UnlockForm : Form
{
    private readonly VaultStore _store;
    private readonly bool _isCreate;

    private readonly UiTextBox _password = new(password: true);
    private readonly UiTextBox _confirm = new(password: true);
    private readonly Label _strength = new() { AutoSize = true, ForeColor = Theme.TextMuted, Font = Theme.Small };

    public UnlockForm(VaultStore store)
    {
        _store = store;
        _isCreate = !store.VaultExists;

        Text = _isCreate ? "Criar cofre" : "Destravar cofre";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, _isCreate ? 350 : 230);
        TopMost = true;
        Theme.ApplyForm(this);

        var header = Theme.Header(
            _isCreate ? "Criar cofre" : "Bem-vindo de volta",
            _isCreate ? "Defina sua senha mestra" : "Informe sua senha mestra para continuar");

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 20), BackColor = Theme.Surface };

        var lblPw = MakeLabel("Senha mestra");
        lblPw.Location = new Point(2, 0);
        body.Controls.Add(lblPw);
        _password.Width = 372;
        _password.Location = new Point(0, 22);
        body.Controls.Add(_password);

        int nextTop = 22 + _password.Height + 14;

        if (_isCreate)
        {
            var lblConfirm = MakeLabel("Confirmar senha");
            lblConfirm.Location = new Point(0, nextTop);
            body.Controls.Add(lblConfirm);

            _confirm.Width = 372;
            _confirm.Location = new Point(0, nextTop + 22);
            body.Controls.Add(_confirm);

            _strength.Location = new Point(2, nextTop + 22 + _confirm.Height + 6);
            body.Controls.Add(_strength);

            var warn = new Label
            {
                Text = "⚠  Esta senha NÃO pode ser recuperada. Se esquecê-la, o cofre fica inacessível.",
                AutoSize = false,
                Width = 372,
                Height = 36,
                Location = new Point(0, nextTop + 22 + _confirm.Height + 26),
                ForeColor = Theme.Danger,
                Font = Theme.Small,
            };
            body.Controls.Add(warn);

            _password.TextChanged += (_, _) => UpdateStrength();
            nextTop = warn.Bottom + 12;
        }

        var ok = new AccentButton(_isCreate ? "Criar cofre" : "Destravar", ButtonKind.Primary) { Width = 150 };
        var cancel = new AccentButton("Cancelar", ButtonKind.Secondary) { Width = 110, DialogResult = DialogResult.Cancel };
        ok.Click += OnOk;

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 60,
            Padding = new Padding(24, 11, 24, 0),
            BackColor = Theme.Surface,
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        // Fill primeiro (igual a MainForm), depois as bordas Top/Bottom.
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
        Location = new Point(2, 0),
    };

    private void UpdateStrength()
    {
        var (text, color) = ScorePassword(_password.Text);
        _strength.Text = $"Força: {text}";
        _strength.ForeColor = color;
    }

    private static (string, Color) ScorePassword(string p)
    {
        if (p.Length == 0) return ("—", Theme.TextMuted);
        int score = 0;
        if (p.Length >= 8) score++;
        if (p.Length >= 12) score++;
        if (p.Length >= 16) score++;
        if (p.Any(char.IsUpper) && p.Any(char.IsLower)) score++;
        if (p.Any(char.IsDigit)) score++;
        if (p.Any(c => !char.IsLetterOrDigit(c))) score++;
        return score switch
        {
            <= 2 => ("fraca", Theme.Danger),
            <= 4 => ("média", Color.DarkOrange),
            _ => ("forte", Theme.Success),
        };
    }

    private void OnOk(object? sender, EventArgs e)
    {
        var pw = _password.Text;
        if (string.IsNullOrEmpty(pw)) { Fail("Informe a senha mestra."); return; }

        try
        {
            if (_isCreate)
            {
                if (pw.Length < 8) { Fail("Use ao menos 8 caracteres na senha mestra."); return; }
                if (pw != _confirm.Text) { Fail("As senhas não conferem."); return; }
                _store.Create(pw);
            }
            else
            {
                _store.Unlock(pw);
            }
            DialogResult = DialogResult.OK;
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
        _password.SelectAllAndFocus();
    }
}
