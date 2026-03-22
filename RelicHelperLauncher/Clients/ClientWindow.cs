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
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        public Bitmap? CaptureCenter()
        {
            try
            {
                if (_windowHandle == IntPtr.Zero || !IsActive)
                    return null;

                var windowRect = GetRect();
                
                // Heuristic to find the character center (offset by sidebars)
                // Standard Tibia sidebars are ~176px. Many players use only the right sidebar.
                // We'll estimate the character to be at the center of the viewport (excluding the right 180px).
                int rightPanelWidth = 180;
                int characterX = windowRect.Left + ((windowRect.Width - rightPanelWidth) / 2);
                int characterY = windowRect.Top + (windowRect.Height / 2);

                // For robustness against different resolutions, we use a larger 250x250 area
                int scanSize = 250;
                var scanRect = new Rectangle(characterX - (scanSize / 2), characterY - (scanSize / 2), scanSize, scanSize);

                if (scanRect.Width <= 0 || scanRect.Height <= 0) return null;

                var bitmap = new Bitmap(scanRect.Width, scanRect.Height, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(scanRect.Left, scanRect.Top, 0, 0, new Size(scanRect.Width, scanRect.Height), CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Bitmap? CaptureGameWorld()
        {
            try
            {
                if (_windowHandle == IntPtr.Zero || !IsActive)
                    return null;

                var windowRect = GetRect();
                // Capture the entire window area except a small margin for the borders/title
                var scanRect = new Rectangle(windowRect.Left + 8, windowRect.Top + 30, windowRect.Width - 16, windowRect.Height - 40);

                if (scanRect.Width <= 0 || scanRect.Height <= 0) return null;

                var bitmap = new Bitmap(scanRect.Width, scanRect.Height, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(scanRect.Left, scanRect.Top, 0, 0, new Size(scanRect.Width, scanRect.Height), CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
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
