using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace dll_procedure
{
    public partial class Form1 : Form
    {
        int W = Screen.PrimaryScreen.Bounds.Size.Width;
        int H = Screen.PrimaryScreen.Bounds.Size.Height;
        int X = Cursor.Position.X;
        int Y = Cursor.Position.Y;
        int flag = 0;
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern int WindowFromPoint(int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        static extern int GetPixel(IntPtr hDC, int x, int y);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        public Form1()
        {
            InitializeComponent();
            MouseHook.Start();
            MouseHook.MouseAction += new EventHandler(Event);
            KeyboardHook.Start();
            KeyboardHook.KeyboardAction += new EventHandler(EventCtrl);
        }

        private void EventCtrl (object sender, EventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(label1.Text+"\n"+ label2.Text + "\n" + label3.Text + "\n" + label4.Text + "\n" + label5.Text);
        }

        private void Event(object sender, EventArgs e)
        {
            X= System.Windows.Forms.Cursor.Position.X;
            Y= System.Windows.Forms.Cursor.Position.Y;
            IntPtr handle = (IntPtr)WindowFromPoint(X, Y);
            Color RGB = Color.Empty;
            int colorRef = GetPixel(GetDC((IntPtr)0), X, Y);
            RGB = Color.FromArgb(
                (int)(colorRef & 0x000000FF),
                (int)(colorRef & 0x0000FF00) >> 8,
                (int)(colorRef & 0x00FF0000) >> 16);
            pictureBox1.BackColor = RGB;
            label1.Text ="X:"+X.ToString();
            label2.Text ="Y:"+Y.ToString();
            label3.Text = handle.ToString() + ":" + GetWindowText(handle);
            label4.Text = handle.ToString() + ":" + GetWindowClass(handle);
            label5.Text = RGB.R.ToString() + ":" + RGB.G.ToString() + ":" + RGB.B.ToString();
            if (flag==0)
            {
                if (X + this.Width + 10 > W)
                {
                    this.Left = W - this.Width;
                }
                else
                {
                    this.Left = X + 10;
                }
                if (Y + this.Height + 40 > H)
                {
                    this.Top = H - this.Height - 40;
                }
                else
                {
                    this.Top = Y + 20;
                }
            }
            else
            {
                this.Left = W - this.Width - 10;
                this.Top = 10;
            }
        }
        string GetWindowText(IntPtr hWnd)
        {
            int len = GetWindowTextLength(hWnd) + 1;
            StringBuilder sb = new StringBuilder(len);
            len = GetWindowText(hWnd, sb, len);
            return sb.ToString(0, len);
        }
        string GetWindowClass(IntPtr hWnd)
        {
            int len = 260;
            StringBuilder sb = new StringBuilder(len);
            len = GetClassName(hWnd, sb, len);
            return sb.ToString(0, len);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            MouseHook.stop();
            KeyboardHook.stop();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (flag==0)
            {
                toolStripMenuItem1.Text = "Освободить надпись";
                flag = 1;
            }
            else
            {
                toolStripMenuItem1.Text = "Закрепить надпись";
                flag = 0;
            }
        }
    }

    public static class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static event EventHandler KeyboardAction = delegate { };

        public static void Start()
        {
            _hookID = SetHook(_proc);
        }
        public static void stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if ((nCode >= 0) && (wParam == (IntPtr)WM_KEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == 162) //нажат Ctrl
                {
//                    MessageBox.Show("Ctrl");
                    KeyboardAction(null, new EventArgs());
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }


    public static class MouseHook
        {
            public static event EventHandler MouseAction = delegate { };

            public static void Start()
            {
                _hookID = SetHook(_proc);
            }
            public static void stop()
            {
                UnhookWindowsHookEx(_hookID);
            }

            private static LowLevelMouseProc _proc = HookCallback;
            private static IntPtr _hookID = IntPtr.Zero;

            private static IntPtr SetHook(LowLevelMouseProc proc)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc,
                      GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

            private static IntPtr HookCallback(
              int nCode, IntPtr wParam, IntPtr lParam)
            {
//                if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                if (nCode >= 0 && MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    MouseAction(null, new EventArgs());
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            private const int WH_MOUSE_LL = 14;

            private enum MouseMessages
            {
                WM_LBUTTONDOWN = 0x0201,
                WM_LBUTTONUP = 0x0202,
                WM_MOUSEMOVE = 0x0200,
                WM_MOUSEWHEEL = 0x020A,
                WM_RBUTTONDOWN = 0x0204,
                WM_RBUTTONUP = 0x0205
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct POINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct MSLLHOOKSTRUCT
            {
                public POINT pt;
                public uint mouseData;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook,
              LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
              IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
