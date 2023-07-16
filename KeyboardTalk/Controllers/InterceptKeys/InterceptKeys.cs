using KeydownEventService;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Controllers.InterceptKeys
{
    public static class InterceptKeys
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static readonly KeydownEventHandler _handler = new();
        private static Dictionary<int, Key> _keys = new();
        private static bool _isHooked = false;

        public static bool Hook(Action<Keys> onPressed, Action<Keys, Exception>? onError = null)
        {
            if (_isHooked) return false;
            _handler.OnPressed += onPressed;
            _handler.OnError += onError;
            _hookID = SetHook(_proc);
            _isHooked = true;
            return true;
        }

        public static bool Unhook()
        {
            if (!_isHooked) return false;
            _handler.RemoveAllListener();
            UnhookWindowsHookEx(_hookID);
            _isHooked = false;
            return true;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                _keys.TryGetValue(vkCode, out Key? key);
                if (key is null)
                {
                    key = new Key(vkCode);
                    _keys.Add(vkCode, key);
                }

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (key.Keydown())
                    {
                        _handler.Press((Keys)vkCode);
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    key.Keyup();
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    class Key
    {
        public int KeyCode { get; private set; }
        public bool IsKeyPressed { get; private set; }

        public Key(int keyCode)
        {
            KeyCode = keyCode;
            IsKeyPressed = false;
        }

        public bool Keydown()
        {
            lock (this)
            {
                if (IsKeyPressed) return false;
                IsKeyPressed = true;
                return true;
            }
        }

        public bool Keyup()
        {
            lock (this)
            {
                if (!IsKeyPressed) return false;
                IsKeyPressed = false;
                return true;
            }
        }
    }
}
