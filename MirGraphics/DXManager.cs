using Client.MirControls;
using Client.MirScenes;
using SharpGen.Runtime;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vortice;
using Vortice.D3DCompiler;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.Direct3D12;
using Vortice.DirectWrite;
using Vortice.Dxc;
using Vortice.DXGI;
using Vortice.WIC;
using Win32.Numerics;

namespace Client.MirGraphics
{
    class DXManager
    {
        //public static SlimDX.Direct3D9.Device Device;
        //public static SlimDX.Direct3D9.Sprite Sprite;
        //public static SlimDX.Direct3D9.Line Line;
        //public static SlimDX.Direct3D9.Texture RadarTexture;
        //public static List<SlimDX.Direct3D9.Texture> Lights = new List<SlimDX.Direct3D9.Texture>();
        //public static SlimDX.Direct3D9.Texture PoisonDotBackground;
        //public static SlimDX.Direct3D9.Texture FloorTexture, LightTexture;
        //public static SlimDX.Direct3D9.Surface FloorSurface, LightSurface;
        //public static SlimDX.Direct3D9.PixelShader GrayScalePixelShader;
        //public static SlimDX.Direct3D9.PixelShader NormalPixelShader;
        //public static SlimDX.Direct3D9.PixelShader MagicPixelShader;
        //public static SlimDX.Direct3D9.Surface CurrentSurface;
        //public static SlimDX.Direct3D9.Surface MainSurface;
        //public static SlimDX.Direct3D9.PresentParameters Parameters;


        public static List<MImage> TextureList = new List<MImage>();
        public static List<MirControl> ControlList = new List<MirControl>();

        /// <summary>
        /// 全局DXGI工厂
        /// </summary>
        public static Vortice.DXGI.IDXGIFactory2 DxgiFactory;
        /// <summary>
        /// 全局交换链
        /// </summary>
        public static Vortice.DXGI.IDXGISwapChain1 DXGISwapChain;
        public static Vortice.DXGI.SwapChainDescription1 swapChainDescription;
        /// <summary>
        /// 全局DXGI表面（交换链后缓冲区）
        /// </summary>
        public static Vortice.DXGI.IDXGISurface DXGISurface;

        /// <summary>
        /// 全局D3D设备
        /// </summary>
        public static Vortice.Direct3D11.ID3D11Device Device;
        /// <summary>
        /// 全局D3D设备上下文(多线程安全访问需通过ID3D11DeviceContext同步)
        /// </summary>
        public static Vortice.Direct3D11.ID3D11DeviceContext DeviceContext;

        /// <summary>
        /// 全局D2D工厂
        /// </summary>
        public static Vortice.Direct2D1.ID2D1Factory1 D2DFactory;
        /// <summary>
        /// 全局D2D设备
        /// </summary>
        public static Vortice.Direct2D1.ID2D1Device D2D1Device;
        /// <summary>
        /// 全局D2D设备上下文
        /// </summary>
        public static Vortice.Direct2D1.ID2D1DeviceContext D2D1Context;

        public static Vortice.DirectWrite.IDWriteFactory DwFactory;
        public static Vortice.WIC.IWICImagingFactory WicFactory;

        #region RenderTarget

        //使用ID2D1RenderTarget代替精灵Sprite进行2D绘制和线条绘制
        public static Vortice.Direct2D1.ID2D1RenderTarget Sprite;
        public static Vortice.Direct2D1.RenderTargetProperties SpriteRenderTargetProperties;
        //public static Vortice.Direct2D1.ID2D1HwndRenderTarget SpriteLine;
        //表面资源
        public static Vortice.Direct3D11.ID3D11RenderTargetView CurrentSurface;
        public static Vortice.Direct3D11.ID3D11RenderTargetView MainSurface;
        public static Vortice.Direct3D11.ID3D11RenderTargetView FloorSurface, LightSurface;

        #endregion

        #region 纹理资源

        /// <summary>
        /// 后缓冲区（纹理资源）
        /// </summary>
        private static Vortice.Direct3D11.ID3D11Texture2D BackBuffer;
        //纹理资源
        //1、动态纹理需设置Usage = ResourceUsage.Dynamic和CPUAccessFlags = CpuAccessFlags.Write
        //2、渲染目标纹理需包含BindFlags.RenderTarget标志，BindFlags决定纹理用途（如同时作为着色器资源和渲染目标）
        //3、多级纹理需配置正确的MipLevels和ArraySize
        //4、Format需与数据格式匹配（如B8G8R8A8_UNorm对应32位RGBA）
        //5、多线程安全访问需通过ID3D11DeviceContext同步
        public static Vortice.Direct3D11.ID3D11Texture2D RadarTexture;
        public static Vortice.Direct3D11.ID3D11Texture2D PoisonDotBackground;
        public static Vortice.Direct3D11.ID3D11Texture2D FloorTexture, LightTexture;
        public static List<Vortice.Direct3D11.ID3D11Texture2D> Lights = new List<Vortice.Direct3D11.ID3D11Texture2D>();

        public static Vortice.Direct3D11.ID3D11Texture2D DepthStencilTexture;
        public static Vortice.Direct3D11.ID3D11DepthStencilView DepthStencilView;

        public static Vortice.Direct2D1.ID2D1SolidColorBrush TextBrush;

        #endregion

        //着色器资源
        public static Vortice.Direct3D11.ID3D11PixelShader GrayScalePixelShader;
        public static Vortice.Direct3D11.ID3D11PixelShader NormalPixelShader;
        public static Vortice.Direct3D11.ID3D11PixelShader MagicPixelShader;

        public struct VorticeParameters
        {
            public bool inited { get; set; }
            public bool Windowed { get; set; }
            public int BackBufferWidth { get; set; }
            public int BackBufferHeight { get; set; }
            public int PresentationInterval { get; set; }
        }
        public static VorticeParameters Parameters;

        public static bool DeviceLost;
        public static float Opacity = 1F;
        public static bool Blending;
        public static float BlendingRate;
        public static BlendMode BlendingMode;

        public static bool GrayScale;

        public static Point[] LightSizes =
        {
            new Point(125,95),
            new Point(205,156),
            new Point(285,217),
            new Point(365,277),
            new Point(445,338),
            new Point(525,399),
            new Point(605,460),
            new Point(685,521),
            new Point(765,581),
            new Point(845,642),
            new Point(925,703)
        };

        public static void Create()
        {
            //以下是SlimDX创建设备、创建交换链、关联窗口、
            //Parameters = new SlimDX.Direct3D9.PresentParameters
            //{
            //    BackBufferFormat = SlimDX.Direct3D9.Format.X8R8G8B8,
            //    PresentFlags = SlimDX.Direct3D9.PresentFlags.LockableBackBuffer,
            //    BackBufferWidth = Settings.ScreenWidth,
            //    BackBufferHeight = Settings.ScreenHeight,
            //    SwapEffect = SlimDX.Direct3D9.SwapEffect.Discard,
            //    PresentationInterval = Settings.FPSCap ? SlimDX.Direct3D9.PresentInterval.One : SlimDX.Direct3D9.PresentInterval.Immediate,
            //    Windowed = !Settings.FullScreen,
            //};

            //SlimDX.Direct3D9.Direct3D d3d = new SlimDX.Direct3D9.Direct3D();
            //SlimDX.Direct3D9.Capabilities devCaps = d3d.GetDeviceCaps(0, SlimDX.Direct3D9.DeviceType.Hardware);
            //SlimDX.Direct3D9.DeviceType devType = SlimDX.Direct3D9.DeviceType.Reference;
            //SlimDX.Direct3D9.CreateFlags devFlags = SlimDX.Direct3D9.CreateFlags.HardwareVertexProcessing;

            //if (devCaps.VertexShaderVersion.Major >= 2 && devCaps.PixelShaderVersion.Major >= 2)
            //    devType = SlimDX.Direct3D9.DeviceType.Hardware;
            //if ((devCaps.DeviceCaps & SlimDX.Direct3D9.DeviceCaps.HWTransformAndLight) != 0)
            //    devFlags = SlimDX.Direct3D9.CreateFlags.HardwareVertexProcessing;
            //if ((devCaps.DeviceCaps & SlimDX.Direct3D9.DeviceCaps.PureDevice) != 0)
            //    devFlags |= SlimDX.Direct3D9.CreateFlags.PureDevice;

            //Device = new SlimDX.Direct3D9.Device(d3d, d3d.Adapters.DefaultAdapter.Adapter, devType, Program.Form.Handle, devFlags, Parameters);

            //强制禁用独占全屏模式，Vortice中该行为现在由PresentParameters.Windowed属性自动控制创建设备时配置呈现参数
            //若需完全禁用全屏切换，需额外处理WM_SIZE消息并拦截ALT+ENTER组合键
            //Device.SetDialogBoxMode(true);

            #region lyo：Vortice创建设备、创建交换链、关联窗口

            DxgiFactory = Vortice.DXGI.DXGI.CreateDXGIFactory1<Vortice.DXGI.IDXGIFactory2>();
            var hardwareAdapter = GetHardwareAdapter(DxgiFactory).ToList().FirstOrDefault();
            if (hardwareAdapter == null)
            {
                throw new InvalidOperationException("Cannot detect D3D11 adapter");
            }
            Vortice.Direct3D.FeatureLevel[] featureLevels = new[]
            {
                Vortice.Direct3D.FeatureLevel.Level_11_1,
                Vortice.Direct3D.FeatureLevel.Level_11_0,
                Vortice.Direct3D.FeatureLevel.Level_10_1,
                Vortice.Direct3D.FeatureLevel.Level_10_0,
                Vortice.Direct3D.FeatureLevel.Level_9_3,
                Vortice.Direct3D.FeatureLevel.Level_9_2,
                Vortice.Direct3D.FeatureLevel.Level_9_1,
            };
            Vortice.DXGI.IDXGIAdapter1 adapter = hardwareAdapter;
            //带调试标志
            Vortice.Direct3D11.DeviceCreationFlags creationFlags = Vortice.Direct3D11.DeviceCreationFlags.BgraSupport | Vortice.Direct3D11.DeviceCreationFlags.Debug;
            //创建设备
            var result = Vortice.Direct3D11.D3D11.D3D11CreateDevice
            (
                adapter,
                Vortice.Direct3D.DriverType.Unknown,
                creationFlags,
                featureLevels,
                out Vortice.Direct3D11.ID3D11Device d3D11Device, out Vortice.Direct3D.FeatureLevel featureLevel,
                out Vortice.Direct3D11.ID3D11DeviceContext d3D11DeviceContext
            );
            if (result.Failure)
            {
                //创建设备
                result = Vortice.Direct3D11.D3D11.D3D11CreateDevice(
                    IntPtr.Zero,
                    Vortice.Direct3D.DriverType.Warp,
                    creationFlags,
                    featureLevels,
                    out d3D11Device, out featureLevel, out d3D11DeviceContext);
                result.CheckError();
            }
            // 启用对象生命周期跟踪
            //D3D11_DEBUG_FEATURE_FINISH_PER_RENDER_OP (0x2)	应用程序将等待 GPU 完成处理呈现操作，然后再继续。
            //D3D11_DEBUG_FEATURE_FLUSH_PER_RENDER_OP (0x1)	运行时还将调用 ID3D11DeviceContext：：Flush。
            //D3D11_DEBUG_FEATURE_PRESENT_PER_RENDER_OP (0x4)	运行时将调用 IDXGISwapChain：:P resent。
            //呈现缓冲区的呈现将根据之前调用 ID3D11Debug：：SetSwapChain 和 ID3D11Debug：：SetPresentPerRenderOpDelay 建立的设置进行。
            //https://learn.microsoft.com/zh-cn/windows/win32/api/d3d11sdklayers/nf-d3d11sdklayers-id3d11debug-setfeaturemask
            d3D11Device.QueryInterface<ID3D11Debug>().FeatureMask = 0x1; //对应D3D11_DEBUG_FEATURE_TRACK_RESOURCE_LIFETIME

            Device = d3D11Device;
            DeviceContext = d3D11DeviceContext;

            ////注意：此处从Device上复制一个新的对象
            //var Device1 = Device.QueryInterface<Vortice.Direct3D11.ID3D11Device1>();
            ////注意：此处从DeviceContext上复制一个新的对象
            //var DeviceContext1 = DeviceContext.QueryInterface<Vortice.Direct3D11.ID3D11DeviceContext1>();

            //调用 Dispose 不会释放掉刚才申请的 D3D 资源，只是减少引用计数
            //Device.Dispose();
            //DeviceContext.Dispose();

            // 创建完设备，接下来就是关联窗口和交换链
            Vortice.DXGI.Format colorFormat = Vortice.DXGI.Format.B8G8R8A8_UNorm;//B8G8R8A8_UNorm、R8G8B8A8_UNorm、
            const int FrameCount = 2;//大部分应用来说，至少需要两个缓存
            swapChainDescription = new()
            {
                Width = (uint)Program.Form.Width,
                Height = (uint)Program.Form.Height,
                Format = colorFormat,
                BufferCount = FrameCount,
                BufferUsage = Vortice.DXGI.Usage.RenderTargetOutput,
                SampleDescription = Vortice.DXGI.SampleDescription.Default,
                Scaling = Vortice.DXGI.Scaling.Stretch,
                SwapEffect = Vortice.DXGI.SwapEffect.FlipDiscard,
                AlphaMode = Vortice.DXGI.AlphaMode.Ignore
            };
            // 设置不进入全屏
            Vortice.DXGI.SwapChainFullscreenDescription fullscreenDescription = new Vortice.DXGI.SwapChainFullscreenDescription
            {
                Windowed = true
            };
            DXGISwapChain = DxgiFactory.CreateSwapChainForHwnd(Device, Program.Form.Handle, swapChainDescription, fullscreenDescription);
            //附带禁止按下 alt+enter 进入全屏，这是可选的
            DxgiFactory.MakeWindowAssociation(Program.Form.Handle, Vortice.DXGI.WindowAssociationFlags.IgnoreAltEnter);
            //这就完成了最重要的交换链的创建，以上完成之后，即可让 D3D 的内容绘制在窗口上。


            //// 创建D2D1工厂并与D3D设备关联
            //D2DFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory1>();
            ////会导致crash
            ////D2DFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory1>(FactoryType.MultiThreaded, DebugLevel.Information);
            D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
            D2D1Device = D2DFactory.CreateDevice(Device.QueryInterface<IDXGIDevice>());
            D2D1Context = D2D1Device.CreateDeviceContext();

            WicFactory = new Vortice.WIC.IWICImagingFactory();
            DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();

            #endregion

            LoadTextures();
            LoadPixelsShaders();

            //var pixelShader = Device.CreatePixelShader(compiledShaderBytecode);
            //DeviceContext.PSSetShader(pixelShader, null, 0);  // 绑定到像素着色器阶段


            #region 以下创建 D2D 绘制

            //通过 D3D 承载 D2D 的内容。以上完成了 D3D 的初始化，接下来可以通过 DXGI 辅助创建 D2D 的 ID2D1RenderTarget 画布
            //如上图的框架，想要使用 D2D 之前，需要先解决让 D2D 绘制到哪。让 D2D 绘制的输出，可以是一个 IDXGISurface 对象。
            //通过 CreateDxgiSurfaceRenderTarget 方法既可以在 IDXGISurface 创建 ID2D1RenderTarget 对象，让绘制可以输出。
            //而 IDXGISurface 可以从 ID3D11Texture2D 获取到。通过交换链的 GetBuffer 方法可以获取到 ID3D11Texture2D 对象
            //本文将按照这个步骤，创建 ID2D1RenderTarget 画布。除了以上步骤之外，还有其他的方法，详细还请看官方文档的转换框架
            //按照惯例创建 D2D 需要先创建工厂
            //先从交换链获取到 ID3D11Texture2D 对象，通过 IDXGISwapChain1 的 GetBuffer 获取交换链的一个后台缓存
            //BackBuffer = DXGISwapChain.GetBuffer<ID3D11Texture2D>(0);

            //接着使用 QueryInterface 将 ID3D11Texture2D 转换为 IDXGISurface 对象
            // 获取到 dxgi 的平面
            //DXGISurface = BackBuffer.QueryInterface<IDXGISurface>();

            // 对接 D2D 需要创建工厂
            //D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();

            //获取到 IDXGISurface 即可通过 D2D 工厂创建 ID2D1RenderTarget 画布
            //var renderTargetProperties = new RenderTargetProperties(Vortice.DCommon.PixelFormat.Premultiplied);

            //Sprite = D2DFactory.CreateDxgiSurfaceRenderTarget(DXGISurface, renderTargetProperties);

            //这里获取到的 ID2D1RenderTarget 就是可以用来方便绘制 2D 的画布

            //以下修改颜色
            //最简单的绘制方式就是使用 Clear 方法修改颜色。本文只是告诉大家如何进行初始化，不会涉及到如何使用 D2D 绘制的内容
            //在开始调用 Clear 方法之前，需要先调用 BeginDraw 方法，告诉 DX 开始绘制。完成绘制，需要调用 EndDraw 方法告诉 DX 绘制完成。
            //这里必须明确的是，在对 ID2D1RenderTarget 调用各个绘制方法时，不是方法调用完成就渲染完成的，这些方法只是收集绘制指令，而不是立刻进行渲染
            //var renderTarget = Sprite;

            // 开始绘制逻辑
            //Sprite.BeginDraw();

            // 随意创建颜色
            //var color = new Color4((byte)Random.Shared.Next(0), (byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255));
            //Sprite.Clear(color);
            ////renderTarget.DrawText("测试测试测试",);

            //Sprite.EndDraw();

            //以上代码使用随意的颜色清理，调用 Clear 时，将会让整个 ID2D1RenderTarget 使用给定的颜色清理，
            //也就是修改颜色在完成之后，调用一下交换链的 Present 和等待刷新
            //DXGISwapChain.Present(1, PresentFlags.None);

            // 等待刷新
            //DeviceContext.Flush();

            #endregion
        }

        /// <summary>
        /// 加载三个全局着色器
        /// </summary>
        private static unsafe void LoadPixelsShaders()
        {
            var shaderNormalPath = Settings.ShadersPath + "normal.ps";
            //var shaderGrayScalePath = Settings.ShadersPath + "grayscale.ps";
            var shaderGrayScalePath = Settings.ShadersPath + "grayscale.hlsl";
            var shaderMagicPath = Settings.ShadersPath + "magic.ps";

            if (System.IO.File.Exists(shaderNormalPath))
            {
                //using (var gs = SlimDX.Direct3D9.ShaderBytecode.AssembleFromFile(shaderNormalPath, SlimDX.Direct3D9.ShaderFlags.None))
                //{
                //    NormalPixelShader = new SlimDX.Direct3D9.PixelShader(Device, gs);
                //}

                // 编译着色器（文件需提前用FXC编译）
                byte[] psBytecode = File.ReadAllBytes(shaderNormalPath);
                // 创建着色器
                NormalPixelShader = Device.CreatePixelShader(psBytecode);
            }
            if (System.IO.File.Exists(shaderGrayScalePath))
            {
                //使用转换后的"ps_4_0"版本hlsl文件代替
                var compilationResult = Vortice.D3DCompiler.Compiler.CompileFromFile(
                    shaderGrayScalePath,
                    "main",
                    "ps_4_0",
                    ShaderFlags.OptimizationLevel3
                );
                GrayScalePixelShader = Device.CreatePixelShader(compilationResult.Span);

                //以下代码因为着色器文件版本为“ps_1_2”，只能使用DX9加载
                ////using (var gs = SlimDX.Direct3D9.ShaderBytecode.AssembleFromFile(shaderGrayScalePath, SlimDX.Direct3D9.ShaderFlags.None))
                ////{
                ////    GrayScalePixelShader = new SlimDX.Direct3D9.PixelShader(Device, gs);
                ////}

                //// 编译着色器（文件需提前用FXC编译）
                //byte[] psBytecode = File.ReadAllBytes(shaderGrayScalePath);
                //// 创建着色器
                //GrayScalePixelShader = Device.CreatePixelShader(psBytecode);
            }
            if (System.IO.File.Exists(shaderMagicPath))
            {
                //using (var gs = SlimDX.Direct3D9.ShaderBytecode.AssembleFromFile(shaderMagicPath, SlimDX.Direct3D9.ShaderFlags.None))
                //{
                //    MagicPixelShader = new SlimDX.Direct3D9.PixelShader(Device, gs);
                //}

                // 编译着色器（文件需提前用FXC编译）
                byte[] psBytecode = File.ReadAllBytes(shaderMagicPath);
                // 创建着色器
                MagicPixelShader = Device.CreatePixelShader(psBytecode);
            }

            CMain.SaveError(PrintParentMethod() + $"加载像素着色器完成，当前表面{CurrentSurface}");
        }

        private static unsafe void LoadTextures()
        {
            //Sprite = new SlimDX.Direct3D9.Sprite(Device);
            //Line = new SlimDX.Direct3D9.Line(Device) { Width = 1F };
            //MainSurface = Device.GetBackBuffer(0, 0);
            //Device.SetRenderTarget(0, MainSurface);
            //CurrentSurface = MainSurface;

            #region 创建后缓冲区2D纹理、RenderTarge（精灵）、RenderTarge视图

            // 获取交换链的后台缓冲区作为 DXGI 表面
            BackBuffer = DXGISwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0);
            DXGISurface = BackBuffer.QueryInterface<Vortice.DXGI.IDXGISurface>();
            //获取到 IDXGISurface 即可通过 D2D 工厂创建 ID2D1RenderTarget 画布
            SpriteRenderTargetProperties = new RenderTargetProperties(
                new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied));
            Sprite = D2DFactory.CreateDxgiSurfaceRenderTarget(DXGISurface, SpriteRenderTargetProperties);

            MainSurface = Device.CreateRenderTargetView(BackBuffer);
            MainSurface.DebugName = "初始表面";
            CurrentSurface = MainSurface;

            TextBrush = Sprite.CreateSolidColorBrush(new Vortice.Mathematics.Color4(1.0f, 1.0f, 1.0f, 1.0f));//初始化的颜色后面会被参数覆盖

            #endregion

            #region 创建2D纹理、DepthStencil视图

            DepthStencilTexture = Device.CreateTexture2D(Format.D24_UNorm_S8_UInt‌, (uint)Program.Form.Width, 
                (uint)Program.Form.Height, 1, 1, null, BindFlags.DepthStencil);
            DepthStencilView = Device.CreateDepthStencilView(DepthStencilTexture!, 
                new Vortice.Direct3D11.DepthStencilViewDescription(DepthStencilTexture, Vortice.Direct3D11.DepthStencilViewDimension.Texture2D));

            #endregion

            #region 创建雷达、毒雾、灯光等特效2D纹理并填充

            if (RadarTexture == null || RadarTexture.Device == null)
            {
                //创建雷达纹理
                //RadarTexture = new SlimDX.Direct3D9.Texture(Device, 2, 2, 1, SlimDX.Direct3D9.Usage.None, SlimDX.Direct3D9.Format.A8R8G8B8, SlimDX.Direct3D9.Pool.Managed);
                var radarDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = 2,
                    Height = 2,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    //如果需要频繁更新的纹理，应使用Dynamic用法
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// 必须为Dynamic才能使用WriteDiscard
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    //如果要作为渲染目标使用，需要设置RenderTarget
                    //BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource | Vortice.Direct3D11.BindFlags.RenderTarget,
                    //
                    //BindFlags.ShaderResource → CreateShaderResourceView
                    //
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.Write, // 必须设置写访问
                    MiscFlags = ResourceOptionFlags.None //MiscFlags.Shared?
                };
                RadarTexture = Device.CreateTexture2D(radarDesc);

                //用白色背景填充雷达纹理
                //SlimDX.DataRectangle stream = RadarTexture.LockRectangle(0, SlimDX.Direct3D9.LockFlags.Discard);
                var stream = DeviceContext.Map(RadarTexture, 0, Vortice.Direct3D11.MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
                using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(2, 2, (int)stream.RowPitch, System.Drawing.Imaging.PixelFormat.Format32bppArgb, stream.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);
                }
                //RadarTexture.UnlockRectangle(0);
                DeviceContext.Unmap(RadarTexture, 0);
            }
            if (PoisonDotBackground == null || PoisonDotBackground.Device == null)
            {
                //创建毒雾纹理
                //PoisonDotBackground = new SlimDX.Direct3D9.Texture(Device, 5, 5, 1, SlimDX.Direct3D9.Usage.None, SlimDX.Direct3D9.Format.A8R8G8B8, SlimDX.Direct3D9.Pool.Managed);
                var PoisonDotBackgroundDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = 5,
                    Height = 5,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    //如果需要频繁更新的纹理，应使用Dynamic用法
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// 必须为Dynamic才能使用WriteDiscard
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    //如果要作为渲染目标使用，需要设置RenderTarget
                    //BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource | Vortice.Direct3D11.BindFlags.RenderTarget,
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.Write, // 必须设置写访问
                    MiscFlags = ResourceOptionFlags.None //MiscFlags.Shared?
                };
                PoisonDotBackground = Device.CreateTexture2D(PoisonDotBackgroundDesc);

                //用白色背景填充毒雾纹理
                //SlimDX.DataRectangle stream = PoisonDotBackground.LockRectangle(0, SlimDX.Direct3D9.LockFlags.Discard);
                var stream = DeviceContext.Map(PoisonDotBackground,0,Vortice.Direct3D11.MapMode.WriteDiscard,Vortice.Direct3D11.MapFlags.None);
                using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(5,5,(int)stream.RowPitch,System.Drawing.Imaging.PixelFormat.Format32bppArgb,stream.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);
                }
                //PoisonDotBackground.UnlockRectangle(0);
                DeviceContext.Unmap(PoisonDotBackground, 0);
            }
            CreateLights();

            #endregion


            CMain.SaveError(PrintParentMethod() + $"初始化纹理、视图完成，当前表面{CurrentSurface}");
        }

        private unsafe static void CreateLights()
        {

            for (int i = Lights.Count - 1; i >= 0; i--)
                Lights[i].Dispose();

            Lights.Clear();

            for (int i = 1; i < LightSizes.Length; i++)
            {
                // int width = 125 + (57 *i);
                //int height = 110 + (57 * i);
                int width = LightSizes[i].X;
                int height = LightSizes[i].Y;

                //创建灯光纹理
                //SlimDX.Direct3D9.Texture light = new SlimDX.Direct3D9.Texture(Device, width, height, 1, SlimDX.Direct3D9.Usage.None, SlimDX.Direct3D9.Format.A8R8G8B8, SlimDX.Direct3D9.Pool.Managed);
                var lightDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    //如果需要频繁更新的纹理，应使用Dynamic用法
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// 必须为Dynamic才能使用WriteDiscard
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    //如果要作为渲染目标使用，需要设置RenderTarget
                    //BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource | Vortice.Direct3D11.BindFlags.RenderTarget,
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.Write, // 必须设置写访问
                    MiscFlags = ResourceOptionFlags.None //MiscFlags.Shared?
                };
                Vortice.Direct3D11.ID3D11Texture2D light = Device.CreateTexture2D(lightDesc);

                //画灯光，复制进灯光纹理
                //SlimDX.DataRectangle stream = light.LockRectangle(0, SlimDX.Direct3D9.LockFlags.Discard);
                var stream = DeviceContext.Map(light,0,Vortice.Direct3D11.MapMode.WriteDiscard,Vortice.Direct3D11.MapFlags.None);
                using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(width,height,width * 4,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb,stream.DataPointer))
                {
                    using (Graphics graphics = Graphics.FromImage(image))
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            //path.AddEllipse(new Rectangle(0, 0, width, height));
                            //using (PathGradientBrush brush = new PathGradientBrush(path))
                            //{
                            //    graphics.Clear(Color.FromArgb(0, 0, 0, 0));
                            //    brush.SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) };
                            //    brush.CenterColor = Color.FromArgb(255, 255, 255, 255);
                            //    graphics.FillPath(brush, path);
                            //    graphics.Save();
                            //}

                            path.AddEllipse(new Rectangle(0, 0, width, height));
                            using (PathGradientBrush brush = new PathGradientBrush(path))
                            {
                                Color[] blendColours = { Color.White,
                                                     Color.FromArgb(255,210,210,210),
                                                     Color.FromArgb(255,160,160,160),
                                                     Color.FromArgb(255,70,70,70),
                                                     Color.FromArgb(255,40,40,40),
                                                     Color.FromArgb(0,0,0,0)};

                                float[] radiusPositions = { 0f, .20f, .40f, .60f, .80f, 1.0f };

                                ColorBlend colourBlend = new ColorBlend();
                                colourBlend.Colors = blendColours;
                                colourBlend.Positions = radiusPositions;

                                graphics.Clear(Color.FromArgb(0, 0, 0, 0));
                                brush.InterpolationColors = colourBlend;
                                brush.SurroundColors = blendColours;
                                brush.CenterColor = Color.White;
                                graphics.FillPath(brush, path);
                                graphics.Save();
                            }
                        }
                    }
                }
                //light.UnlockRectangle(0);
                DeviceContext.Unmap(light, 0);
                //light.Disposing += (o, e) => Lights.Remove(light);
                Lights.Add(light);
            }
        }

        /// <summary>
        /// 给CurrentSurface赋值
        /// </summary>
        /// <param name="surface"></param>
        public static void SetSurface(ref Vortice.Direct3D11.ID3D11RenderTargetView surface)
        {
            CMain.SaveError(PrintParentMethod() + $"准备设置表面，当前表面{CurrentSurface}");
            if (CurrentSurface == surface)
                return;

            //Sprite.Flush();
            Sprite_Flush();
            CurrentSurface = surface;

            //Device.SetRenderTarget(0, surface);
            surface = Device.CreateRenderTargetView(DXGISwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0));

            CMain.SaveError($"设置表面完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// 直接设置全局着色器
        /// </summary>
        /// <param name="value"></param>
        public static void SetGrayscale(bool value)
        {
            GrayScale = value;

            if (value == true)
            {
                //if (Device.PixelShader == GrayScalePixelShader)
                if (DeviceContext.PSGetShader() == GrayScalePixelShader)
                {
                    return;
                }
                //Sprite.Flush();
                Sprite_Flush();
                //Device.PixelShader = GrayScalePixelShader;
                DeviceContext.PSSetShader(GrayScalePixelShader, null, 0);  // 绑定到像素着色器阶段
            }
            else
            {
                //if (Device.PixelShader == null)
                if (DeviceContext.PSGetShader() == null)
                {
                    return;
                }
                //Sprite.Flush();
                Sprite_Flush();
                //Device.PixelShader = null;
                DeviceContext.PSSetShader(null, null, 0);  // 绑定到像素着色器阶段
            }
            CMain.SaveError(PrintParentMethod() + $"设置着色器完成，当前表面{CurrentSurface}");
        }

        #region 测试代码

        /// <summary>
        /// 修正采样器状态创建、渲染流程（测试用）
        /// </summary>
        /// <param name="texture"></param>
        public static void RenderTexture(Vortice.Direct3D11.ID3D11Texture2D texture)
        {
            var samplerDesc = new Vortice.Direct3D11.SamplerDescription
            {
                Filter = Vortice.Direct3D11.Filter.MinMagMipLinear, // 使用线性过滤
                AddressU = Vortice.Direct3D11.TextureAddressMode.Clamp,
                AddressV = Vortice.Direct3D11.TextureAddressMode.Clamp,
                AddressW = Vortice.Direct3D11.TextureAddressMode.Clamp,
                MipLODBias = 0.0f,
                MaxAnisotropy = 1,
                ComparisonFunc = Vortice.Direct3D11.ComparisonFunction.Always,
                BorderColor = new Vortice.Mathematics.Color4(0, 0, 0, 0),
                MinLOD = 0,
                MaxLOD = (float)0x7F7FFFFF
            };
            ID3D11SamplerState sampler = Device.CreateSamplerState(samplerDesc);

            // 创建着色器资源视图
            var srvDesc = new Vortice.Direct3D11.ShaderResourceViewDescription
            {
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = { MipLevels = 1 }
            };
            var srv = Device.CreateShaderResourceView(texture, srvDesc);
            // 设置渲染状态 (关键修正点4)
            DeviceContext.PSSetShaderResources(0, 1, new[] { srv });
            DeviceContext.PSSetSamplers(0, 1, new[] { sampler });
            // 执行绘制调用
            DeviceContext.Draw(3, 0);
            // 释放资源
            srv.Release();
        }

        static bool debugflag = false;
        static MLibrary testmlib = new MLibrary(Settings.DataPath + "Prguse");
        static int num = 30;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textured">图像</param>
        /// <param name="sourceRect">图像大小</param>
        /// <param name="position">绘制坐标（左上角）</param>
        /// <param name="colord">填充颜色</param>
        /// <param name="opacity">透明度</param>
        public static void DrawTest(Vortice.Direct3D11.ID3D11Texture2D textured, Rectangle? sourceRect, Vector3? position, System.Drawing.Color colord, float opacity)
        {
            ////原：SlimDX.color4.Alpha = opacity;
            ////原：Draw(texture, sourceRect, position, color);

            #region 纯色、线条、文字、图像（直接加载方式）加载测试成功

            //var color = ToColor4_Vortice(colord);
            //var brush = Sprite.CreateSolidColorBrush(color);
            //var text = "测试测试测试";
            //var layoutRect = new Vortice.Mathematics.Rect(50, 50, 700, 100);
            ////1、纯色成功(Sprite换成D2D1Context失败)
            //Sprite.Clear(new Color4((byte)0, (byte)255, (byte)255));
            ////2、线条成功(Sprite换成D2D1Context失败)
            //Sprite.DrawLine(new Vector2(0, 0), new Vector2(200, 200), brush);
            ////3、文字成功(Sprite换成D2D1Context失败)
            //using IDWriteFactory factory = DWrite.DWriteCreateFactory<IDWriteFactory>();
            //using var textFormat = factory.CreateTextFormat("Arial", 32.0f);
            //Sprite.DrawText(text, textFormat, layoutRect, brush, DrawTextOptions.None, MeasuringMode.Natural);
            ////4、图像成功(Sprite换成D2D1Context失败)
            //using var wicFactory = new IWICImagingFactory();
            //using var decoder = wicFactory.CreateDecoderFromFileName("10points.png");
            //using var frame = decoder.GetFrame(0);
            //using var converter = wicFactory.CreateFormatConverter();
            //converter.Initialize(frame, Vortice.WIC.PixelFormat.Format32bppPBGRA);
            //using var d2dBitmap = Sprite.CreateBitmapFromWicBitmap(converter);
            //Sprite.Clear(new Color4(0.1f, 0.1f, 0.1f, 1.0f));//深色背景
            //Sprite.DrawBitmap(d2dBitmap, 1.0f, Vortice.Direct2D1.BitmapInterpolationMode.Linear);//绘制完整位图（自动缩放）
            //Sprite.DrawBitmap(d2dBitmap, 0.8f, Vortice.Direct2D1.BitmapInterpolationMode.Linear, new Vortice.Mathematics.Rect(100, 100, 200, 200));//绘制位图部分区域（源图像裁剪区域）

            #endregion

            #region 标准动态方式（ID3D11Texture2D + Map + Unmap）加载本地图片测试成功（ShaderResource + CreateShaderResourceView + PSSetShaderResource三步生效）

            //// 加载本地文件
            //string imagePath = "0.png";
            //using var wicFactory = new IWICImagingFactory();
            //using var decoder = wicFactory.CreateDecoderFromFileName(imagePath);
            //using var frame = decoder.GetFrame(0);

            //// 创建兼容DXGI的格式转换器
            //using var converter = wicFactory.CreateFormatConverter();
            //converter.Initialize(frame, Vortice.WIC.PixelFormat.Format32bppBGRA, BitmapDitherType.None, null, 0.0, BitmapPaletteType.Custom);
            //int width = converter.Size.Width;
            //int height = converter.Size.Height;
            //uint stride = (uint)(width * 4); // 32bpp = 4字节/像素

            //// 创建Texture
            //var textureDesc = new Texture2DDescription
            //{
            //    Width = (uint)width,// 纹理宽度
            //    Height = (uint)height,// 纹理高度
            //    MipLevels = 1,// 不使用多级mipmap
            //    ArraySize = 1,// 单纹理数组
            //    Format = Format.B8G8R8A8_UNorm,// 必须使用BGRA格式
            //    SampleDescription = new SampleDescription(1, 0),// 非多重采样
            //    Usage = ResourceUsage.Dynamic,// 默认使用方式
            //    BindFlags = BindFlags.ShaderResource,//这里用纯ShaderResource不能加RenderTarget，否则Map时报错
            //    CPUAccessFlags = CpuAccessFlags.Write,// 不需要CPU访问
            //    MiscFlags = ResourceOptionFlags.None // 如果需要共享则设置此标志
            //};
            //ID3D11Texture2D texture = Device.CreateTexture2D(textureDesc);

            //// Map+Unmap复制资源到Texture中
            //try
            //{
            //    var mappedSubresource = DeviceContext.Map(texture, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
            //    byte[] pixelData = new byte[stride * height];
            //    converter.CopyPixels(new Vortice.Mathematics.RectI(0, 0, width, height), stride, pixelData);
            //    Marshal.Copy(pixelData, 0, mappedSubresource.DataPointer, pixelData.Length);
            //}
            //finally
            //{
            //    DeviceContext.Unmap(texture, 0);
            //}

            //// 为ShaderResource方式创建着色器资源视图
            //// 创建纹理及SRV（Vortice.Direct3D11）
            ////从文件加载时需调用D3DX11CreateTextureFromFile生成ID3D11Texture2D对象
            ////必须额外创建着色器资源视图（SRV）才能用于渲染管线，Direct3D11要求纹理资源必须与SRV关联，而Direct3D9可直接使用纹理对象
            ////using var srv = Device.CreateShaderResourceView(texture);
            ////DeviceContext.PSSetShaderResources(0, srv);
            //var srvDesc = new Vortice.Direct3D11.ShaderResourceViewDescription
            //{
            //    Format = textureDesc.Format,
            //    ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
            //    Texture2D = { MipLevels = 1 }
            //};
            //using var shaderResourceView = Device.CreateShaderResourceView(texture, srvDesc);
            //DeviceContext.PSSetShaderResource(0, shaderResourceView);

            //// ID3D11Texture2D(texture)转ID2D1Bitmap以便使用
            //using var surface = texture.QueryInterface<IDXGISurface>();
            //var bitmapProps = new BitmapProperties
            //{
            //    PixelFormat = SpriteRenderTargetProperties.PixelFormat
            //};
            //using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);

            //// 绘制
            //Sprite.Clear(new Vortice.Mathematics.Color4(0, 0, 0, 1));
            //var destRect = new RectangleF(0, 0, d2dBitmap.Size.Width, d2dBitmap.Size.Height);
            //Sprite.DrawBitmap(d2dBitmap, destRect, 1.0f, Vortice.Direct2D1.BitmapInterpolationMode.Linear, null);


            #endregion

            #region 测试加载ChrSel.Lib成功（MLibrary、MImage系统没有问题了）

            //ChrSel-0（800*600背景图）、解图成功
            //Prguse-79（100*117韩版测试logo）、画面问题
            //Prguse-360（456*190警告框）、画面问题
            //Title-203（76*25取消按钮）、画面问题
            //Title-204（76*25取消按钮）、
            //Title-205（76*25取消按钮）、
            testmlib.Initialize();
            if (!testmlib.CheckImage(256))
                return;
            MImage mi = testmlib._images[256];
            var imagelength = mi.Length;
            var texture = mi.Image;

            if (!debugflag)
            {
                debugflag = true;
                GrabImage.ShowImageFromGPU(Device, mi.Image);
            }

            sourceRect = new Rectangle(0, 0, mi.Width, mi.Height);
            position = new Vector3((float)0, (float)0, 0.0F);
            //colord = Color.White;
            opacity = 1;

            //// 为ShaderResource方式创建着色器资源视图
            //if (texture.Description.BindFlags == BindFlags.ShaderResource)
            //{
            //    var srvDesc = new Vortice.Direct3D11.ShaderResourceViewDescription
            //    {
            //        Format = texture.Description.Format,
            //        ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
            //        Texture2D = { MipLevels = 1 }
            //    };
            //    using var shaderResourceView = Device.CreateShaderResourceView(texture, srvDesc);
            //    DeviceContext.PSSetShaderResource(0, shaderResourceView);
            //}
            RenderTexture(texture);

            // ID3D11Texture2D(texture)转ID2D1Bitmap以便使用
            using var surface = texture.QueryInterface<IDXGISurface>();
            var bitmapProps = new BitmapProperties
            {
                PixelFormat = SpriteRenderTargetProperties.PixelFormat
            };
            using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);
            //Sprite.DrawBitmap(d2dBitmap, null, opacity, Vortice.Direct2D1.BitmapInterpolationMode.Linear, null);
            Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, ToColor4_Vortice(colord));

            CMain.SaveError($"DX.DrawTest()完成：DrawBitmap");
            CMain.DPSCounter++;

            texture.Dispose();

            #endregion

            #region 正式代码

            //////CMain.SaveError($"DrawOpaque():textured:{textured.Description.Format}-{textured.Description.Usage}-{textured.Description.Width}-{textured.Description.Height}," +
            //////    $"sourceRect:{sourceRect.Value.Left}-{sourceRect.Value.Top}-{sourceRect.Value.Right}-{sourceRect.Value.Bottom}," +
            //////    $"position:{position.Value.X}-{position.Value.Y}-{position.Value.Z}," +
            //////    $"colord:{colord.Name}," +
            //////    $"opacity:{opacity}");

            ////var option = Vortice.Direct2D1.BitmapOptions.None;
            ////if (textured.Description.Usage == ResourceUsage.Default)
            ////{
            ////    //此处会报错0x80070057
            ////    option = Vortice.Direct2D1.BitmapOptions.Target; // 如果用作渲染目标
            ////}

            ////var color = ToColor4(colord);
            ////var bitmapProperties = new Vortice.Direct2D1.BitmapProperties1(
            ////    pixelFormat: new Vortice.DCommon.PixelFormat(
            ////        Vortice.DXGI.Format.B8G8R8A8_UNorm,
            ////        //1、DXGI_FORMAT_B8G8R8A8_UNORM应搭配D2D1_ALPHA_MODE_PREMULTIPLIED或D2D1_ALPHA_MODE_STRAIGHT
            ////        //2、若格式不含Alpha通道（如DXGI_FORMAT_B8G8R8X8_UNORM），则必须使用D2D1_ALPHA_MODE_IGNORE
            ////        //3、当DXGI表面包含预乘颜色数据时（常见于交换链后台缓冲区），应选择D2D1_ALPHA_MODE_PREMULTIPLIED。这种模式下颜色分量已预先乘以Alpha值，可避免混合时的颜色失真
            ////        //4、若表面数据使用独立Alpha通道（如PNG图像原始数据），需采用D2D1_ALPHA_MODE_STRAIGHT。此时Direct2D会在绘制时自动执行预乘计算
            ////        Vortice.DCommon.AlphaMode.Premultiplied),
            ////    dpiX: 96,//96.0f是Win系统的标准显示DPI设置，该值对应每英寸96像素的比例关系，与大多数显示设备匹配
            ////    dpiY: 96,
            ////    bitmapOptions: option
            ////);
            ////CMain.SaveError($"SpriteLine.DrawOpaque()准备：CreateBitmapFromDxgiSurface");


            //// 为ShaderResource方式创建着色器资源视图
            //if (textured.Description.BindFlags == BindFlags.ShaderResource)
            //{
            //    var srvDesc = new Vortice.Direct3D11.ShaderResourceViewDescription
            //    {
            //        Format = textured.Description.Format,
            //        ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
            //        Texture2D = { MipLevels = 1 }
            //    };
            //    using var shaderResourceView = Device.CreateShaderResourceView(textured, srvDesc);
            //    DeviceContext.PSSetShaderResource(0, shaderResourceView);
            //}

            //// ID3D11Texture2D(texture)转ID2D1Bitmap以便使用
            //using var surface = textured.QueryInterface<IDXGISurface>();
            //var bitmapProps = new BitmapProperties
            //{
            //    PixelFormat = SpriteRenderTargetProperties.PixelFormat
            //};
            //using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);

            //// 设置绘制参数
            //var destRect = new RectangleF(position.Value.X, position.Value.Y,
            //    position.Value.X + sourceRect.Value.Width, position.Value.Y + sourceRect.Value.Height);
            //var sourceRectD2D = new RectangleF(sourceRect.Value.X, sourceRect.Value.Y,
            //    sourceRect.Value.X + sourceRect.Value.Width, sourceRect.Value.Y + sourceRect.Value.Height);
            ////Sprite.DrawBitmap(d2dBitmap, destRect, 1.0f, Vortice.Direct2D1.BitmapInterpolationMode.Linear, null);
            //Sprite.DrawBitmap(d2dBitmap, destRect, opacity, Vortice.Direct2D1.BitmapInterpolationMode.Linear, sourceRectD2D);

            //CMain.SaveError($"Sprite.DrawTest()完成：DrawBitmap");
            //CMain.DPSCounter++;

            #endregion
        }

        #endregion

        /// <summary>
        /// 绘制不透明层
        /// </summary>
        /// <param name="textured">图像</param>
        /// <param name="sourceRect">图片大小</param>
        /// <param name="position">图片坐标偏移量</param>
        /// <param name="colord">填充颜色</param>
        /// <param name="opacity">透明度</param>
        public static void DrawOpaque(Vortice.Direct3D11.ID3D11Texture2D textured, Rectangle? sourceRect, Vector3? position, System.Drawing.Color colord, float opacity)
        {
            CMain.SaveError(PrintParentMethod() + $"绘制不透明层，当前表面{CurrentSurface}，{sourceRect.Value.X}-{sourceRect.Value.Y}-{sourceRect.Value.Width}-{sourceRect.Value.Height}," +
                $"{position.Value.X}-{position.Value.Y}-{position.Value.Z}");

            //DrawTest(textured, sourceRect, position, colord, opacity);
            //return;

            //try
            //{
            //    if (num-- > 0)
            //    {
            //        GrabImage.ShowImageFromGPU(Device, textured);
            //    }
            //}
            //catch (Exception e)
            //{
            //}


            // 为ShaderResource方式创建着色器资源视图
            //RenderTexture(textured);
            if (textured.Description.BindFlags == BindFlags.RenderTarget) //红屏
            {
                return;//测试
            }
            else if (textured.Description.BindFlags == BindFlags.ShaderResource)
            {
                var srvDesc = new Vortice.Direct3D11.ShaderResourceViewDescription
                {
                    Format = textured.Description.Format,
                    ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = { MipLevels = 1 }
                };
                using var shaderResourceView = Device.CreateShaderResourceView(textured, srvDesc);
                DeviceContext.PSSetShaderResource(0, shaderResourceView);
            }
            //else if (textured.Description.BindFlags == BindFlags.DepthStencil)
            //{
            //    var depthStencilViewDesc = new Vortice.Direct3D11.DepthStencilViewDescription
            //    {
            //        Format = textured.Description.Format,
            //        ViewDimension = Vortice.Direct3D11.DepthStencilViewDimension.Texture2D
            //    };
            //    using ID3D11DepthStencilView depthStencilView =
            //        Device.CreateDepthStencilView(textured, depthStencilViewDesc);
            //}

            //原：SlimDX.color4.Alpha = opacity;
            //原：Draw(texture, sourceRect, position, color);


            // ID3D11Texture2D(texture)转ID2D1Bitmap以便使用
            using var surface = textured.QueryInterface<IDXGISurface>();
            var bitmapProps = new BitmapProperties
            {
                PixelFormat = SpriteRenderTargetProperties.PixelFormat
            };
            using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);
            //CMain.SaveError($"DX.DrawOpaque().DrawBitmap()，图片位置x{destRect.X}y{destRect.Y}宽{destRect.Width}高{destRect.Height}，");
            Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, ToColor4_Vortice(colord));

            //CMain.SaveError($"DX.DrawOpaque()：DrawTexture()执行完成\r\n\r\n");
            CMain.DPSCounter++;

            ////测试
            //Result result = DXGISwapChain.Present(1, PresentFlags.None);
            //if (result.Failure && (result.Code == Vortice.DXGI.ResultCode.DeviceRemoved.Code))
            //{
            //    throw new Exception();
            //}

            #region 备份

            //var option = Vortice.Direct2D1.BitmapOptions.None;
            //if (textured.Description.Usage == ResourceUsage.Default)
            //{
            //    //此处会报错0x80070057
            //    option = Vortice.Direct2D1.BitmapOptions.Target; // 如果用作渲染目标
            //}

            //var color = ToColor4(colord);
            //var bitmapProperties = new Vortice.Direct2D1.BitmapProperties1(
            //    pixelFormat: new Vortice.DCommon.PixelFormat(
            //        Vortice.DXGI.Format.B8G8R8A8_UNorm,
            //        //1、DXGI_FORMAT_B8G8R8A8_UNORM应搭配D2D1_ALPHA_MODE_PREMULTIPLIED或D2D1_ALPHA_MODE_STRAIGHT
            //        //2、若格式不含Alpha通道（如DXGI_FORMAT_B8G8R8X8_UNORM），则必须使用D2D1_ALPHA_MODE_IGNORE
            //        //3、当DXGI表面包含预乘颜色数据时（常见于交换链后台缓冲区），应选择D2D1_ALPHA_MODE_PREMULTIPLIED。这种模式下颜色分量已预先乘以Alpha值，可避免混合时的颜色失真
            //        //4、若表面数据使用独立Alpha通道（如PNG图像原始数据），需采用D2D1_ALPHA_MODE_STRAIGHT。此时Direct2D会在绘制时自动执行预乘计算
            //        Vortice.DCommon.AlphaMode.Premultiplied),
            //    dpiX: 96,//96.0f是Win系统的标准显示DPI设置，该值对应每英寸96像素的比例关系，与大多数显示设备匹配
            //    dpiY: 96,
            //    bitmapOptions: option
            //);
            ////CMain.SaveError($"SpriteLine.DrawOpaque()准备：CreateBitmapFromDxgiSurface");

            //// 从Texture创建Bitmap
            //using (var dxgiSurface = textured.QueryInterface<Vortice.DXGI.IDXGISurface>())
            //{
            //    var bitmap = D2D1Context.CreateBitmapFromDxgiSurface(dxgiSurface, bitmapProperties);
            //    //var bitmap = D2D1Context.CreateBitmapFromDxgiSurface(DXGISurface, bitmapProperties);
            //    // 设置绘制参数
            //    var destRect = new RectangleF(
            //        position.Value.X,
            //        position.Value.Y,
            //        position.Value.X + sourceRect.Value.Width,
            //        position.Value.Y + sourceRect.Value.Height
            //    );
            //    var sourceRectD2D = new RectangleF(
            //        sourceRect.Value.X,
            //        sourceRect.Value.Y,
            //        sourceRect.Value.X + sourceRect.Value.Width,
            //        sourceRect.Value.Y + sourceRect.Value.Height
            //    );
            //    // 创建颜色矩阵（示例：红色调增强）
            //    var colorMatrix = new Matrix4x4(
            //        color.R, 0, 0, 0,  // 增强R通道
            //        0, color.G, 0, 0,  // 减弱G通道
            //        0, 0, color.B, 0,  // 减弱B通道
            //        0, 0, 0, 1   // Alpha通道不变
            //                     //0, 0, 0, 0    // 偏移量
            //    );
            //    // 执行绘制
            //    //D2D1Context.BeginDraw();
            //    D2D1Context.DrawBitmap(
            //        bitmap,
            //        destRect,
            //        opacity,
            //        Vortice.Direct2D1.InterpolationMode.Linear,
            //        sourceRectD2D,
            //        colorMatrix
            //    );
            //    //D2D1Context.EndDraw();
            //}
            //CMain.SaveError($"Sprite.DrawOpaque()完成：DrawBitmap");
            //CMain.DPSCounter++;

            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="textured">图像</param>
        /// <param name="sourceRect">图像大小</param>
        /// <param name="position">绘制坐标（左上角）</param>
        /// <param name="colord">填充颜色</param>
        /// <param name="opacity">透明度</param>
        public static void Draw(Vortice.Direct3D11.ID3D11Texture2D textured, Rectangle? sourceRect, Vector3? position, System.Drawing.Color colord)
        {
            CMain.SaveError(PrintParentMethod() + $"正常绘制，当前表面{CurrentSurface}，{sourceRect.Value.X}-{sourceRect.Value.Y}-{sourceRect.Value.Width}-{sourceRect.Value.Height}," +
                $"{position.Value.X}-{position.Value.Y}-{position.Value.Z}");

            //原：Sprite.Draw(texture, sourceRect, Vector3.Zero, position, color);
            //SpriteLine.BeginDraw();
            //SpriteLine.SetTransform(Matrix3x2.Identity);
            //SpriteLine.DrawBitmap(bitmap, new RectF(0, 0, 100, 100));
            //SpriteLine.EndDraw();


            if (textured.Description.BindFlags == BindFlags.RenderTarget) //红屏
            {
                return;//测试
            }
            else if (textured.Description.BindFlags == BindFlags.ShaderResource)
            {
                var srvDesc = new Vortice.Direct3D11.ShaderResourceViewDescription
                {
                    Format = textured.Description.Format,
                    ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = { MipLevels = 1 }
                };
                using var shaderResourceView = Device.CreateShaderResourceView(textured, srvDesc);
                DeviceContext.PSSetShaderResource(0, shaderResourceView);
            }
            //else if (textured.Description.BindFlags == BindFlags.DepthStencil)
            //{
            //    var depthStencilViewDesc = new Vortice.Direct3D11.DepthStencilViewDescription
            //    {
            //        Format = textured.Description.Format,
            //        ViewDimension = Vortice.Direct3D11.DepthStencilViewDimension.Texture2D
            //    };
            //    using ID3D11DepthStencilView depthStencilView =
            //        Device.CreateDepthStencilView(textured, depthStencilViewDesc);
            //}

            // ID3D11Texture2D(texture)转ID2D1Bitmap以便使用
            using var surface = textured.QueryInterface<IDXGISurface>();
            var bitmapProps = new BitmapProperties
            {
                PixelFormat = SpriteRenderTargetProperties.PixelFormat
            };
            using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);

            // 设置绘制参数
            Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, ToColor4_Vortice(colord));

            //CMain.SaveError($"DX.Draw()完成：DrawBitmap\r\n\r\n");//太多
            CMain.DPSCounter++;
            //CMain.SaveError($"DX.Draw()：DrawTexture()执行完成\r\n\r\n");
        }


        public static void AttemptReset()
        {
            CMain.SaveError($"设备丢失DeviceLost，执行AttemptReset，当前表面{CurrentSurface}");
            try
            {
                //SlimDX.Result result = DXManager.Device.TestCooperativeLevel();
                //if (result.Code == SlimDX.Direct3D9.ResultCode.DeviceLost.Code)
                //{
                //    return;
                //}
                //if (result.Code == SlimDX.Direct3D9.ResultCode.DeviceNotReset.Code)
                //{
                //    DXManager.ResetDevice();
                //    return;
                //}
                //if (result.Code != SlimDX.Direct3D9.ResultCode.Success.Code)
                //{
                //    return;
                //}
                //DXManager.DeviceLost = false;


                DeviceLost = false;
                //Vortice.DXGI.ResultCode.DeviceRemoved.Code
                if (Device.DeviceRemovedReason == SharpGen.Runtime.Result.Ok)
                {
                    return;
                }
                else if (Device.DeviceRemovedReason == SharpGen.Runtime.Result.False)
                {
                    ResetDevice();
                    return;
                }
                else if (Device.DeviceRemovedReason == SharpGen.Runtime.Result.Fail
                    || Device.DeviceRemovedReason == SharpGen.Runtime.Result.UnexpectedFailure)
                {
                    DeviceLost = false;
                    return;
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void ResetDevice()
        {
            CMain.SaveError(PrintParentMethod() + $"重置设备开始，当前表面{CurrentSurface}");

            CleanUp();
            DeviceLost = true;

            //if (DXManager.Parameters == null) return;

            Size clientSize = Program.Form.ClientSize;
            if (clientSize.Width == 0 || clientSize.Height == 0)
                return;

            Parameters.Windowed = !Settings.FullScreen;
            Parameters.BackBufferWidth = clientSize.Width;
            Parameters.BackBufferHeight = clientSize.Height;
            Parameters.PresentationInterval = Settings.FPSCap ? 0 : -1;
            //DXManager.Device.Reset(DXManager.Parameters);//实现：1、释放现有资源renderTargetView?.Dispose()，2、调整交换链；2、重建渲染管线；

            LoadTextures();

            CMain.SaveError($"重置设备完成，当前表面{CurrentSurface}");
        }

        public static void AttemptRecovery()
        {
            try
            {
                //Sprite.End(); 
                Sprite_End();
            }
            catch (Exception ex)
            {
            }

            try
            {
                //Device.EndScene();
                //Device.Release();
            }
            catch (Exception ex)
            {
            }

            try
            {
                //MainSurface = Device.GetBackBuffer(0, 0);
                //CurrentSurface = MainSurface;
                //Device.SetRenderTarget(0, MainSurface);
                // 获取后缓冲区纹理
                BackBuffer = DXGISwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0);
                MainSurface = Device.CreateRenderTargetView(BackBuffer);
                CurrentSurface = MainSurface;
            }
            catch (Exception ex)
            {
            }
            CMain.SaveError(PrintParentMethod() + $"发生异常，执行Attempt恢复，当前表面{CurrentSurface}");
        }

        #region lyo：Vortice.Direct3D11.SetRenderState

        /*
         * lyo:
            | Direct3D9状态参数 | Direct3D11对应实现 |
            |---------------------------|--------------------------------------------|
            | D3DRS_FILLMODE | RasterizerStateDescription.FillMode |
            | D3DRS_CULLMODE | RasterizerStateDescription.CullMode |
            | D3DRS_ZENABLE | DepthStencilStateDescription.DepthEnable |
            | D3DRS_ALPHABLENDENABLE | BlendStateDescription.RenderTarget[0].BlendEnable |

            SlimDX.Direct3D9 方法	Vortice.Direct3D11 实现	说明
            |---------------------------|--------------------------------------------|
            SetRenderState(D3DRS_FILLMODE, ...)	RasterizerStateDescription.FillMode	设置填充模式
            SetRenderState(D3DRS_CULLMODE, ...)	RasterizerStateDescription.CullMode	设置背面剔除模式
            SetRenderState(D3DRS_ZENABLE, ...)	DepthStencilStateDescription.DepthEnable	启用/禁用深度测试
            SetRenderState(D3DRS_ALPHABLENDENABLE, ...)	BlendStateDescription.RenderTarget.BlendEnable	启用/禁用Alpha混合
            TestCooperativeLevel()	Device.IsValid	检查设备状态
         */

        /// <summary>
        /// lyo
        /// 设置线框模式(对应D3DRS_FILLMODE)
        /// </summary>
        /// <param name="enable"></param>
        public static void SetWireframeMode(bool enable)
        {
            CMain.SaveError(PrintParentMethod());
            var desc = new Vortice.Direct3D11.RasterizerDescription
            {
                FillMode = enable ? Vortice.Direct3D11.FillMode.Wireframe : Vortice.Direct3D11.FillMode.Solid,
                CullMode = Vortice.Direct3D11.CullMode.Back
            };
            using var state = Device.CreateRasterizerState(desc);
            Device.ImmediateContext.RSSetState(state);
        }

        /// <summary>
        /// lyo
        /// 设置Alpha混合(对应D3DRS_ALPHABLENDENABLE)
        /// </summary>
        /// <param name="enable"></param>
        public static void ALPHABLENDENABLE(bool enable)
        {
            CMain.SaveError(PrintParentMethod());
            var desc = new Vortice.Direct3D11.BlendDescription(Vortice.Direct3D11.Blend.SourceAlpha,
                    Vortice.Direct3D11.Blend.InverseSourceAlpha);
            using var state = Device.CreateBlendState(desc);
            Device.ImmediateContext.OMSetBlendState(state);
        }

        #endregion

        public static void SetOpacity(float opacity)
        {
            CMain.SaveError(PrintParentMethod() + $"执行设置透明度开始，当前表面{CurrentSurface}，透明度为{opacity}");
            if (Opacity == opacity)
                return;

            //Sprite.Flush();
            Sprite_Flush();

            //Device.SetRenderState(SlimDX.Direct3D9.RenderState.AlphaBlendEnable, true);
            //DeviceSetRenderState_AlphaBlendEnable(true);

            //Device.ImmediateContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription(
            //    Vortice.Direct3D11.Blend.SourceAlpha, Vortice.Direct3D11.Blend.InverseSourceAlpha)));

            //// 创建混合状态描述
            //var blendDesc = new Vortice.Direct3D11.BlendDescription
            //{
            //    RenderTarget = new []
            //    {
            //        new Vortice.Direct3D11.RenderTargetBlendDescription
            //        {
            //            BlendEnable = true,
            //            SourceBlend = Vortice.Direct3D11.Blend.SourceAlpha,
            //            DestinationBlend = Vortice.Direct3D11.Blend.InverseSourceAlpha,
            //            BlendOperation = Vortice.Direct3D11.BlendOperation.Add,
            //            SourceBlendAlpha = Vortice.Direct3D11.Blend.One,
            //            DestinationBlendAlpha = Vortice.Direct3D11.Blend.Zero,
            //            BlendOperationAlpha = Vortice.Direct3D11.BlendOperation.Add,
            //            RenderTargetWriteMask = Vortice.Direct3D11.ColorWriteEnable.All
            //        }
            //    }
            //};
            //// 生成状态对象
            //using var blendState = Device.CreateBlendState(blendDesc);
            //// 绑定到输出合并阶段
            //Device.ImmediateContext.OMSetBlendState(blendState);

            if (opacity >= 1 || opacity < 0)
            {
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.SourceBlend, SlimDX.Direct3D9.Blend.SourceAlpha);
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.DestinationBlend, SlimDX.Direct3D9.Blend.InverseSourceAlpha);
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.SourceBlendAlpha, SlimDX.Direct3D9.Blend.One);
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.BlendFactor, Color.FromArgb(255, 255, 255, 255).ToArgb());

                Device.ImmediateContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription
                {
                    RenderTarget = new Vortice.Direct3D11.BlendDescription.RenderTarget__FixedBuffer()
                    {
                        e0 = new Vortice.Direct3D11.RenderTargetBlendDescription
                        {
                            BlendEnable = true,  // 对应D3DRS_ALPHABLENDENABLE
                            SourceBlend = Vortice.Direct3D11.Blend.SourceAlpha,//SourceBlend=SourceAlpha
                            DestinationBlend = Vortice.Direct3D11.Blend.InverseSourceAlpha,//DestinationBlend=InverseSourceAlpha
                            SourceBlendAlpha = Vortice.Direct3D11.Blend.One,//SourceBlendAlpha=One
                        }
                    }
                }), new Vortice.Mathematics.Color4(1.0f, 1.0f, 1.0f, 1.0f));//BlendFactor=Color.FromArgb(255, 255, 255, 255).ToArgb()// RGBA(255,255,255,255)
            }
            else
            {
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.SourceBlend, SlimDX.Direct3D9.Blend.BlendFactor);
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.DestinationBlend, SlimDX.Direct3D9.Blend.InverseBlendFactor);
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.SourceBlendAlpha, SlimDX.Direct3D9.Blend.SourceAlpha);
                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.BlendFactor, Color.FromArgb((byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity)).ToArgb());

                Device.ImmediateContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription   //报错0x80070057，CreateBlendState参数错误
                {
                    RenderTarget = new Vortice.Direct3D11.BlendDescription.RenderTarget__FixedBuffer()
                    {
                        e0 = new Vortice.Direct3D11.RenderTargetBlendDescription
                        {
                            BlendEnable = true,  // 对应D3DRS_ALPHABLENDENABLE
                            SourceBlend = Vortice.Direct3D11.Blend.BlendFactor,//SourceBlend=BlendFactor
                            DestinationBlend = Vortice.Direct3D11.Blend.InverseBlendFactor,//DestinationBlend=InverseBlendFactor
                            SourceBlendAlpha = Vortice.Direct3D11.Blend.SourceAlpha,//SourceBlendAlpha=SourceAlpha
                        }
                    }
                }), new Vortice.Mathematics.Color4((byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity)));
            }
            Opacity = opacity;
            CMain.SaveError($"执行设置透明度完成，当前表面{CurrentSurface}，透明度为{opacity}");
            //Sprite.Flush();
            Sprite_Flush();
        }
        public static void SetBlend(bool value, float rate = 1F, BlendMode mode = BlendMode.NORMAL)
        {
            CMain.SaveError(PrintParentMethod() + $"开始执行Blend，当前表面{CurrentSurface},rate={rate},mode={mode}");
            if (value == Blending && BlendingRate == rate && BlendingMode == mode) 
                return;

            Blending = value;
            BlendingRate = rate;
            BlendingMode = mode;

            //Sprite.Flush();
            Sprite_Flush();

            //Sprite.End();
            Sprite_End();

            if (Blending)
            {
                //Sprite.Begin(SlimDX.Direct3D9.SpriteFlags.DoNotSaveState);
                SpriteBegin_DoNotSaveState();

                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.AlphaBlendEnable, true);
                DeviceSetRenderState_AlphaBlendEnable(true);

                switch (BlendingMode)
                {
                    case BlendMode.INVLIGHT:
                        //Device.SetRenderState(SlimDX.Direct3D9.RenderState.BlendOperation, SlimDX.Direct3D9.BlendOperation.Add);
                        //Device.SetRenderState(SlimDX.Direct3D9.RenderState.SourceBlend, SlimDX.Direct3D9.Blend.BlendFactor);
                        //Device.SetRenderState(SlimDX.Direct3D9.RenderState.DestinationBlend, SlimDX.Direct3D9.Blend.InverseSourceColor);

                        Device.ImmediateContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription
                        {
                            RenderTarget = new Vortice.Direct3D11.BlendDescription.RenderTarget__FixedBuffer()
                            {
                                e0 = new Vortice.Direct3D11.RenderTargetBlendDescription
                                {
                                    //BlendEnable = true,  // 对应D3DRS_ALPHABLENDENABLE
                                    BlendOperation = Vortice.Direct3D11.BlendOperation.Add,  //BlendOperation=Add
                                    SourceBlend = Vortice.Direct3D11.Blend.BlendFactor,//SourceBlend=BlendFactor
                                    DestinationBlend = Vortice.Direct3D11.Blend.InverseSourceColor,//DestinationBlend=InverseSourceColor
                                }
                            }
                        }), new Vortice.Mathematics.Color4((byte)(255 * BlendingRate), (byte)(255 * BlendingRate), (byte)(255 * BlendingRate), (byte)(255 * BlendingRate)));
                        break;
                    default:
                        //Device.SetRenderState(SlimDX.Direct3D9.RenderState.SourceBlend, SlimDX.Direct3D9.Blend.SourceAlpha);
                        //Device.SetRenderState(SlimDX.Direct3D9.RenderState.DestinationBlend, SlimDX.Direct3D9.Blend.One);

                        Device.ImmediateContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription  //报错0x80070057，参数错误
                        {
                            RenderTarget = new Vortice.Direct3D11.BlendDescription.RenderTarget__FixedBuffer()
                            {
                                e0 = new Vortice.Direct3D11.RenderTargetBlendDescription
                                {
                                    //BlendEnable = true,  // 对应D3DRS_ALPHABLENDENABLE
                                    SourceBlend = Vortice.Direct3D11.Blend.SourceAlpha,//SourceBlend=SourceAlpha
                                    DestinationBlend = Vortice.Direct3D11.Blend.One,//DestinationBlend=One
                                }
                            }
                        }), new Vortice.Mathematics.Color4((byte)(255 * BlendingRate), (byte)(255 * BlendingRate), (byte)(255 * BlendingRate), (byte)(255 * BlendingRate)));
                        break;
                }

                //Device.SetRenderState(SlimDX.Direct3D9.RenderState.BlendFactor, Color.FromArgb((byte)(255 * BlendingRate), (byte)(255 * BlendingRate),
                //                                                (byte)(255 * BlendingRate), (byte)(255 * BlendingRate)).ToArgb());
            }
            else
            {
                //Sprite.Begin(SlimDX.Direct3D9.SpriteFlags.AlphaBlend);
                SpriteBegin_AlphaBlend();
            }

            //Device.SetRenderTarget(0, CurrentSurface);
            CurrentSurface = Device.CreateRenderTargetView(DXGISwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0));
            CMain.SaveError($"执行Blend完成，当前表面{CurrentSurface},rate={rate},mode={mode}");
        }

        #region lyo：原方法快捷平替

        public void DrawTextToTexture(ID2D1Bitmap textureBitmap, string text, System.Drawing.Font font,
    System.Drawing.Color foreColor, System.Drawing.Color backColor, Size size, DrawTextOptions drawOptions,
    int outLine, System.Drawing.Color borderColor)
        {
            foreColor = Color.White;
            backColor = Color.White;
            borderColor = Color.White;

            // 开始绘制
            //Sprite.BeginDraw();

            //背景色
            Sprite.Clear(new Vortice.Mathematics.Color4(
                backColor.R / 255f,
                backColor.G / 255f,
                backColor.B / 255f,
                backColor.A / 255f
            ));
            // 设置文本颜色
            TextBrush.Color = new Vortice.Mathematics.Color4(
                foreColor.R / 255.0f,
                foreColor.G / 255.0f,
                foreColor.B / 255.0f,
                foreColor.A / 255.0f);
            // 设置边框颜色
            var _borderColor = new Vortice.Mathematics.Color4(
                borderColor.R / 255.0f,
                borderColor.G / 255.0f,
                borderColor.B / 255.0f,
                borderColor.A / 255.0f);

            // 创建D2D文本格式
            using (var textFormat = DXManager.DwFactory.CreateTextFormat(
                font.FontFamily.Name,
                font.Bold ? FontWeight.Bold : FontWeight.Normal,
                font.Italic ? Vortice.DirectWrite.FontStyle.Italic : Vortice.DirectWrite.FontStyle.Normal,
                font.Size))
            {
                textFormat.TextAlignment = TextAlignment.Leading;
                textFormat.ParagraphAlignment = ParagraphAlignment.Near;

                //画边框
                // 计算文本边界（需配合DirectWrite测量）
                using var borderBrush = Sprite.CreateSolidColorBrush(_borderColor);
                var textLayout = DXManager.DwFactory.CreateTextLayout(text, textFormat, textureBitmap.Size.Width, textureBitmap.Size.Height);
                var metrics = textLayout.Metrics;
                switch (outLine)
                {
                    case 0://无边框
                        break;
                    case 1://普通边框
                        // 扩展边界作为边框区域
                        var borderRect = new Vortice.Mathematics.Rect(
                            0 - 2,    // 左边距
                            0 - 2,     // 上边距
                            0 + metrics.Width + 4,  // 右边界
                            0 + metrics.Height + 4   // 下边界
                        );
                        Sprite.DrawRectangle(borderRect, borderBrush, 1.5f);
                        break;
                    case 2://圆角矩形边框
                        //// 绘制圆角矩形边框
                        //Sprite.DrawRoundedRectangle(
                        //    new RoundedRectangle(borderRect, 3, 3),  // 圆角半径
                        //    TextBrush,
                        //    2.0f  // 边框粗细
                        //);
                        break;
                    case 3://虚线边框
                        //// 虚线边框
                        //var strokeStyle = Sprite.Factory.CreateStrokeStyle(new StrokeStyleProperties
                        //{
                        //    DashStyle = DashStyle.Dash
                        //});
                        //Sprite.DrawRectangle(borderRect, borderBrush, 1.5f, strokeStyle);
                        break;
                    case 4://渐变边框
                        //// 渐变边框
                        //using var gradientBrush = Sprite.CreateLinearGradientBrush(/* 渐变参数 */);
                        //Sprite.DrawRectangle(borderRect, gradientBrush, 3.0f);
                        break;
                    default:
                        break;
                }

                // 绘制文本
                Sprite.DrawText(text, textFormat, new RawRectF(1, 0, size.Width, size.Height), TextBrush, drawOptions);
            }
            // 结束绘制
            //Sprite.EndDraw();
        }

        public static ID2D1Bitmap CreateTextureBitmap(Size size)
        {
            // 创建WIC位图
            using (var wicBitmap = DXManager.WicFactory.CreateBitmap((uint)size.Width, (uint)size.Height,
                Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnLoad))
            {
                // 从WIC位图创建D2D位图
                return Sprite.CreateBitmapFromWicBitmap(wicBitmap);
            }
        }

        public static ID3D11Texture2D CreateTextureFromBytes(byte[] data, uint width, uint height, ref nint point)
        {
            //原：
            //Image = new Texture(DXManager.Device, w, h, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
            //DataRectangle stream = Image.LockRectangle(0, LockFlags.Discard);
            //Data = (byte*)stream.Data.DataPointer;
            //DecompressImage(reader.ReadBytes(Length), stream.Data);
            //stream.Data.Dispose();
            //Image.UnlockRectangle(0);

            ////检查图像完整1
            //if (num1++ < count)
            //{
            //    GrabImage.ShowImageFromCPU(data, Width, Height);
            //}

            // 验证输入数据
            if (data == null || data.Length == 0)
                throw new ArgumentException("Invalid image data");

            if (data.Length < width * height * 4)
                throw new ArgumentException("Image data size does not match dimensions");

            // 配置纹理描述 (关键修正点1)
            var texDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// Pool.Managed等效配置
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,// Usage.None默认绑定
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
            };

            // 准备子资源数据 (关键修正点2)
            var initData = new Vortice.Direct3D11.SubresourceData(Marshal.AllocHGlobal(data.Length), (uint)width * 4, 0);
            point = initData.DataPointer;

            try
            {
                // 复制数据到非托管内存
                Marshal.Copy(data, 0, initData.DataPointer, data.Length);

                // 创建纹理资源
                return DXManager.Device.CreateTexture2D(texDesc, new[] { initData });
            }
            finally
            {
                Marshal.FreeHGlobal(initData.DataPointer);
            }
        }

        /// <summary>
        /// 调试神器：获取调用链信息
        /// </summary>
        public static string PrintParentMethod()
        {
            var ret = "调用链:";
            // 获取当前方法调用的详细信息
            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            // 通常我们关心的是除了当前方法之外的下一个方法，即父方法
            if (stackFrames != null && stackFrames.Length > 1)
            {
                StackFrame pFrame = stackFrames[2]; // 获取调用栈中的第二个帧，即父方法
                var pmethod = pFrame.GetMethod(); // 获取方法信息
                var pdeclaringType = pmethod.DeclaringType; // 获取声明此方法的类型（类）
                string pmethodName = pmethod.Name; // 获取方法名称
                string ptypeName = pdeclaringType.Name; // 获取类型名称（类名）
                string pnamespaceName = pdeclaringType.Namespace; // 获取命名空间

                StackFrame frame = stackFrames[1]; // 获取调用栈中的第一个帧，即本方法
                MethodBase method = frame.GetMethod(); // 获取方法信息
                Type declaringType = method.DeclaringType; // 获取声明此方法的类型（类）
                string methodName = method.Name; // 获取方法名称
                string typeName = declaringType.Name; // 获取类型名称（类名）
                string namespaceName = declaringType.Namespace; // 获取命名空间

                //ret = $"{pnamespaceName}::{ptypeName}.{pmethodName}({pFrame.GetFileLineNumber()}) → {namespaceName}::{typeName}.{methodName}({frame.GetFileLineNumber()})";
                ret = $"{ptypeName}.{pmethodName}({pFrame.GetFileLineNumber()}) → {typeName}.{methodName}({frame.GetFileLineNumber()})";
            }
            return ret;
        }

        public static bool CheckDeviceLost()
        {
            CMain.SaveError(PrintParentMethod() + $"检查设备丢失，当前表面{CurrentSurface}");
            try
            {
                var reason = Device.DeviceRemovedReason;
                if (reason.Success)
                    return false;
                //Console.WriteLine($"Device lost reason: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Device check failed: {ex.Message}");
                return true;
            }
        }

        public static bool QueryTextureStateFail(ID3D11Texture2D texture)
        {
            CMain.SaveError(PrintParentMethod() + $"查询DXGI资源接口，当前表面{CurrentSurface}");
            // 查询DXGI资源接口
            try
            {
                var dxgiResource = texture.QueryInterface<IDXGIResource>();
                //Console.WriteLine($"DXGI Resource Usage: {dxgiResource.Usage}");
                dxgiResource.Release();
                return false;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Failed to query DXGI resource: {ex.Message}");
                return true;
            }

            //// 获取纹理描述
            //var desc = texture.Description;
            //Console.WriteLine($"Texture Format: {desc.Format}");
            //Console.WriteLine($"Texture Size: {desc.Width}x{desc.Height}");
            //Console.WriteLine($"MipLevels: {desc.MipLevels}");
            //Console.WriteLine($"ArraySize: {desc.ArraySize}");
            //Console.WriteLine($"Sample Count: {desc.SampleDescription.Count}");
            //Console.WriteLine($"Bind Flags: {desc.BindFlags}");
        }

        public static void LineDraw(System.Numerics.Vector2[] lines, System.Drawing.Color color)
        {
            CMain.SaveError(PrintParentMethod() + $"画线条，当前表面{CurrentSurface}");
            DrawLines(lines, color);
        }

        public static void DrawLines(Vector2[] points, Color color)
        {
            //// 创建画刷
            //ID2D1SolidColorBrush brush = SpriteLine.CreateSolidColorBrush(
            //    new Vortice.Mathematics.Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f)
            //);
            //// 绘制线段
            ////SpriteLine.BeginDraw();
            //for (int i = 0; i < points.Length - 1; i++)
            //{
            //    SpriteLine.DrawLine(
            //        new Vector2(points[i].X, points[i].Y),
            //        new Vector2(points[i + 1].X, points[i + 1].Y),
            //        brush
            //    );
            //}
            ////SpriteLine.EndDraw();


            // 创建画刷
            ID2D1SolidColorBrush brush = Sprite.CreateSolidColorBrush(
                new Vortice.Mathematics.Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f)
            );
            // 绘制线段
            //Sprite.BeginDraw();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Sprite.DrawLine(
                    new Vector2(points[i].X, points[i].Y),
                    new Vector2(points[i + 1].X, points[i + 1].Y),
                    brush
                );
            }
            //Sprite.EndDraw();
        }

        /// <summary>
        /// Texture.GetSurfaceLevel(0)
        /// </summary>
        /// <returns></returns>
        public static unsafe Vortice.Direct3D11.ID3D11RenderTargetView GetSurfaceLevel(Vortice.Direct3D11.ID3D11Texture2D texture, int level)
        {
            CMain.SaveError(PrintParentMethod() + $"CreateRenderTargetView完成，当前表面{CurrentSurface}");

            // 验证纹理是否支持RenderTarget
            if ((texture.Description.BindFlags & BindFlags.RenderTarget) == 0)
            {
                throw new InvalidOperationException("Texture was not created with RenderTarget bind flag");
            }
            // 创建RenderTargetView描述符
            var rtvDesc = new Vortice.Direct3D11.RenderTargetViewDescription
            {
                Format = texture.Description.Format,
                ViewDimension = Vortice.Direct3D11.RenderTargetViewDimension.Texture2D,
                Texture2D = new Vortice.Direct3D11.Texture2DRenderTargetView
                {
                    MipSlice = 0
                }
            };
            // 创建并返回RenderTargetView
            return Device.CreateRenderTargetView(texture, rtvDesc);
        }

        /// <summary>
        /// 释放纹理资源
        /// Texture.UnlockRectangle(0)
        /// </summary>
        public static void TextureUnlockRectangle(Vortice.Direct3D11.ID3D11Resource Texture, uint source)
        {
            CMain.SaveError(PrintParentMethod());
            //Texture.UnlockRectangle(0);
            DeviceContext.Unmap(Texture, source);
        }

        /// <summary>
        /// 锁住纹理资源
        /// Texture.LockRectangle(0, LockFlags.Discard)
        /// </summary>
        /// <returns></returns>
        public static Vortice.Direct3D11.MappedSubresource TextureLockRectangle_0Discard(Vortice.Direct3D11.ID3D11Resource Texture)
        {
            CMain.SaveError(PrintParentMethod());
            //DataRectangle stream = Texture.LockRectangle(0, LockFlags.Discard);
            return DeviceContext.Map(Texture, 0, Vortice.Direct3D11.MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        }

        /// <summary>
        /// ControlTexture、FloorTexture、LightTexture使用RenderTarget创建
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Vortice.Direct3D11.ID3D11Texture2D NewTexture_RenderTarget_Default(uint width, uint height)
        {
            //return CreateTexture_DepthStencil(width, height);

            //CMain.SaveError(PrintParentMethod());

            /* 关于BindFlags的详细说明：
            //
            //1. 基础绑定类型‌
            //BindFlags.ShaderResource → Device.CreateShaderResourceView（允许纹理作为着色器资源，如纹理采样）
            //将资源绑定为着色器输入（如纹理采样），需配合CreateShaderResourceView使用，适用于贴图、缓冲区等需在着色器中读取的场景
            //ShaderResource (D3D11_BIND_SHADER_RESOURCE)‌
            //允许资源作为着色器输入（如纹理采样）
            //需配合CreateShaderResourceView创建视图绑定到管线
            //示例：将贴图绑定到像素着色器进行UV映射
            //
            //BindFlags.RenderTarget → Device.CreateRenderTargetView（将纹理作为渲染目标输出）
            //将纹理作为渲染目标输出，需通过CreateRenderTargetView创建视图，常用于离屏渲染或多重渲染目标（MRT）            
            //RenderTarget (D3D11_BIND_RENDER_TARGET)‌
            //将资源作为渲染目标输出（如帧缓冲区）
            //需调用CreateRenderTargetView创建视图
            //典型用途：交换链的后备缓冲区或离屏渲染目标
            //
            //BindFlags.DepthStencil → Device.CreateDepthStencilView（用作深度/模板缓冲区）
            //指定资源为深度/模板缓冲区，格式需为深度兼容类型（如DXGI_FORMAT_D24_UNORM_S8_UINT）
            //DepthStencil (D3D11_BIND_DEPTH_STENCIL)‌
            //用作深度/模板缓冲区
            //需通过CreateDepthStencilView创建视图
            //格式必须为深度兼容格式（如DXGI_FORMAT_D24_UNORM_S8_UINT）
            //
            //BindFlags.UnorderedAccess → Device.CreateUnorderedAccessView（无序访问标志，支持计算着色器的随机读写访问，通用计算场景需启用）
            //支持计算着色器的随机读写（如粒子系统），需配合CreateUnorderedAccessView，适用于GPU通用计算
            //UnorderedAccess (D3D11_BIND_UNORDERED_ACCESS)‌
            //支持计算着色器的随机读写（如GPU计算）
            //需配合CreateUnorderedAccessView使用
            //典型场景：粒子系统或通用计算任务
            //
            //2. 缓冲区专用绑定‌
            //BindFlags.VertexBuffer → DeviceContext.IASetVertexBuffers
            //将缓冲区作为顶点数据源绑定到输入装配阶段
            //VertexBuffer (D3D11_BIND_VERTEX_BUFFER)‌
            //指定缓冲区存储顶点数据
            //需通过IASetVertexBuffers绑定到输入装配阶段
            //
            //BindFlags.IndexBuffer → DeviceContext.IASetIndexBuffer
            //将缓冲区作为索引数据源绑定到输入装配阶段
            //IndexBuffer (D3D11_BIND_INDEX_BUFFER)‌
            //指定缓冲区存储索引数据
            //需通过IASetIndexBuffer绑定
            //
            //BindFlags.ConstantBuffer → DeviceContext.VSSetConstantBuffers
            //用于存储常量数据（如矩阵），需单独使用且不可与其他标志组合
            //ConstantBuffer (D3D11_BIND_CONSTANT_BUFFER)‌
            //用于存储常量数据（如变换矩阵）
            //需通过VSSetConstantBuffers等绑定到着色器
            //
            //BindFlags = BindFlags.VertexBuffer | BindFlags.StreamOutput
            //允许几何着色器输出顶点到缓冲区，需避免同时绑定到输入装配阶段
            //
            //
            //若需CPU动态更新纹理数据，需配合Usage和CPUAccessFlags参数：
            //desc.Usage = ResourceUsage.Dynamic;
            //desc.CPUAccessFlags = CpuAccessFlags.Write;
            //desc.BindFlags = BindFlags.ShaderResource; // 动态纹理，此时仅作为输入资源
            //

            8、组合使用示例‌
            渲染到纹理并采样：BindFlags.RenderTarget | BindFlags.ShaderResource
            计算着色器读写纹理：BindFlags.UnorderedAccess | BindFlags.ShaderResource

            9、注意事项‌
            标志组合需符合硬件限制（如部分格式不支持渲染目标）
            动态资源需配合Usage和CPUAccessFlags参数
            深度缓冲区必须单独设置DepthStencil标志，不可与其他类型组合
            ------------------------------------------------------------------------
            在Direct3D 11中，BindFlags的配置需根据资源在渲染管线中的具体用途进行组合设置，以下是各场景的详细配置指南：

            3. 组合使用场景‌

            渲染到纹理并采样‌
            同时作为渲染目标和着色器资源：
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource

            计算着色器读写纹理‌
            支持计算着色器访问且允许采样：
            BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource

            动态更新资源‌
            需配合Dynamic用法和Write CPU访问权限：
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write

            4. 注意事项‌

            硬件限制‌：部分格式不支持特定绑定（如R8G8B8A8_UNORM不可用作深度缓冲区）
            标志冲突‌：如ConstantBuffer不可与其他标志组合，StreamOutput需避免与输入装配阶段同时绑定
            视图创建‌：每种绑定类型需对应创建特定视图（如RenderTarget需CreateRenderTargetView）
            通过合理组合BindFlags，可高效利用GPU资源并满足不同渲染管线的需求
            ------------------------------------------------------------------------------------------
            在Direct3D 11中，BindFlags的设置需紧密结合渲染管线的具体阶段需求，以下是分场景的配置指南：

            1. 管线阶段与BindFlags的对应关系‌

            输入装配阶段（IA）‌
            需绑定顶点/索引缓冲区：
            BindFlags = BindFlags.VertexBuffer | BindFlags.IndexBuffer
            用于向顶点着色器传递几何数据

            顶点/几何着色器阶段‌
            若需动态更新常量数据（如变换矩阵）：
            BindFlags = BindFlags.ConstantBuffer
            常量缓冲区需单独设置且不可与其他标志组合

            光栅化与输出合并阶段‌
            渲染目标纹理需同时支持写入和采样：
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            深度缓冲区则需单独设置DepthStencil标志

            2. 计算着色器专用配置‌

            通用计算（GPGPU）‌
            需启用无序访问以支持计算着色器读写：
            BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource
            适用于粒子系统或物理模拟等场景

            3. 动态资源管理‌

            CPU频繁更新的资源‌
            需配合动态用法和CPU写权限：
            BindFlags = BindFlags.ShaderResource,
            Usage = ResourceUsage.Dynamic,
            CPUAccessFlags = CpuAccessFlags.Write
            常用于动态纹理或缓冲区更新

            4. 混合用途资源‌

            多重渲染目标（MRT）‌
            多个纹理同时作为渲染目标和着色器输入：
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            需确保纹理格式支持双重绑定

            关键注意事项‌
            硬件限制‌：如DXGI_FORMAT_R8G8B8A8_UNORM不支持DepthStencil绑定
            视图匹配‌：每种绑定标志需创建对应的视图（如UnorderedAccess需UAV视图）
            性能优化‌：避免过度组合标志（如不必要的UnorderedAccess会限制资源重用）
            通过上述配置可精准适配渲染管线各阶段需求，平衡功能与性能
            -----------------------------------------------------------------------------------------------
            优化BindFlags性能需结合资源用途与硬件特性，以下是关键策略：

            1. 按需精简绑定标志‌
            避免冗余标志‌：仅启用必要的绑定类型（如仅需采样的纹理无需RenderTarget标志）
            常量缓冲区隔离‌：D3D11_BIND_CONSTANT_BUFFER禁止与其他标志组合，强制独立使用以提升访问效率
            2. 资源复用与视图优化‌
            多重用途资源‌：对需同时读写和采样的纹理，组合ShaderResource与UnorderedAccess标志，减少内存重复分配
            动态资源控制‌：频繁更新的缓冲区应启用Dynamic用法，配合CPUAccessFlags.Write降低GPU等待时间
            3. 硬件适配与格式选择‌
            格式兼容性‌：确保纹理格式支持目标绑定（如DXGI_FORMAT_R32_TYPELESS可同时用于深度缓冲和着色器资源）
            多线程优化‌：利用DirectX 11多线程特性，分离资源创建与绑定操作以减少管线阻塞
            4. 性能敏感场景实践‌
            渲染目标复用‌：离屏渲染时组合RenderTarget和ShaderResource，避免中间拷贝操作
            计算着色器优化‌：为计算缓冲区启用UnorderedAccess时，优先使用结构化缓冲区而非纹理以减少采样开销
            注意事项‌
            视图匹配‌：每种绑定标志需对应创建视图（如UnorderedAccess需UAV视图），错误配置会导致性能下降
            API调用合并‌：通过合并资源绑定操作（如批量设置常量缓冲区）减少驱动层开销

            通过上述策略可显著提升渲染效率，尤其在实时视频处理和高帧率游戏中效果显著。
            */

            try
            {
                //ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                // 创建Direct3D11兼容的渲染目标纹理
                var textureDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Default, // Pool.Default等效参数
                    //注意，这里必须要用ShaderResource，否则加载不出
                    //注意，ShaderResource需配合CreateShaderResourceView创建视图绑定到管线，RenderTarget需调用CreateRenderTargetView才能作为渲染目标使用
                    BindFlags = Vortice.Direct3D11.BindFlags.RenderTarget, // Usage.RenderTarget映射
                    CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.None,// Default资源通常不需要CPU访问
                    MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None, //MiscFlags.Shared?
                    //若需要动态CPU写入，需修改为：
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    //CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write | Vortice.Direct3D11.CpuAccessFlags.Read
                };
                var texture = Device.CreateTexture2D(textureDesc);
                //debug：
                CMain.SaveError(PrintParentMethod() + $"DX.NewTexture_RenderTarget_Default()：新建纹理{texture.NativePointer}-{texture.Description.BindFlags}-{texture.Description.CPUAccessFlags}" +
                $"-{texture.Description.Format}-{texture.Description.MiscFlags}-{texture.Description.Usage},");
                return texture;
                //注意：创建后需额外调用CreateRenderTargetView才能作为渲染目标使用
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static Vortice.Direct3D11.ID3D11Texture2D CreateTexture_DepthStencil(uint width, uint height)
        {
            //CMain.SaveError(PrintParentMethod());
            try
            {
                //ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                // 创建Direct3D11兼容的渲染目标纹理
                var textureDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.D24_UNorm_S8_UInt‌, // 对应A8R8G8B8格式
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Default, // Pool.Default等效参数
                    BindFlags = Vortice.Direct3D11.BindFlags.DepthStencil, // Usage.RenderTarget映射
                    CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.None,// Default资源通常不需要CPU访问
                    MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None, //MiscFlags.Shared?
                    //若需要动态CPU写入，需修改为：
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    //CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write | Vortice.Direct3D11.CpuAccessFlags.Read
                };
                var texture = Device.CreateTexture2D(textureDesc);
                //debug：
                CMain.SaveError($"DX.NewTexture_RenderTarget_Default()：新建纹理{texture.NativePointer}-{texture.Description.BindFlags}-{texture.Description.CPUAccessFlags}" +
                $"-{texture.Description.Format}-{texture.Description.MiscFlags}-{texture.Description.Usage},");
                return texture;
                //注意：创建后需额外调用CreateRenderTargetView才能作为渲染目标使用
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public static Vortice.Direct3D11.ID3D11Texture2D NewTexture_None_Managed(uint width, uint height)
        {
            //CMain.SaveError(PrintParentMethod());
            try
            {
                // 创建Direct3D11兼容的托管纹理
                var textureDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic, // Pool.Managed等效配置
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource, // Usage.None默认绑定，需配合CreateShaderResourceView创建视图绑定到管线
                    CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write, // 支持CPU写入
                    MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None, //MiscFlags.Shared?
                    //如需默认GPU内存改为以下：
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,
                    //CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.None

                };
                var texture = Device.CreateTexture2D(textureDesc);
                //debug：
                CMain.SaveError($"DX.NewTexture_None_Managed()：新建纹理{texture.NativePointer}-{texture.Description.BindFlags}-{texture.Description.CPUAccessFlags}" +
                $"-{texture.Description.Format}-{texture.Description.MiscFlags}-{texture.Description.Usage},");
                return texture;
                //注意：动态纹理需通过Map/Unmap方法进行数据更新，而默认纹理需使用UpdateSubresource方法
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 弃用
        /// </summary>
        /// <returns></returns>
        public static Vortice.Direct3D11.ID3D11Texture2D NewTexture(uint width, uint height, uint level, string usage, string pool)
        {
            CMain.SaveError(PrintParentMethod());
            try
            {
                var bindflag = Vortice.Direct3D11.BindFlags.ShaderResource;
                if (usage.Equals("RenderTarget"))
                {
                    bindflag = Vortice.Direct3D11.BindFlags.ShaderResource | Vortice.Direct3D11.BindFlags.RenderTarget;
                }
                var miscflag = Vortice.Direct3D11.CpuAccessFlags.None;
                if (pool.Equals("Managed"))
                {
                    miscflag = Vortice.Direct3D11.CpuAccessFlags.Write;
                }

                //ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                var textureDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = level,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,  // A8R8G8B8对应格式:ml-citation{ref="7,10" data="citationList"}
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    //Usage = Vortice.Direct3D11.ResourceUsage.Default,    // Pool.Default的等效参数:ml-citation{ref="13,14" data="citationList"}
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// 必须为Dynamic才能使用WriteDiscard
                                                                     //Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// 必须为Dynamic才能使用WriteDiscard
                    BindFlags = bindflag, // Usage.RenderTarget的等效参数:ml-citation{ref="1,3" data="citationList"}
                                          //CPUAccessFlags = miscflag,
                    CPUAccessFlags = CpuAccessFlags.Write, // 必须设置写访问
                    MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None, //MiscFlags.Shared?
                };
                return Device.CreateTexture2D(textureDesc);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static Win32.Numerics.Color4 ToColor4(System.Drawing.Color color)
        {
            return new Win32.Numerics.Color4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f
            );
        }

        public static Vortice.Mathematics.Color4 ToColor4_Vortice(System.Drawing.Color color)
        {
            return new Vortice.Mathematics.Color4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f
            );
        }

        /// <summary>
        /// GZipStream.CopyTo(destination)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="des"></param>
        public static void Stream2MappedSubresource(byte[] data, Vortice.Direct3D11.MappedSubresource des)
        {
            //DeviceContext.Map(buffer, 0, MapMode.WriteDiscard, 0, out des);
            //byte[] data = new byte[source.Length];
            //source.Read(data, 0, data.Length);
            Marshal.Copy(data, 0, des.DataPointer, data.Length);
            //DeviceContext.Unmap(buffer, 0);
        }

        /// <summary>
        /// 成对使用
        /// </summary>
        /// <returns></returns>
        public static MemoryStream SurfaceToStream_Start(Vortice.Direct3D11.ID3D11Texture2D texture)
        {
            CMain.SaveError(PrintParentMethod() + $"SurfaceToStream_Start开始，当前表面{CurrentSurface}");
            //using (var stream = Surface.ToStream(backbuffer, ImageFileFormat.Png))

            var mapped = DeviceContext.Map(texture, 0, Vortice.Direct3D11.MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            var totalSize = mapped.RowPitch * texture.Description.Height;//二维纹理长度计算
            byte[] data = new byte[totalSize];
            Marshal.Copy(mapped.DataPointer, data, 0, (int)totalSize);
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 成对使用，释放资源
        /// </summary>
        public static void SurfaceToStream_End(Vortice.Direct3D11.ID3D11Texture2D texture, MemoryStream stream)
        {
            stream.Close();
            stream.Dispose();
            stream = null;
            DeviceContext.Unmap(texture, 0);
            CMain.SaveError(PrintParentMethod() + $"SurfaceToStream_End释放完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// 什么都不做
        /// Device.BeginScene()
        /// </summary>
        public static void DeviceBeginScene()
        {
            //DXManager.Device.BeginScene();

            //说明：Direct3D11使用‌立即模式渲染‌，设备上下文自动管理渲染状态，无需Begin/EndScene
            //Direct3D11不再需要显式调用BeginScene/EndScene，
            //渲染过程直接通过设备上下文(ID3D11DeviceContext)的绘制命令实现
        }

        /// <summary>
        /// 什么都不做
        /// Device.EndScene()
        /// </summary>
        public static void DeviceEndScene()
        {
            //DXManager.Device.EndScene();

            //说明：Direct3D11使用‌立即模式渲染‌，设备上下文自动管理渲染状态，无需Begin/EndScene
            //Direct3D11不再需要显式调用BeginScene/EndScene，
            //渲染过程直接通过设备上下文(ID3D11DeviceContext)的绘制命令实现
        }

        /// <summary>
        /// Device.Present()
        /// </summary>
        public static void DevicePresent()
        {
            //CMain.SaveError(PrintParentMethod());
            //DXManager.Device.Present();

            //通过交换链的Present方法提交渲染结果
            DXGISwapChain.Present(1, Vortice.DXGI.PresentFlags.None); // 垂直同步间隔为1

            CMain.SaveError(PrintParentMethod() + $"Present，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Device.Clear(Color.Black)
        /// 清屏为黑色
        /// </summary>
        public static void DeviceClear_Target(System.Drawing.Color color)
        {
            CMain.SaveError(PrintParentMethod() + $"执行清屏{color}，当前表面{CurrentSurface}");
            //DXManager.Device.Clear(ClearFlags.Target, Color.Black, 0, 0);

            //使用ID3D11DeviceContext.ClearRenderTargetView和ID3D11DeviceContext.ClearDepthStencilView分别清除渲染目标和深度模板缓冲区
            //1、清理“渲染目标视图”
            DeviceContext.ClearRenderTargetView(CurrentSurface, ToColor4_Vortice(color));
            ////2、清理“深度模板视图”
            //DeviceContext.ClearDepthStencilView(DepthStencilView, Vortice.Direct3D11.DepthStencilClearFlags.Depth, 1.0f, 0);

            ////设置渲染目标、视口和划片矩形（除文字外通用）
            ////1、设置渲染目标（渲染目标+深度模板）
            //DeviceContext.OMSetRenderTargets(CurrentSurface, DepthStencilView);
            ////2、设置视口
            //DeviceContext.RSSetViewport(new Viewport(Program.Form.Width, Program.Form.Height));
            ////3、设置划片区域
            //DeviceContext.RSSetScissorRect(Program.Form.Width, Program.Form.Height);

            //CMain.SaveError($"DX.DeviceClear_Target()完成：清屏{CurrentSurface.NativePointer},颜色{color}");
        }

        /// <summary>
        /// Sprite.Flush()
        /// 一般不用主动调用，会自动同步，主动调用时是因为需要强制同步
        /// </summary>
        public static void Sprite_Flush()
        {
            //Sprite.Flush();
            //SpriteLine.Flush(out ulong _, out ulong _);

            //SpriteLine.Flush(out _, out _);

            //以上代码使用随意的颜色清理，调用 Clear 时，将会让整个 ID2D1RenderTarget 使用给定的颜色清理，
            //也就是修改颜色在完成之后，调用一下交换链的 Present 和等待刷新
            DXGISwapChain.Present(1, PresentFlags.None);
            // 等待刷新
            DeviceContext.Flush();
            CMain.SaveError(PrintParentMethod() + $"执行Present + Flush完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Sprite.End()
        /// </summary>
        public static void Sprite_End()
        {
            //Sprite.End();
            //SpriteLine.EndDraw();
            Sprite.EndDraw();

            CMain.SaveError(PrintParentMethod() + $"EndDraw完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Device.SetRenderState(AlphaBlendEnable)
        /// </summary>
        /// <param name="flag"></param>
        public static void DeviceSetRenderState_AlphaBlendEnable(bool flag)
        {
            var ee = new Vortice.Direct3D11.RenderTargetBlendDescription
            {
                BlendEnable = flag,  // 对应D3DRS_ALPHABLENDENABLE
                //SourceBlend = Vortice.Direct3D11.Blend.SourceAlpha,
                //DestinationBlend = Vortice.Direct3D11.Blend.InverseSourceAlpha,
                //BlendOperation = Vortice.Direct3D11.BlendOperation.Add
            };
            //Device.SetRenderState(SlimDX.Direct3D9.RenderState.AlphaBlendEnable, true);
            Device.ImmediateContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription     //CreateBlendState参数错误
            {
                RenderTarget = new Vortice.Direct3D11.BlendDescription.RenderTarget__FixedBuffer()
                {
                    e0 = ee,
                    e1 = ee,
                    e2 = ee,
                    e3 = ee,
                    e4 = ee,
                    e5 = ee,
                    e6 = ee,
                    e7 = ee,
                }
            }));
            CMain.SaveError(PrintParentMethod() + $"SetRenderState完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Sprite.Begin(DoNotSaveState)
        /// </summary>
        public static void SpriteBegin_DoNotSaveState()
        {
            //CMain.SaveError(PrintParentMethod());
            //Sprite.Begin(SlimDX.Direct3D9.SpriteFlags.DoNotSaveState);
            // 显式设置所需状态（不依赖自动保存）
            DeviceContext.OMSetBlendState(null); // 使用默认混合状态
            DeviceContext.OMSetDepthStencilState(null); // 禁用深度测试
            DeviceContext.RSSetState(null); // 使用默认光栅化状态

            //SpriteLine.BeginDraw();
            Sprite.BeginDraw();

            CMain.SaveError(PrintParentMethod() + $"执行BeginDraw_DoNotSaveState完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Sprite.Begin(AlphaBlend)
        /// </summary>
        public static void SpriteBegin_AlphaBlend()
        {
            //CMain.SaveError(PrintParentMethod());

            //Sprite.Begin(SlimDX.Direct3D9.SpriteFlags.AlphaBlend);
            DeviceContext.OMSetBlendState(Device.CreateBlendState(new Vortice.Direct3D11.BlendDescription
            {
                RenderTarget = new Vortice.Direct3D11.BlendDescription.RenderTarget__FixedBuffer()
                {
                    e0 = new Vortice.Direct3D11.RenderTargetBlendDescription
                    {
                        BlendEnable = true,
                        SourceBlend = Vortice.Direct3D11.Blend.SourceAlpha,
                        DestinationBlend = Vortice.Direct3D11.Blend.InverseSourceAlpha,
                        BlendOperation = Vortice.Direct3D11.BlendOperation.Add,
                        SourceBlendAlpha = Vortice.Direct3D11.Blend.One,
                        DestinationBlendAlpha = Vortice.Direct3D11.Blend.Zero,
                        BlendOperationAlpha = Vortice.Direct3D11.BlendOperation.Add,
                        RenderTargetWriteMask = Vortice.Direct3D11.ColorWriteEnable.All
                    }
                }
            }));

            //SpriteLine.BeginDraw();
            Sprite.BeginDraw();

            CMain.SaveError(PrintParentMethod() + $"执行OMSetBlendState完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Matrix.Scaling(scaleX, scaleY, 0)
        /// </summary>
        /// <returns></returns>
        public static Matrix4x4 MatrixScaling0(float scaleX, float scaleY)
        {
            //创建缩放矩阵（Z轴缩放值设为1.0f，与SlimDX的0等效）
            return Matrix4x4.CreateScale(scaleX, scaleY, 1.0f);
        }

        /// <summary>
        /// Sprite.Transform = matrix
        /// </summary>
        public static void SpriteTransform(Matrix4x4 matrix)
        {
            //DXManager.Sprite.Transform = matrix;
            //通过常量缓冲区将创建的矩阵传递到着色器
            var bufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                //
                //BindFlags.ConstantBuffer → VSSetConstantBuffers（完成）
                //
                BindFlags = BindFlags.ConstantBuffer,
                ByteWidth = (uint)Marshal.SizeOf<Matrix4x4>(),
                CPUAccessFlags = CpuAccessFlags.Write
            };
            var constantBuffer = Device.CreateBuffer(bufferDesc);
            var dataStream = DeviceContext.Map(constantBuffer, MapMode.WriteDiscard);
            unsafe
            {
                var matrixPtr = (Matrix4x4*)dataStream.DataPointer;// 等效于SlimDX.Sprite.Transform = matrix
                *matrixPtr = matrix; //创建缩放矩阵（Z轴缩放值设为1.0f，与SlimDX的0等效）
            }
            DeviceContext.Unmap(constantBuffer);
            DeviceContext.VSSetConstantBuffer(0, constantBuffer);

            CMain.SaveError(PrintParentMethod() + $"SpriteTransform完成，当前表面{CurrentSurface}");
        }

        #endregion

        #region lyo，常量缓冲区设置

        // 定义数据结构（需16字节对齐）
        [StructLayout(LayoutKind.Sequential)]
        struct MatrixBuffer
        {
            public Vector4 param1;
            public Vector4 param2;
        }

        /// <summary>
        /// lyo，常量缓冲区设置
        /// </summary>
        /// <param name="blend"></param>
        /// <param name="tintcolor"></param>
        private static void MakePSSetConstantBuffer(float blend, System.Drawing.Color tintcolor)
        {
            // 创建常量缓冲区
            var buffer = new Vortice.Direct3D11.BufferDescription
            {
                // 缓冲区字节大小（需16字节对齐）
                ByteWidth = (uint)Marshal.SizeOf<MatrixBuffer>(),
                //
                //BindFlags.VertexBuffer → IASetVertexBuffers（完成）
                //
                // 绑定到管线阶段
                BindFlags = Vortice.Direct3D11.BindFlags.VertexBuffer,
                // 特殊选项 //MiscFlags.Shared?
                MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
                // 结构化缓冲的步长
                StructureByteStride = 0,
                //如果需要频繁更新的纹理
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write | Vortice.Direct3D11.CpuAccessFlags.Read,
                //CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.None,  // CPU访问权限
                //Usage = Vortice.Direct3D11.ResourceUsage.Default,  // 资源使用模式
                // 资源使用模式
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
            };
            var constantBuffer = Device.CreateBuffer(buffer);
            var matrixData = new MatrixBuffer
            {
                param1 = new Vector4(1.0F, 1.0F, 1.0F, blend),
                param2 = new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F)
            };
            // 更新缓冲区数据，绑定到管线
            DeviceContext.UpdateSubresource(in matrixData, constantBuffer);
            DeviceContext.PSSetConstantBuffer(0, constantBuffer);
            DeviceContext.PSSetConstantBuffer(1, constantBuffer);
            DeviceContext.IASetVertexBuffer(0, constantBuffer, 0);//add

            CMain.SaveError(PrintParentMethod() + $"常量缓冲区设置完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// 直接设置全局着色器
        /// </summary>
        /// <param name="blend"></param>
        /// <param name="tintcolor"></param>
        public static void SetNormal(float blend, System.Drawing.Color tintcolor)
        {
            CMain.SaveError(PrintParentMethod());
            //if (Device.PixelShader == NormalPixelShader)
            if (DeviceContext.PSGetShader() == NormalPixelShader)
            {
                return;
            }
            //Sprite.Flush();
            Sprite_Flush();

            //Device.PixelShader = NormalPixelShader;
            // 绑定到像素着色器阶段
            DeviceContext.PSSetShader(NormalPixelShader, null, 0);

            //Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            //Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F) });
            MakePSSetConstantBuffer(blend, tintcolor);

            //Sprite.Flush();
            Sprite_Flush();
        }

        /// <summary>
        /// 直接设置全局着色器
        /// </summary>
        /// <param name="blend"></param>
        /// <param name="tintcolor"></param>
        public static void SetGrayscale(float blend, System.Drawing.Color tintcolor)
        {
            CMain.SaveError(PrintParentMethod());
            //if (Device.PixelShader == GrayScalePixelShader)
            if (DeviceContext.PSGetShader() == GrayScalePixelShader)
            {
                return;
            }

            //Sprite.Flush();
            Sprite_Flush();

            //Device.PixelShader = GrayScalePixelShader;
            // 绑定到像素着色器阶段
            DeviceContext.PSSetShader(GrayScalePixelShader, null, 0);

            //Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            //Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F) });
            MakePSSetConstantBuffer(blend, tintcolor);

            //Sprite.Flush();
            Sprite_Flush();
        }

        /// <summary>
        /// 直接设置全局着色器
        /// </summary>
        /// <param name="blend"></param>
        /// <param name="tintcolor"></param>
        public static void SetBlendMagic(float blend, System.Drawing.Color tintcolor)
        {
            CMain.SaveError(PrintParentMethod());
            //if (Device.PixelShader == MagicPixelShader || MagicPixelShader == null)
            if (DeviceContext.PSGetShader() == MagicPixelShader || MagicPixelShader == null)
            {
                return;
            }

            //Sprite.Flush();
            Sprite_Flush();

            //Device.PixelShader = MagicPixelShader;
            // 绑定到像素着色器阶段
            DeviceContext.PSSetShader(MagicPixelShader, null, 0);

            //Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            //Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F) });
            MakePSSetConstantBuffer(blend, tintcolor);

            //Sprite.Flush();
            Sprite_Flush();
        }

        #endregion

        public void ReleaseTexture(ID3D11Texture2D texture)
        {
            Marshal.ReleaseComObject(texture); // 释放COM资源
            texture = null; // 防止C# GC过早回收资源
        }

        public static void Clean()
        {
            for (int i = TextureList.Count - 1; i >= 0; i--)
            {
                MImage m = TextureList[i];

                if (m == null)
                {
                    TextureList.RemoveAt(i);
                    continue;
                }

                if (CMain.Time <= m.CleanTime) continue;

                m.DisposeTexture();
            }

            for (int i = ControlList.Count - 1; i >= 0; i--)
            {
                MirControl c = ControlList[i];

                if (c == null)
                {
                    ControlList.RemoveAt(i);
                    continue;
                }

                if (CMain.Time <= c.CleanTime) continue;

                c.DisposeTexture();
            }
            CMain.SaveError(PrintParentMethod() + $"Clean完成，当前表面{CurrentSurface}");
        }


        private static void CleanUp()
        {
            //if (SpriteLine != null)
            //{
            //    //SpriteLine.Release();//递减COM对象引用计数，当计数归零时触发实际释放
            //    SpriteLine.Dispose();//托管代码显式释放资源，调用后会标记对象为已释放状态
            //    SpriteLine = null;

            //    CMain.SaveError($"SpriteLine.CleanUp()完成：Dispose→null");
            //}

            if (Sprite != null)
            {
                Sprite.Dispose();
                Sprite = null;
            }

            if (CurrentSurface != null)
            {
                //CurrentSurface.Release();
                CurrentSurface.Dispose();
                CurrentSurface = null;
            }

            if (PoisonDotBackground != null)
            {
                //Marshal.ReleaseComObject(PoisonDotBackground); // 释放COM资源
                PoisonDotBackground.Dispose();
                PoisonDotBackground = null; // 防止C# GC过早回收资源
            }

            if (RadarTexture != null)
            {
                //Marshal.ReleaseComObject(RadarTexture); // 释放COM资源
                RadarTexture.Dispose();
                RadarTexture = null; // 防止C# GC过早回收资源
            }

            if (FloorTexture != null)
            {
                //Marshal.ReleaseComObject(FloorTexture); // 释放COM资源
                FloorTexture.Dispose();
                FloorTexture = null; // 防止C# GC过早回收资源
                GameScene.Scene.MapControl.FloorValid = false;

                if (FloorSurface != null)
                {
                    //DXManager.FloorSurface.Release();
                    FloorSurface.Dispose();
                }

                FloorSurface = null;
            }

            if (LightTexture != null)
            {
                //Marshal.ReleaseComObject(LightTexture); // 释放COM资源
                LightTexture.Dispose();
                LightTexture = null; // 防止C# GC过早回收资源

                if (LightSurface != null)
                {
                    //DXManager.LightSurface.Release();
                    LightSurface.Dispose();
                }

                LightSurface = null;
            }

            if (Lights != null)
            {
                for (int i = 0; i < Lights.Count; i++)
                {
                    //Marshal.ReleaseComObject(Lights[i]); // 释放COM资源
                    Lights[i].Dispose();
                    Lights[i] = null; // 防止C# GC过早回收资源
                }
                Lights.Clear();
            }

            for (int i = TextureList.Count - 1; i >= 0; i--)
            {
                MImage m = TextureList[i];

                if (m == null) continue;

                m.DisposeTexture();
            }
            TextureList.Clear();


            for (int i = ControlList.Count - 1; i >= 0; i--)
            {
                MirControl c = ControlList[i];

                if (c == null) continue;

                c.DisposeTexture();
            }
            ControlList.Clear();

            CMain.SaveError(PrintParentMethod() + $"CleanUp完成，当前表面{CurrentSurface}");
        }

        public static void Dispose()
        {
            CleanUp();

            //Device?.Direct3D?.Dispose(); 
            Device?.ImmediateContext?.Dispose();
            Device?.Dispose();
            DeviceContext = null;
            Device = null;

            NormalPixelShader?.Dispose();
            GrayScalePixelShader?.Dispose();
            MagicPixelShader?.Dispose();

            CMain.SaveError(PrintParentMethod() + $"Dispose完成，当前表面{CurrentSurface}");
        }

        /// <summary>
        /// Vortice为设备创建显卡
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        private static IEnumerable<Vortice.DXGI.IDXGIAdapter1> GetHardwareAdapter(Vortice.DXGI.IDXGIFactory2 factory)
        {
            Vortice.DXGI.IDXGIFactory6? factory6 = factory.QueryInterfaceOrNull<Vortice.DXGI.IDXGIFactory6>();
            if (factory6 != null)
            {
                for (int adapterIndex = 0;
                     factory6.EnumAdapterByGpuPreference((uint)adapterIndex, Vortice.DXGI.GpuPreference.HighPerformance,
                         out Vortice.DXGI.IDXGIAdapter1? adapter).Success;
                     adapterIndex++)
                {
                    if (adapter == null)
                    {
                        continue;
                    }
                    Vortice.DXGI.AdapterDescription1 desc = adapter.Description1;
                    if ((desc.Flags & Vortice.DXGI.AdapterFlags.Software) != Vortice.DXGI.AdapterFlags.None)
                    {
                        adapter.Dispose();
                        continue;
                    }
                    yield return adapter;
                }
                factory6.Dispose();
            }
            for (int adapterIndex = 0;
                 factory.EnumAdapters1((uint)adapterIndex, out Vortice.DXGI.IDXGIAdapter1? adapter).Success;
                 adapterIndex++)
            {
                Vortice.DXGI.AdapterDescription1 desc = adapter.Description1;

                if ((desc.Flags & Vortice.DXGI.AdapterFlags.Software) != Vortice.DXGI.AdapterFlags.None)
                {
                    adapter.Dispose();

                    continue;
                }
                //Debug.Print($"枚举到 {adapter.Description1.Description} 显卡");
                yield return adapter;
            }
        }

        public static byte[] GetBitmapPixelData(Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try
            {
                int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];
                Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);
                return rgbValues;
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }
        }
    }

    /// <summary>
    /// Direct2D会根据目标矩形自动拉伸源纹理，而SlimDX需要手动计算缩放比例并通过世界矩阵或矩形参数控制，会造成渲染结果不一致
    /// </summary>
    public static class Direct2DTextureRenderer
    {
        public static void DrawTexture(
            ID2D1RenderTarget renderTarget,
            ID2D1Bitmap bitmap,
            Rectangle? sourceRect,
            System.Numerics.Vector3? position,
            Vortice.Mathematics.Color4 color)
        {
            // 获取DPI缩放比例
            float dpiX = renderTarget.Dpi.Width;
            float dpiY = renderTarget.Dpi.Height;

            // 计算目标矩形
            var destRect = CalculateDestinationRect(
                bitmap.PixelSize,
                sourceRect,
                position,
                dpiX, dpiY);

            // 转换源矩形
            RawRectF? sourceRectF = sourceRect.HasValue
                ? new RawRectF(
                    sourceRect.Value.Left,
                    sourceRect.Value.Top,
                    sourceRect.Value.Right,
                    sourceRect.Value.Bottom)
                : null;

            // 计算透明度
            float opacity = (color.R + color.G + color.B) / 3.0f * color.A;

            // 执行绘制
            renderTarget.DrawBitmap(
                bitmap,
                destRect,
                opacity,
                Vortice.Direct2D1.BitmapInterpolationMode.Linear,
                sourceRectF);
        }

        private static RawRectF CalculateDestinationRect(
            Vortice.Mathematics.SizeI textureSize,
            Rectangle? sourceRect,
            System.Numerics.Vector3? position,
            float dpiX, float dpiY)
        {
            // 计算实际使用的纹理区域
            int width = sourceRect.HasValue ? sourceRect.Value.Width : textureSize.Width;
            int height = sourceRect.HasValue ? sourceRect.Value.Height : textureSize.Height;

            // 转换为DIPs(设备无关像素)
            float dipWidth = width * (96.0f / dpiX);
            float dipHeight = height * (96.0f / dpiY);

            // 应用位置偏移
            float left = position.HasValue ? position.Value.X : 0;
            float top = position.HasValue ? position.Value.Y : 0;

            return new RawRectF(
                left,
                top,
                left + dipWidth,
                top + dipHeight);
        }
    }

    public class Direct2DText : IDisposable
    {
        private readonly Vortice.Direct2D1.ID2D1RenderTarget _renderTarget;
        private readonly Vortice.Direct2D1.ID2D1SolidColorBrush _textBrush;

        public Direct2DText(ID2D1RenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
            _textBrush = _renderTarget.CreateSolidColorBrush(new Vortice.Mathematics.Color4(1.0f, 1.0f, 1.0f, 1.0f));
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
            _textBrush.Dispose();
        }

        public void DrawTextToTexture(ID2D1Bitmap textureBitmap, string text, System.Drawing.Font font,
            System.Drawing.Color foreColor, System.Drawing.Color backColor, Size size, DrawTextOptions drawOptions,
            int outLine, System.Drawing.Color borderColor)
        {
            foreColor = Color.White;
            backColor = Color.White;
            borderColor = Color.White;

            // 开始绘制
            //_renderTarget.BeginDraw();

            //背景色
            _renderTarget.Clear(new Vortice.Mathematics.Color4(
                backColor.R / 255f,
                backColor.G / 255f,
                backColor.B / 255f,
                backColor.A / 255f
            ));
            // 设置文本颜色
            _textBrush.Color = new Vortice.Mathematics.Color4(
                foreColor.R / 255.0f,
                foreColor.G / 255.0f,
                foreColor.B / 255.0f,
                foreColor.A / 255.0f);
            // 设置边框颜色
            var _borderColor = new Vortice.Mathematics.Color4(
                borderColor.R / 255.0f,
                borderColor.G / 255.0f,
                borderColor.B / 255.0f,
                borderColor.A / 255.0f);

            // 创建D2D文本格式
            using (var textFormat = DXManager.DwFactory.CreateTextFormat(
                font.FontFamily.Name,
                font.Bold ? FontWeight.Bold : FontWeight.Normal,
                font.Italic ? Vortice.DirectWrite.FontStyle.Italic : Vortice.DirectWrite.FontStyle.Normal,
                font.Size))
            {
                textFormat.TextAlignment = TextAlignment.Leading;
                textFormat.ParagraphAlignment = ParagraphAlignment.Near;

                //画边框
                // 计算文本边界（需配合DirectWrite测量）
                using var borderBrush = _renderTarget.CreateSolidColorBrush(_borderColor);
                var textLayout = DXManager.DwFactory.CreateTextLayout(text, textFormat, textureBitmap.Size.Width, textureBitmap.Size.Height);
                var metrics = textLayout.Metrics;
                switch (outLine)
                {
                    case 0://无边框
                        break;
                    case 1://普通边框
                        // 扩展边界作为边框区域
                        var borderRect = new Vortice.Mathematics.Rect(
                            0 - 2,    // 左边距
                            0 - 2,     // 上边距
                            0 + metrics.Width + 4,  // 右边界
                            0 + metrics.Height + 4   // 下边界
                        );
                        _renderTarget.DrawRectangle(borderRect, borderBrush, 1.5f);
                        break;
                    case 2://圆角矩形边框
                        //// 绘制圆角矩形边框
                        //_renderTarget.DrawRoundedRectangle(
                        //    new RoundedRectangle(borderRect, 3, 3),  // 圆角半径
                        //    _textBrush,
                        //    2.0f  // 边框粗细
                        //);
                        break;
                    case 3://虚线边框
                        //// 虚线边框
                        //var strokeStyle = _renderTarget.Factory.CreateStrokeStyle(new StrokeStyleProperties
                        //{
                        //    DashStyle = DashStyle.Dash
                        //});
                        //_renderTarget.DrawRectangle(borderRect, borderBrush, 1.5f, strokeStyle);
                        break;
                    case 4://渐变边框
                        //// 渐变边框
                        //using var gradientBrush = _renderTarget.CreateLinearGradientBrush(/* 渐变参数 */);
                        //_renderTarget.DrawRectangle(borderRect, gradientBrush, 3.0f);
                        break;
                    default:
                        break;
                }

                // 绘制文本
                _renderTarget.DrawText(text, textFormat, new RawRectF(1, 0, size.Width, size.Height), _textBrush, drawOptions);
            }
            // 结束绘制
            //_renderTarget.EndDraw();
        }

        public ID2D1Bitmap CreateTextureBitmap(Size size)
        {
            // 创建WIC位图
            using (var wicBitmap = DXManager.WicFactory.CreateBitmap((uint)size.Width, (uint)size.Height,
                Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnLoad))
            {
                // 从WIC位图创建D2D位图
                return _renderTarget.CreateBitmapFromWicBitmap(wicBitmap);
            }
        }
    }


    public static class GrabImage
    {
        public static void ShowImageFromGPU(ID3D11Device device, ID3D11Texture2D texture)
        {
            // 获取纹理描述
            var desc = texture.Description;

            // 创建临时纹理用于CPU读取
            var stagingDesc = desc with
            {
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read
                //Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                //BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                //CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
            };

            using var stagingTexture = device.CreateTexture2D(stagingDesc);

            // 复制GPU纹理到CPU可读纹理
            device.ImmediateContext.CopyResource(stagingTexture, texture);

            // 映射纹理数据
            var map = device.ImmediateContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
            //var map = device.ImmediateContext.Map(stagingTexture, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
            try
            {
                // 创建Bitmap并复制数据
                var bitmap = new Bitmap((int)desc.Width, (int)desc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                // 逐行复制数据（处理可能的行间距差异）
                for (int y = 0; y < desc.Height; y++)
                {
                    unsafe
                    {
                        var srcPtr = (byte*)map.DataPointer + y * map.RowPitch;
                        var dstPtr = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;
                        Buffer.MemoryCopy(srcPtr, dstPtr, bitmapData.Stride, desc.Width * 4);
                    }
                }

                bitmap.UnlockBits(bitmapData);
                //return bitmap;

                var form = new Form();
                form.Text = "GPU图像";
                form.Size = new System.Drawing.Size(bitmap.Width == 0 ? 300 : bitmap.Width, bitmap.Height == 0 ? 200 : bitmap.Height);
                var pictureBox = new PictureBox();
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox.Image = bitmap;
                form.Controls.Add(pictureBox);
                form.Show();
            }
            finally
            {
                device.ImmediateContext.Unmap(stagingTexture, 0);
            }
        }

        public static void ShowImageFromCPU(byte[] rgbaData, int width, int height)
        {
            var pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            Bitmap bitmap = new Bitmap(width, height, pixelFormat);

            BitmapData bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                pixelFormat);

            IntPtr intPtr = bitmapData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(rgbaData, 0, intPtr, rgbaData.Length);
            bitmap.UnlockBits(bitmapData);

            //return bitmap;

            var form = new Form();
            form.Text = "CPU图像";
            form.Size = new System.Drawing.Size(bitmap.Width == 0 ? 300 : bitmap.Width, bitmap.Height == 0 ? 200 : bitmap.Height);
            var pictureBox = new PictureBox();
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Image = bitmap;
            form.Controls.Add(pictureBox);
            form.Show();
        }
    }

}