using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Linq;

namespace RelicHelper.Clients
{
    internal class ClientWindow
    {
        private readonly IntPtr _windowHandle;

        public ClientWindow(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            Activate();
        }

        public void Activate()
        {
            WinApi.SetForegroundWindow(_windowHandle);
        }

        public bool IsActive
        {
            get
            {
                if (_windowHandle == IntPtr.Zero)
                    return false;

                return WinApi.GetForegroundWindow() == _windowHandle;
            }
        }

        private Rectangle GetRightPanelScanRect()
        {
            var windowRect = GetRect();
            var imageWidth = 160;
            var rightOffset = 24;

            return new Rectangle(windowRect.Right - imageWidth - rightOffset, windowRect.Top, imageWidth, windowRect.Height - 10);
        }

        public Bitmap? CaptureRightPanel()
        {
            if (_windowHandle == IntPtr.Zero || !IsActive)
                return null;

            var scanRect = GetRightPanelScanRect();

            var bitmap = new Bitmap(scanRect.Width, scanRect.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(scanRect.Left, scanRect.Top, 0, 0, new Size(scanRect.Width, scanRect.Height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        public Rectangle GetRect()
        {
            if (_windowHandle == IntPtr.Zero)
                return Rectangle.Empty;

            var rect = new WinApi.Rect();
            WinApi.GetWindowRect(_windowHandle, ref rect);
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            return bounds;
        }
    }
}
