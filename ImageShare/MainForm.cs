using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageShare
{
	public partial class MainForm : Form
	{
		private readonly SolidBrush fillBrush = new SolidBrush(Color.FromArgb(120, Color.Gray));
		private readonly Rectangle screenSize;
		private Point[] selection = { new Point(0, 0), new Point(0, 0) };

		public MainForm()
		{
			InitializeComponent();
			screenSize = Screen.PrimaryScreen.Bounds;
			BackgroundImage = CaptureDesktop(screenSize);
		}

		private Bitmap CaptureDesktop(Rectangle size)
		{
			var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppRgb);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
			}
			return bitmap;
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			fillBrush?.Dispose();
		}

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

		private void MainForm_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				selection[0] = e.Location;
				selection[1] = e.Location;
				Invalidate();
			}
		}

		private void MainForm_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				selection[1] = e.Location;
				Invalidate();
			}
		}

		private void MainForm_Paint(object sender, PaintEventArgs e)
		{
			if (selection[0].X == selection[1].X || selection[0].Y == selection[1].Y)
			{
				e.Graphics.FillRectangle(fillBrush, screenSize);
			}
			else
			{
				int top, bottom, left, right;
				FindSelectionBounds(out top, out bottom, out left, out right);
				var selectionRect = new Rectangle(left, top, right - left, bottom - top);
				using (var region = new Region(screenSize))
				{
					using (var path = new GraphicsPath())
					{
						path.AddRectangle(selectionRect);
						region.Exclude(path);
						e.Graphics.FillRegion(fillBrush, region);
					}
				}
				e.Graphics.DrawRectangle(Pens.Black, selectionRect);
			}
		}

		private void FindSelectionBounds(out int top, out int bottom, out int left, out int right)
		{
			if (selection[0].X < selection[1].X)
			{
				left = selection[0].X;
				right = selection[1].X;
			}
			else
			{
				left = selection[1].X;
				right = selection[0].X;
			}
			if (selection[0].Y < selection[1].Y)
			{
				top = selection[0].Y;
				bottom = selection[1].Y;
			}
			else
			{
				top = selection[1].Y;
				bottom = selection[0].Y;
			}
		}
	}
}

