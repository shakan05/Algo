using System;
using System.Drawing;
using System.Windows.Forms;

namespace MagniSnap
{
    #region
    /// 4d17639adfad0a300acd78759e07a4f2
    #endregion
    public partial class MainForm : Form
    {

        RGBPixel[,] ImageMatrix;
        bool isLassoEnabled = false;
        PixelGraph graph;
        ShortestPath shortestPath;
        (int x, int y)[,] parent;
        double[,] dist;
        int anchorX, anchorY;
        bool hasAnchor = false;

        public MainForm()
        {
            InitializeComponent();
            indicator_pnl.Hide();
        }

        private void menuButton_Click(object sender, EventArgs e)
        {
            #region Do Change Remove Template Code
            /// 4d17639adfad0a300acd78759e07a4f2
            #endregion

            indicator_pnl.Top = ((Control)sender).Top;
            indicator_pnl.Height = ((Control)sender).Height;
            indicator_pnl.Left = ((Control)sender).Left;
            ((Control)sender).BackColor = Color.FromArgb(37, 46, 59);
            indicator_pnl.Show();
        }

        private void menuButton_Leave(object sender, EventArgs e)
        {
            ((Control)sender).BackColor = Color.FromArgb(26, 32, 40);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {         
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string OpenedFilePath = openFileDialog1.FileName;

                ImageMatrix = ImageToolkit.OpenImage(OpenedFilePath);
                ImageToolkit.ViewImage(ImageMatrix, mainPictureBox);

                graph = new PixelGraph(ImageMatrix);
                graph.ExportGraph("graph.txt");

                shortestPath = new ShortestPath(
                    ImageToolkit.GetWidth(ImageMatrix),
                    ImageToolkit.GetHeight(ImageMatrix)
                );

                txtWidth.Text = ImageToolkit.GetWidth(ImageMatrix).ToString();
                txtHeight.Text = ImageToolkit.GetHeight(ImageMatrix).ToString();
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainPictureBox.Refresh();
        }

        private void btnLivewire_Click(object sender, EventArgs e)
        {
            menuButton_Click(sender, e);

            mainPictureBox.Cursor = Cursors.Cross;

            isLassoEnabled = true;
        }

        private void btnLivewire_Leave(object sender, EventArgs e)
        {
            menuButton_Leave(sender, e);

            mainPictureBox.Cursor = Cursors.Default;
            isLassoEnabled = false;
        }

        private void mainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left &&
       isLassoEnabled &&
       ImageMatrix != null)
            {
                anchorX = e.X;
                anchorY = e.Y;
                hasAnchor = true;

                // 🔥 THIS IS THE CALL YOU WERE ASKING ABOUT
                (parent, dist) = shortestPath.Dijkstra(
                    (anchorX, anchorY),
                    (e.X, e.Y),   // destination (can be same initially)
                    graph
                );
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void mainPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            txtMousePosX.Text = e.X.ToString();
            txtMousePosY.Text = e.Y.ToString();

            if (ImageMatrix != null && isLassoEnabled)
            {
                // Refresh to redraw points
                mainPictureBox.Refresh();
            }
        }
    }
}
