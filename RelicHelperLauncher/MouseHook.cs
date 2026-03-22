using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RelicHelper
{
    internal class MouseHook : IDisposable
    {
        private WinApi.LowLevelProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler<System.Drawing.Point>? LeftClick;
        public event EventHandler<System.Drawing.Point>? RightClick;

        public MouseHook()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            _hookID = SetHook(_proc);
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                WinApi.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(WinApi.LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return WinApi.SetWindowsHookEx(WinApi.WH_MOUSE_LL, proc,
                    WinApi.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WinApi.WM_LBUTTONDOWN)
                {
                    var hookStruct = (WinApi.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(WinApi.MSLLHOOKSTRUCT));
                    LeftClick?.Invoke(this, new System.Drawing.Point(hookStruct.pt.x, hookStruct.pt.y));
                }
                else if (wParam == (IntPtr)WinApi.WM_RBUTTONDOWN)
                {
                    var hookStruct = (WinApi.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(WinApi.MSLLHOOKSTRUCT));
                    RightClick?.Invoke(this, new System.Drawing.Point(hookStruct.pt.x, hookStruct.pt.y));
                }
            }
            return WinApi.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
