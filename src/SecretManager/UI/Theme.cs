using System.Drawing.Drawing2D;

namespace SecretManager.UI;

/// <summary>Paleta, fontes e helpers de estilo compartilhados por todas as telas.</summary>
public static class Theme
{
    // Paleta (claro, moderno) — o azul casa com o icone do tray.
    public static readonly Color Accent = Color.FromArgb(0x26, 0x84, 0xFF);
    public static readonly Color AccentHover = Color.FromArgb(0x12, 0x6B, 0xE6);
    public static readonly Color AccentPressed = Color.FromArgb(0x0E, 0x59, 0xC2);

    public static readonly Color AppBackground = Color.FromArgb(0xF4, 0xF6, 0xFA);
    public static readonly Color Surface = Color.White;
    public static readonly Color HeaderText = Color.White;

    public static readonly Color Text = Color.FromArgb(0x1B, 0x21, 0x33);
    public static readonly Color TextMuted = Color.FromArgb(0x6B, 0x77, 0x88);
    public static readonly Color Border = Color.FromArgb(0xDD, 0xE2, 0xEA);
    public static readonly Color BorderFocus = Accent;
    public static readonly Color FieldBackground = Color.White;
    public static readonly Color RowHover = Color.FromArgb(0xEF, 0xF4, 0xFF);
    public static readonly Color RowSelected = Color.FromArgb(0xDD, 0xEA, 0xFF);
    public static readonly Color RowAlt = Color.FromArgb(0xFB, 0xFC, 0xFE);

    public static readonly Color Danger = Color.FromArgb(0xE5, 0x48, 0x4D);
    public static readonly Color DangerHover = Color.FromArgb(0xC8, 0x3A, 0x3F);
    public static readonly Color Success = Color.FromArgb(0x2E, 0x9E, 0x6B);

    // Fontes
    public static Font Base => new("Segoe UI", 9.75f);
    public static Font Bold => new("Segoe UI Semibold", 9.75f);
    public static Font Title => new("Segoe UI Semibold", 15f);
    public static Font Subtitle => new("Segoe UI", 9f);
    public static Font Small => new("Segoe UI", 8.5f);

    /// <summary>Aplica fundo, fonte e suavizacao basica a um formulario.</summary>
    public static void ApplyForm(Form form)
    {
        form.BackColor = Surface;
        form.Font = Base;
        form.ForeColor = Text;
    }

    /// <summary>Faixa de cabecalho com gradiente de destaque, titulo e subtitulo.</summary>
    public static Panel Header(string title, string? subtitle = null, int height = 72)
    {
        var panel = new GradientPanel
        {
            Dock = DockStyle.Top,
            Height = height,
            Padding = new Padding(20, 0, 20, 0),
        };

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = subtitle is null ? 1 : 2,
            BackColor = Color.Transparent,
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblTitle = new Label
        {
            Text = title,
            Font = Title,
            ForeColor = HeaderText,
            AutoSize = true,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, subtitle is null ? 0 : 14, 0, 0),
        };
        stack.Controls.Add(lblTitle, 0, 0);

        if (subtitle is not null)
        {
            var lblSub = new Label
            {
                Text = subtitle,
                Font = Subtitle,
                ForeColor = Color.FromArgb(220, 235, 255),
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, 12),
            };
            stack.Controls.Add(lblSub, 0, 1);
        }

        panel.Controls.Add(stack);
        return panel;
    }

    public static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0) { path.AddRectangle(r); return path; }
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Painel com gradiente diagonal de destaque (cabecalho).</summary>
public sealed class GradientPanel : Panel
{
    public GradientPanel() => DoubleBuffered = true;

    protected override void OnPaint(PaintEventArgs e)
    {
        var rect = ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0) return;
        using var brush = new LinearGradientBrush(
            rect,
            Theme.Accent,
            Theme.AccentPressed,
            LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(brush, rect);
    }
}
