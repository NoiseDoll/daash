using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace ImageShare
{

    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        private Point[] selection = { new Point(0, 0), new Point(0, 0) };
        private readonly Rectangle screenSize;
        private bool isPressed = false;
        private bool isMousePressed = false;
        private int keyId = 0;

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public MainForm()
        {
            InitializeComponent();
            // TODO Add support for multiple desktops
            screenSize = Screen.PrimaryScreen.Bounds;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            // Hide tastbar icon
            ShowIcon = false;
            ShowInTaskbar = false;

            // TODO Make tray application controlls

            TransparencyKey = Color.Red;
            BackColor = Color.Red;
            Opacity = 0;

            // TODO Make custom shortcuts + shortcut parser
            RegisterHotKey(Handle, keyId, (int)(KeyModifier.Shift | KeyModifier.Alt), Keys.A.GetHashCode());

            // TODO Make two options whether it to pause the screen or not
            // BackgroundImage = CaptureDesktop(screenSize);
        }


        protected override void WndProc(ref Message m)
        {
            const uint WM_HOTKEY = 0x0312;
            const uint WM_NCHITTEST = 0x84;
            const uint WM_MOUSEMOVE = 0x0200;
            const uint WM_LBUTTONDOWN = 0x0201;
            const uint WM_LBUTTONUP = 0x0202;

            const int HTTRANSPARENT = -1;
            const int HTCLIENT = 1;

            if (m.Msg == WM_HOTKEY)
            {
                isPressed = true;
                Opacity = 1;
                return;
            }

            if (m.Msg == WM_LBUTTONDOWN)
            {
                if (isPressed)
                {
                    isMousePressed = true;
                    selection[0] = MousePosition;
                    selection[1] = MousePosition;
                    Invalidate();
                }
            }

            if (m.Msg == WM_LBUTTONUP)
            {
                if (isPressed)
                {
                    isMousePressed = false;
                    isPressed = false;
                    Invalidate();
                }
            }

            if (m.Msg == WM_MOUSEMOVE)
            {
                if (isPressed && isMousePressed)
                {
                    selection[1] = MousePosition;
                    Invalidate();
                }
            }

            // Not working at all for some reason. Probably will not be needed anymore
            if (m.Msg == WM_NCHITTEST)
            {
                if (isPressed)
                    m.Result = new IntPtr(HTCLIENT);
                else
                    m.Result = new IntPtr(HTTRANSPARENT);
            }

            base.WndProc(ref m);
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

        // Should be removed later
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            if (!isPressed)
            {
                e.Graphics.Flush();
                Opacity = 0;
            }
            else
            {
                int top, bottom, left, right;
                FindSelectionBounds(out top, out bottom, out left, out right);
                var selectionRect = new Rectangle(left, top, right - left, bottom - top);

                // TODO Fix fill colors for selection. Right now it shows invalid color due to BackColor variable
                //e.Graphics.CompositingQuality = CompositingQuality.GammaCorrected;
                //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 0, 225, 225)), selectionRect);

                /* Testing purposes

                // Gets bitmap of a region
                e.Graphics.Clear(Color.Red);
                var bitmap = new Bitmap(selectionRect.Width + 1, selectionRect.Height + 1, PixelFormat.Format32bppRgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(new Point(selectionRect.X, selectionRect.Y), new Point(selectionRect.X, selectionRect.Y), selectionRect.Size);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(10, 220, 220, 220)), selectionRect);
                }

                // Trying to draw directly on screen
                var temp = Graphics.FromHwnd(IntPtr.Zero);
                temp.CopyFromScreen(left, top, selectionRect.Width, selectionRect.Height, selectionRect.Size);
                temp.DrawRectangle(Pens.Black, selectionRect);
                temp.FillRectangle(new SolidBrush(Color.FromArgb(80, 220, 220, 220)), selectionRect);
                temp.Dispose();
                // We need to collect garbage ourselves oO
                GC.Collect();
                */

                e.Graphics.DrawRectangle(Pens.Black, selectionRect);

            }
        }
        /*
        public static Bitmap CopyBitmap(Bitmap bmp_source, Rectangle region)
        {
            var bmp_dest = new Bitmap(region.Width + 1, region.Height + 1);
            using (var g = Graphics.FromImage(bmp_dest))
            {
                g.DrawImage(bmp_source, region.X, region.Y, region, GraphicsUnit.Pixel);
            }
            return bmp_dest;
        }*/

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

