using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Zeichnet das Tray-Icon zur Laufzeit: die Wochennummer auf einem farbigen
    /// abgerundeten Quadrat. So ist die KW direkt im Tray ablesbar, ohne den
    /// Tooltip aufklappen zu müssen - und es wird keine Icon-Datei pro Woche gebraucht.
    /// </summary>
    internal static class TrayIconFactory
    {
        private static readonly Color BackgroundColor = Color.FromArgb(255, 0, 120, 212); // Windows-Blau
        private static readonly Color ForegroundColor = Color.White;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

        /// <summary>
        /// Erzeugt ein Icon in Tray-Größe mit der übergebenen Wochennummer.
        /// Der Aufrufer ist für Dispose() zuständig.
        /// </summary>
        public static Icon CreateWeekIcon(int week)
        {
            // SmallIconSize folgt der DPI-Skalierung: 16px bei 100%, 20px bei 125% usw.
            Size iconSize = SystemInformation.SmallIconSize;
            return CreateWeekIcon(week, Math.Max(16, Math.Max(iconSize.Width, iconSize.Height)));
        }

        /// <summary>
        /// Wie <see cref="CreateWeekIcon(int)"/>, aber mit fester Kantenlänge in Pixeln.
        /// </summary>
        public static Icon CreateWeekIcon(int week, int size)
        {
            using (var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.Clear(Color.Transparent);

                    DrawBackground(graphics, size);
                    DrawWeekNumber(graphics, week, size);
                }

                return CreateIconFromBitmap(bitmap);
            }
        }

        private static void DrawBackground(Graphics graphics, int size)
        {
            // Halbes Pixel Rand, damit die Antialiasing-Kante nicht abgeschnitten wird.
            var bounds = new RectangleF(0.5f, 0.5f, size - 1f, size - 1f);

            using (GraphicsPath path = CreateRoundedRectangle(bounds, size * 0.22f))
            using (var brush = new SolidBrush(BackgroundColor))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static void DrawWeekNumber(Graphics graphics, int week, int size)
        {
            string text = week.ToString(System.Globalization.CultureInfo.InvariantCulture);

            using (var family = new FontFamily("Segoe UI"))
            using (var path = new GraphicsPath())
            {
                // In beliebiger Größe zeichnen und danach exakt einpassen - das
                // vermeidet die Ausrichtungsprobleme von MeasureString bei kleinen Fonts.
                path.AddString(text, family, (int)FontStyle.Bold, 100f, PointF.Empty, StringFormat.GenericTypographic);

                RectangleF ink = path.GetBounds();
                if (ink.Width <= 0f || ink.Height <= 0f)
                {
                    return;
                }

                // Zielfläche: zweistellige Zahlen dürfen breiter, aber nicht höher werden.
                float targetWidth = size * (text.Length > 1 ? 0.80f : 0.46f);
                float targetHeight = size * 0.62f;
                float scale = Math.Min(targetWidth / ink.Width, targetHeight / ink.Height);

                using (var transform = new Matrix())
                {
                    transform.Translate(size / 2f, size / 2f);
                    transform.Scale(scale, scale);
                    transform.Translate(-(ink.X + ink.Width / 2f), -(ink.Y + ink.Height / 2f));
                    path.Transform(transform);
                }

                using (var brush = new SolidBrush(ForegroundColor))
                {
                    graphics.FillPath(brush, path);
                }
            }
        }

        private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
        {
            float diameter = radius * 2f;
            var path = new GraphicsPath();

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// Icon.FromHandle besitzt das Handle nicht. Deshalb einmal klonen (das erzeugt
        /// ein eigenständiges Icon) und das GDI-Handle danach sofort freigeben.
        /// </summary>
        private static Icon CreateIconFromBitmap(Bitmap bitmap)
        {
            IntPtr handle = bitmap.GetHicon();

            try
            {
                using (Icon unowned = Icon.FromHandle(handle))
                {
                    return (Icon)unowned.Clone();
                }
            }
            finally
            {
                DestroyIcon(handle);
            }
        }
    }
}
