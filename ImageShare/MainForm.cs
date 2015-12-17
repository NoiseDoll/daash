using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using RestSharp;

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

        private string imageFormatName;
        private ImageFormat imageFormatType;
        private EncoderParameters imageEncoderParameters;
        private long encoderQuality;

        private int shortcutKeyId = 0;
        private string uploadKey;

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
            RegisterHotKey(Handle, shortcutKeyId, (int)(KeyModifier.Shift | KeyModifier.Alt), Keys.A.GetHashCode());

            // TODO Make two options whether to pause the screen or not
            // BackgroundImage = CaptureDesktop(screenSize);

            // TODO Read uploadKey from config
            uploadKey = "";

            // Set Image Format to PNG (false == JPEG)
            SetImageFormat(true);
        }

        protected override void WndProc(ref Message m)
        {
            const uint WM_HOTKEY = 0x0312;
            const uint WM_MOUSEMOVE = 0x0200;
            const uint WM_LBUTTONDOWN = 0x0201;
            const uint WM_LBUTTONUP = 0x0202;

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

                    ImageCapturedAction();
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

            base.WndProc(ref m);
        }

        private async Task<IRestResponse> UploadImage(Bitmap curImage, string filename)
        {
            using (var memoryStream = new MemoryStream())
            {
                var codecInfo = GetEncoder(imageFormatType);
                if (codecInfo.FormatDescription == "JPEG")
                    curImage.Save(memoryStream, codecInfo, imageEncoderParameters);
                else
                    curImage.Save(memoryStream, imageFormatType);

                var restClient = new RestClient("http://daash.pw/cgi-bin/upload.pl");
                var request = new RestRequest(Method.POST);
                request.AddParameter("key", uploadKey);
                request.AddFileBytes("fileToUpload0", memoryStream.ToArray(), filename, "multipart/form-data");

                try
                {
                    return await restClient.ExecuteTaskAsync(request);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private async void ImageCapturedAction()
        {
            int top, bottom, left, right;
            FindSelectionBounds(out top, out bottom, out left, out right);
            var curBitmap = CaptureScreenshot(new Rectangle(left, top, right - left, bottom - top));

            // Change hardcoded filename according to config
            var curDate = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            var imageSize = "-" + curBitmap.Width + "x" + curBitmap.Height;
            var filePostfix = @"";
            var filename = curDate + imageSize + filePostfix + this.imageFormatName;

            SaveImage(curBitmap, filename);
            var responseString = (await UploadImage(curBitmap, filename)).Content;
            if (responseString == null)
                MessageBox.Show("Image cannot be uploaded");
            else
                Clipboard.SetText(responseString);
        }

        private void SaveImage(Bitmap curBitmap, string filename)
        {
            // TODO Change hardcoded path
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\daash\";

            try
            {
                Directory.CreateDirectory(path);
                var imageFullPath = path + filename;

                var codecInfo = GetEncoder(imageFormatType);
                if (codecInfo.FormatDescription == "JPEG")
                    curBitmap.Save(imageFullPath, codecInfo, this.imageEncoderParameters);
                else
                    curBitmap.Save(imageFullPath, imageFormatType);
            }
            catch (Exception)
            {
                MessageBox.Show("Image cannot be saved");
            }
        }

        private EncoderParameters GetEncoderParams(long encoderQuality)
        {
            var qualityEncoder = Encoder.Quality;
            var encoderParameters = new EncoderParameters(1);
            var encoderParameter = new EncoderParameter(qualityEncoder, encoderQuality);
            encoderParameters.Param[0] = encoderParameter;
            return encoderParameters;
        }

        private void SetImageFormat(bool isPng)
        {
            // TODO Encapsulate fields
            if (isPng)
            {
                imageFormatType = ImageFormat.Png;
                imageFormatName = ".png";
            }
            else
            {
                imageFormatType = ImageFormat.Jpeg;
                imageFormatName = ".jpeg";

                // TODO Read encoder quality from config
                encoderQuality = 100L;
                imageEncoderParameters = GetEncoderParams(encoderQuality);
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

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
                if (codec.FormatID == format.Guid)
                    return codec;

            return null;
        }
    }
}

