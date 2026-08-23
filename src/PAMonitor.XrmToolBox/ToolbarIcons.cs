using System.Drawing;
using System.Drawing.Drawing2D;

namespace PAMonitor.XrmToolBox
{
    /// <summary>
    /// 16×16 toolbar icons drawn in code (no external image assets).
    /// </summary>
    internal static class ToolbarIcons
    {
        public static Image RefreshSolutions { get; } = Create(DrawRefreshSolutions);
        public static Image RefreshFlows { get; } = Create(DrawRefreshFlows);
        public static Image RefreshRuns { get; } = Create(DrawRefreshRuns);
        public static Image ExpandFailures { get; } = Create(DrawExpandFailures);
        public static Image OpenRun { get; } = Create(DrawOpenRun);
        public static Image Copy { get; } = Create(DrawCopy);

        private delegate void IconDrawer(Graphics g);

        private static Image Create(IconDrawer draw)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                draw(g);
            }

            return bmp;
        }

        private static void DrawRefreshSolutions(Graphics g)
        {
            // Package / solution box
            using (var pen = new Pen(Color.FromArgb(70, 110, 170), 1.2f))
            using (var fill = new SolidBrush(Color.FromArgb(210, 228, 248)))
            {
                var box = new RectangleF(1.5f, 3.5f, 9f, 9f);
                g.FillRectangle(fill, box);
                g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
                g.DrawLine(pen, 1.5f, 6.5f, 10.5f, 6.5f);
                g.DrawLine(pen, 6f, 3.5f, 6f, 12.5f);
            }

            DrawRefreshBadge(g, 8.5f, 8.5f, Color.FromArgb(40, 120, 200));
        }

        private static void DrawRefreshFlows(Graphics g)
        {
            // Flow nodes connected by lines
            using (var pen = new Pen(Color.FromArgb(20, 130, 120), 1.4f))
            using (var fill = new SolidBrush(Color.FromArgb(20, 130, 120)))
            {
                g.FillEllipse(fill, 2f, 2f, 3.5f, 3.5f);
                g.FillEllipse(fill, 2f, 10.5f, 3.5f, 3.5f);
                g.FillEllipse(fill, 8.5f, 6f, 3.5f, 3.5f);
                g.DrawLine(pen, 4f, 5.2f, 8.8f, 7.2f);
                g.DrawLine(pen, 4f, 11.2f, 8.8f, 8.5f);
            }

            DrawRefreshBadge(g, 10f, 1.5f, Color.FromArgb(0, 140, 130));
        }

        private static void DrawRefreshRuns(Graphics g)
        {
            // Play triangle + circular refresh
            using (var fill = new SolidBrush(Color.FromArgb(50, 140, 70)))
            {
                PointF[] play =
                {
                    new PointF(2.5f, 3f),
                    new PointF(2.5f, 13f),
                    new PointF(9.5f, 8f)
                };
                g.FillPolygon(fill, play);
            }

            DrawRefreshBadge(g, 9f, 8.5f, Color.FromArgb(40, 120, 60));
        }

        private static void DrawExpandFailures(Graphics g)
        {
            // Collapsed tree branch expanding downward
            using (var pen = new Pen(Color.FromArgb(90, 90, 90), 1.3f))
            {
                g.DrawLine(pen, 3f, 2.5f, 3f, 12.5f);
                g.DrawLine(pen, 3f, 5f, 7.5f, 5f);
                g.DrawLine(pen, 3f, 9f, 7.5f, 9f);
                g.DrawLine(pen, 3f, 12.5f, 7.5f, 12.5f);
            }

            using (var fill = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                // Expand chevrons
                PointF[] c1 = { new PointF(8.5f, 4f), new PointF(11.5f, 5.5f), new PointF(8.5f, 7f) };
                PointF[] c2 = { new PointF(8.5f, 8f), new PointF(11.5f, 9.5f), new PointF(8.5f, 11f) };
                g.FillPolygon(fill, c1);
                g.FillPolygon(fill, c2);
            }

            // Failure mark
            using (var brush = new SolidBrush(Color.FromArgb(190, 45, 45)))
            {
                g.FillEllipse(brush, 11.5f, 1f, 4f, 4f);
            }

            using (var pen = new Pen(Color.White, 1.1f))
            {
                g.DrawLine(pen, 12.4f, 1.9f, 14.6f, 4.1f);
                g.DrawLine(pen, 14.6f, 1.9f, 12.4f, 4.1f);
            }
        }

        private static void DrawOpenRun(Graphics g)
        {
            // Window with external-link arrow
            using (var pen = new Pen(Color.FromArgb(50, 90, 160), 1.3f))
            using (var fill = new SolidBrush(Color.FromArgb(230, 238, 250)))
            {
                var box = new RectangleF(1.5f, 4f, 8.5f, 9.5f);
                g.FillRectangle(fill, box);
                g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
                g.DrawLine(pen, 1.5f, 6.5f, 10f, 6.5f);
            }

            using (var pen = new Pen(Color.FromArgb(30, 100, 190), 1.5f)
            {
                EndCap = LineCap.ArrowAnchor,
                StartCap = LineCap.Round
            })
            {
                g.DrawLine(pen, 7.5f, 8.5f, 13.5f, 2.5f);
            }

            using (var pen = new Pen(Color.FromArgb(30, 100, 190), 1.4f))
            {
                g.DrawLine(pen, 10.5f, 2.5f, 13.5f, 2.5f);
                g.DrawLine(pen, 13.5f, 2.5f, 13.5f, 5.5f);
            }
        }

        private static void DrawCopy(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(80, 80, 90), 1.2f))
            using (var back = new SolidBrush(Color.FromArgb(245, 245, 248)))
            using (var front = new SolidBrush(Color.White))
            {
                var rear = new RectangleF(2f, 2f, 8f, 9.5f);
                var frontBox = new RectangleF(5f, 5f, 8f, 9.5f);
                g.FillRectangle(back, rear);
                g.DrawRectangle(pen, rear.X, rear.Y, rear.Width, rear.Height);
                g.FillRectangle(front, frontBox);
                g.DrawRectangle(pen, frontBox.X, frontBox.Y, frontBox.Width, frontBox.Height);
            }
        }

        private static void DrawRefreshBadge(Graphics g, float x, float y, Color color)
        {
            using (var pen = new Pen(color, 1.35f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            })
            {
                var rect = new RectangleF(x, y, 5.5f, 5.5f);
                g.DrawArc(pen, rect, 40f, 250f);
            }
        }
    }
}
