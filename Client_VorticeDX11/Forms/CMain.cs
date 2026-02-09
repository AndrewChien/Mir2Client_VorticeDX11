using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Security;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirScenes;
using Client.MirSounds;
using Vortice.WinForms;
using Vortice.Direct3D11;
using Font = System.Drawing.Font;

namespace Client
{
    public partial class CMain : RenderForm
    {
        public static MirControl DebugBaseLabel, HintBaseLabel;
        public static MirLabel DebugTextLabel, HintTextLabel, ScreenshotTextLabel;
        public static Graphics Graphics;
        public static Point MPoint;
        private static bool _sysKeyMessageFilterInstalled;

        public readonly static Stopwatch Timer = Stopwatch.StartNew();
        public readonly static DateTime StartTime = DateTime.Now;
        public static long Time;
        public static DateTime Now { get { return StartTime.AddMilliseconds(Time); } }
        public static readonly Random Random = new Random();

        public static string DebugText = "";

        private static long _fpsTime;
        private static int _fps;
        private static long _cleanTime;
        private static long _drawTime;
        public static int FPS;
        public static int DPS;
        public static int DPSCounter;

        public static long PingTime;
        public static long NextPing = 10000;

        public static bool Shift, Alt, Ctrl, Tilde, SpellTargetLock;
        public static double BytesSent, BytesReceived;

        public static KeyBindSettings InputKeys = new KeyBindSettings();

        public CMain()
        {
            InitializeComponent();

            EnsureSysKeyMessageFilter();

            Application.Idle += Application_Idle;

            MouseClick += CMain_MouseClick;
            MouseDown += CMain_MouseDown;
            MouseUp += CMain_MouseUp;
            MouseMove += CMain_MouseMove;
            MouseDoubleClick += CMain_MouseDoubleClick;
            KeyPress += CMain_KeyPress;
            KeyDown += CMain_KeyDown;
            KeyUp += CMain_KeyUp;
            Deactivate += CMain_Deactivate;
            MouseWheel += CMain_MouseWheel;


            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = Settings.FullScreen ? FormBorderStyle.None : FormBorderStyle.FixedDialog;
            TopMost = Settings.TopMost;

            Graphics = CreateGraphics();
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Graphics.CompositingQuality = CompositingQuality.HighQuality;
            Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Graphics.TextContrast = 0;
        }

        private void CMain_Load(object sender, EventArgs e)
        {
            this.Text = GameLanguage.GameName;
            try
            {
                ApplyWindowMode();

                LoadMouseCursors();
                SetMouseCursor(MouseCursor.Default);

                DXManager.Create();
                SoundManager.Create();

                if (!Settings.FullScreen)
                    CenterToScreen();
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        static bool flag = false;
        private static void Application_Idle(object sender, EventArgs e)
        {
            try
            {
                while (AppStillIdle)
                {
                    UpdateTime();
                    UpdateEnviroment();

                    if (IsDrawTime())
                    {
                        RenderEnvironment();
                        UpdateFrameTime();
                    }
                }

            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        private static void CMain_Deactivate(object sender, EventArgs e)
        {
            MapControl.MapButtons = MouseButtons.None;
            Shift = false;
            Alt = false;
            Ctrl = false;
            Tilde = false;
            SpellTargetLock = false;
        }

        public static void CMain_KeyDown(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (!String.IsNullOrEmpty(InputKeys.GetKey(KeybindOptions.TargetSpellLockOn)))
            {
                SpellTargetLock = e.KeyCode == (Keys)Enum.Parse(typeof(Keys), InputKeys.GetKey(KeybindOptions.TargetSpellLockOn), true);
            }
            else
            {
                SpellTargetLock = false;
            }


            if (e.KeyCode == Keys.Oem8)
                CMain.Tilde = true;

            try
            {
                if (e.Alt && e.KeyCode == Keys.Enter)
                {
                    ToggleFullScreen();
                    return;
                }

                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnKeyDown(e);

            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.FullScreen || Settings.MouseClip)
                Cursor.Clip = Program.Form.RectangleToScreen(Program.Form.ClientRectangle);

            DXManager.TryMapClientToVirtual(e.Location, out MPoint);
            var mappedArgs = new MouseEventArgs(e.Button, e.Clicks, MPoint.X, MPoint.Y, e.Delta);

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseMove(mappedArgs);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_KeyUp(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (!String.IsNullOrEmpty(InputKeys.GetKey(KeybindOptions.TargetSpellLockOn)))
            {
                SpellTargetLock = e.KeyCode == (Keys)Enum.Parse(typeof(Keys), InputKeys.GetKey(KeybindOptions.TargetSpellLockOn), true);
            }
            else
            {
                SpellTargetLock = false;
            }

            if (e.KeyCode == Keys.Oem8)
                CMain.Tilde = false;

            foreach (KeyBind KeyCheck in CMain.InputKeys.Keylist)
            {
                if (KeyCheck.function != KeybindOptions.Screenshot) continue;
                if (KeyCheck.Key != e.KeyCode)
                    continue;
                if ((KeyCheck.RequireAlt != 2) && (KeyCheck.RequireAlt != (Alt ? 1 : 0)))
                    continue;
                if ((KeyCheck.RequireShift != 2) && (KeyCheck.RequireShift != (Shift ? 1 : 0)))
                    continue;
                if ((KeyCheck.RequireCtrl != 2) && (KeyCheck.RequireCtrl != (Ctrl ? 1 : 0)))
                    continue;
                if ((KeyCheck.RequireTilde != 2) && (KeyCheck.RequireTilde != (Tilde ? 1 : 0)))
                    continue;
                Program.Form.CreateScreenShot();
                break;

            }
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnKeyUp(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnKeyPress(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                {
                    DXManager.TryMapClientToVirtual(e.Location, out MPoint);
                    var mappedArgs = new MouseEventArgs(e.Button, e.Clicks, MPoint.X, MPoint.Y, e.Delta);
                    MirScene.ActiveScene.OnMouseClick(mappedArgs);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseUp(object sender, MouseEventArgs e)
        {
            MapControl.MapButtons &= ~e.Button;
            if (e.Button != MouseButtons.Right || !Settings.NewMove)
                GameScene.CanRun = false;

            try
            {
                if (MirScene.ActiveScene != null)
                {
                    DXManager.TryMapClientToVirtual(e.Location, out MPoint);
                    var mappedArgs = new MouseEventArgs(e.Button, e.Clicks, MPoint.X, MPoint.Y, e.Delta);
                    MirScene.ActiveScene.OnMouseUp(mappedArgs);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (Program.Form.ActiveControl is TextBox)
            {
                MirTextBox textBox = Program.Form.ActiveControl.Tag as MirTextBox;

                if (textBox != null && textBox.CanLoseFocus)
                    Program.Form.ActiveControl = null;
            }

            if (e.Button == MouseButtons.Right && (GameScene.SelectedCell != null || GameScene.PickedUpGold))
            {
                GameScene.SelectedCell = null;
                GameScene.PickedUpGold = false;
                return;
            }

            try
            {
                if (MirScene.ActiveScene != null)
                {
                    DXManager.TryMapClientToVirtual(e.Location, out MPoint);
                    var mappedArgs = new MouseEventArgs(e.Button, e.Clicks, MPoint.X, MPoint.Y, e.Delta);
                    MirScene.ActiveScene.OnMouseDown(mappedArgs);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                {
                    DXManager.TryMapClientToVirtual(e.Location, out MPoint);
                    var mappedArgs = new MouseEventArgs(e.Button, e.Clicks, MPoint.X, MPoint.Y, e.Delta);
                    MirScene.ActiveScene.OnMouseClick(mappedArgs);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                {
                    DXManager.TryMapClientToVirtual(e.Location, out MPoint);
                    var mappedArgs = new MouseEventArgs(e.Button, e.Clicks, MPoint.X, MPoint.Y, e.Delta);
                    MirScene.ActiveScene.OnMouseWheel(mappedArgs);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        private static void UpdateTime()
        {
            Time = Timer.ElapsedMilliseconds;
        }

        private static void UpdateFrameTime()
        {
            if (Time >= _fpsTime)
            {
                _fpsTime = Time + 1000;
                FPS = _fps;
                _fps = 0;

                DPS = DPSCounter;
                DPSCounter = 0;
            }
            else
                _fps++;
        }

        private static bool IsDrawTime()
        {
            const int TargetUpdates = 1000 / 60; // 60 frames per second

            if (Time >= _drawTime)
            {
                _drawTime = Time + TargetUpdates;
                return true;
            }
            return false;
        }

        private static void UpdateEnviroment()
        {
            if (Time >= _cleanTime)
            {
                _cleanTime = Time + 1000;

                DXManager.Clean(); // Clean once a second.
            }

            Network.Process();

            if (MirScene.ActiveScene != null)
                MirScene.ActiveScene.Process();

            for (int i = 0; i < MirAnimatedControl.Animations.Count; i++)
                MirAnimatedControl.Animations[i].UpdateOffSet();

            for (int i = 0; i < MirAnimatedButton.Animations.Count; i++)
                MirAnimatedButton.Animations[i].UpdateOffSet();

            CreateHintLabel();

            if (Settings.DebugMode)
            {
                CreateDebugLabel();
            }
        }

        private static void RenderEnvironment()
        {
            try
            {
                if (DXManager.DeviceLost)
                {
                    DXManager.AttemptReset();
                    Thread.Sleep(1);
                    return;
                }

                DXManager.SetSurface(ref DXManager.MainSurface);
                DXManager.DeviceClear_Target(Color.Black);
                DXManager.SpriteBegin_AlphaBlend();

                if (MirScene.ActiveScene != null)
                {
                    MirScene.ActiveScene.Draw();
                }

                DXManager.Sprite_End();
                DXManager.DevicePresent();
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());

                DXManager.AttemptRecovery();
            }
        }

        private static void CreateDebugLabel()
        {
            string text;

            if (MirControl.MouseControl != null)
            {
                text = string.Format("FPS: {0}", FPS);

                text += string.Format(", DPS: {0}", DPS);

                text += string.Format(", Time: {0:HH:mm:ss}", Now);

                if (MirControl.MouseControl is MapControl)
                    text += string.Format(", Co Ords: {0}", MapControl.MapLocation);

                if (MirControl.MouseControl is MirImageControl)
                    text += string.Format(", Control: {0}", MirControl.MouseControl.GetType().Name);

                if (MirScene.ActiveScene is GameScene)
                    text += string.Format(", Objects: {0}", MapControl.Objects.Count);

                if (MirScene.ActiveScene is GameScene && !string.IsNullOrEmpty(DebugText))
                    text += string.Format(", Debug: {0}", DebugText);

                if (MirObjects.MapObject.MouseObject != null)
                {
                    text += string.Format(", Target: {0}", MirObjects.MapObject.MouseObject.Name);
                }
                else
                {
                    text += string.Format(", Target: none");
                }
            }
            else
            {
                text = string.Format("FPS: {0}", FPS);
            }

            text += string.Format(", Ping: {0}", PingTime);

            text += string.Format(", Sent: {0}, Received: {1}", Functions.ConvertByteSize(BytesSent), Functions.ConvertByteSize(BytesReceived));

            text += string.Format(", TLC: {0}", DXManager.TextureList.Count(x => x.TextureValid));
            text += string.Format(", CLC: {0}", DXManager.ControlList.Count(x => x.IsDisposed == false));

            if (Settings.FullScreen)
            {
                if (DebugBaseLabel == null || DebugBaseLabel.IsDisposed)
                {
                    DebugBaseLabel = new MirControl
                    {
                        BackColour = Color.FromArgb(50, 50, 50),
                        Border = true,
                        BorderColour = Color.Black,
                        DrawControlTexture = true,
                        Location = new Point(5, 5),
                        NotControl = true,
                        Opacity = 0.5F
                    };
                }

                if (DebugTextLabel == null || DebugTextLabel.IsDisposed)
                {
                    DebugTextLabel = new MirLabel
                    {
                        AutoSize = true,
                        BackColour = Color.Transparent,
                        ForeColour = Color.White,
                        Parent = DebugBaseLabel,
                    };

                    DebugTextLabel.SizeChanged += (o, e) => DebugBaseLabel.Size = DebugTextLabel.Size;
                }

                DebugTextLabel.Text = text;
            }
            else
            {
                if (DebugBaseLabel != null && DebugBaseLabel.IsDisposed == false)
                {
                    DebugBaseLabel.Dispose();
                    DebugBaseLabel = null;
                }
                if (DebugTextLabel != null && DebugTextLabel.IsDisposed == false)
                {
                    DebugTextLabel.Dispose();
                    DebugTextLabel = null;
                }

                Program.Form.Text = $"{GameLanguage.GameName} - {text}";
            }
        }

        private static void CreateHintLabel()
        {
            if (HintBaseLabel == null || HintBaseLabel.IsDisposed)
            {
                HintBaseLabel = new MirControl
                {
                    BackColour = Color.FromArgb(255, 0, 0, 0),
                    Border = true,
                    DrawControlTexture = true,
                    BorderColour = Color.FromArgb(255, 144, 144, 0),
                    ForeColour = Color.Yellow,
                    Parent = MirScene.ActiveScene,
                    NotControl = true,
                    Opacity = 0.5F
                };
            }


            if (HintTextLabel == null || HintTextLabel.IsDisposed)
            {
                HintTextLabel = new MirLabel
                {
                    AutoSize = true,
                    BackColour = Color.Transparent,
                    ForeColour = Color.Yellow,
                    Parent = HintBaseLabel,
                };

                HintTextLabel.SizeChanged += (o, e) => HintBaseLabel.Size = HintTextLabel.Size;
            }

            if (MirControl.MouseControl == null || string.IsNullOrEmpty(MirControl.MouseControl.Hint))
            {
                HintBaseLabel.Visible = false;
                return;
            }

            HintBaseLabel.Visible = true;

            HintTextLabel.Text = MirControl.MouseControl.Hint;

            Point point = MPoint.Add(-HintTextLabel.Size.Width, 20);

            if (point.X + HintBaseLabel.Size.Width >= Settings.ScreenWidth)
                point.X = Settings.ScreenWidth - HintBaseLabel.Size.Width - 1;
            if (point.Y + HintBaseLabel.Size.Height >= Settings.ScreenHeight)
                point.Y = Settings.ScreenHeight - HintBaseLabel.Size.Height - 1;

            if (point.X < 0)
                point.X = 0;
            if (point.Y < 0)
                point.Y = 0;

            HintBaseLabel.Location = point;
        }

        private static void ToggleFullScreen()
        {
            Settings.FullScreen = !Settings.FullScreen;

            ApplyWindowMode();

            DXManager.ResetDevice();

            if (MirScene.ActiveScene == GameScene.Scene)
            {
                GameScene.Scene.MapControl.FloorValid = false; 
                GameScene.Scene.TextureValid = false;
            }

            if (!Settings.FullScreen)
                Program.Form.CenterToScreen();
        }

        private static void ApplyWindowMode()
        {
            if (Program.Form == null || Program.Form.IsDisposed)
                return;

            Program.Form.TopMost = Settings.TopMost;

            if (Settings.FullScreen)
            {
                Program.Form.WindowState = FormWindowState.Normal;
                if (Program.Form.FormBorderStyle != FormBorderStyle.None)
                    Program.Form.FormBorderStyle = FormBorderStyle.None;

                Screen screen;
                try
                {
                    screen = Program.Form.IsHandleCreated
                        ? Screen.FromHandle(Program.Form.Handle)
                        : Screen.PrimaryScreen;
                }
                catch
                {
                    screen = Screen.PrimaryScreen;
                }

                Program.Form.Bounds = screen.Bounds;
                return;
            }

            Program.Form.WindowState = FormWindowState.Normal;
            if (Program.Form.FormBorderStyle != FormBorderStyle.FixedDialog)
                Program.Form.FormBorderStyle = FormBorderStyle.FixedDialog;
            Program.Form.ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);
        }

        public IntPtr ForceRecreateHandleForSwapChain()
        {
            if (IsDisposed)
                return IntPtr.Zero;

            if (InvokeRequired)
            {
                try
                {
                    return (IntPtr)Invoke(new Func<IntPtr>(ForceRecreateHandleForSwapChain));
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }

            try
            {
                if (!IsHandleCreated)
                    CreateControl();

                // Force a native HWND recreation. This can help DXGI recover from bad window/swapchain states.
                RecreateHandle();
            }
            catch (Exception ex)
            {
                SaveError($"[HWND] RecreateHandle failed: {ex.Message}");
            }

            return Handle;
        }

        private static void EnsureSysKeyMessageFilter()
        {
            if (_sysKeyMessageFilterInstalled)
                return;

            Application.AddMessageFilter(new SysKeyMessageFilter());
            _sysKeyMessageFilterInstalled = true;
        }

        private sealed class SysKeyMessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                const int WM_SYSKEYDOWN = 0x0104;
                const int WM_SYSKEYUP = 0x0105;
                const int WM_SYSCOMMAND = 0x0112;
                const int VK_F10 = 0x79;
                const int SC_KEYMENU = 0xF100;

                if (m.Msg == WM_SYSKEYDOWN && m.WParam.ToInt32() == VK_F10)
                {
                    CMain_KeyDown(Program.Form ?? Form.ActiveForm, new KeyEventArgs(Keys.F10));
                    return true;
                }

                if (m.Msg == WM_SYSKEYUP && m.WParam.ToInt32() == VK_F10)
                {
                    CMain_KeyUp(Program.Form ?? Form.ActiveForm, new KeyEventArgs(Keys.F10));
                    return true;
                }

                if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_KEYMENU)
                    return true;

                return false;
            }
        }

        public void CreateScreenShot()
        {
            string text = string.Format("[{0} Server {1}] {2} {3:hh\\:mm\\:ss}",
                Settings.P_ServerName.Length > 0 ? Settings.P_ServerName : "Crystal",
                MapControl.User != null ? MapControl.User.Name : "",
                Now.ToShortDateString(),
                Now.TimeOfDay);

            if (DXManager.DXGISwapChain == null || DXManager.Device == null || DXManager.DeviceContext == null)
                return;

            Vortice.Direct3D11.ID3D11Texture2D backbuffer = null;
            Vortice.Direct3D11.ID3D11Texture2D staging = null;

            try
            {
                backbuffer = DXManager.DXGISwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0);
                var desc = backbuffer.Description;

                var stagingDesc = desc;
                stagingDesc.Usage = ResourceUsage.Staging;
                stagingDesc.BindFlags = BindFlags.None;
                stagingDesc.CPUAccessFlags = CpuAccessFlags.Read;
                stagingDesc.MiscFlags = ResourceOptionFlags.None;
                stagingDesc.MipLevels = 1;
                stagingDesc.ArraySize = 1;
                stagingDesc.SampleDescription = new Vortice.DXGI.SampleDescription(1, 0);

                staging = DXManager.Device.CreateTexture2D(stagingDesc);
                DXManager.DeviceContext.CopyResource(staging, backbuffer);

                var mapped = DXManager.DeviceContext.Map(staging, 0, MapMode.Read, MapFlags.None);
                try
                {
                    using var temp = new Bitmap((int)desc.Width, (int)desc.Height, (int)mapped.RowPitch, PixelFormat.Format32bppPArgb, mapped.DataPointer);
                    using var image = new Bitmap(temp);

                    using (Graphics graphics = Graphics.FromImage(image))
                    {
                        StringFormat sf = new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        };

                        graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 3, 10), sf);
                        graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 9), sf);
                        graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 5, 10), sf);
                        graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 11), sf);
                        graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.White, new Point((Settings.ScreenWidth / 2) + 4, 10), sf);//SandyBrown               

                        string path = Path.Combine(Application.StartupPath, @"Screenshots\");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        int count = Directory.GetFiles(path, "*.png").Length;

                        image.Save(Path.Combine(path, string.Format("Image {0}.png", count)), ImageFormat.Png);
                    }
                }
                finally
                {
                    DXManager.DeviceContext.Unmap(staging, 0);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
            finally
            {
                staging?.Dispose();
                backbuffer?.Dispose();
            }
        }

        public static void SaveError(string ex)
        {
            try
            {
                if (Settings.RemainingErrorLogs-- > 0)
                {
                    File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", Now, ex, Environment.NewLine));
                }
            }
            catch (Exception exx)
            {
            }
        }

        public static void SetResolution(int width, int height)
        {
            if (Settings.ScreenWidth == width && Settings.ScreenHeight == height) return;

            Settings.ScreenWidth = width;
            Settings.ScreenHeight = height;

            ApplyWindowMode();

            DXManager.ResetDevice();

            if (!Settings.FullScreen)
                Program.Form.CenterToScreen();
        }

        #region ScreenCapture

        //private Bitmap CaptureScreen()
        //{
            
        //}

        #endregion

        #region Idle Check
        private static bool AppStillIdle
        {
            get
            {
                PeekMsg msg;
                return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool PeekMessage(out PeekMsg msg, IntPtr hWnd, uint messageFilterMin,
                                               uint messageFilterMax, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct PeekMsg
        {
            private readonly IntPtr hWnd;
            private readonly Message msg;
            private readonly IntPtr wParam;
            private readonly IntPtr lParam;
            private readonly uint time;
            private readonly Point p;
        }
        #endregion

        private void CMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CMain.Time < GameScene.LogTime && !Settings.UseTestConfig && !GameScene.Observing)
            {
                GameScene.Scene.ChatDialog.ReceiveChat(string.Format(GameLanguage.CannotLeaveGame, (GameScene.LogTime - CMain.Time) / 1000), ChatType.System);
                e.Cancel = true;
            }
            else
            {
                Settings.Save();

                DXManager.Dispose();
                SoundManager.Dispose();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSKEYDOWN = 0x0104;
            const int WM_SYSKEYUP = 0x0105;
            const int WM_SYSCOMMAND = 0x0112;
            const int VK_F10 = 0x79;

            if (m.Msg == WM_SYSKEYDOWN && m.WParam.ToInt32() == VK_F10)
            {
                CMain_KeyDown(this, new KeyEventArgs(Keys.F10));
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == WM_SYSKEYUP && m.WParam.ToInt32() == VK_F10)
            {
                CMain_KeyUp(this, new KeyEventArgs(Keys.F10));
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_SYSCOMMAND) // WM_SYSCOMMAND
            {
                if (m.WParam.ToInt32() == 0xF100) // SC_KEYMENU
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
                else if (m.WParam.ToInt32() == 0xF030) // SC_MAXIMISE
                {
                    ToggleFullScreen();
                    return;
                }
            }

            base.WndProc(ref m);
        }


        public static Cursor[] Cursors;
        public static MouseCursor CurrentCursor = MouseCursor.None;
        public static void SetMouseCursor(MouseCursor cursor)
        {
            if (!Settings.UseMouseCursors) return;

            if (CurrentCursor != cursor)
            {
                CurrentCursor = cursor;
                Program.Form.Cursor = Cursors[(byte)cursor];
            }
        }

        private static void LoadMouseCursors()
        {
            Cursors = new Cursor[8];

            Cursors[(int)MouseCursor.None] = Program.Form.Cursor;

            string path = $"{Settings.MouseCursorPath}Cursor_Default.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.Default] = LoadCustomCursor(path);

            path = $"{Settings.MouseCursorPath}Cursor_Normal_Atk.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.Attack] = LoadCustomCursor(path);

            path = $"{Settings.MouseCursorPath}Cursor_Compulsion_Atk.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.AttackRed] = LoadCustomCursor(path);

            path = $"{Settings.MouseCursorPath}Cursor_Npc.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.NPCTalk] = LoadCustomCursor(path);

            path = $"{Settings.MouseCursorPath}Cursor_TextPrompt.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.TextPrompt] = LoadCustomCursor(path);

            path = $"{Settings.MouseCursorPath}Cursor_Trash.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.Trash] = LoadCustomCursor(path);

            path = $"{Settings.MouseCursorPath}Cursor_Upgrade.CUR";
            if (File.Exists(path))
                Cursors[(int)MouseCursor.Upgrade] = LoadCustomCursor(path);
        }

        public static Cursor LoadCustomCursor(string path)
        {
            IntPtr hCurs = LoadCursorFromFile(path);
            if (hCurs == IntPtr.Zero) throw new Win32Exception();
            var curs = new Cursor(hCurs);
            return curs;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursorFromFile(string path);
    }
}
