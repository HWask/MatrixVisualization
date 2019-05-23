using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace MatrixVisualization
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            panel1.MouseWheel += new MouseEventHandler(panel1_MouseWheel);
            pictureBox1.MouseDown += new MouseEventHandler(pictureBox1_MouseDown);
            pictureBox1.MouseUp += new MouseEventHandler(pictureBox1_MouseUp);
            pictureBox1.MouseMove += new MouseEventHandler(pictureBox1_MouseMove);
            pictureBox1.Cursor = Cursors.Hand;

            pictureBox1.Location = new Point(panel1.Width / 2 - pictureBox1.Size.Width / 2, panel1.Height / 2 - pictureBox1.Size.Height / 2);
        }

        private int MaxMemoryUsage = 5240;
        private List<Color> matrixColors;
        private List<int> matrixIntegers;

        private int GridWidth = 0;
        private int X, Y;


        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                pictureBox1.Capture = true;
                X = e.X;
                Y = e.Y;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pictureBox1.Capture = false;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                var x = e.X - X;
                var y = e.Y - Y;

                pictureBox1.Location = new Point(pictureBox1.Left + x, pictureBox1.Top + y);
            }
        }

        private void panel1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                var CurrentCellSize = CalculateCellSizeToGivenImageSize(pictureBox1.Image.Width,
                                      Convert.ToInt32(Math.Sqrt(matrixColors.Count)), GridWidth);

                if (e.Delta > 0)
                    CurrentCellSize++;
                else
                    CurrentCellSize--;

                if (CurrentCellSize == 0)
                {
                    MessageBox.Show("Cannot zoom out any further.", "Matrix Visualization");
                    return;
                }
                
                if(CalculateImageWidth(CurrentCellSize, Convert.ToInt32(Math.Sqrt(matrixColors.Count))) >= 32767)
                {
                    MessageBox.Show("Cannot zoom in any further.", "Matrix Visualization");
                    return;
                }

                //var memory = CalculateImageSize(CurrentCellSize, Convert.ToInt32(Math.Sqrt(matrixColors.Count)));
                if (CalculateImageSize(CurrentCellSize, Convert.ToInt32(Math.Sqrt(matrixColors.Count))) > MaxMemoryUsage)
                {
                    MessageBox.Show("Cannot zoom in any further.", "Matrix Visualization");
                    return;
                }

                var form = new Form2();
                Task.Factory.StartNew(new Action(() =>
                    {
                        float xP = e.X - pictureBox1.Location.X;
                        float yP = e.Y - pictureBox1.Location.Y;
                        float rX = xP / pictureBox1.Width;
                        float rY = yP / pictureBox1.Height;

                        pictureBox1.Image.Dispose();
                        pictureBox1.Image = null;

                        var bmp = DrawVisualisation(CurrentCellSize, Convert.ToInt32(Math.Sqrt(matrixColors.Count)), GridWidth, matrixColors);

                        rY *= bmp.Height;
                        rX *= bmp.Width;

                        BeginInvoke(new Action(() =>
                            {
                                pictureBox1.Location = new Point(Convert.ToInt32(e.X - rX), Convert.ToInt32(e.Y - rY));
                                pictureBox1.Size = new Size(bmp.Width, bmp.Height);
                                pictureBox1.Image = bmp;
                                form.DialogResult = System.Windows.Forms.DialogResult.OK;
                            }));
                    }));
                form.ShowDialog();
            }
        }

        private int CalculateImageSize(int CellSize, int Cells)
        {
            int imgSize = (CellSize + GridWidth) * Cells + 50;
            return Convert.ToInt32(Math.Ceiling((Math.Pow(imgSize, 2) * 4) / Math.Pow(10, 6)));
        }

        private int CalculateImageWidth(int CellSize, int Cells)
        {
            return (CellSize + GridWidth) * Cells + 50;
        }

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            panel1.Focus();
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            panel1.Focus();
        }

        private void ChooseFile()
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Select a matrix to visualize";
            ofd.Filter = "csv files (*.csv)|*.csv";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var form = new Form2();
                Task.Factory.StartNew(new Action(() => Thread(ofd.FileName, form)));
                form.ShowDialog();
            }
        }

        private void Thread(String fileName, Form2 form)
        {
            ProcessMatrix(fileName);
            int panelSize = Math.Min(panel1.Size.Width, panel1.Size.Height);
            int cellSize = CalculateCellSizeToGivenImageSize(panelSize, Convert.ToInt32(Math.Sqrt(matrixColors.Count)), GridWidth);
            if (cellSize <= 0)
                cellSize = 1;

            var bmp = DrawVisualisation(cellSize, Convert.ToInt32(Math.Sqrt(matrixColors.Count)), GridWidth, matrixColors);
            matrixIntegers = null;

            BeginInvoke(new Action(() =>
            {
                pictureBox1.Location = new Point(panel1.Width / 2 - bmp.Width / 2, panel1.Height / 2 - bmp.Height / 2);
                pictureBox1.Size = new Size(bmp.Width, bmp.Height);
                pictureBox1.Image = bmp;
                pictureBox1.BackgroundImage = null;
                form.DialogResult = DialogResult.OK;
            }));
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Location = new Point(panel1.Width / 2 - pictureBox1.Size.Width / 2, panel1.Height / 2 - pictureBox1.Size.Height / 2);
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                ChooseFile();
            }
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ChooseFile();
            }
        }

        private Bitmap CreateWhiteBackgroundPicture(int width, int height)
        {
            var img = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using(var g = Graphics.FromImage(img))
            {
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, width, height));
            }

            return img;
        }

        private Bitmap DrawVisualisation(int cellSize, int cells, int GridWidth, List<Color> colors)
        {
            int imgSize = (cellSize + GridWidth) * cells;
            Color[] arrayColors = colors.ToArray();

            //25 as padding for top left right bottom
            var img = CreateWhiteBackgroundPicture(imgSize + 50, imgSize + 50);

            using (var g = Graphics.FromImage(img))
            {
                var pen = new Pen(Brushes.Red, GridWidth);

                for (int i = 25, j = 0; j < cells; i += (cellSize + GridWidth), j++)
                    for (int k = 25, l = 0; l < cells; k += (cellSize + GridWidth), l++)
                    {
                        g.FillRectangle(new SolidBrush(arrayColors[l * cells + j]), new Rectangle(i, k, cellSize, cellSize));
                    }

                if (GridWidth > 0)
                {
                    for (int i = 25, j = 0; j <= cells; i += (cellSize + GridWidth), j++)
                    {
                        g.DrawLine(pen, 25, i, imgSize + 25, i);
                        g.DrawLine(pen, i, 25, i, imgSize + 25);
                    }
                }
            }

            return img;
        }

        private int CalculateCellSizeToGivenImageSize(int size, int cells, int gridWidth)
        {
            //25 padding on each side
            var cellSize = (float)(size-50) / (float)cells - gridWidth;

            return Convert.ToInt32(Math.Floor(cellSize));
        }

        //x in a,b
        private float MapIntervalLinearly(float a, float b, float c, float d, float x)
        {
            float alpha = (c - d) / (a - b);
            float beta = c - alpha * a;

            return alpha * x + beta;
        }

        private Color GrayToRGB(float gray)
        {
            int g = Convert.ToInt32(gray);

            return Color.FromArgb(g, g, g);
        }

        private void ProcessMatrix(String fileName)
        {
            bool once = true;
            int columns = 0;
            int rows;

            matrixColors = new List<Color>();
            matrixIntegers = new List<int>();

            var lines = File.ReadAllLines(fileName);
            rows = lines.Length;

            foreach(var line in lines)
            {
                var split = line.Split(',');
                if (once)
                {
                    columns = split.Length;
                    once = false;
                }

                foreach(var data in split)
                {
                    var numeric = Convert.ToInt32(data);
                    matrixIntegers.Add(numeric);
                }
            }

            var max = matrixIntegers.Max();
            var min = matrixIntegers.Min();

            foreach(var numeric in matrixIntegers)
            {
                var mapped = MapIntervalLinearly(min, max, 178, 360, numeric);
                matrixColors.Add(HSVtoRGB(mapped, 1, 1));
            }


            if (rows != columns)
            {
                MessageBox.Show("The matrix is not quadratic!", "Matrix Visualization", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            //MessageBox.Show("Matrix has " + rows + " rows and " + columns + " columns", "Matrix Visualization");
        }

        Color HSVtoRGB(float h, float s, float v)
        {
            int i;
            float f, p, q, t;
            if (s == 0)
            {
                // achromatic (grey)
                return Color.FromArgb((int)(v * 255), (int)(v * 255), (int)(v * 255));
            }
            h /= 60;			// sector 0 to 5
            i = (int)Math.Floor(h);
            f = h - i;			// factorial part of h
            p = v * (1 - s);
            q = v * (1 - s * f);
            t = v * (1 - s * (1 - f));

            switch (i)
            {
                case 0:
                    return Color.FromArgb((int)(v * 255), (int)(t * 255), (int)(p * 255));
                case 1:
                    return Color.FromArgb((int)(q * 255), (int)(v * 255), (int)(p * 255));
                case 2:
                    return Color.FromArgb((int)(p * 255), (int)(v * 255), (int)(t * 255));
                case 3:
                    return Color.FromArgb((int)(p * 255), (int)(q * 255), (int)(v * 255));
                case 4:
                    return Color.FromArgb((int)(t * 255), (int)(p * 255), (int)(v * 255));
                default:
                    return Color.FromArgb((int)(v * 255), (int)(p * 255), (int)(q * 255));
            }
        }
    }
}
