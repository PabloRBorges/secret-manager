using System.Drawing.Drawing2D;

namespace SecretManager.UI;

public enum ButtonKind { Primary, Secondary, Danger, Ghost }

/// <summary>Botao chato, arredondado, com estados de hover/press — substitui o Button cinza padrao.</summary>
public sealed class AccentButton : Button
{
    private bool _hover;
    private bool _pressed;
    public int Radius { get; set; } = 8;

    private readonly ButtonKind _kind;

    public AccentButton(string text, ButtonKind kind = ButtonKind.Primary)
    {
        _kind = kind;
        Text = text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.Transparent;
        Font = new Font("Segoe UI Semibold", 9.5f);
        Cursor = Cursors.Hand;
        AutoSize = false;
        Height = 38;
        MinimumSize = new Size(96, 38);
        Padding = new Padding(14, 0, 14, 0);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.RoundedRect(rect, Radius);

        var (fill, fg, border) = Colors();

        if (Parent is not null)
            g.Clear(Parent.BackColor);

        if (fill != Color.Transparent)
        {
            using var b = new SolidBrush(fill);
            g.FillPath(b, path);
        }
        if (border != Color.Transparent)
        {
            using var p = new Pen(border, 1.4f);
            g.DrawPath(p, path);
        }

        TextRenderer.DrawText(g, Text, Font, rect, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private (Color fill, Color fg, Color border) Colors()
    {
        switch (_kind)
        {
            case ButtonKind.Primary:
                var pa = _pressed ? Theme.AccentPressed : _hover ? Theme.AccentHover : Theme.Accent;
                return (pa, Color.White, Color.Transparent);
            case ButtonKind.Danger:
                var da = _pressed ? Theme.DangerHover : _hover ? Theme.DangerHover : Theme.Danger;
                return (da, Color.White, Color.Transparent);
            case ButtonKind.Secondary:
                var sf = _hover ? Theme.RowHover : Theme.Surface;
                return (sf, Theme.Text, Theme.Border);
            case ButtonKind.Ghost:
            default:
                var gf = _hover ? Theme.RowHover : Color.Transparent;
                return (gf, Theme.Accent, Color.Transparent);
        }
    }
}
