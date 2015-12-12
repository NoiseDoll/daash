using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;


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

        private ImageFormat imageFormatType;
        private string imageFormatName;

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

            // TODO Make two options whether to pause the screen or not
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

                    int top, bottom, left, right;
                    FindSelectionBounds(out top, out bottom, out left, out right);
                    var curBitmap = CaptureScreenshot(new Rectangle(left, top, right - left, bottom - top));

                    SaveImage(curBitmap);

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

        // TODO Implement image uploading
        private bool UploadImage(Bitmap curImage)
        {
            return false;
        }

        private void SaveImage(Bitmap curImage)
        {
            // Do not hardcode path and filenames, thus will be changed later
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\daash\";
            var curDate = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            var imageSize = "-" + curImage.Width + "x" + curImage.Height;
            var filename = @"";

            // Set Image Format to PNG (false == JPEG)
            setImageFormat(true);

            try
            {
                Directory.CreateDirectory(path);
                curImage.Save(path + curDate + imageSize + filename + this.imageFormatName, this.imageFormatType);
            }
            catch (Exception)
            {
                MessageBox.Show("Image cannot be saved");
            }
        }

        private void setImageFormat(bool isPng)
        {
            // TODO Encapsulate fields
            // TODO Add encoder params
            if (isPng)
            {
                imageFormatType = ImageFormat.Png;
                imageFormatName = ".png";
            }
            else
            {
                imageFormatType = ImageFormat.Jpeg;
                imageFormatName = ".jpeg";
            }
        }

        private Bitmap CaptureScreenshot(Rectangle size)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(size.Left, size.Top, 0, 0, size.Size);
            }
            return bitmap;
        }

        // Might become obsolete and will be removed later
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
                // Right now it seems to be not possible via region screenshoting method due to screen refresh rate
                //e.Graphics.CopyFromScreen(selectionRect.Left, selectionRect.Top, selectionRect.Left, selectionRect.Top, selectionRect.Size);
                //e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(10, 220, 220, 220)), selectionRect);
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

