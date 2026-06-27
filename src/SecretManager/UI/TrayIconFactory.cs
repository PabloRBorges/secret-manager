using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SecretManager.UI;

/// <summary>Desenha um icone de cadeado em runtime para o tray (sem arquivo .ico embarcado).</summary>
public static class TrayIconFactory
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon CreateLockIcon(bool locked)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var body = locked ? Color.FromArgb(38, 132, 255) : Color.FromArgb(120, 130, 140);
            using var bodyBrush = new SolidBrush(body);
            using var arcPen = new Pen(body, 3f);

            // Arco do cadeado (aberto quando destravado)
            var arcRect = new Rectangle(9, 4, 14, 16);
            if (locked)
                g.DrawArc(arcPen, arcRect, 180, 180);
            else
                g.DrawArc(arcPen, arcRect, 150, 150);

            // Corpo do cadeado
            g.FillRectangle(bodyBrush, 7, 14, 18, 14);

            // Buraco da fechadura
            using var holeBrush = new SolidBrush(Color.White);
            g.FillEllipse(holeBrush, 14, 18, 4, 4);
            g.FillRectangle(holeBrush, 15, 20, 2, 5);
        }

        IntPtr hIcon = bmp.GetHicon();
        try
        {
            // Clona para um Icon gerenciado e destroi o handle nativo.
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }
}
