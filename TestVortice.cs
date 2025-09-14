using Client.Resolution;
using Launcher;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
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
using static System.Console;
using static Vortice.Direct3D11.D3D11;
using FeatureLevel = Vortice.Direct3D.FeatureLevel;
using SharpGen.Runtime;


namespace Client
{
    public class TestVortice
    {
        #region Vortice测试1

        // 设置可以支持 Win7 和以上版本。如果用到 WinRT 可以设置为支持 win10 和以上。这个特性只是给 VS 看的，没有实际影响运行的逻辑
        [SupportedOSPlatform("Windows7.0")]
        public static unsafe void TestVortice1()
        {
            // 准备创建窗口
            // 使用 Win32 创建窗口需要很多参数，这些参数系列不是本文的重点，还请自行了解
            SizeI clientSize = new SizeI(1024, 768);

            // 窗口标题
            var title = "Demo";
            var windowClassName = "lindexi doubi";

            // 窗口样式，窗口样式含义请执行参阅官方文档，样式只要不离谱，自己随便写，影响不大
            WINDOW_STYLE style = WS_CAPTION |
                                 WS_SYSMENU |
                                 WS_MINIMIZEBOX |
                                 WS_CLIPSIBLINGS |
                                 WS_BORDER |
                                 WS_DLGFRAME |
                                 WS_THICKFRAME |
                                 WS_GROUP |
                                 WS_TABSTOP |
                                 WS_SIZEBOX;

            //根据上面设置的窗口尺寸，尝试根据样式算出实际可用的尺寸
            var rect = new RECT
            {
                right = clientSize.Width,
                bottom = clientSize.Height
            };

            // Adjust according to window styles
            AdjustWindowRectEx(&rect, style, false, WS_EX_APPWINDOW);

            // 决定窗口在哪显示，这个不影响大局
            int x = 0;
            int y = 0;
            int windowWidth = rect.right - rect.left;
            int windowHeight = rect.bottom - rect.top;

            // 随便，放在屏幕中间好了。多个显示器？忽略
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            x = (screenWidth - windowWidth) / 2;
            y = (screenHeight - windowHeight) / 2;

            //准备完成，开始创建窗口
            var hInstance = GetModuleHandle((string)null);

            fixed (char* lpszClassName = windowClassName)
            {
                PCWSTR szCursorName = new((char*)IDC_ARROW);

                var wndClassEx = new WNDCLASSEXW
                {
                    cbSize = (uint)Unsafe.SizeOf<WNDCLASSEXW>(),
                    style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC,
                    // 核心逻辑，设置消息循环
                    lpfnWndProc = new WNDPROC(WndProc),
                    hInstance = (HINSTANCE)hInstance.DangerousGetHandle(),
                    hCursor = LoadCursor((HINSTANCE)IntPtr.Zero, szCursorName),
                    hbrBackground = (Windows.Win32.Graphics.Gdi.HBRUSH)IntPtr.Zero,
                    hIcon = (HICON)IntPtr.Zero,
                    lpszClassName = lpszClassName
                };

                ushort atom = RegisterClassEx(wndClassEx);

                if (atom == 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to register window class. Error: {Marshal.GetLastWin32Error()}"
                    );
                }
            }

            // 创建窗口
            var hWnd = CreateWindowEx
            (
                WS_EX_APPWINDOW,
                windowClassName,
                title,
                style,
                x,
                y,
                windowWidth,
                windowHeight,
                hWndParent: default,
                hMenu: default,
                hInstance: default,
                lpParam: null
            );

            // 创建完成，那就显示
            ShowWindow(hWnd, SW_NORMAL);

            //获取实际的窗口大小，这将用来决定后续交换链的创建。什么是交换链？自己去了解
            RECT windowRect;
            GetClientRect(hWnd, &windowRect);
            clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);



            Vortice.Direct3D11.ID3D11Texture2D BackBuffer;
            Vortice.DXGI.IDXGISurface DXGISurface;
            Vortice.Direct2D1.ID2D1Factory1 D2DFactory;
            Vortice.Direct2D1.ID2D1RenderTarget Sprite;

            //先尝试使用 IDXGIFactory6 提供的 EnumAdapterByGpuPreference 方法枚举显卡，这个方法的功能是可以按照给定的参数进行排序，特别方便开发时，获取首个可用显卡
            //想要使用 EnumAdapterByGpuPreference 方法，需要先获取 IDXGIFactory6 对象。而 IDXGIFactory6 对象可以通过工厂创建 IDXGIFactory2 对象间接获取
            //接下来咱也会用到 IDXGIFactory2 提供的功能
            // 开始创建工厂创建 D3D 的逻辑
            var DxgiFactory = DXGI.DXGI.CreateDXGIFactory1<DXGI.IDXGIFactory2>();

            //为了让大家方便阅读获取显卡的代码，将获取显卡的代码放入到 GetHardwareAdapter 方法
            var hardwareAdapter = GetHardwareAdapter(DxgiFactory)

                //为了方便调试，这里就加上 ToList 让所有代码都执行
                // 这里 ToList 只是想列出所有的 IDXGIAdapter1 在实际代码里，大部分都是获取第一个
                .ToList().FirstOrDefault();
            if (hardwareAdapter == null)
            {
                throw new InvalidOperationException("Cannot detect D3D11 adapter");
            }

            //以下初始化 D3D 交换链
            // 功能等级
            // [C# 从零开始写 SharpDx 应用 聊聊功能等级]
            // (https://blog.lindexi.com/post/C-%E4%BB%8E%E9%9B%B6%E5%BC%80%E5%A7%8B%E5%86%99-SharpDx-%E5%BA%94%E7%94%A8-%E8%81%8A%E8%81%8A%E5%8A%9F%E8%83%BD%E7%AD%89%E7%BA%A7.html)
            D3D.FeatureLevel[] featureLevels = new[]
            {
            D3D.FeatureLevel.Level_11_1,
            D3D.FeatureLevel.Level_11_0,
            D3D.FeatureLevel.Level_10_1,
            D3D.FeatureLevel.Level_10_0,
            D3D.FeatureLevel.Level_9_3,
            D3D.FeatureLevel.Level_9_2,
            D3D.FeatureLevel.Level_9_1,
        };

            //使用以上获取的显示适配器接口创建设备
            DXGI.IDXGIAdapter1 adapter = hardwareAdapter;
            D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport;
            var result = D3D11.D3D11.D3D11CreateDevice
            (
                adapter,
                D3D.DriverType.Unknown,
                creationFlags,
                featureLevels,
                out D3D11.ID3D11Device d3D11Device, out D3D.FeatureLevel featureLevel,
                out D3D11.ID3D11DeviceContext d3D11DeviceContext
            );

            //也许使用这个显示适配器接口创建不出设备，通过判断返回值即可了解是否成功。创建失败，那就不指定具体的参数，使用 WARP 的方法创建
            if (result.Failure)
            {
                // 如果失败了，那就不指定显卡，走 WARP 的方式
                // http://go.microsoft.com/fwlink/?LinkId=286690
                result = D3D11.D3D11.D3D11CreateDevice(
                    IntPtr.Zero,
                    D3D.DriverType.Warp,
                    creationFlags,
                    featureLevels,
                    out d3D11Device, out featureLevel, out d3D11DeviceContext);

                // 如果失败，就不能继续
                result.CheckError();
            }

            //以上代码的 CheckError 方法，将会在失败抛出异常
            //创建成功，可以获取到 ID3D11Device 和 ID3D11DeviceContext 类型的对象和实际的功能等级。
            //这里的 ID3D11Device 就是 D3D 设备，提供给交换链绑定的功能，可以绘制到交换链的缓存里，从而被交换链刷新到屏幕上。
            //这里的 ID3D11DeviceContext 是包含了 D3D 设备的环境和配置，可以用来设置渲染状态等
            //由于后续期望使用的是 ID3D11Device1 接口，按照惯例，从 d3D11Device 获取
            // 大部分情况下，用的是 ID3D11Device1 和 ID3D11DeviceContext1 类型
            // 从 ID3D11Device 转换为 ID3D11Device1 类型
            var d3D11Device1 = d3D11Device.QueryInterface<D3D11.ID3D11Device1>();

            //理论上当前能运行 dotnet 6 的 Windows 系统，都是支持 ID3D11Device1 的
            //同理，获取 ID3D11DeviceContext1 接口
            var d3D11DeviceContext1 = d3D11DeviceContext.QueryInterface<D3D11.ID3D11DeviceContext1>();

            //获取到了新的两个接口，就可以减少 d3D11Device 和 d3D11DeviceContext 的引用计数。
            //调用 Dispose 不会释放掉刚才申请的 D3D 资源，只是减少引用计数
            // 转换完成，可以减少对 ID3D11Device1 的引用计数
            // 调用 Dispose 不会释放掉刚才申请的 D3D 资源，只是减少引用计数

            //d3D11Device.Dispose();
            //d3D11DeviceContext.Dispose();

            //创建设备完成之后，接下来就是创建交换链和关联窗口。创建交换链需要很多参数，在 DX 的设计上，
            //将参数放入到 SwapChainDescription 类型里面。和 DX 的接口设计一样，也有多个 SwapChainDescription 版本
            //创建 SwapChainDescription1 参数的代码如下
            // 创建设备，接下来就是关联窗口和交换链
            DXGI.Format colorFormat = DXGI.Format.B8G8R8A8_UNorm;

            // 缓存的数量，包括前缓存。大部分应用来说，至少需要两个缓存，这个玩过游戏的伙伴都知道
            const int FrameCount = 2;

            DXGI.SwapChainDescription1 swapChainDescription = new()
            {
                Width = (uint)clientSize.Width,
                Height = (uint)clientSize.Height,
                Format = colorFormat,
                BufferCount = FrameCount,
                BufferUsage = DXGI.Usage.RenderTargetOutput,
                SampleDescription = DXGI.SampleDescription.Default,
                Scaling = DXGI.Scaling.Stretch,
                SwapEffect = DXGI.SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore
            };

            //参数上面的各个参数的排列组合可以实现很多不同的功能，但是 DX 有一个坑的地方在于，参数是不正交的，有些参数设置不对，
            //将会在后续创建失败再设置是否进入全屏模式，对于现在很多游戏和应用，都可以使用设置窗口进入最大化的全屏模式，这里就设置不进入全屏
            // 设置是否全屏
            DXGI.SwapChainFullscreenDescription fullscreenDescription = new DXGI.SwapChainFullscreenDescription
            {
                Windowed = true
            };

            //设置了参数，就可以创建交换链。可以通过 HWnd 窗口句柄创建，也可以创建和 UWP 对接的 CreateSwapChainForCoreWindow 方式，
            //也可以通过 DirectComposition 的 CreateSwapChainForComposition 创建。本文这里采用 CreateSwapChainForHwnd 创建，关联窗口
            DXGI.IDXGISwapChain1 DXGISwapChain =
                DxgiFactory.CreateSwapChainForHwnd(d3D11Device1, hWnd, swapChainDescription, fullscreenDescription);

            //附带禁止按下 alt+enter 进入全屏，这是可选的
            // 不要被按下 alt+enter 进入全屏
            DxgiFactory.MakeWindowAssociation(hWnd, DXGI.WindowAssociationFlags.IgnoreAltEnter);

            //这就完成了最重要的交换链的创建，以上完成之后，即可让 D3D 的内容绘制在窗口上。接下来准备再加上 D2D 的绘制


            //// 获取交换链的后台缓冲区作为 DXGI 表面
            //Vortice.DXGI.IDXGISurface DXGISurface;
            //swapChain.GetBuffer(0, out DXGISurface);

            //// 创建D2D1工厂并与D3D设备关联
            //Vortice.Direct2D1.ID2D1Factory1 D2DFactory;
            //D2DFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory1>();
            ////会导致crash
            ////D2DFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory1>(FactoryType.MultiThreaded, DebugLevel.Information);
            //Vortice.Direct2D1.ID2D1Device D2D1Device;
            //Vortice.Direct2D1.ID2D1DeviceContext D2D1Context;
            //D2D1Device = D2DFactory.CreateDevice(d3D11Device1.QueryInterface<IDXGIDevice>());
            //D2D1Context = D2D1Device.CreateDeviceContext();


            #region 以下创建 D2D 绘制

            ////通过 D3D 承载 D2D 的内容。以上完成了 D3D 的初始化，接下来可以通过 DXGI 辅助创建 D2D 的 ID2D1RenderTarget 画布
            ////如上图的框架，想要使用 D2D 之前，需要先解决让 D2D 绘制到哪。让 D2D 绘制的输出，可以是一个 IDXGISurface 对象。
            ////通过 CreateDxgiSurfaceRenderTarget 方法既可以在 IDXGISurface 创建 ID2D1RenderTarget 对象，让绘制可以输出。
            ////而 IDXGISurface 可以从 ID3D11Texture2D 获取到。通过交换链的 GetBuffer 方法可以获取到 ID3D11Texture2D 对象
            ////本文将按照这个步骤，创建 ID2D1RenderTarget 画布。除了以上步骤之外，还有其他的方法，详细还请看官方文档的转换框架
            ////按照惯例创建 D2D 需要先创建工厂
            ////先从交换链获取到 ID3D11Texture2D 对象，通过 IDXGISwapChain1 的 GetBuffer 获取交换链的一个后台缓存
            //D3D11.ID3D11Texture2D backBufferTexture = DXGISwapChain.GetBuffer<D3D11.ID3D11Texture2D>(0);

            ////接着使用 QueryInterface 将 ID3D11Texture2D 转换为 IDXGISurface 对象
            //// 获取到 dxgi 的平面
            //DXGI.IDXGISurface DXGISurface = backBufferTexture.QueryInterface<DXGI.IDXGISurface>();

            //// 对接 D2D 需要创建工厂
            //D2D.ID2D1Factory1 D2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();

            ////获取到 IDXGISurface 即可通过 D2D 工厂创建 ID2D1RenderTarget 画布
            //var renderTargetProperties = new D2D.RenderTargetProperties(PixelFormat.Premultiplied);

            //D2D.ID2D1RenderTarget d2D1RenderTarget =
            //    D2DFactory.CreateDxgiSurfaceRenderTarget(DXGISurface, renderTargetProperties);

            ////这里获取到的 ID2D1RenderTarget 就是可以用来方便绘制 2D 的画布

            ////以下修改颜色
            ////最简单的绘制方式就是使用 Clear 方法修改颜色。本文只是告诉大家如何进行初始化，不会涉及到如何使用 D2D 绘制的内容
            ////在开始调用 Clear 方法之前，需要先调用 BeginDraw 方法，告诉 DX 开始绘制。完成绘制，需要调用 EndDraw 方法告诉 DX 绘制完成。
            ////这里必须明确的是，在对 ID2D1RenderTarget 调用各个绘制方法时，不是方法调用完成就渲染完成的，这些方法只是收集绘制指令，而不是立刻进行渲染
            //var renderTarget = d2D1RenderTarget;

            //// 开始绘制逻辑
            //renderTarget.BeginDraw();

            //// 随意创建颜色
            //var color = new Color4((byte)Random.Shared.Next(0), (byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255));
            //renderTarget.Clear(color);
            ////renderTarget.DrawText("测试测试测试",);

            //renderTarget.EndDraw();

            ////以上代码使用随意的颜色清理，调用 Clear 时，将会让整个 ID2D1RenderTarget 使用给定的颜色清理，
            ////也就是修改颜色在完成之后，调用一下交换链的 Present 和等待刷新
            //DXGISwapChain.Present(1, DXGI.PresentFlags.None);

            //// 等待刷新
            //d3D11DeviceContext1.Flush();

            #endregion

            #region 以下创建 D2D 绘制

            //通过 D3D 承载 D2D 的内容。以上完成了 D3D 的初始化，接下来可以通过 DXGI 辅助创建 D2D 的 ID2D1RenderTarget 画布
            //如上图的框架，想要使用 D2D 之前，需要先解决让 D2D 绘制到哪。让 D2D 绘制的输出，可以是一个 IDXGISurface 对象。
            //通过 CreateDxgiSurfaceRenderTarget 方法既可以在 IDXGISurface 创建 ID2D1RenderTarget 对象，让绘制可以输出。
            //而 IDXGISurface 可以从 ID3D11Texture2D 获取到。通过交换链的 GetBuffer 方法可以获取到 ID3D11Texture2D 对象
            //本文将按照这个步骤，创建 ID2D1RenderTarget 画布。除了以上步骤之外，还有其他的方法，详细还请看官方文档的转换框架
            //按照惯例创建 D2D 需要先创建工厂
            //先从交换链获取到 ID3D11Texture2D 对象，通过 IDXGISwapChain1 的 GetBuffer 获取交换链的一个后台缓存
            BackBuffer = DXGISwapChain.GetBuffer<ID3D11Texture2D>(0);
            //接着使用 QueryInterface 将 ID3D11Texture2D 转换为 IDXGISurface 对象
            // 获取到 dxgi 的平面
            DXGISurface = BackBuffer.QueryInterface<IDXGISurface>();
            // 对接 D2D 需要创建工厂
            D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
            //获取到 IDXGISurface 即可通过 D2D 工厂创建 ID2D1RenderTarget 画布
            var renderTargetProperties = new RenderTargetProperties(Vortice.DCommon.PixelFormat.Premultiplied);
            Sprite = D2DFactory.CreateDxgiSurfaceRenderTarget(DXGISurface, renderTargetProperties);

            //这里获取到的 ID2D1RenderTarget 就是可以用来方便绘制 2D 的画布

            //以下修改颜色
            //最简单的绘制方式就是使用 Clear 方法修改颜色。本文只是告诉大家如何进行初始化，不会涉及到如何使用 D2D 绘制的内容
            //在开始调用 Clear 方法之前，需要先调用 BeginDraw 方法，告诉 DX 开始绘制。完成绘制，需要调用 EndDraw 方法告诉 DX 绘制完成。
            //这里必须明确的是，在对 ID2D1RenderTarget 调用各个绘制方法时，不是方法调用完成就渲染完成的，这些方法只是收集绘制指令，而不是立刻进行渲染
            //var renderTarget = Sprite;

            // 开始绘制逻辑
            Sprite.BeginDraw();
            // 随意创建颜色
            var color = new Color4((byte)Random.Shared.Next(0), (byte)Random.Shared.Next(255), (byte)Random.Shared.Next(255));
            Sprite.Clear(color);
            //renderTarget.DrawText("测试测试测试",);
            Sprite.EndDraw();
            //以上代码使用随意的颜色清理，调用 Clear 时，将会让整个 ID2D1RenderTarget 使用给定的颜色清理，
            //也就是修改颜色在完成之后，调用一下交换链的 Present 和等待刷新
            DXGISwapChain.Present(1, PresentFlags.None);
            // 等待刷新
            d3D11DeviceContext1.Flush();

            #endregion

            //调用交换链的 Present 函数在屏幕上显示渲染缓冲区的内容 swapChain.Present(1, PresentFlags.None); 是等待垂直同步，
            //在刷新完成在完成这个方法，第一个参数是同步间隔，第二个参数是演示的标志
            //尝试运行一下代码，就可以看到创建出了一个窗口，窗口的设置了一个诡异的颜色
            //这就是入门级的使用 Vortice 从零开始控制台创建窗口，在窗口上使用 D2D 绘制的方法
            //在完成初始化的逻辑之后，就可以使用 D2D 绘制复杂的界面了。 在 ID2D1RenderTarget 可以方便调用各个方法进行绘制，
            //如绘制矩形，画圆等。详细请看 C# 从零开始写 SharpDx 应用 绘制基础图形

            // 开个消息循环等待
            Windows.Win32.UI.WindowsAndMessaging.MSG msg;
            while (true)
            {
                if (PeekMessage(out msg, default, 0, 0, PM_REMOVE) != false)
                {
                    _ = TranslateMessage(&msg);
                    _ = DispatchMessage(&msg);

                    if (msg.message is WM_QUIT or WM_CLOSE)
                    {
                        return;
                    }
                }
            }
        }

        private static IEnumerable<DXGI.IDXGIAdapter1> GetHardwareAdapter(DXGI.IDXGIFactory2 factory)
        {
            //先尝试从 IDXGIFactory2 对象获取 IDXGIFactory6 对象
            //在 DX 的设计上，接口都是一个个版本迭代的，为了保持兼容性，只是新加接口，而不是更改原来的接口定义。
            //也就是获取到的对象，也许有在这台设备上的 DX 版本，能支持到 IDXGIFactory6 版本，通用的做法是调用 QueryInterface*方法，
            //例如 QueryInterfaceOrNull 方法，尝试获取到更新的版本的接口对象。使用封装的 QueryInterfaceOrNull 方法，
            //可以在不支持时返回空，通过判断返回值即可了解是否支持
            DXGI.IDXGIFactory6? factory6 = factory.QueryInterfaceOrNull<DXGI.IDXGIFactory6>();
            if (factory6 != null)
            {
                //在 IDXGIFactory6 新加的 EnumAdapterByGpuPreference 方法可以支持传入参数，通过参数按照顺序返回显示适配器接口
                //传入高性能参数开始获取，将会按照顺序获取到 DX 认为的高性能排列的顺序
                // 先告诉系统，要高性能的显卡
                for (int adapterIndex = 0;
                     factory6.EnumAdapterByGpuPreference((uint)adapterIndex, DXGI.GpuPreference.HighPerformance,
                         out DXGI.IDXGIAdapter1? adapter).Success;
                     adapterIndex++)
                {
                    if (adapter == null)
                    {
                        continue;
                    }

                    //再扔掉使用软渲染的，扔掉软渲染的这一步只是为了演示如何判断获取到的显示适配器接口是采用软渲染的
                    DXGI.AdapterDescription1 desc = adapter.Description1;

                    if ((desc.Flags & DXGI.AdapterFlags.Software) != DXGI.AdapterFlags.None)
                    {
                        // Don't select the Basic Render Driver adapter.
                        adapter.Dispose();
                        continue;
                    }

                    //factory6.Dispose();
                    //这里可以输出获取到的显示适配器接口的描述，可以看看在不同的设备上的输出
                    Debug.Print($"枚举到 {adapter.Description1.Description} 显卡");
                    yield return adapter;
                }

                factory6.Dispose();
            }

            //如果获取不到，那就使用旧的方法枚举
            // 如果枚举不到，那系统返回啥都可以
            for (int adapterIndex = 0;
                 factory.EnumAdapters1((uint)adapterIndex, out DXGI.IDXGIAdapter1? adapter).Success;
                 adapterIndex++)
            {
                DXGI.AdapterDescription1 desc = adapter.Description1;

                if ((desc.Flags & DXGI.AdapterFlags.Software) != DXGI.AdapterFlags.None)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Dispose();

                    continue;
                }

                Debug.Print($"枚举到 {adapter.Description1.Description} 显卡");
                yield return adapter;
            }
        }

        private static LRESULT WndProc(HWND hWnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            return DefWindowProc(hWnd, message, wParam, lParam);
        }

        #endregion

        #region Vortice测试2

        public static void TestVortice2()
        {
            ApplicationConfiguration.Initialize();

            var form = new RenderForm();
            var stopwatch = new Stopwatch();
            var d2dFactory1 = D2D.D2D1.D2D1CreateFactory<ID2D1Factory1>();
            var renderTarget = d2dFactory1.CreateHwndRenderTarget(default,
                new()
                {
                    Hwnd = form.Handle,
                    PixelSize = new SizeI(form.ClientSize.Width, form.ClientSize.Height)
                }
            );

            stopwatch.Start();
            Vortice.WinForms.RenderLoop.Run(form, () =>
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;
                var clearColor = new Color4(
                    red: (float)(Math.Sin(elapsed) * 0.5 + 0.5f),
                    green: (float)(Math.Sin(elapsed + Math.PI / 2) * 0.5 + 0.5f),
                    blue: (float)(Math.Sin(elapsed + Math.PI) * 0.5 + 0.5f),
                    alpha: 1
                );
                if (form.InvokeRequired)
                {
                    form.BeginInvoke(new Action(() => {
                        renderTarget.BeginDraw();
                        renderTarget.Clear(clearColor);
                        renderTarget.EndDraw();
                    }));
                }
                else
                {
                    renderTarget.BeginDraw();
                    renderTarget.Clear(clearColor);
                    renderTarget.EndDraw();
                }
            });
        }

        #endregion

        #region Vortice官方测试

        public static void Test_DX2D1HelloWindowApp()
        {
            DX2D1HelloWindowApp app = new();
            app.Run();
        }

        public static void Test_HelloWindowApp()
        {
            HelloWindowApp app = new();
            app.Run();
        }

        public static void Test_TriangleApp()
        {
            TriangleApp app = new();
            app.Run();
        }

        public static void Test_BufferOffsetsApp()
        {
            BufferOffsetsApp app = new();
            app.Run();
        }

        public static void Test_DrawQuadApp()
        {
            DrawQuadApp app = new();
            app.Run();
        }

        public static void Test_CubeApp()
        {
            CubeApp app = new();
            app.Run();
        }

        public static void Test_CubeAlphaBlendApp()
        {
            CubeAlphaBlendApp app = new();
            app.Run();
        }

        public static void Test_TexturedCubeApp()
        {
            TexturedCubeApp app = new();
            app.Run();
        }

        public static void Test_TexturedCubeFromFileApp()
        {
            TexturedCubeFromFileApp app = new();
            app.Run();
        }

        public static void Test_MipmappingApp()
        {
            MipmappingApp app = new();
            app.Run();
        }

        public static void Test_DrawTextApp()
        {
            DrawTextApp app = new();
            app.Run();
        }

        #endregion
    }

    public class HelloWindowApp : D3D11Application
    {
        protected override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }
    }

    public class TriangleApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;

        protected override void Initialize()
        {
            ReadOnlySpan<VertexPositionColor> triangleVertices = stackalloc VertexPositionColor[]
            {
            new VertexPositionColor(new Vector3(0f, 0.5f, 0.0f), new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 1.0f)),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 1.0f))
        };
            _vertexBuffer = Device.CreateBuffer(triangleVertices, BindFlags.VertexBuffer);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("HelloTriangle.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("HelloTriangle.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);

            _inputLayout = Device.CreateInputLayout(VertexPositionColor.InputElements, vertexShaderByteCode.Span);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionColor.SizeInBytes);
            DeviceContext.Draw(3, 0);
        }

    }

    public sealed class DrawQuadApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;
        private readonly Random _random = new();
        private readonly bool _dynamicBuffer = true;

        protected override unsafe void Initialize()
        {
            ReadOnlySpan<VertexPositionColor> source =
            [
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.0f), new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
            new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 1.0f)),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 1.0f)),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 1.0f))
            ];
            if (_dynamicBuffer)
            {
                _vertexBuffer = Device.CreateBuffer((uint)(source.Length * VertexPositionColor.SizeInBytes), BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
                // It can be updated in this way
                // MappedSubresource mappedResource = DeviceContext.Map(_vertexBuffer, MapMode.WriteDiscard);
                // source.CopyTo(new Span<VertexPositionColor>(mappedResource.DataPointer.ToPointer(), source.Length));
                // DeviceContext.Unmap(_vertexBuffer, 0);

                // Or with helper method
                _vertexBuffer.SetData(DeviceContext, source, MapMode.WriteDiscard);
            }
            else
            {
                _vertexBuffer = Device.CreateBuffer(source, BindFlags.VertexBuffer);
            }

            ReadOnlySpan<ushort> quadIndices = [0, 1, 2, 0, 2, 3];
            _indexBuffer = Device.CreateBuffer(quadIndices, BindFlags.IndexBuffer);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("HelloTriangle.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("HelloTriangle.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionColor.InputElements, vertexShaderByteCode.Span);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected override void OnRender()
        {
            if (_dynamicBuffer)
            {
                ReadOnlySpan<VertexPositionColor> source =
                [
                    new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.0f), RandomColor()),
                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 1.0f)),
                new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.0f), RandomColor()),
                new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.0f), new Color4(0.0f, 0.0f, 1.0f, 1.0f))
                ];

                _vertexBuffer.SetData(DeviceContext, source, MapMode.WriteDiscard);
            }

            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionColor.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            DeviceContext.DrawIndexed(6, 0, 0);
        }

        private Color4 RandomColor()
        {
            return new Color4((float)_random.NextDouble(), (float)_random.NextDouble(), (float)_random.NextDouble(), 1.0f);
        }
    }

    public sealed class BufferOffsetsApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;

        protected override void Initialize()
        {
            ReadOnlySpan<VertexPosition2DColor> quadVertices = stackalloc VertexPosition2DColor[]
            {
            // Triangle
            new VertexPosition2DColor(new Vector2(0.0f, 0.55f), new Color3(1.0f, 0.0f, 0.0f)),
            new VertexPosition2DColor(new Vector2(0.25f, 0.05f), new Color3(0.0f, 1.0f, 0.0f)),
            new VertexPosition2DColor(new Vector2(-0.25f, 0.05f), new Color3(0.0f, 0.0f, 1.0f)),

            // Quad
            new VertexPosition2DColor(new Vector2(-0.25f, -0.05f), new Color3(0.0f, 0.0f, 1.0f)),
            new VertexPosition2DColor(new Vector2(0.25f, -0.05f), new Color3(0.0f, 1.0f, 0.0f)),
            new VertexPosition2DColor(new Vector2(0.25f, -0.55f), new Color3(1.0f, 0.0f, 0.0f)),
            new VertexPosition2DColor(new Vector2(-0.25f, -0.55f), new Color3(1.0f, 1.0f, 0.0f)),
        };
            _vertexBuffer = Device.CreateBuffer(quadVertices, BindFlags.VertexBuffer);

            ReadOnlySpan<ushort> quadIndices = [
                0,
            1,
            2,
            0,
            1,
            2,
            0,
            2,
            3
            ];
            _indexBuffer = Device.CreateBuffer(quadIndices, BindFlags.IndexBuffer);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("HelloTriangle.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("HelloTriangle.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPosition2DColor.InputElements, vertexShaderByteCode.Span);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);

            // Bind Vertex buffers and index buffer
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPosition2DColor.SizeInBytes, 0);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);

            // Triangle
            DeviceContext.DrawIndexed(3, 0, 0);

            // Quad
            bool secondWay = false;
            if (secondWay)
            {
                DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPosition2DColor.SizeInBytes, 3 * VertexPosition2DColor.SizeInBytes);
                DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 3 * sizeof(ushort));
                DeviceContext.DrawIndexed(6, 0, 0);
            }
            else
            {
                DeviceContext.DrawIndexed(6, 3, 3);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe readonly struct VertexPosition2DColor
    {
        public static uint SizeInBytes => (uint)sizeof(VertexPosition2DColor);

        public static readonly Vortice.Direct3D11.InputElementDescription[] InputElements = new[]
        {
        new Vortice.Direct3D11.InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
        new Vortice.Direct3D11.InputElementDescription("COLOR", 0, Format.R32G32B32_Float, 8, 0)
    };

        public VertexPosition2DColor(in Vector2 position, in Color3 color)
        {
            Position = position;
            Color = color;
        }

        public readonly Vector2 Position;
        public readonly Color3 Color;
    }

    public class CubeApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private D3D11ConstantBuffer<Matrix4x4> _constantBuffer;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;

        protected override void Initialize()
        {
            MeshData mesh = MeshUtilities.CreateCube(5.0f);
            _vertexBuffer = Device.CreateBuffer(mesh.Vertices, BindFlags.VertexBuffer);
            _indexBuffer = Device.CreateBuffer(mesh.Indices, BindFlags.IndexBuffer);

            _constantBuffer = new D3D11ConstantBuffer<Matrix4x4>(Device);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("Cube.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("Cube.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionNormalTexture.InputElements, vertexShaderByteCode.Span);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _constantBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected unsafe override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            float deltaTime = (float)Time.Total.TotalSeconds;
            Matrix4x4 world = Matrix4x4.CreateRotationX(deltaTime) * Matrix4x4.CreateRotationY(deltaTime * 2) * Matrix4x4.CreateRotationZ(deltaTime * .7f);

            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 25), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, AspectRatio, 0.1f, 100);
            Matrix4x4 viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4 worldViewProjection = Matrix4x4.Multiply(world, viewProjection);

            // Update constant buffer data
            _constantBuffer.SetData(DeviceContext, ref worldViewProjection);

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.VSSetConstantBuffer(0, _constantBuffer);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionNormalTexture.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            DeviceContext.DrawIndexed(36, 0, 0);
        }
    }

    public class CubeAlphaBlendApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private D3D11ConstantBuffer<Matrix4x4> _constantBuffer;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;
        private ID3D11RasterizerState _rasterizerState;
        private ID3D11DepthStencilState _depthStencilState;
        private ID3D11BlendState _blendState;

        protected override void Initialize()
        {
            (_vertexBuffer, _indexBuffer) = CreateBox(new Vector3(5.0f));

            _constantBuffer = new D3D11ConstantBuffer<Matrix4x4>(Device);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("Cube.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("Cube.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionColor.InputElements, vertexShaderByteCode.Span);

            _rasterizerState = Device.CreateRasterizerState(RasterizerDescription.CullNone);
            _depthStencilState = Device.CreateDepthStencilState(DepthStencilDescription.Default);
            _blendState = Device.CreateBlendState(D3D11.BlendDescription.NonPremultiplied);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _constantBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
            _rasterizerState.Dispose();
            _depthStencilState.Dispose();
            _blendState.Dispose();
        }

        protected unsafe override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, new Color4(0.5f, 0.5f, 0.5f, 1.0f));
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            float deltaTime = (float)Time.Total.TotalSeconds;
            Matrix4x4 world = Matrix4x4.CreateRotationX(deltaTime) * Matrix4x4.CreateRotationY(deltaTime * 2) * Matrix4x4.CreateRotationZ(deltaTime * .7f);

            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 25), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, AspectRatio, 0.1f, 100);
            Matrix4x4 viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4 worldViewProjection = Matrix4x4.Multiply(world, viewProjection);

            // Update constant buffer data
            _constantBuffer.SetData(DeviceContext, ref worldViewProjection);

            DeviceContext.RSSetState(_rasterizerState);
            DeviceContext.OMSetDepthStencilState(_depthStencilState);
            DeviceContext.OMSetBlendState(_blendState);

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.VSSetConstantBuffer(0, _constantBuffer);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionColor.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);


            DeviceContext.DrawIndexed(36, 0, 0);
        }

        private (ID3D11Buffer, ID3D11Buffer) CreateBox(in Vector3 size)
        {
            const int CubeFaceCount = 6;
            List<VertexPositionColor> vertices = new();
            Span<ushort> indices = stackalloc ushort[36];

            Vector3[] faceNormals =
            [
                Vector3.UnitZ,
            new Vector3(0.0f, 0.0f, -1.0f),
            Vector3.UnitX,
            new Vector3(-1.0f, 0.0f, 0.0f),
            Vector3.UnitY,
            new Vector3(0.0f, -1.0f, 0.0f),
        ];

            Color4[] faceColors =
            [
                new(1.0f, 0.0f, 0.0f, 0.4f),
            new(0.0f, 1.0f, 0.0f, 0.4f),
            new(0.0f, 0.0f, 1.0f, 0.4f),
            new(1.0f, 1.0f, 0.0f, 0.4f),
            new(1.0f, 0.0f, 1.0f, 0.4f),
            new(0.0f, 1.0f, 1.0f, 0.4f),
        ];

            Vector3 tsize = size / 2.0f;

            // Create each face in turn.
            int vbase = 0;
            int indicesCount = 0;
            for (int i = 0; i < CubeFaceCount; i++)
            {
                Vector3 normal = faceNormals[i];
                Color4 color = faceColors[i];

                // Get two vectors perpendicular both to the face normal and to each other.
                Vector3 basis = (i >= 4) ? Vector3.UnitZ : Vector3.UnitY;

                Vector3 side1 = Vector3.Cross(normal, basis);
                Vector3 side2 = Vector3.Cross(normal, side1);

                // Six indices (two triangles) per face.
                indices[indicesCount++] = (ushort)(vbase + 0);
                indices[indicesCount++] = (ushort)(vbase + 1);
                indices[indicesCount++] = (ushort)(vbase + 2);

                indices[indicesCount++] = (ushort)(vbase + 0);
                indices[indicesCount++] = (ushort)(vbase + 2);
                indices[indicesCount++] = (ushort)(vbase + 3);

                // Four vertices per face.
                // (normal - side1 - side2) * tsize // normal // t0
                vertices.Add(new VertexPositionColor(
                    Vector3.Multiply(Vector3.Subtract(Vector3.Subtract(normal, side1), side2), tsize),
                    color
                    ));

                // (normal - side1 + side2) * tsize // normal // t1
                vertices.Add(new VertexPositionColor(
                    Vector3.Multiply(Vector3.Add(Vector3.Subtract(normal, side1), side2), tsize),
                    color
                    ));

                // (normal + side1 + side2) * tsize // normal // t2
                vertices.Add(new VertexPositionColor(
                    Vector3.Multiply(Vector3.Add(normal, Vector3.Add(side1, side2)), tsize),
                    color
                    ));

                // (normal + side1 - side2) * tsize // normal // t3
                vertices.Add(new VertexPositionColor(
                    Vector3.Multiply(Vector3.Subtract(Vector3.Add(normal, side1), side2), tsize),
                    color
                    ));

                vbase += 4;
            }

            ID3D11Buffer vertexBuffer = Device.CreateBuffer(vertices.ToArray(), BindFlags.VertexBuffer);
            ID3D11Buffer indexBuffer = Device.CreateBuffer(indices.ToArray(), BindFlags.IndexBuffer);

            return (vertexBuffer, indexBuffer);
        }
    }

    public unsafe class TexturedCubeApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private D3D11ConstantBuffer<Matrix4x4> _constantBuffer;
        private ID3D11Texture2D _texture;
        private ID3D11ShaderResourceView _textureSRV;
        private ID3D11SamplerState _textureSampler;

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;
        private bool _dynamicTexture;

        protected override void Initialize()
        {
            MeshData mesh = MeshUtilities.CreateCube(5.0f);
            _vertexBuffer = Device.CreateBuffer(mesh.Vertices, BindFlags.VertexBuffer);
            _indexBuffer = Device.CreateBuffer(mesh.Indices, BindFlags.IndexBuffer);

            _constantBuffer = new(Device);

            ReadOnlySpan<Vortice.Mathematics.Color> pixels = [
                0xFFFFFFFF,
            0x00000000,
            0xFFFFFFFF,
            0x00000000,
            0x00000000,
            0xFFFFFFFF,
            0x00000000,
            0xFFFFFFFF,
            0xFFFFFFFF,
            0x00000000,
            0xFFFFFFFF,
            0x00000000,
            0x00000000,
            0xFFFFFFFF,
            0x00000000,
            0xFFFFFFFF,
        ];
            _texture = Device.CreateTexture2D(pixels, Format.R8G8B8A8_UNorm, 4, 4, mipLevels: 1);
            _textureSRV = Device.CreateShaderResourceView(_texture);
            _textureSampler = Device.CreateSamplerState(SamplerDescription.PointWrap);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("Cube.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("Cube.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionNormalTexture.InputElements, vertexShaderByteCode.Span);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _constantBuffer.Dispose();
            _textureSRV.Dispose();
            _textureSampler.Dispose();
            _texture.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected override void OnKeyboardEvent(KeyboardKey key, bool pressed)
        {
            if (key == KeyboardKey.D && pressed)
            {
                _dynamicTexture = !_dynamicTexture;
            }
        }

        protected override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            float deltaTime = (float)Time.Total.TotalSeconds;
            Matrix4x4 world = Matrix4x4.CreateRotationX(deltaTime) * Matrix4x4.CreateRotationY(deltaTime * 2) * Matrix4x4.CreateRotationZ(deltaTime * .7f);

            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 25), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, AspectRatio, 0.1f, 100);
            Matrix4x4 viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4 worldViewProjection = Matrix4x4.Multiply(world, viewProjection);

            // Update constant buffer data
            _constantBuffer.SetData(DeviceContext, ref worldViewProjection);

            // Update texture data
            if (_dynamicTexture)
            {
                ReadOnlySpan<Vortice.Mathematics.Color> pixels = [
                    (Vortice.Mathematics.Color)Colors.Red,
                0x00000000,
                (Vortice.Mathematics.Color)Colors.Green,
                0x00000000,
                0x00000000,
                (Vortice.Mathematics.Color)Colors.Blue,
                0x00000000,
                0xFFFFFFFF,
                0xFFFFFFFF,
                0x00000000,
                0xFFFFFFFF,
                0x00000000,
                0x00000000,
                0xFFFFFFFF,
                0x00000000,
                0xFFFFFFFF,
            ];
                DeviceContext.WriteTexture(_texture, 0, 0, pixels);
            }

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.VSSetConstantBuffer(0, _constantBuffer);
            DeviceContext.PSSetShaderResource(0, _textureSRV);
            DeviceContext.PSSetSampler(0, _textureSampler);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionNormalTexture.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            DeviceContext.DrawIndexed(36, 0, 0);
        }
    }

    public class TexturedCubeFromFileApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private ID3D11Buffer _constantBuffer;
        private ID3D11Texture2D _texture;
        private ID3D11ShaderResourceView _textureSRV;
        private ID3D11SamplerState _textureSampler;

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;

        protected override void Initialize()
        {
            MeshData mesh = MeshUtilities.CreateCube(5.0f);
            _vertexBuffer = Device.CreateBuffer(mesh.Vertices, BindFlags.VertexBuffer);
            _indexBuffer = Device.CreateBuffer(mesh.Indices, BindFlags.IndexBuffer);

            _constantBuffer = Device.CreateConstantBuffer<Matrix4x4>();

            string assetsPath = Path.Combine(AppContext.BaseDirectory, "Textures");
            string textureFile = Path.Combine(assetsPath, "10points.png");
            (_texture, _textureSRV) = LoadTexture(textureFile, 0);
            _textureSampler = Device.CreateSamplerState(SamplerDescription.PointWrap);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("Cube.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("Cube.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionNormalTexture.InputElements, vertexShaderByteCode.Span);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _constantBuffer.Dispose();
            _textureSRV.Dispose();
            _textureSampler.Dispose();
            _texture.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected unsafe override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            float deltaTime = (float)Time.Total.TotalSeconds;
            Matrix4x4 world = Matrix4x4.CreateRotationX(deltaTime) * Matrix4x4.CreateRotationY(deltaTime * 2) * Matrix4x4.CreateRotationZ(deltaTime * .7f);

            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 25), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, AspectRatio, 0.1f, 100);
            Matrix4x4 viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4 worldViewProjection = Matrix4x4.Multiply(world, viewProjection);

            // Update constant buffer data
            MappedSubresource mappedResource = DeviceContext.Map(_constantBuffer, MapMode.WriteDiscard);
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref worldViewProjection);
            DeviceContext.Unmap(_constantBuffer, 0);

            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.VSSetConstantBuffer(0, _constantBuffer);
            DeviceContext.PSSetShaderResource(0, _textureSRV);
            DeviceContext.PSSetSampler(0, _textureSampler);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionNormalTexture.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            DeviceContext.DrawIndexed(36, 0, 0);
        }
    }

    public sealed class MipmappingApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private D3D11ConstantBuffer<Matrix4x4> _constantBuffer;
        private D3D11ConstantBuffer<Vector4> _mipLevelsCB;
        private ID3D11Texture2D _texture;
        private ID3D11ShaderResourceView _textureSRV;
        private ID3D11SamplerState _textureSampler;

        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;
        private Stopwatch _mipLevelslock;
        private float _time;
        private int _mipLevel;

        protected override void Initialize()
        {
            MeshData mesh = MeshUtilities.CreateCube(5.0f);
            _vertexBuffer = Device.CreateBuffer(mesh.Vertices, BindFlags.VertexBuffer);
            _indexBuffer = Device.CreateBuffer(mesh.Indices, BindFlags.IndexBuffer);

            _constantBuffer = new(Device);
            _mipLevelsCB = new(Device);

            string assetsPath = Path.Combine(AppContext.BaseDirectory, "Textures");
            string textureFile = Path.Combine(assetsPath, "10points.png");
            (_texture, _textureSRV) = LoadTexture(textureFile);
            _textureSampler = Device.CreateSamplerState(SamplerDescription.LinearClamp);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("Cube.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("Cube.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionNormalTexture.InputElements, vertexShaderByteCode.Span);

            _mipLevelslock = Stopwatch.StartNew();
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _constantBuffer.Dispose();
            _mipLevelsCB.Dispose();
            _textureSRV.Dispose();
            _textureSampler.Dispose();
            _texture.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
        }

        protected unsafe override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            float deltaTime = (float)Time.Total.TotalSeconds;
            _time += _mipLevelslock.ElapsedMilliseconds / 1000.0f;
            if (_time > 2)
            {
                _time = 0;
                _mipLevelslock.Restart();

                _mipLevel++;

                if (_mipLevel >= _texture.Description.MipLevels)
                {
                    _mipLevel = 0;
                }

                Vector4 mipData = new Vector4(_mipLevel, 0.0f, 0.0f, 0.0f);
                _mipLevelsCB.SetData(DeviceContext, ref mipData);
            }

            Matrix4x4 world = Matrix4x4.CreateRotationX(deltaTime) * Matrix4x4.CreateRotationY(deltaTime * 2) * Matrix4x4.CreateRotationZ(deltaTime * .7f);

            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 25), new Vector3(0, 0, 0), Vector3.UnitY);
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 4, AspectRatio, 0.1f, 100);
            Matrix4x4 viewProjection = Matrix4x4.Multiply(view, projection);
            Matrix4x4 worldViewProjection = Matrix4x4.Multiply(world, viewProjection);

            // Update constant buffer data
            _constantBuffer.SetData(DeviceContext, ref worldViewProjection);



            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.VSSetShader(_vertexShader);
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.VSSetConstantBuffer(0, _constantBuffer);
            DeviceContext.PSSetConstantBuffer(1, _mipLevelsCB);
            DeviceContext.PSSetShaderResource(0, _textureSRV);
            DeviceContext.PSSetSampler(0, _textureSampler);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionNormalTexture.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            DeviceContext.DrawIndexed(36, 0, 0);
        }
    }

    public class DrawTextApp : D3D11Application
    {
        private ID3D11Buffer _vertexBuffer;
        private ID3D11Buffer _indexBuffer;
        private ID3D11VertexShader _vertexShader;
        private ID3D11PixelShader _pixelShader;
        private ID3D11InputLayout _inputLayout;
        private ID3D11Texture2D _texture;
        private ID3D11ShaderResourceView _textureSRV;
        private ID3D11RenderTargetView _textureRTV;
        private ID3D11SamplerState _textureSampler;

        static IDWriteFactory _directWriteFactory;
        static IDWriteTextFormat _textFormat;
        static ID2D1Factory7 _direct2dFactory;
        static ID2D1SolidColorBrush _brush;
        static ID2D1RenderTarget _renderTarget2d;

        protected override void Initialize()
        {
            ReadOnlySpan<VertexPositionTexture> source =
            [
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0.0f), new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0.0f), new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(0.5f, -0.5f, 0.0f), new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-0.5f, -0.5f, 0.0f), new Vector2(0, 1))
            ];

            _vertexBuffer = Device.CreateBuffer(source, BindFlags.VertexBuffer);

            ReadOnlySpan<ushort> quadIndices = [0, 1, 2, 0, 2, 3];
            _indexBuffer = Device.CreateBuffer(quadIndices, BindFlags.IndexBuffer);

            ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode("TextureShaders.hlsl", "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode("TextureShaders.hlsl", "PSMain", "ps_4_0");

            _vertexShader = Device.CreateVertexShader(vertexShaderByteCode.Span);
            _pixelShader = Device.CreatePixelShader(pixelShaderByteCode.Span);
            _inputLayout = Device.CreateInputLayout(VertexPositionTexture.InputElements, vertexShaderByteCode.Span);

            // create texture and associated resources
            Texture2DDescription desc = new()
            {
                ArraySize = 1,
                CPUAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                Format = Format.B8G8R8A8_UNorm,
                Height = 378,
                MipLevels = 1,
                MiscFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                Width = 720
            };
            _texture = Device.CreateTexture2D(desc);
            _textureSRV = Device.CreateShaderResourceView(_texture);
            _textureSampler = Device.CreateSamplerState(SamplerDescription.LinearWrap);
            _textureRTV = Device.CreateRenderTargetView(_texture);
            DeviceContext.ClearRenderTargetView(_textureRTV, Colors.MediumBlue);

            // create DWrite factory - used to create some of the objects we need.
            _directWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();

            // create an instance of IDWriteTextFormat - this describes the text's appearance.
            _textFormat = _directWriteFactory.CreateTextFormat(
                "Arial",
                FontWeight.Bold,
                Vortice.DirectWrite.FontStyle.Normal,
                FontStretch.Normal,
                100);

            // set text alignment
            _textFormat.TextAlignment = TextAlignment.Center;
            _textFormat.ParagraphAlignment = ParagraphAlignment.Center;

            // create Direct2D factory
            _direct2dFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<ID2D1Factory7>(Vortice.Direct2D1.FactoryType.SingleThreaded, DebugLevel.Information);

            // draw text onto the texture
            DrawText("Hello Text!", _texture);
        }

        protected override void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _vertexShader.Dispose();
            _pixelShader.Dispose();
            _inputLayout.Dispose();
            _textureSRV.Dispose();
            _textureRTV.Dispose();
            _texture.Dispose();
            _textureSampler.Dispose();
            _textFormat.Dispose();
            _brush?.Dispose();
            _direct2dFactory.Dispose();
            _directWriteFactory.Dispose();
        }

        protected override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            // input assembler
            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.IASetInputLayout(_inputLayout);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, VertexPositionTexture.SizeInBytes);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            // vertex shader
            DeviceContext.VSSetShader(_vertexShader);
            // pixel shader
            DeviceContext.PSSetShader(_pixelShader);
            DeviceContext.PSSetShaderResource(0, _textureSRV);
            DeviceContext.PSSetSampler(0, _textureSampler);
            // draw
            DeviceContext.DrawIndexed(6, 0, 0);
        }

        private static void DrawText(string text, ID3D11Texture2D target)
        {
            // the dxgi runtime layer provides the video memory sharing mechanism to allow
            // Direct2D and Direct3D to work together. One way to use the two technologies
            // together is by obtaining a IDXGISurface and then use CreateDxgiSurfaceRenderTarget
            // to create an ID2D1RenderTarget, which can then be drawn to with Direct2D.

            using IDXGISurface1 dxgiSurface = target.QueryInterface<IDXGISurface1>();

            RenderTargetProperties rtvProps = new()
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = Vortice.Direct2D1.FeatureLevel.Default,
                PixelFormat = Vortice.DCommon.PixelFormat.Premultiplied,
                Type = RenderTargetType.Hardware,
                Usage = RenderTargetUsage.None
            };
            _renderTarget2d = _direct2dFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, rtvProps);

            // Create the brush
            _brush?.Release();
            _brush = _renderTarget2d.CreateSolidColorBrush(Colors.Black);

            Rect layoutRect = new(0, 0, 720, 378);

            _renderTarget2d.BeginDraw();
            _renderTarget2d.Transform = Matrix3x2.Identity;
            _renderTarget2d.Clear(Colors.White);
            _renderTarget2d.DrawText(text, _textFormat, layoutRect, _brush);
            _renderTarget2d.EndDraw();

            _renderTarget2d.Dispose();
        }
    }

    public class VideoProcessorEnumerator
    {
        public void Test() 
        {
            FeatureLevel[] featureLevelsWin10 =
            [
                FeatureLevel.Level_12_1,
                FeatureLevel.Level_12_0
            ];

            FeatureLevel[] featureLevelsWin8 =
            [
                FeatureLevel.Level_11_1
            ];

            FeatureLevel[] featureLevelsWin7 =
            [
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            ];

            VideoProcessorContentDescription videoProcessorContentDescription = new VideoProcessorContentDescription()
            {
                InputFrameFormat = VideoFrameFormat.InterlacedTopFieldFirst,
                InputWidth = 800,
                InputHeight = 600,
                OutputWidth = 800,
                OutputHeight = 600
            };

            ID3D11VideoProcessor videoProcessor;

            List<FeatureLevel> featureLevels = new();
            if (OperatingSystem.IsWindowsVersionAtLeast(10))
            {
                featureLevels.AddRange(featureLevelsWin10);
                featureLevels.AddRange(featureLevelsWin8);
                featureLevels.AddRange(featureLevelsWin7);
            }
            else if (OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                featureLevels.AddRange(featureLevelsWin8);
                featureLevels.AddRange(featureLevelsWin7);
            }
            else
            {
                featureLevels.AddRange(featureLevelsWin7);
            }

            Result result = D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.VideoSupport, featureLevels.ToArray(), out ID3D11Device? device);
            if (!result.Success)
            {
                WriteLine("Failed to create D3D11 Device");
                return;
            }

            WriteLine($"D3D11 Device created successfully [{device!.FeatureLevel}]");

            ID3D11VideoDevice1 videoDevice = device.QueryInterface<ID3D11VideoDevice1>();
            ID3D11VideoContext videoContext = device.ImmediateContext.QueryInterface<ID3D11VideoContext1>();
            result = videoDevice.CreateVideoProcessorEnumerator(videoProcessorContentDescription, out ID3D11VideoProcessorEnumerator videoProcessorEnumerator);
            if (!result.Success)
            {
                WriteLine("Failed to create D3D11 Video Processor Enumerator");
                videoContext.Dispose();
                videoDevice.Dispose();
                return;
            }

            WriteLine($"D3D11 Video Processor Enumerator created successfully");

            ID3D11VideoProcessorEnumerator1 videoProcessorEnumerator1 = videoProcessorEnumerator.QueryInterface<ID3D11VideoProcessorEnumerator1>();
            videoProcessorEnumerator.Dispose();

            bool supportHLG = videoProcessorEnumerator1.CheckVideoProcessorFormatConversion(Format.P010, ColorSpaceType.YcbcrStudioGhlgTopLeftP2020, Format.B8G8R8A8_UNorm, ColorSpaceType.RgbFullG22NoneP709);
            bool supportHDR10Limited = videoProcessorEnumerator1.CheckVideoProcessorFormatConversion(Format.P010, ColorSpaceType.YcbcrStudioG2084TopLeftP2020, Format.B8G8R8A8_UNorm, ColorSpaceType.RgbStudioG2084NoneP2020);

            VideoProcessorCaps vpCaps = videoProcessorEnumerator1.VideoProcessorCaps;

            WriteLine($"=====================================================");
            WriteLine($"MaxInputStreams           {vpCaps.MaxInputStreams}");
            WriteLine($"MaxStreamStates           {vpCaps.MaxStreamStates}");
            WriteLine($"HDR10 Limited             {(supportHDR10Limited ? "yes" : "no")}");
            WriteLine($"HLG                       {(supportHLG ? "yes" : "no")}");

            WriteLine($"\n[Video Processor Device Caps]");
            foreach (VideoProcessorDeviceCaps cap in Enum.GetValues(typeof(VideoProcessorDeviceCaps)))
            {
                WriteLine($"{cap,-25} {((vpCaps.DeviceCaps & cap) != 0 ? "yes" : "no")}");
            }

            WriteLine($"\n[Video Processor Feature Caps]");
            foreach (VideoProcessorFeatureCaps cap in Enum.GetValues(typeof(VideoProcessorFeatureCaps)))
                WriteLine($"{cap,-25} {((vpCaps.FeatureCaps & cap) != 0 ? "yes" : "no")}");

            WriteLine($"\n[Video Processor Stereo Caps]");
            foreach (VideoProcessorStereoCaps cap in Enum.GetValues(typeof(VideoProcessorStereoCaps)))
            {
                WriteLine($"{cap,-25} {((vpCaps.StereoCaps & cap) != 0 ? "yes" : "no")}");
            }

            WriteLine($"\n[Video Processor Input Format Caps]");
            foreach (VideoProcessorFormatCaps cap in Enum.GetValues(typeof(VideoProcessorFormatCaps)))
            {
                WriteLine($"{cap,-25} {((vpCaps.InputFormatCaps & cap) != 0 ? "yes" : "no")}");
            }

            WriteLine($"\n[Video Processor Filter Caps]");
            foreach (VideoProcessorFilterCaps filter in Enum.GetValues(typeof(VideoProcessorFilterCaps)))
            {
                if ((vpCaps.FilterCaps & filter) != 0)
                {
                    videoProcessorEnumerator1.GetVideoProcessorFilterRange(ConvertFromVideoProcessorFilterCaps(filter), out VideoProcessorFilterRange range);
                    WriteLine($"{filter.ToString().PadRight(25, ' ')} [{range.Minimum.ToString().PadLeft(6, ' ')} - {range.Maximum.ToString().PadLeft(4, ' ')}] | x{range.Multiplier.ToString().PadLeft(4, ' ')} | *{range.Default}");
                }
                else
                {
                    WriteLine($"{filter.ToString().PadRight(25, ' ')} no");
                }
            }

            WriteLine($"\n[Video Processor Input Format Caps]");
            foreach (VideoProcessorAutoStreamCaps cap in Enum.GetValues(typeof(VideoProcessorAutoStreamCaps)))
            {
                WriteLine($"{cap.ToString().PadRight(25, ' ')} {((vpCaps.AutoStreamCaps & cap) != 0 ? "yes" : "no")}");
            }

            uint bobRate = ~0u;
            uint lastRate = ~0u;

            for (uint i = 0; i < vpCaps.RateConversionCapsCount; i++)
            {
                WriteLine($"\n[Video Processor Rate Conversion Caps #{i + 1}]");

                WriteLine($"\n\t[Video Processor Rate Conversion Caps]");
                videoProcessorEnumerator1.GetVideoProcessorRateConversionCaps(i, out VideoProcessorRateConversionCaps rcCap);
                var todo = typeof(VideoProcessorRateConversionCaps).GetFields();
                foreach (FieldInfo field in todo)
                    WriteLine($"\t{field.Name.PadRight(35, ' ')} {field.GetValue(rcCap)}");

                WriteLine($"\n\t[Video Processor Processor Caps]");
                foreach (VideoProcessorProcessorCaps cap in Enum.GetValues(typeof(VideoProcessorProcessorCaps)))
                    WriteLine($"\t{cap.ToString().PadRight(35, ' ')} {(((VideoProcessorProcessorCaps)rcCap.ProcessorCaps & cap) != 0 ? "yes" : "no")}");

                if (((VideoProcessorProcessorCaps)rcCap.ProcessorCaps & VideoProcessorProcessorCaps.DeinterlaceBob) != 0)
                    bobRate = i;

                lastRate = i;
            }

            if (bobRate == ~0u)
            {
                WriteLine("DeinterlaceBob not found");
            }

            uint usedRate = bobRate == ~0u ? lastRate : bobRate;
            result = videoDevice.CreateVideoProcessor(videoProcessorEnumerator1, usedRate, out videoProcessor);
            WriteLine($"\n=====================================================");
            if (!result.Success)
            {
                WriteLine($"Failed to create D3D11 Video Processor [#{usedRate}]");
            }
            else
            {
                WriteLine($"D3D11 Video Processor created successfully {(bobRate != ~0u ? "[bob method]" : "")}");
                videoProcessor.Dispose();
            }

            videoProcessorEnumerator1.Dispose();
            videoContext.Dispose();
            videoDevice.Dispose();

            static VideoProcessorFilter ConvertFromVideoProcessorFilterCaps(VideoProcessorFilterCaps filter)
            {
                return filter switch
                {
                    VideoProcessorFilterCaps.Brightness => VideoProcessorFilter.Brightness,
                    VideoProcessorFilterCaps.Contrast => VideoProcessorFilter.Contrast,
                    VideoProcessorFilterCaps.Hue => VideoProcessorFilter.Hue,
                    VideoProcessorFilterCaps.Saturation => VideoProcessorFilter.Saturation,
                    VideoProcessorFilterCaps.EdgeEnhancement => VideoProcessorFilter.EdgeEnhancement,
                    VideoProcessorFilterCaps.NoiseReduction => VideoProcessorFilter.NoiseReduction,
                    VideoProcessorFilterCaps.AnamorphicScaling => VideoProcessorFilter.AnamorphicScaling,
                    VideoProcessorFilterCaps.StereoAdjustment => VideoProcessorFilter.StereoAdjustment,
                    _ => VideoProcessorFilter.StereoAdjustment,
                };
            }
        }
    }


    class DX2D1HelloWindowApp : D3D11Application
    {
        private ID2D1Factory1? _direct2dFactory;
        private ID2D1RenderTarget _renderTarget2d;
        private ID2D1SolidColorBrush _brush;

        protected override void Initialize()
        {
            base.Initialize();

            // Create Direct2D factory
            _direct2dFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<ID2D1Factory1>(Vortice.Direct2D1.FactoryType.SingleThreaded, DebugLevel.Information);

            using IDXGISurface1 dxgiSurface = ColorTexture.QueryInterface<IDXGISurface1>();

            RenderTargetProperties rtvProps = new()
            {
                DpiX = 0,
                DpiY = 0,
                MinLevel = Vortice.Direct2D1.FeatureLevel.Default,
                PixelFormat = Vortice.DCommon.PixelFormat.Premultiplied,
                Type = RenderTargetType.Hardware,
                Usage = RenderTargetUsage.None
            };
            _renderTarget2d = _direct2dFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, rtvProps);

            _brush = _renderTarget2d.CreateSolidColorBrush(Colors.Black);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _brush!.Dispose();
            _renderTarget2d!.Dispose();
            _direct2dFactory!.Dispose();
        }

        protected override void OnRender()
        {
            DeviceContext.ClearRenderTargetView(ColorTextureView, Colors.CornflowerBlue);
            DeviceContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            Vector2 screenCenter = new Vector2(MainWindow.ClientSize.Width / 2f, MainWindow.ClientSize.Height / 2f);

            _renderTarget2d.BeginDraw();
            _renderTarget2d.Transform = Matrix3x2.Identity;
            _renderTarget2d.Clear(Colors.White);
            _renderTarget2d.DrawRectangle(new Rect(screenCenter.X, screenCenter.Y, 256, 256), _brush);
            //_renderTarget2d.DrawText(text, _textFormat, layoutRect, _brush);
            _renderTarget2d.EndDraw();

            //_renderTarget2d.Dispose();
        }
    }
}
