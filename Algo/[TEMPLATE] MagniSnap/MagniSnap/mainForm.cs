using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagniSnap
{
    public partial class MainForm : Form
    {
        RGBPixel[,] ImageMatrix;
        PixelGraph graph;
        ShortestPath shortestPath;

        List<Point> anchors = new List<Point>();
        List<(int x, int y)> committedPath = new List<(int x, int y)>();
        List<(int x, int y)> previewPath = new List<(int x, int y)>();

        bool isLassoEnabled;
        int lastPreviewTick;
        const int PreviewMs = 80;
        CancellationTokenSource buildCts;

        public MainForm()
        {
            InitializeComponent();
            indicator_pnl.Hide();
            mainPictureBox.Paint += mainPictureBox_Paint;
        }

        private void MainForm_Load(object sender, EventArgs e) { }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const int MaxDim = 1600;
            using (var dlg = new OpenFileDialog()) if (dlg.ShowDialog() == DialogResult.OK)
            {
                buildCts?.Cancel(); buildCts = new CancellationTokenSource(); var token = buildCts.Token;
                var loaded = ImageToolkit.OpenImage(dlg.FileName);
                if (loaded == null) { MessageBox.Show("Failed to open image."); return; }
                var w = ImageToolkit.GetWidth(loaded); var h = ImageToolkit.GetHeight(loaded);
                ImageMatrix = (Math.Max(w, h) > MaxDim) ? DownscaleImageMatrix(loaded, MaxDim) : loaded;
                ImageToolkit.ViewImage(ImageMatrix, mainPictureBox);
                mainPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                txtWidth.Text = ImageToolkit.GetWidth(ImageMatrix).ToString();
                txtHeight.Text = ImageToolkit.GetHeight(ImageMatrix).ToString();
                anchors.Clear(); committedPath.Clear(); previewPath.Clear(); isLassoEnabled = false; mainPictureBox.Invalidate();

                try
                {
                    await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        var g = new PixelGraph(ImageMatrix);
                        token.ThrowIfCancellationRequested();
                        g.ExportGraph("graph.txt");
                        token.ThrowIfCancellationRequested();
                        var sp = new ShortestPath(ImageToolkit.GetWidth(ImageMatrix), ImageToolkit.GetHeight(ImageMatrix));
                        Invoke((Action)(() => { graph = g; shortestPath = sp; }));
                    }, token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { MessageBox.Show($"Failed to build graph: {ex.Message}"); graph = null; shortestPath = null; }
                finally { buildCts = null; }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e) { anchors.Clear(); committedPath.Clear(); previewPath.Clear(); mainPictureBox.Refresh(); }

        private void btnLivewire_Click(object sender, EventArgs e) { menuButton_Click(sender, e); mainPictureBox.Cursor = Cursors.Cross; isLassoEnabled = true; }
        private void btnLivewire_Leave(object sender, EventArgs e) { mainPictureBox.Cursor = Cursors.Default; isLassoEnabled = false; }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();

        private void menuButton_Click(object sender, EventArgs e)
        {
            var c = (Control)sender; indicator_pnl.Top = c.Top; indicator_pnl.Height = c.Height; indicator_pnl.Left = c.Left;
            c.BackColor = Color.FromArgb(37, 46, 59); indicator_pnl.Show();
        }
        private void menuButton_Leave(object sender, EventArgs e) => ((Control)sender).BackColor = Color.FromArgb(26, 32, 40);

        private void mainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (!isLassoEnabled || ImageMatrix == null || shortestPath == null) return;
            var img = ControlToImage(new Point(e.X, e.Y)); if (!ValidImagePoint(img)) return;
            if (e.Button == MouseButtons.Left) { anchors.Add(img); previewPath.Clear(); if (anchors.Count >= 2) BuildCommittedPath(); }
            else if (e.Button == MouseButtons.Right && anchors.Count > 0) { anchors.RemoveAt(anchors.Count - 1); BuildCommittedPath(); previewPath.Clear(); }
            mainPictureBox.Invalidate();
        }

        private void mainPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            txtMousePosX.Text = e.X.ToString(); txtMousePosY.Text = e.Y.ToString();
            if (!isLassoEnabled || ImageMatrix == null || anchors.Count == 0 || shortestPath == null) return;
            var img = ControlToImage(new Point(e.X, e.Y)); if (!ValidImagePoint(img)) { if (previewPath.Count > 0) { previewPath.Clear(); mainPictureBox.Invalidate(); } return; }
            var tick = Environment.TickCount; if (tick - lastPreviewTick < PreviewMs) return; lastPreviewTick = tick;
            var start = anchors.Last(); if (!ValidImagePoint(start)) return;
            var (p, _, _) = shortestPath.Dijkstra((start.X, start.Y), (img.X, img.Y), graph);
            previewPath = p ?? new List<(int x, int y)>(); mainPictureBox.Invalidate();
        }

        private void mainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (mainPictureBox.Image == null) return; var g = e.Graphics;
            if (committedPath.Count > 1) using (var pen = new Pen(Color.Magenta, 2)) DrawListPath(g, committedPath, pen);
            if (previewPath.Count > 1) using (var pen = new Pen(Color.Purple, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash }) DrawListPath(g, previewPath, pen);
            using (var b = new SolidBrush(Color.Blue)) using (var p = new Pen(Color.White, 1)) using (var f = new Font("Segoe UI", 8))
                for (int i = 0; i < anchors.Count; i++) { var c = ImageToControl(anchors[i]); if (c.X < 0 || c.Y < 0) continue; var r = new Rectangle(c.X - 4, c.Y - 4, 8, 8); g.FillEllipse(b, r); g.DrawEllipse(p, r); g.DrawString((i + 1).ToString(), f, Brushes.White, c.X + 6, c.Y - 6); }
        }

        private void DrawListPath(Graphics g, List<(int x, int y)> pts, Pen pen)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                var s = ImageToControl(new Point(pts[i].x, pts[i].y)); var t = ImageToControl(new Point(pts[i + 1].x, pts[i + 1].y));
                if (s.X < 0 || t.X < 0) continue; g.DrawLine(pen, s, t);
            }
        }

        private void BuildCommittedPath()
        {
            committedPath.Clear();
            if (anchors.Count < 2 || shortestPath == null || graph == null) return;
            for (int i = 0; i + 1 < anchors.Count; i++)
            {
                var a = anchors[i]; var b = anchors[i + 1]; if (!ValidImagePoint(a) || !ValidImagePoint(b)) continue;
                var (segment, _, _) = shortestPath.Dijkstra((a.X, a.Y), (b.X, b.Y), graph); if (segment == null || segment.Count == 0) continue;
                if (committedPath.Count == 0) committedPath.AddRange(segment); else committedPath.AddRange(segment.Skip(1));
            }
        }

        private (int W, int H) ImageSize => ImageMatrix != null ? (ImageToolkit.GetWidth(ImageMatrix), ImageToolkit.GetHeight(ImageMatrix)) : (mainPictureBox.Image?.Width ?? 0, mainPictureBox.Image?.Height ?? 0);
        bool ValidImagePoint(Point p) { var (W, H) = ImageSize; return p.X >= 0 && p.Y >= 0 && p.X < W && p.Y < H; }

        private Point ControlToImage(Point ctl)
        {
            var (w, h) = ImageSize; int cw = mainPictureBox.ClientSize.Width, ch = mainPictureBox.ClientSize.Height;
            if (w == 0 || h == 0 || cw == 0 || ch == 0) return new Point(-1, -1);
            switch (mainPictureBox.SizeMode)
            {
                case PictureBoxSizeMode.StretchImage: return new Point(Clamp((int)(ctl.X * (w / (double)cw)), 0, w - 1), Clamp((int)(ctl.Y * (h / (double)ch)), 0, h - 1));
                case PictureBoxSizeMode.Zoom:
                    double ir = (double)w / h, cr = (double)cw / ch; int dw = cr < ir ? cw : (int)(ch * ir), dh = cr < ir ? (int)(cw / ir) : ch; int ox = (cw - dw) / 2, oy = (ch - dh) / 2;
                    if (ctl.X < ox || ctl.X > ox + dw - 1 || ctl.Y < oy || ctl.Y > oy + dh - 1) return new Point(-1, -1);
                    return new Point(Clamp((int)((ctl.X - ox) * (w / (double)dw)), 0, w - 1), Clamp((int)((ctl.Y - oy) * (h / (double)dh)), 0, h - 1));
                case PictureBoxSizeMode.CenterImage:
                    int cx = ctl.X - (cw - w) / 2, cy = ctl.Y - (ch - h) / 2; return ValidImagePoint(new Point(cx, cy)) ? new Point(cx, cy) : new Point(-1, -1);
                default: return new Point(Clamp(ctl.X, 0, w - 1), Clamp(ctl.Y, 0, h - 1));
            }
        }

        private Point ImageToControl(Point imgPt)
        {
            var (w, h) = ImageSize; int cw = mainPictureBox.ClientSize.Width, ch = mainPictureBox.ClientSize.Height;
            if (w == 0 || h == 0) return imgPt;
            switch (mainPictureBox.SizeMode)
            {
                case PictureBoxSizeMode.StretchImage: return new Point((int)(imgPt.X * (cw / (double)w)), (int)(imgPt.Y * (ch / (double)h)));
                case PictureBoxSizeMode.Zoom:
                    double ir = (double)w / h, cr = (double)cw / ch; int dw = cr < ir ? cw : (int)(ch * ir), dh = cr < ir ? (int)(cw / ir) : ch; int ox = (cw - dw) / 2, oy = (ch - dh) / 2;
                    return new Point((int)(imgPt.X * (dw / (double)w) + ox), (int)(imgPt.Y * (dh / (double)h) + oy));
                case PictureBoxSizeMode.CenterImage: return new Point(imgPt.X + (cw - w) / 2, imgPt.Y + (ch - h) / 2);
                default: return imgPt;
            }
        }

        private RGBPixel[,] DownscaleImageMatrix(RGBPixel[,] src, int maxDim)
        {
            int srcW = src.GetLength(0), srcH = src.GetLength(1);
            if (Math.Max(srcW, srcH) <= maxDim) return src;
            double scale = maxDim / (double)Math.Max(srcW, srcH);
            int tw = Math.Max(1, (int)Math.Round(srcW * scale)), th = Math.Max(1, (int)Math.Round(srcH * scale));
            var dst = new RGBPixel[tw, th];
            for (int y = 0; y < th; y++) { int sy = Math.Min(srcH - 1, Math.Max(0, (int)Math.Floor(y / scale))); for (int x = 0; x < tw; x++) { int sx = Math.Min(srcW - 1, Math.Max(0, (int)Math.Floor(x / scale))); dst[x, y] = src[sx, sy]; } }
            return dst;
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}