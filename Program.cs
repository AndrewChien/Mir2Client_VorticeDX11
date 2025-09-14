using Client.Resolution;
using Launcher;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Framework;
using Vortice.Mathematics;
using Vortice.WinForms;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.PEEK_MESSAGE_REMOVE_TYPE;
using static Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.WNDCLASS_STYLES;
using AlphaMode = Vortice.DXGI.AlphaMode;
using D2D = Vortice.Direct2D1;
using D3D = Vortice.Direct3D;
using D3D11 = Vortice.Direct3D11;
using DXGI = Vortice.DXGI;
//using Vortice.Windows;

namespace Client
{
    internal static class Program
    {
        public static CMain Form;
        public static AMain PForm;

        public static bool Restart;
        public static bool Launch;

        [STAThread]
        private static void Main(string[] args)
        {
            //TestVortice.TestVortice1();
            //TestVortice.TestVortice2();
            //TestVortice.Test_HelloWindowApp();
            //TestVortice.Test_TriangleApp();
            //TestVortice.Test_BufferOffsetsApp();
            //TestVortice.Test_DrawQuadApp();//闪烁
            //TestVortice.Test_CubeApp();//3D旋转立方体
            //TestVortice.Test_CubeAlphaBlendApp();//错误
            //TestVortice.Test_TexturedCubeApp();//3D旋转立方体
            //TestVortice.Test_TexturedCubeFromFileApp();//3D旋转立方体
            //TestVortice.Test_MipmappingApp();//3D旋转立方体，动态糊化
            //TestVortice.Test_DrawTextApp();//文字
            //return;

            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (arg.ToLower() == "-tc") Settings.UseTestConfig = true;
                }
            }

#if DEBUG
            Settings.UseTestConfig = true;
#endif

            try
            {
                if (UpdatePatcher()) return;

                if (RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully == true) { }

                Packet.IsServer = false;
                Settings.Load();

                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                CheckResolutionSetting();

                Launch = false;
                if (Settings.P_Patcher)
                    System.Windows.Forms.Application.Run(PForm = new AMain());
                else
                    Launch = true;

                if (Launch)
                    System.Windows.Forms.Application.Run(Form = new CMain());

                Settings.Save();

                if (Restart)
                {
                    System.Windows.Forms.Application.Restart();
                }
            }
            catch (Exception ex)
            {
                CMain.SaveError(ex.ToString());
            }
        }

        private static bool UpdatePatcher()
        {
            try
            {
                const string fromName = @".\AutoPatcher.gz", toName = @".\AutoPatcher.exe";
                if (!File.Exists(fromName)) return false;

                Process[] processes = Process.GetProcessesByName("AutoPatcher");

                if (processes.Length > 0)
                {
                    string patcherPath = System.Windows.Forms.Application.StartupPath + @"\AutoPatcher.exe";

                    for (int i = 0; i < processes.Length; i++)
                        if (processes[i].MainModule.FileName == patcherPath)
                            processes[i].Kill();

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    bool wait = true;
                    processes = Process.GetProcessesByName("AutoPatcher");

                    while (wait)
                    {
                        wait = false;
                        for (int i = 0; i < processes.Length; i++)
                            if (processes[i].MainModule.FileName == patcherPath)
                            {
                                wait = true;
                            }

                        if (stopwatch.ElapsedMilliseconds <= 3000) continue;
                        MessageBox.Show("更新期间无法关闭自动修补程序");
                        return true;
                    }
                }

                if (File.Exists(toName)) File.Delete(toName);
                File.Move(fromName, toName);
                Process.Start(toName, "Auto");

                return true;
            }
            catch (Exception ex)
            {
                CMain.SaveError(ex.ToString());

                throw;
            }
        }

        public static class RuntimePolicyHelper
        {
            public static bool LegacyV2RuntimeEnabledSuccessfully { get; private set; }

            static RuntimePolicyHelper()
            {
                //ICLRRuntimeInfo clrRuntimeInfo =
                //    (ICLRRuntimeInfo)RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                //        Guid.Empty,
                //        typeof(ICLRRuntimeInfo).GUID);

                //try
                //{
                //    clrRuntimeInfo.BindAsLegacyV2Runtime();
                //    LegacyV2RuntimeEnabledSuccessfully = true;
                //}
                //catch (COMException)
                //{
                //    // This occurs with an HRESULT meaning 
                //    // "A different runtime was already bound to the legacy CLR version 2 activation policy."
                //    LegacyV2RuntimeEnabledSuccessfully = false;
                //}
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
            private interface ICLRRuntimeInfo
            {
                void xGetVersionString();
                void xGetRuntimeDirectory();
                void xIsLoaded();
                void xIsLoadable();
                void xLoadErrorString();
                void xLoadLibrary();
                void xGetProcAddress();
                void xGetInterface();
                void xSetDefaultStartupFlags();
                void xGetDefaultStartupFlags();

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void BindAsLegacyV2Runtime();
            }
        }

        public static void CheckResolutionSetting()
        {
            var parsedOK = DisplayResolutions.GetDisplayResolutions();
            if (!parsedOK)
            {
                MessageBox.Show("无法获取显示分辨率", "获取显示分辨率问题", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            if (!DisplayResolutions.IsSupported(Settings.Resolution))
            {
                MessageBox.Show($"客户端不支持 {Settings.Resolution} 将设置成默认分辨率 1024x768",
                                "无效的客户端分辨率",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                Settings.Resolution = (int)eSupportedResolution.w1024h768;
                Settings.Save();
            }
        }

    }
}