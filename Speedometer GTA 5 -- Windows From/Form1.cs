using memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Speedometer_GTA_5____Windows_From
{
    public partial class Form1 : Form
    {
        private const string ProcessName = "GTA5";
        private static Process _process;
        private static long _time1;
        private static double _x1, _y1;
        private static long _worldPtr;
        private static int _k;
        private static int _oldSpeed;
        private static int _speed;

        // Константы для WinAPI
        private const int SwpNoactivate = 0x0010;
        private const int SwpShowwindow = 0x0040;
        private const int GwHwndnext = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        public Form1()
        {
            InitializeComponent();
            Enabled = false;
            TopMost = true;
            ShowInTaskbar = false;
            _time1 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _x1 = 0;
            _y1 = 0;
            Opacity = 0.75;
            _worldPtr = GetWorldPtr();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            label1.ForeColor = Color.DarkTurquoise;
            label2.ForeColor = Color.DarkTurquoise;
            var timer = new System.Windows.Forms.Timer();
            timer.Tick += UpdateValues;
            timer.Interval = 1;
            _speed = 0;
            _oldSpeed = 0;
            timer.Start();
        }

        private void UpdateValues(object sender, EventArgs e)
        {
            _process = Process.GetProcessesByName("GTA5").FirstOrDefault();
            if (_process != null && IsPlayerInVehicle())
            {
                if (label1.Text == "-1")
                {
                    _speed = _oldSpeed = 0;
                }
                _oldSpeed = _speed;
                _speed = (int)GetSpeed();
                if (_speed > _oldSpeed)
                {
                    for (var i = _oldSpeed; i <= _speed; i++)
                    {
                        label1.Text = i.ToString();
                        label1.ForeColor = Color.DarkTurquoise;
                        label2.ForeColor = Color.DarkTurquoise;
                        Show();
                        if (_k == 0)
                        {
                            SetForegroundWindow(_process.MainWindowHandle);
                            AttachToGta5Window();
                            _k++;
                        }

                        BringToFront();
                        MoveToRightBottomOfGta5Window();
                        Update();
                        Thread.Sleep(200 / (_speed - _oldSpeed));
                    }
                }
                else if (_speed < _oldSpeed)
                {
                    for (var i = _oldSpeed; i >= _speed; i--)
                    {
                        label1.Text = i.ToString();
                        label1.ForeColor = Color.DarkTurquoise;
                        label2.ForeColor = Color.DarkTurquoise;
                        Show();
                        if (_k == 0)
                        {
                            SetForegroundWindow(_process.MainWindowHandle);
                            AttachToGta5Window();
                            _k++;
                        }

                        BringToFront();
                        MoveToRightBottomOfGta5Window();
                        Update();
                        Thread.Sleep(10 / (_oldSpeed - _speed));
                    }
                }
                else
                {
                    label1.Text = _speed.ToString();
                    label1.ForeColor = Color.DarkTurquoise;
                    label2.ForeColor = Color.DarkTurquoise;
                    Show();
                    if (_k == 0)
                    {
                        SetForegroundWindow(_process.MainWindowHandle);
                        AttachToGta5Window();
                        _k++;
                    }

                    BringToFront();
                    MoveToRightBottomOfGta5Window();
                    Update();
                }
                Show();
            }
            else
            {
                Hide();
            }
        }


        private static long GetWorldPtr()
        {
            _process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (_process == null) return 12345;
            var obj = new Scanner(_process, _process.Handle, "48 8B 05 ?? ?? ?? ?? 45 ?? ?? ?? ?? 48 8B 48 08 48 85 C9 74 07");

            obj.setModule(_process.MainModule);
            var address = (long)obj.FindPattern();

            var worldPtr = address + ReadInteger(address + 0x3) + 0x7;

            return worldPtr;

        }

        private static bool IsPlayerInVehicle()
        {
            const string moduleName = "GTA5.exe";
            var baseAddress = GetModuleBaseAddress(moduleName);
            if (baseAddress == 0) return false;

            const long offset = 0x259CDE0; // Оффсет для проверки, сидит ли персонаж в машине

            var value = ReadInteger(baseAddress + offset);

            return value == 1; // Возвращаем true, если персонаж сидит в машине, иначе false
        }

        private static long GetModuleBaseAddress(string moduleName)
        {
            var processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length == 0) return 0;
            var process = processes[0];

            return (from ProcessModule module in process.Modules where module.ModuleName == moduleName select (long)module.BaseAddress).FirstOrDefault();
        }

        private static double GetSpeed()
        {
            var timeSpan = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _time1;
            var lst = GetPlayerXy();
            var distance = Math.Sqrt(Math.Pow(lst[0] - _x1, 2) + Math.Pow(lst[1] - _y1, 2));
            var speed = Math.Round(((distance * 1000) / timeSpan) * 2.25);
            lst = GetPlayerXy();
            _time1 = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _x1 = lst[0];
            _y1 = lst[1];
            return speed;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AttachToGta5Window();
        }

        private void AttachToTopWindow()
        {
            var topWindow = GetTopWindow();
            if (topWindow != IntPtr.Zero)
            {
                SetWindowPos(Handle, topWindow, 0, 0, 0, 0, SwpShowwindow | SwpNoactivate);
            }
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int wsExTopmost = 0x00000008;
                var cp = base.CreateParams;
                cp.ExStyle |= wsExTopmost;
                return cp;
            }
        }

        private static IntPtr GetTopWindow()
        {
            var hwnd = IntPtr.Zero;
            while ((hwnd = GetWindow(hwnd, GwHwndnext)) != IntPtr.Zero)
            {
                if (IsWindowVisible(hwnd))
                {
                    return hwnd;
                }
            }
            return IntPtr.Zero;
        }



        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private static List<double> GetPlayerXy()
        {
            var ptrV3PlayerPos = GetAddress2(_worldPtr, 0x8, 0x90);
            var v3X = Math.Round(ReadFloat(ptrV3PlayerPos + 0), 0);
            var v3Y = Math.Round(ReadFloat(ptrV3PlayerPos + 4), 0);
            var coords = new List<double> { v3X, v3Y };
            return coords;
        }

        private static int ReadInteger(long baseAddress)
        {
            var buffer = new byte[4];
            ReadProcessMemory(_process.Handle, baseAddress, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        private static float ReadFloat(long baseAddress)
        {
            var buffer = new byte[4];
            ReadProcessMemory(_process.Handle, baseAddress, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        private static long GetAddress2(long baseAddress, long offset1, long finalOffset)
        {
            var basePtr1 = ReadInt64(baseAddress);
            var basePtr2 = ReadInt64(basePtr1 + offset1);
            var finalAddress = basePtr2 + finalOffset;
            return finalAddress;
        }

        private static long ReadInt64(long baseAddress)
        {
            var buffer = new byte[8];
            ReadProcessMemory(_process.Handle, baseAddress, buffer, buffer.Length, out _);
            return BitConverter.ToInt64(buffer, 0);
        }

        private void AttachToGta5Window()
        {
            var processes = Process.GetProcessesByName(ProcessName);
            if (processes.Length > 0)
            {
                var hWnd = processes[0].MainWindowHandle;
                if (!GetWindowRect(hWnd, out var rect)) return;
                // Получаем размеры и позицию окна GTA 5

                // Устанавливаем позицию вашего окна относительно окна GTA 5
                Left = rect.Right - Width;
                Top = rect.Bottom - Height;
            }
            else
            {
                MessageBox.Show("Процесс GTA 5 не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AttachToTopWindow();
        }

        private void MoveToRightBottomOfGta5Window()
        {
            var gtaHwnd = FindWindow(null, "GTA5");
            if (gtaHwnd == IntPtr.Zero) return;
            if (!GetWindowRect(gtaHwnd, out var gtaRect)) return;
            var formWidth = Width;
            var formHeight = Height;
            var newX = gtaRect.Right - formWidth;
            var newY = gtaRect.Bottom - formHeight;
            SetWindowPos(this.Handle, IntPtr.Zero, newX, newY, formWidth, formHeight, SwpShowwindow);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
    }
}
