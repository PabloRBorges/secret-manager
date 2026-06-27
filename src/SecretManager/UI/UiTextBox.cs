using System.Drawing.Drawing2D;

namespace SecretManager.UI;

/// <summary>
/// Campo de texto com borda arredondada suave e realce no foco — embrulha um
/// TextBox sem borda dentro de um painel pintado. Substitui a caixa cinza padrao.
/// </summary>
public sealed class UiTextBox : Panel
{
    private readonly TextBox _tb;
    private bool _focused;

    public UiTextBox(bool password = false, bool multiline = false)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.FieldBackground;
        Padding = new Padding(11, 9, 11, 9);

        _tb = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10f),
            BackColor = Theme.FieldBackground,
            ForeColor = Theme.Text,
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = password,
            Multiline = multiline,
        };
        if (multiline)
        {
            _tb.ScrollBars = ScrollBars.Vertical;
            _tb.AcceptsReturn = true;
            Height = 78;
        }
        else
        {
            Height = 38;
        }

        _tb.GotFocus += (_, _) => { _focused = true; Invalidate(); };
        _tb.LostFocus += (_, _) => { _focused = false; Invalidate(); };

        Controls.Add(_tb);
    }

    public override string Text
    {
        get => _tb.Text;
        set => _tb.Text = value;
    }

    public string PlaceholderText
    {
        get => _tb.PlaceholderText;
        set => _tb.PlaceholderText = value;
    }

    public bool UseSystemPasswordChar
    {
        get => _tb.UseSystemPasswordChar;
        set => _tb.UseSystemPasswordChar = value;
    }

    public TextBox Inner => _tb;

    public new event EventHandler? TextChanged
    {
        add => _tb.TextChanged += value;
        remove => _tb.TextChanged -= value;
    }

    public void SelectAllAndFocus()
    {
        _tb.SelectAll();
        _tb.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (Parent is not null) g.Clear(Parent.BackColor);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(rect, 8);
        using (var fill = new SolidBrush(Theme.FieldBackground))
            g.FillPath(fill, path);
        using var pen = new Pen(_focused ? Theme.BorderFocus : Theme.Border, _focused ? 1.6f : 1.2f);
        g.DrawPath(pen, path);
    }
}
