using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace RelicHelper
{
    internal class KeyboardHook : IDisposable
    {
        private WinApi.LowLevelProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler<Key>? KeyDown;

        public KeyboardHook()
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
                return WinApi.SetWindowsHookEx(WinApi.WH_KEYBOARD_LL, proc,
                    WinApi.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WinApi.WM_KEYDOWN || wParam == (IntPtr)WinApi.WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                KeyDown?.Invoke(this, key);
            }
            return WinApi.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
