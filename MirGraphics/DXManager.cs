using Client.MirControls;
using Client.MirScenes;
using SharpGen.Runtime;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vortice;
using Vortice.D3DCompiler;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.DXGI.Debug;

namespace Client.MirGraphics
{
    class DXManager
    {
        public static List<MImage> TextureList = new List<MImage>();
        public static List<MirControl> ControlList = new List<MirControl>();
        public static Vortice.DXGI.IDXGIFactory2 DxgiFactory;
        public static Vortice.DXGI.IDXGISwapChain1 DXGISwapChain;
        public static Vortice.DXGI.SwapChainDescription1 swapChainDescription;
        public static Vortice.DXGI.IDXGISurface DXGISurface;
        public static Vortice.Direct3D11.ID3D11Device Device;
        public static Vortice.Direct3D11.ID3D11DeviceContext DeviceContext;
        public static Vortice.Direct2D1.ID2D1Factory1 D2DFactory;
        public static Vortice.Direct2D1.ID2D1Device D2D1Device;
        public static Vortice.Direct2D1.ID2D1DeviceContext D2D1Context;
        public static Vortice.DirectWrite.IDWriteFactory DwFactory;
        public static Vortice.WIC.IWICImagingFactory WicFactory;
        public static Vortice.Direct2D1.ID2D1RenderTarget Sprite;
        public static Vortice.Direct2D1.RenderTargetProperties SpriteRenderTargetProperties;
        public static Vortice.Direct3D11.ID3D11RenderTargetView CurrentSurface;
        public static Vortice.Direct3D11.ID3D11RenderTargetView MainSurface;
        public static Vortice.Direct3D11.ID3D11RenderTargetView FloorSurface, LightSurface;
        private static Vortice.Direct3D11.ID3D11Texture2D BackBuffer;
        public static Vortice.Direct3D11.ID3D11Texture2D RadarTexture;
        public static Vortice.Direct3D11.ID3D11Texture2D PoisonDotBackground;
        public static Vortice.Direct3D11.ID3D11Texture2D FloorTexture, LightTexture;
        public static List<Vortice.Direct3D11.ID3D11Texture2D> Lights = new List<Vortice.Direct3D11.ID3D11Texture2D>();
        public static Vortice.Direct3D11.ID3D11Texture2D DepthStencilTexture;
        public static Vortice.Direct3D11.ID3D11DepthStencilView DepthStencilView;
        public static Vortice.Direct2D1.ID2D1SolidColorBrush TextBrush;
        private static readonly Dictionary<nint, ID2D1Bitmap1> _targetBitmaps = new();
        private static bool _spriteDrawing;
        private static ID3D11BlendState _alphaBlendState;
        private static IDXGIInfoQueue _dxgiInfoQueue;
        private static ulong _dxgiInfoNext;
        private static ID2D1SolidColorBrush _lineBrush;
        private static ID3D11Query _gpuIdleQuery;
        private static bool _gpuIdleQuerySupported = true;
        private static nint _frameLatencyWaitableObject;
        private static bool _frameLatencyWaitableObjectSupported = true;
        private static nint _swapChainHwnd;
        private static long _nextResetAttemptTime;
        public static Vortice.Direct3D11.ID3D11PixelShader GrayScalePixelShader;
        public static Vortice.Direct3D11.ID3D11PixelShader NormalPixelShader;
        public static Vortice.Direct3D11.ID3D11PixelShader MagicPixelShader;
        public static VorticeParameters Parameters;
        public static bool DeviceLost;
        public static float Opacity = 1F;
        public static bool Blending;
        public static float BlendingRate;
        public static BlendMode BlendingMode;
        public static bool GrayScale;
        private static float _presentScale = 1.0f;
        private static float _presentOffsetX;
        private static float _presentOffsetY;
        private static float _presentClientToTargetScaleX = 1.0f;
        private static float _presentClientToTargetScaleY = 1.0f;
        private static int _presentVirtualWidth;
        private static int _presentVirtualHeight;
        private static Matrix3x2 _presentTransform = Matrix3x2.Identity;
        private static Matrix3x2 _presentInverseTransform = Matrix3x2.Identity;
        private static bool _presentationLogInitialized;
        private static int _presentationLogTargetWidth;
        private static int _presentationLogTargetHeight;
        private static int _presentationLogClientWidth;
        private static int _presentationLogClientHeight;
        private static int _presentationLogVirtualWidth;
        private static int _presentationLogVirtualHeight;
        private static float _presentationLogScale;
        private static float _presentationLogOffsetX;
        private static float _presentationLogOffsetY;

        public struct VorticeParameters
        {
            public bool inited { get; set; }
            public bool Windowed { get; set; }
            public int BackBufferWidth { get; set; }
            public int BackBufferHeight { get; set; }
            public int PresentationInterval { get; set; }
        }

        public static bool TryMapClientToVirtual(Point clientPoint, out Point virtualPoint)
        {
            float targetX = clientPoint.X * _presentClientToTargetScaleX;
            float targetY = clientPoint.Y * _presentClientToTargetScaleY;
            var mapped = Vector2.Transform(new Vector2(targetX, targetY), _presentInverseTransform);
            int x = (int)MathF.Floor(mapped.X);
            int y = (int)MathF.Floor(mapped.Y);
            if (_presentVirtualWidth > 0 && _presentVirtualHeight > 0)
            {
                if (x < 0 || y < 0 || x >= _presentVirtualWidth || y >= _presentVirtualHeight)
                {
                    virtualPoint = new Point(-1, -1);
                    return false;
                }
            }
            virtualPoint = new Point(x, y);
            return true;
        }

        private static void UpdatePresentationTransform(int targetWidth, int targetHeight)
        {
            if (D2D1Context == null)
                return;
            int virtualWidth = Settings.ScreenWidth;
            int virtualHeight = Settings.ScreenHeight;
            _presentVirtualWidth = virtualWidth;
            _presentVirtualHeight = virtualHeight;
            if (virtualWidth <= 0 || virtualHeight <= 0 || targetWidth <= 0 || targetHeight <= 0)
            {
                _presentScale = 1.0f;
                _presentOffsetX = 0.0f;
                _presentOffsetY = 0.0f;
                _presentClientToTargetScaleX = 1.0f;
                _presentClientToTargetScaleY = 1.0f;
                _presentTransform = Matrix3x2.Identity;
                _presentInverseTransform = Matrix3x2.Identity;
                D2D1Context.Transform = Matrix3x2.Identity;
                return;
            }
            var clientSize = Program.Form?.ClientSize ?? new Size(targetWidth, targetHeight);
            _presentClientToTargetScaleX = clientSize.Width > 0 ? targetWidth / (float)clientSize.Width : 1.0f;
            _presentClientToTargetScaleY = clientSize.Height > 0 ? targetHeight / (float)clientSize.Height : 1.0f;
            float scale = MathF.Min(targetWidth / (float)virtualWidth, targetHeight / (float)virtualHeight);
            if (!Settings.FullScreen && scale > 1.0f)
                scale = 1.0f;
            float offsetX = MathF.Round((targetWidth - virtualWidth * scale) * 0.5f);
            float offsetY = MathF.Round((targetHeight - virtualHeight * scale) * 0.5f);
            _presentScale = scale;
            _presentOffsetX = offsetX;
            _presentOffsetY = offsetY;
            _presentTransform = Matrix3x2.CreateScale(scale, scale) * Matrix3x2.CreateTranslation(offsetX, offsetY);
            Matrix3x2.Invert(_presentTransform, out _presentInverseTransform);
            D2D1Context.Transform = _presentTransform;
            LogPresentationState(targetWidth, targetHeight, clientSize, virtualWidth, virtualHeight);
        }

        private static void LogPresentationState(int targetWidth, int targetHeight, Size clientSize, int virtualWidth, int virtualHeight)
        {
            if (Program.Form == null)
                return;
            int clientWidth = clientSize.Width;
            int clientHeight = clientSize.Height;
            if (_presentationLogInitialized
                && _presentationLogTargetWidth == targetWidth
                && _presentationLogTargetHeight == targetHeight
                && _presentationLogClientWidth == clientWidth
                && _presentationLogClientHeight == clientHeight
                && _presentationLogVirtualWidth == virtualWidth
                && _presentationLogVirtualHeight == virtualHeight
                && MathF.Abs(_presentationLogScale - _presentScale) < 0.0001f
                && MathF.Abs(_presentationLogOffsetX - _presentOffsetX) < 0.0001f
                && MathF.Abs(_presentationLogOffsetY - _presentOffsetY) < 0.0001f)
            {
                return;
            }
            _presentationLogInitialized = true;
            _presentationLogTargetWidth = targetWidth;
            _presentationLogTargetHeight = targetHeight;
            _presentationLogClientWidth = clientWidth;
            _presentationLogClientHeight = clientHeight;
            _presentationLogVirtualWidth = virtualWidth;
            _presentationLogVirtualHeight = virtualHeight;
            _presentationLogScale = _presentScale;
            _presentationLogOffsetX = _presentOffsetX;
            _presentationLogOffsetY = _presentOffsetY;
            int deviceDpi = 0;
            try
            {
                deviceDpi = Program.Form.DeviceDpi;
            }
            catch
            {
                deviceDpi = 0;
            }

            CMain.SaveError(
                $"[RenderInfo] Client={clientWidth}x{clientHeight}, BackBuffer={targetWidth}x{targetHeight}, Virtual={virtualWidth}x{virtualHeight}, " +
                $"PresentScale={_presentScale:0.####}, Offset=({_presentOffsetX:0.##},{_presentOffsetY:0.##}), " +
                $"ClientToTarget=({_presentClientToTargetScaleX:0.####},{_presentClientToTargetScaleY:0.####}), " +
                $"FormDpi={deviceDpi}");
        }

        private static void DxgiInfoSet()
        {
#if DEBUG
            try
            {
                if (_dxgiInfoQueue == null) return;
                _dxgiInfoNext = _dxgiInfoQueue.GetNumStoredMessages(Vortice.DXGI.DXGI.DebugAll);
            }
            catch
            {
            }
#endif
        }

        private static void DxgiInfoDump(string prefix, int maxMessages = 32)
        {
#if DEBUG
            try
            {
                if (_dxgiInfoQueue == null) return;

                ulong end = _dxgiInfoQueue.GetNumStoredMessages(Vortice.DXGI.DXGI.DebugAll);
                if (end <= _dxgiInfoNext) return;

                int printed = 0;
                for (ulong i = _dxgiInfoNext; i < end && printed < maxMessages; i++)
                {
                    var message = _dxgiInfoQueue.GetMessage(Vortice.DXGI.DXGI.DebugAll, i);
                    CMain.SaveError($"{prefix} {message.Description}");
                    printed++;
                }

                _dxgiInfoNext = end;
            }
            catch
            {
            }
#endif
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(nint hObject);

        private static void InitFrameLatencyWaitableObject()
        {
            try
            {
                if (_frameLatencyWaitableObject != nint.Zero)
                {
                    CloseHandle(_frameLatencyWaitableObject);
                    _frameLatencyWaitableObject = nint.Zero;
                }

                if (DXGISwapChain == null || DXGISwapChain.NativePointer == nint.Zero)
                    return;

                try
                {
                    using var swapChain2 = DXGISwapChain.QueryInterface<IDXGISwapChain2>();
                    swapChain2.MaximumFrameLatency = 1;
                    _frameLatencyWaitableObject = swapChain2.FrameLatencyWaitableObject;
                }
                catch
                {
                    _frameLatencyWaitableObject = nint.Zero;
                }
            }
            catch (Exception ex)
            {
                _frameLatencyWaitableObjectSupported = false;
                CMain.SaveError($"[FrameLatency] init failed: {ex.Message}");
            }
        }

        private static void WaitForFrameLatency(string logPrefix, int timeoutMs = 250)
        {
            if (!_frameLatencyWaitableObjectSupported)
                return;

            if (_frameLatencyWaitableObject == nint.Zero)
                return;

            try
            {
                uint wait = WaitForSingleObject(_frameLatencyWaitableObject, (uint)Math.Max(timeoutMs, 0));
                if (wait == 0xFFFFFFFF)
                {
                    _frameLatencyWaitableObjectSupported = false;
                    CMain.SaveError($"{logPrefix} FrameLatency wait failed: {Marshal.GetLastWin32Error()}");
                }
            }
            catch (Exception ex)
            {
                _frameLatencyWaitableObjectSupported = false;
                CMain.SaveError($"{logPrefix} FrameLatency wait failed: {ex.Message}");
            }
        }

        private static void ApplySwapChainFullscreenState(Size targetSize)
        {
            // This client uses a borderless window to emulate fullscreen (like modern DXGI apps).
            // Calling IDXGISwapChain::SetFullscreenState can fail on some configurations and isn't required here.
            _ = targetSize;
            return;
        }

        private static void WaitForGpuIdle(string logPrefix)
        {
            if (!_gpuIdleQuerySupported)
                return;

            if (Device == null || Device.NativePointer == nint.Zero)
                return;

            if (DeviceContext == null || DeviceContext.NativePointer == nint.Zero)
                return;

            try
            {
                _gpuIdleQuery ??= Device.CreateQuery(new QueryDescription(Vortice.Direct3D11.QueryType.Event, Vortice.Direct3D11.QueryFlags.None));

                DeviceContext.End(_gpuIdleQuery);

                var timeout = Stopwatch.StartNew();
                while (DeviceContext.GetData(_gpuIdleQuery, IntPtr.Zero, 0, AsyncGetDataFlags.None) == Result.False)
                {
                    if (timeout.ElapsedMilliseconds > 500)
                    {
                        CMain.SaveError($"{logPrefix} GPU wait timeout.");
                        break;
                    }

                    Thread.Sleep(0);
                }
            }
            catch (Exception ex)
            {
                _gpuIdleQuerySupported = false;
                CMain.SaveError($"{logPrefix} GPU wait failed: {ex.Message}");
            }
        }

        private static bool RecreateSwapChain(Size clientSize)
        {
            try
            {
                if (DxgiFactory == null || DxgiFactory.NativePointer == nint.Zero)
                    return false;
                if (Device == null || Device.NativePointer == nint.Zero)
                    return false;
                if (Program.Form == null || Program.Form.IsDisposed)
                    return false;

                try
                {
                    ForceEndSpriteDraw();

                    if (D2D1Context != null)
                    {
                        D2D1Context.Target = null;
                        D2D1Context.Flush(out ulong _, out ulong _);
                        D2D1Context.Transform = Matrix3x2.Identity;
                    }
                }
                catch { }

                try
                {
                    DeviceContext?.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView)null);
                    DeviceContext?.UnsetRenderTargets();
                    DeviceContext?.ClearState();
                    DeviceContext?.Flush();
                }
                catch { }

                try
                {
                    WaitForFrameLatency("[RecreateSwapChain]");
                    WaitForGpuIdle("[RecreateSwapChain]");
                }
                catch { }

                try
                {
                    if (_frameLatencyWaitableObject != nint.Zero)
                    {
                        CloseHandle(_frameLatencyWaitableObject);
                        _frameLatencyWaitableObject = nint.Zero;
                    }
                }
                catch { }

                try { DXGISwapChain?.Dispose(); }
                catch { }
                DXGISwapChain = null;
                _swapChainHwnd = nint.Zero;

                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                catch { }

                swapChainDescription.Width = (uint)clientSize.Width;
                swapChainDescription.Height = (uint)clientSize.Height;

                if (swapChainDescription.BufferCount == 0)
                    swapChainDescription.BufferCount = 2;
                if (swapChainDescription.Format == Format.Unknown)
                    swapChainDescription.Format = Format.B8G8R8A8_UNorm;

                var fullscreenDescription = new SwapChainFullscreenDescription
                {
                    Windowed = true
                };

                nint hwnd = Program.Form.Handle;
                if (hwnd == nint.Zero)
                    return false;

                try
                {
                    DXGISwapChain = DxgiFactory.CreateSwapChainForHwnd(Device, hwnd, swapChainDescription, fullscreenDescription);
                }
                catch (SharpGen.Runtime.SharpGenException ex) when (
                    ex.HResult == unchecked((int)0x80070005) || // E_ACCESSDENIED
                    ex.HResult == unchecked((int)0x887A0001) || // DXGI_ERROR_INVALID_CALL
                    ex.HResult == unchecked((int)0x887A002B)    // DXGI_ERROR_ACCESS_DENIED
                )
                {
                    CMain.SaveError($"[RecreateSwapChain] CreateSwapChainForHwnd failed (0x{ex.HResult:X8}); trying HWND recreate. {ex.Message}");
                    if (TryRecreateFormHandle("[RecreateSwapChain]"))
                    {
                        hwnd = Program.Form.Handle;
                        DXGISwapChain = DxgiFactory.CreateSwapChainForHwnd(Device, hwnd, swapChainDescription, fullscreenDescription);
                    }
                    else
                    {
                        throw;
                    }
                }
                DxgiFactory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter);
                _swapChainHwnd = hwnd;

                InitFrameLatencyWaitableObject();
                return DXGISwapChain != null && DXGISwapChain.NativePointer != nint.Zero;
            }
            catch (Exception ex)
            {
                CMain.SaveError($"[ResetDevice] RecreateSwapChain failed: {ex}");
                return false;
            }
        }

#if DEBUG
        private static void DxgiReportLiveObjects(string prefix)
        {
            try
            {
                if (_dxgiInfoQueue == null) return;

                if (DXGI.DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug).Failure || dxgiDebug == null)
                    return;

                using (dxgiDebug)
                {
                    DxgiInfoSet();
                    dxgiDebug.ReportLiveObjects(DXGI.DebugAll, ReportLiveObjectFlags.Detail | ReportLiveObjectFlags.IgnoreInternal);
                    DxgiInfoDump(prefix, maxMessages: 256);
                }
            }
            catch
            {
            }
        }
#endif

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
            DxgiFactory = Vortice.DXGI.DXGI.CreateDXGIFactory1<Vortice.DXGI.IDXGIFactory2>();
#if DEBUG
            try
            {
                if (Vortice.Direct3D11.D3D11.SdkLayersAvailable())
                {
                    _dxgiInfoQueue = Vortice.DXGI.DXGI.DXGIGetDebugInterface1<IDXGIInfoQueue>();
                }
            }
            catch
            {
            }
#endif
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
            Vortice.Direct3D11.DeviceCreationFlags creationFlags = Vortice.Direct3D11.DeviceCreationFlags.BgraSupport;
#if DEBUG
            creationFlags |= Vortice.Direct3D11.DeviceCreationFlags.Debug;
#endif
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
                result = Vortice.Direct3D11.D3D11.D3D11CreateDevice(
                    IntPtr.Zero,
                    Vortice.Direct3D.DriverType.Warp,
                    creationFlags,
                    featureLevels,
                    out d3D11Device, out featureLevel, out d3D11DeviceContext);
                result.CheckError();
            }
            Device = d3D11Device;
            DeviceContext = d3D11DeviceContext;
            Vortice.DXGI.Format colorFormat = Vortice.DXGI.Format.B8G8R8A8_UNorm;
            const int FrameCount = 2;
            Size clientSize = Program.Form.ClientSize;
            if (clientSize.Width <= 0 || clientSize.Height <= 0)
            {
                clientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);
            }

            swapChainDescription = new()
            {
                Width = (uint)clientSize.Width,
                Height = (uint)clientSize.Height,
                Format = colorFormat,
                BufferCount = FrameCount,
                BufferUsage = Vortice.DXGI.Usage.RenderTargetOutput,
                SampleDescription = Vortice.DXGI.SampleDescription.Default,
                Scaling = Vortice.DXGI.Scaling.Stretch,
                SwapEffect = Vortice.DXGI.SwapEffect.FlipDiscard,
                AlphaMode = Vortice.DXGI.AlphaMode.Ignore,
                Flags = SwapChainFlags.FrameLatencyWaitableObject
            };
            Vortice.DXGI.SwapChainFullscreenDescription fullscreenDescription = new Vortice.DXGI.SwapChainFullscreenDescription
            {
                Windowed = true
            };

            nint hwnd = Program.Form.Handle;
            if (hwnd == nint.Zero)
                throw new InvalidOperationException("Form handle is not created.");

            try
            {
                DXGISwapChain = DxgiFactory.CreateSwapChainForHwnd(Device, hwnd, swapChainDescription, fullscreenDescription);
            }
            catch (SharpGen.Runtime.SharpGenException ex) when (
                ex.HResult == unchecked((int)0x80070005) || // E_ACCESSDENIED
                ex.HResult == unchecked((int)0x887A0001) || // DXGI_ERROR_INVALID_CALL
                ex.HResult == unchecked((int)0x887A002B)    // DXGI_ERROR_ACCESS_DENIED
            )
            {
                CMain.SaveError($"[Create] CreateSwapChainForHwnd failed (0x{ex.HResult:X8}); trying HWND recreate. {ex.Message}");
                if (TryRecreateFormHandle("[Create]"))
                {
                    hwnd = Program.Form.Handle;
                    DXGISwapChain = DxgiFactory.CreateSwapChainForHwnd(Device, hwnd, swapChainDescription, fullscreenDescription);
                }
                else
                {
                    throw;
                }
            }

            _swapChainHwnd = hwnd;
            DxgiFactory.MakeWindowAssociation(hwnd, Vortice.DXGI.WindowAssociationFlags.IgnoreAltEnter);
            InitFrameLatencyWaitableObject();

            D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
            using (var dxgiDevice = Device.QueryInterface<IDXGIDevice>())
            {
                D2D1Device = D2DFactory.CreateDevice(dxgiDevice);
            }
            D2D1Context = D2D1Device.CreateDeviceContext();
            WicFactory = new Vortice.WIC.IWICImagingFactory();
            DwFactory = Vortice.DirectWrite.DWrite.DWriteCreateFactory<IDWriteFactory>();


            Parameters.inited = true;
            Parameters.Windowed = !Settings.FullScreen;
            Parameters.BackBufferWidth = clientSize.Width;
            Parameters.BackBufferHeight = clientSize.Height;
            Parameters.PresentationInterval = Settings.FPSCap ? 1 : 0;

            LoadTextures();
            LoadPixelsShaders();
        }

        private static unsafe void LoadPixelsShaders()
        {
            var shaderNormalPath = Settings.ShadersPath + "normal.ps";
            var shaderGrayScalePath = Settings.ShadersPath + "grayscale.hlsl";
            var shaderMagicPath = Settings.ShadersPath + "magic.ps";

            if (System.IO.File.Exists(shaderNormalPath))
            {
                byte[] psBytecode = File.ReadAllBytes(shaderNormalPath);
                NormalPixelShader = Device.CreatePixelShader(psBytecode);
            }
            if (System.IO.File.Exists(shaderGrayScalePath))
            {
                var compilationResult = Vortice.D3DCompiler.Compiler.CompileFromFile(
                    shaderGrayScalePath,
                    "main",
                    "ps_4_0",
                    ShaderFlags.OptimizationLevel3
                );
                GrayScalePixelShader = Device.CreatePixelShader(compilationResult.Span);
            }
            if (System.IO.File.Exists(shaderMagicPath))
            {
                byte[] psBytecode = File.ReadAllBytes(shaderMagicPath);
                MagicPixelShader = Device.CreatePixelShader(psBytecode);
            }

        }

        private static unsafe void LoadTextures()
        {
            ForceEndSpriteDraw();

            try
            {
                if (D2D1Context != null)
                {
                    D2D1Context.Target = null;
                    D2D1Context.Flush(out ulong _, out ulong _);
                    D2D1Context.Transform = Matrix3x2.Identity;
                }
            }
            catch
            {
            }

            TextBrush?.Dispose();
            TextBrush = null;
            foreach (var bitmap in _targetBitmaps.Values)
                bitmap.Dispose();
            _targetBitmaps.Clear();
            DepthStencilView?.Dispose();
            DepthStencilView = null;
            DepthStencilTexture?.Dispose();
            DepthStencilTexture = null;
            CurrentSurface?.Dispose();
            CurrentSurface = null;
            MainSurface?.Dispose();
            MainSurface = null;
            DXGISurface?.Dispose();
            DXGISurface = null;
            BackBuffer?.Dispose();
            BackBuffer = null;
            BackBuffer = DXGISwapChain.GetBuffer<Vortice.Direct3D11.ID3D11Texture2D>(0);
            DXGISurface = BackBuffer.QueryInterface<Vortice.DXGI.IDXGISurface>();
            SpriteRenderTargetProperties = new RenderTargetProperties(
                new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied));
            Sprite = D2D1Context;
            Sprite.SetDpi(96.0f, 96.0f);
            D2D1Context.UnitMode = UnitMode.Pixels;
            var backBufferPixelFormat = new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Ignore);
            var backBufferBitmapProperties = new BitmapProperties1(
                backBufferPixelFormat,
                96.0f,
                96.0f,
                BitmapOptions.Target | BitmapOptions.CannotDraw);
            var backBufferBitmap = D2D1Context.CreateBitmapFromDxgiSurface(DXGISurface, backBufferBitmapProperties);
            _targetBitmaps[BackBuffer.NativePointer] = backBufferBitmap;
            D2D1Context.Target = backBufferBitmap;
            MainSurface = Device.CreateRenderTargetView(BackBuffer);
            MainSurface.DebugName = "初始表面";
            CurrentSurface = MainSurface;
            TextBrush = Sprite.CreateSolidColorBrush(new Vortice.Mathematics.Color4(1.0f, 1.0f, 1.0f, 1.0f));
            var backBufferDesc = BackBuffer.Description;
            var depthStencilDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = backBufferDesc.Width,
                Height = backBufferDesc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.D24_UNorm_S8_UInt,
                SampleDescription = backBufferDesc.SampleDescription,
                Usage = Vortice.Direct3D11.ResourceUsage.Default,
                BindFlags = Vortice.Direct3D11.BindFlags.DepthStencil,
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.None,
                MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None
            };
            DepthStencilTexture = Device.CreateTexture2D(depthStencilDesc);
            DepthStencilView = Device.CreateDepthStencilView(
                DepthStencilTexture,
                new Vortice.Direct3D11.DepthStencilViewDescription(DepthStencilTexture, Vortice.Direct3D11.DepthStencilViewDimension.Texture2D));

            DeviceContext.OMSetRenderTargets(MainSurface, DepthStencilView);
            DeviceContext.RSSetViewport(new Vortice.Mathematics.Viewport(backBufferDesc.Width, backBufferDesc.Height));
            DeviceContext.RSSetScissorRect(0, 0, (int)backBufferDesc.Width, (int)backBufferDesc.Height);

            if (RadarTexture == null || RadarTexture.Device == null)
            {
                var radarDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = 2,
                    Height = 2,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.Write,
                    MiscFlags = ResourceOptionFlags.None
                };
                RadarTexture = Device.CreateTexture2D(radarDesc);
                var stream = DeviceContext.Map(RadarTexture, 0, Vortice.Direct3D11.MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
                using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(2, 2, (int)stream.RowPitch, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, stream.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);
                }
                DeviceContext.Unmap(RadarTexture, 0);
            }
            if (PoisonDotBackground == null || PoisonDotBackground.Device == null)
            {
                var PoisonDotBackgroundDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = 5,
                    Height = 5,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.Write,
                    MiscFlags = ResourceOptionFlags.None
                };
                PoisonDotBackground = Device.CreateTexture2D(PoisonDotBackgroundDesc);
                var stream = DeviceContext.Map(PoisonDotBackground,0,Vortice.Direct3D11.MapMode.WriteDiscard,Vortice.Direct3D11.MapFlags.None);
                using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(5,5,(int)stream.RowPitch,System.Drawing.Imaging.PixelFormat.Format32bppPArgb,stream.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(Color.White);
                }
                DeviceContext.Unmap(PoisonDotBackground, 0);
            }
            CreateLights();
        }

        private unsafe static void CreateLights()
        {

            for (int i = Lights.Count - 1; i >= 0; i--)
                Lights[i].Dispose();
            Lights.Clear();
            for (int i = 1; i < LightSizes.Length; i++)
            {
                int width = LightSizes[i].X;
                int height = LightSizes[i].Y;
                var lightDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                    BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.Write,
                    MiscFlags = ResourceOptionFlags.None
                };
                Vortice.Direct3D11.ID3D11Texture2D light = Device.CreateTexture2D(lightDesc);
                var stream = DeviceContext.Map(light,0,Vortice.Direct3D11.MapMode.WriteDiscard,Vortice.Direct3D11.MapFlags.None);
                using (System.Drawing.Bitmap image = new System.Drawing.Bitmap(
                    width,
                    height,
                    (int)stream.RowPitch,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                    stream.DataPointer))
                {
                    using (Graphics graphics = Graphics.FromImage(image))
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
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
                DeviceContext.Unmap(light, 0);
                Lights.Add(light);
            }
        }

        private static ID2D1Bitmap1 GetOrCreateTargetBitmap(ID3D11Texture2D texture)
        {
            if (D2D1Context == null)
                throw new InvalidOperationException("Direct2D device context is not initialized.");

            if (_targetBitmaps.TryGetValue(texture.NativePointer, out var existing))
                return existing;

            using var dxgiSurface = texture.QueryInterface<IDXGISurface>();
            var bitmapProperties = new BitmapProperties1(
                SpriteRenderTargetProperties.PixelFormat,
                96.0f,
                96.0f,
                BitmapOptions.Target);

            var bitmap = D2D1Context.CreateBitmapFromDxgiSurface(dxgiSurface, bitmapProperties);
            _targetBitmaps.Add(texture.NativePointer, bitmap);
            return bitmap;
        }

        public static void ReleaseTargetBitmap(ID3D11Texture2D texture)
        {
            if (texture == null) return;
            ReleaseTargetBitmap(texture.NativePointer);
        }

        public static void ReleaseTargetBitmap(nint textureNativePointer)
        {
            if (textureNativePointer == nint.Zero) return;

            if (_targetBitmaps.TryGetValue(textureNativePointer, out var bitmap))
            {
                bitmap.Dispose();
                _targetBitmaps.Remove(textureNativePointer);
            }
        }

        private static void EnsureSpriteBegin()
        {
            if (Sprite == null || _spriteDrawing) return;

            Sprite.BeginDraw();
            _spriteDrawing = true;
        }

        public static void SetSurface(ref Vortice.Direct3D11.ID3D11RenderTargetView surface)
        {
            if (surface == null || surface.NativePointer == nint.Zero)
            {
                DeviceLost = true;
                return;
            }

            if (DeviceContext == null || DeviceContext.NativePointer == nint.Zero)
            {
                DeviceLost = true;
                return;
            }

            if (CurrentSurface == surface)
            {
                if (D2D1Context != null && ReferenceEquals(surface, MainSurface) && BackBuffer != null && BackBuffer.NativePointer != nint.Zero)
                {
                    var desc = BackBuffer.Description;
                    UpdatePresentationTransform((int)desc.Width, (int)desc.Height);
                }
                return;
            }

            bool resumeDrawing = _spriteDrawing;
            if (resumeDrawing)
            {
                Sprite_End();
            }

            CurrentSurface = surface;
            var depthStencilView = DepthStencilView;
            if (depthStencilView != null && depthStencilView.NativePointer == nint.Zero)
                depthStencilView = null;
            DeviceContext.OMSetRenderTargets(CurrentSurface, depthStencilView);

            if (D2D1Context != null)
            {
                Sprite = D2D1Context;

                if (ReferenceEquals(surface, MainSurface))
                {
                    if (BackBuffer == null || BackBuffer.NativePointer == nint.Zero)
                    {
                        DeviceLost = true;
                        if (resumeDrawing)
                            EnsureSpriteBegin();
                        return;
                    }

                    var targetBitmap = GetOrCreateTargetBitmap(BackBuffer);
                    if (!ReferenceEquals(D2D1Context.Target, targetBitmap))
                        D2D1Context.Target = targetBitmap;

                    var desc = BackBuffer.Description;
                    DeviceContext.RSSetViewport(new Vortice.Mathematics.Viewport(desc.Width, desc.Height));
                    DeviceContext.RSSetScissorRect(0, 0, (int)desc.Width, (int)desc.Height);

                    UpdatePresentationTransform((int)desc.Width, (int)desc.Height);
                }
                else
                {
                    ID3D11Texture2D texture = null;
                    try
                    {
                        using var resource = surface.Resource;
                        texture = resource.QueryInterface<ID3D11Texture2D>();
                    }
                    catch (Exception ex)
                    {
                        texture?.Dispose();
                        DeviceLost = true;
                        CMain.SaveError($"[SetSurface] Failed to resolve RTV resource: {ex}");
                        if (resumeDrawing)
                            EnsureSpriteBegin();
                        return;
                    }
                    if (texture == null)
                    {
                        DeviceLost = true;
                        CMain.SaveError("[SetSurface] RTV resource did not resolve to a texture.");
                        if (resumeDrawing)
                            EnsureSpriteBegin();
                        return;
                    }
                    using (texture)
                    {
                        if (texture.NativePointer == nint.Zero)
                        {
                            DeviceLost = true;
                            if (resumeDrawing)
                                EnsureSpriteBegin();
                            return;
                        }

                        var targetBitmap = GetOrCreateTargetBitmap(texture);
                        if (!ReferenceEquals(D2D1Context.Target, targetBitmap))
                            D2D1Context.Target = targetBitmap;

                        var desc = texture.Description;
                        DeviceContext.RSSetViewport(new Vortice.Mathematics.Viewport(desc.Width, desc.Height));
                        DeviceContext.RSSetScissorRect(0, 0, (int)desc.Width, (int)desc.Height);

                        D2D1Context.Transform = Matrix3x2.Identity;
                    }
                }
            }

            if (resumeDrawing)
            {
                EnsureSpriteBegin();
            }
        }

        public static void SetGrayscale(bool value)
        {
            if (GrayScale == value) return;
            GrayScale = value;
            Sprite_Flush();
        }

        public static void DrawOpaque(Vortice.Direct3D11.ID3D11Texture2D textured, Rectangle? sourceRect, Vector3? position, System.Drawing.Color colord, float opacity)
        {
            if (textured == null || textured.NativePointer == nint.Zero)
                return;

            if (Sprite == null || D2D1Context == null)
            {
                DeviceLost = true;
                return;
            }
            var baseColor = ToColor4_Vortice(colord);
            var finalOpacity = Math.Clamp(opacity * Opacity, 0.0f, 1.0f);
            var color = new Vortice.Mathematics.Color4(baseColor.R, baseColor.G, baseColor.B, finalOpacity);

            if (D2D1Context != null && (textured.Description.BindFlags & BindFlags.RenderTarget) != 0)
            {
                var d2dBitmap = GetOrCreateTargetBitmap(textured);
                Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, color);
            }
            else
            {
                using var surface = textured.QueryInterface<IDXGISurface>();
                var alphaMode = textured.Description.Format is Format.B8G8R8X8_UNorm
                    ? Vortice.DCommon.AlphaMode.Ignore
                    : Vortice.DCommon.AlphaMode.Premultiplied;

                var bitmapProps = new BitmapProperties
                {
                    PixelFormat = new Vortice.DCommon.PixelFormat(textured.Description.Format, alphaMode)
                };
                using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);
                Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, color);
            }

            CMain.DPSCounter++;
        }

        public static void Draw(Vortice.Direct3D11.ID3D11Texture2D textured, Rectangle? sourceRect, Vector3? position, System.Drawing.Color colord)
        {
            if (textured == null || textured.NativePointer == nint.Zero)
                return;

            if (Sprite == null || D2D1Context == null)
            {
                DeviceLost = true;
                return;
            }

            var baseColor = ToColor4_Vortice(colord);
            var finalAlpha = Math.Clamp(baseColor.A * Opacity, 0.0f, 1.0f);
            var finalColor = new Vortice.Mathematics.Color4(baseColor.R, baseColor.G, baseColor.B, finalAlpha);

            if (D2D1Context != null && (textured.Description.BindFlags & BindFlags.RenderTarget) != 0)
            {
                var d2dBitmap = GetOrCreateTargetBitmap(textured);
                Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, finalColor);
            }
            else
            {
                using var surface = textured.QueryInterface<IDXGISurface>();
                var alphaMode = textured.Description.Format is Format.B8G8R8X8_UNorm
                    ? Vortice.DCommon.AlphaMode.Ignore
                    : Vortice.DCommon.AlphaMode.Premultiplied;

                var bitmapProps = new BitmapProperties
                {
                    PixelFormat = new Vortice.DCommon.PixelFormat(textured.Description.Format, alphaMode)
                };
                using var d2dBitmap = Sprite.CreateSharedBitmap(surface, bitmapProps);
                Direct2DTextureRenderer.DrawTexture(Sprite, d2dBitmap, sourceRect, position, finalColor);
            }

            CMain.DPSCounter++;
        }

        public static void DrawMultiply(ID3D11Texture2D baseTexture, ID3D11Texture2D multiplyTexture, System.Numerics.Vector2? offset = null)
        {
            if (D2D1Context == null)
                throw new InvalidOperationException("Direct2D device context is not initialized.");

            var baseBitmap = GetOrCreateTargetBitmap(baseTexture);
            var multiplyBitmap = GetOrCreateTargetBitmap(multiplyTexture);

            using var blendEffect = new Vortice.Direct2D1.Effects.Blend(D2D1Context)
            {
                Mode = Vortice.Direct2D1.BlendMode.Multiply
            };
            blendEffect.SetInput(0, baseBitmap, true);
            blendEffect.SetInput(1, multiplyBitmap, true);

            D2D1Context.DrawImage(
                blendEffect,
                offset ?? System.Numerics.Vector2.Zero,
                Vortice.Direct2D1.InterpolationMode.NearestNeighbor,
                Vortice.Direct2D1.CompositeMode.SourceOver);

            CMain.DPSCounter++;
        }


        public static void AttemptReset()
        {
            long now = CMain.Time;
            if (now < _nextResetAttemptTime)
                return;

            _nextResetAttemptTime = now + 250;
            CMain.SaveError($"设备丢失DeviceLost，执行AttemptReset，当前表面{CurrentSurface}");
            try
            {
                if (Device == null || Device.NativePointer == nint.Zero)
                    return;

                var removedReason = Device.DeviceRemovedReason;
                if (removedReason != SharpGen.Runtime.Result.Ok)
                {
                    DeviceLost = true;
                    CMain.SaveError($"[AttemptReset] DeviceRemovedReason={removedReason}");
                    return;
                }

                if (DXGISwapChain == null || DXGISwapChain.NativePointer == nint.Zero
                    || MainSurface == null || MainSurface.NativePointer == nint.Zero
                    || D2D1Context == null || D2D1Context.NativePointer == nint.Zero
                    || Sprite == null || Sprite.NativePointer == nint.Zero)
                {
                    ResetDevice();
                    return;
                }

                DeviceLost = false;
                _nextResetAttemptTime = 0;
            }
            catch (Exception ex)
            {
            }
        }

        public static void ResetDevice()
        {
            try
            {
                ForceEndSpriteDraw();

                CleanUp();
                DeviceLost = true;

                Size clientSize = Program.Form.ClientSize;
                if (clientSize.Width == 0 || clientSize.Height == 0)
                    return;

                Parameters.Windowed = !Settings.FullScreen;
                Parameters.BackBufferWidth = clientSize.Width;
                Parameters.BackBufferHeight = clientSize.Height;
                Parameters.PresentationInterval = Settings.FPSCap ? 1 : 0;

                TextBrush?.Dispose();
                TextBrush = null;

                _lineBrush?.Dispose();
                _lineBrush = null;

                foreach (var bitmap in _targetBitmaps.Values)
                    bitmap.Dispose();
                _targetBitmaps.Clear();

                _alphaBlendState?.Dispose();
                _alphaBlendState = null;

                DepthStencilView?.Dispose();
                DepthStencilView = null;
                DepthStencilTexture?.Dispose();
                DepthStencilTexture = null;

                CurrentSurface?.Dispose();
                CurrentSurface = null;
                MainSurface?.Dispose();
                MainSurface = null;

                DXGISurface?.Dispose();
                DXGISurface = null;
                BackBuffer?.Dispose();
                BackBuffer = null;

                try
                {
                    if (D2D1Context != null)
                    {
                        D2D1Context.Target = null;
                        D2D1Context.Flush(out ulong _, out ulong _);
                    }
                }
                catch
                {
                }

                try
                {
                    D2D1Context?.Dispose();
                }
                catch
                {
                }
                D2D1Context = null;

                try
                {
                    D2D1Device?.Dispose();
                }
                catch
                {
                }
                D2D1Device = null;
                Sprite = null;
                _spriteDrawing = false;

                try
                {
                    DeviceContext?.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView)null);
                    DeviceContext?.UnsetRenderTargets();
                    DeviceContext?.ClearState();
                    DeviceContext?.Flush();
                }
                catch
                {
                }

                if (Program.Form == null || Program.Form.IsDisposed)
                    return;

                bool swapChainRecreated = false;
                if (DXGISwapChain == null || DXGISwapChain.NativePointer == nint.Zero || _swapChainHwnd != Program.Form.Handle)
                {
                    CMain.SaveError("[ResetDevice] SwapChain is missing or HWND changed; recreating.");

                    if (!RecreateSwapChain(clientSize))
                    {
                        CMain.SaveError("[ResetDevice] SwapChain recreate failed; attempting full device recreate.");
                        try { Dispose(); }
                        catch { }

                        try
                        {
                            Create();
                            DeviceLost = false;
                            _nextResetAttemptTime = 0;
                        }
                        catch (Exception ex)
                        {
                            DeviceLost = true;
                            CMain.SaveError($"[ResetDevice] Full recreate failed: {ex}");
                        }
                        return;
                    }

                    swapChainRecreated = true;
                }

                swapChainDescription.Width = (uint)clientSize.Width;
                swapChainDescription.Height = (uint)clientSize.Height;

                WaitForFrameLatency("[ResizeBuffers]");
                WaitForGpuIdle("[ResizeBuffers]");
                WaitForFrameLatency("[ResizeBuffers]");

                var resizeFlags = swapChainDescription.Flags;
                uint bufferCount = swapChainDescription.BufferCount;
                var bufferFormat = swapChainDescription.Format;
                if (bufferCount == 0) bufferCount = 2;
                if (bufferFormat == Format.Unknown) bufferFormat = Format.B8G8R8A8_UNorm;

                Result resizeResult = Result.Ok;
                if (!swapChainRecreated)
                {
                    DxgiInfoSet();
                    resizeResult = DXGISwapChain.ResizeBuffers(
                        bufferCount,
                        (uint)clientSize.Width,
                        (uint)clientSize.Height,
                        bufferFormat,
                        resizeFlags);

                    if (resizeResult.Failure)
                    {
                        DxgiInfoDump("[DXGI][ResizeBuffers]");
                        WaitForFrameLatency("[ResizeBuffers]");
                        DxgiInfoSet();
                        var retryResult = DXGISwapChain.ResizeBuffers(
                            bufferCount,
                            0,
                            0,
                            bufferFormat,
                            resizeFlags);
                        if (retryResult.Success)
                            resizeResult = retryResult;
                        else
                            DxgiInfoDump("[DXGI][ResizeBuffers]");
                    }
                }

                if (resizeResult.Failure)
                {
                    CMain.SaveError($"[ResizeBuffers] Failed: {resizeResult} (Client={clientSize.Width}x{clientSize.Height})");

#if DEBUG
                    DxgiReportLiveObjects("[DXGI][LiveObjects]");
#endif

                    try
                    {
                        D2D1Device?.Dispose();
                    }
                    catch
                    {
                    }
                    D2D1Device = null;

                    try
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                    catch
                    {
                    }

                    WaitForFrameLatency("[ResizeBuffers]");
                    var gcRetryResult = DXGISwapChain.ResizeBuffers(
                        bufferCount,
                        (uint)clientSize.Width,
                        (uint)clientSize.Height,
                        bufferFormat,
                        resizeFlags);
                    if (gcRetryResult.Failure)
                    {
                        DxgiInfoDump("[DXGI][ResizeBuffers]");
                        WaitForFrameLatency("[ResizeBuffers]");
                        DxgiInfoSet();
                        gcRetryResult = DXGISwapChain.ResizeBuffers(
                            bufferCount,
                            0,
                            0,
                            bufferFormat,
                            resizeFlags);
                    }

                    if (gcRetryResult.Success)
                        resizeResult = gcRetryResult;
                    else
                        DxgiInfoDump("[DXGI][ResizeBuffers]");
                }

                if (resizeResult.Failure)
                {
                    CMain.SaveError($"[ResizeBuffers] Failed: {resizeResult} (Client={clientSize.Width}x{clientSize.Height}), try recreate swapchain.");
                    if (!RecreateSwapChain(clientSize))
                    {
                        CMain.SaveError("[ResetDevice] SwapChain recreate failed; attempting full device recreate.");
                        try { Dispose(); }
                        catch { }

                        try
                        {
                            Create();
                            DeviceLost = false;
                            _nextResetAttemptTime = 0;
                        }
                        catch (Exception ex)
                        {
                            DeviceLost = true;
                            CMain.SaveError($"[ResetDevice] Full recreate failed: {ex}");
                        }
                        return;
                    }

                    resizeResult = Result.Ok;
                }
                else
                {
                    InitFrameLatencyWaitableObject();
                }

                try
                {
                    if (D2DFactory != null && Device != null && D2D1Device == null)
                    {
                        using (var dxgiDevice = Device.QueryInterface<IDXGIDevice>())
                        {
                            D2D1Device = D2DFactory.CreateDevice(dxgiDevice);
                        }
                    }

                    if (D2D1Device != null && D2D1Context == null)
                    {
                        D2D1Context = D2D1Device.CreateDeviceContext();
                        D2D1Context.UnitMode = UnitMode.Pixels;
                    }
                }
                catch (Exception ex)
                {
                    CMain.SaveError($"[ResetDevice] Recreate D2D failed: {ex}");
                }

                if (D2D1Context == null)
                {
                    CMain.SaveError("[ResetDevice] D2D context is null after reset; rendering will be skipped.");
                    DeviceLost = true;
                    return;
                }

                LoadTextures();

                if (BackBuffer != null)
                {
                    var desc = BackBuffer.Description;
                    if ((int)desc.Width != clientSize.Width || (int)desc.Height != clientSize.Height)
                    {
                        CMain.SaveError(
                            $"[ResizeBuffers] Mismatch: Client={clientSize.Width}x{clientSize.Height}, BackBuffer={desc.Width}x{desc.Height}");
                    }
                }

                DeviceLost = false;
                _nextResetAttemptTime = 0;
            }
            catch (Exception ex)
            {
                DeviceLost = true;
                CMain.SaveError(ex.ToString());
            }
        }

        public static void AttemptRecovery()
        {
            try
            {
                ForceEndSpriteDraw();
            }
            catch (Exception ex)
            {
            }

            try
            {
                LoadTextures();
                DeviceLost = false;
                _nextResetAttemptTime = 0;
            }
            catch (Exception ex)
            {
            }
        }

        public static void SetOpacity(float opacity)
        {
            if (Opacity == opacity)
                return;

            Opacity = opacity;
        }
        public static void SetBlend(bool value, float rate = 1F, BlendMode mode = BlendMode.NORMAL)
        {
            if (value == Blending && BlendingRate == rate && BlendingMode == mode) 
                return;

            Blending = value;
            BlendingRate = rate;
            BlendingMode = mode;

            if (D2D1Context != null)
            {
                Sprite_Flush();
                D2D1Context.PrimitiveBlend = Blending
                    ? Vortice.Direct2D1.PrimitiveBlend.Add
                    : Vortice.Direct2D1.PrimitiveBlend.SourceOver;
            }
        }


        public static ID3D11Texture2D CreateTextureFromBytes(byte[] data, uint width, uint height, ref nint point)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Invalid image data");

            int expectedBytes = checked((int)(width * height * 4));
            if (data.Length < expectedBytes)
                throw new ArgumentException("Image data size does not match dimensions");

            PremultiplyAlphaInPlace_Bgra32(data, expectedBytes);

            var texDesc = new Vortice.Direct3D11.Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, 
                SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,
                BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,
                CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
            };

            var initData = new Vortice.Direct3D11.SubresourceData(Marshal.AllocHGlobal(data.Length), (uint)width * 4, 0);
            point = initData.DataPointer;

            try
            {
                Marshal.Copy(data, 0, initData.DataPointer, data.Length);

                return DXManager.Device.CreateTexture2D(texDesc, new[] { initData });
            }
            finally
            {
                Marshal.FreeHGlobal(initData.DataPointer);
            }
        }

        private static void PremultiplyAlphaInPlace_Bgra32(byte[] data, int bytesToProcess)
        {
            int length = Math.Min(bytesToProcess, data.Length);
            for (int i = 0; i + 3 < length; i += 4)
            {
                byte a = data[i + 3];
                if (a == 0)
                {
                    data[i] = 0;
                    data[i + 1] = 0;
                    data[i + 2] = 0;
                    continue;
                }

                if (a == 255)
                    continue;

                int alpha = a;
                data[i] = (byte)((data[i] * alpha + 127) / 255);
                data[i + 1] = (byte)((data[i + 1] * alpha + 127) / 255);
                data[i + 2] = (byte)((data[i + 2] * alpha + 127) / 255);
            }
        }

        public static string PrintParentMethod()
        {
            var ret = "调用链:";
            StackTrace stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();

            if (stackFrames != null && stackFrames.Length > 1)
            {
                StackFrame pFrame = stackFrames[2];
                var pmethod = pFrame.GetMethod(); 
                var pdeclaringType = pmethod.DeclaringType; 
                string pmethodName = pmethod.Name;
                string ptypeName = pdeclaringType.Name; 
                string pnamespaceName = pdeclaringType.Namespace; 
                StackFrame frame = stackFrames[1]; 
                MethodBase method = frame.GetMethod(); 
                Type declaringType = method.DeclaringType; 
                string methodName = method.Name;
                string typeName = declaringType.Name; 
                string namespaceName = declaringType.Namespace; 
                ret = $"{ptypeName}.{pmethodName}({pFrame.GetFileLineNumber()}) → {typeName}.{methodName}({frame.GetFileLineNumber()})";
            }
            return ret;
        }

        public static void LineDraw(System.Numerics.Vector2[] lines, System.Drawing.Color color)
        {
            DrawLines(lines, color);
        }

        public static void DrawLines(Vector2[] points, Color color)
        {
            if (Sprite == null || Sprite.NativePointer == nint.Zero)
                return;
            if (_lineBrush == null || _lineBrush.NativePointer == nint.Zero)
            {
                _lineBrush?.Dispose();
                _lineBrush = Sprite.CreateSolidColorBrush(new Vortice.Mathematics.Color4(1, 1, 1, 1));
            }

            _lineBrush.Color = new Vortice.Mathematics.Color4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Sprite.DrawLine(
                    new Vector2(points[i].X, points[i].Y),
                    new Vector2(points[i + 1].X, points[i + 1].Y),
                    _lineBrush
                );
            }
        }

        public static unsafe Vortice.Direct3D11.ID3D11RenderTargetView GetSurfaceLevel(Vortice.Direct3D11.ID3D11Texture2D texture, int level)
        {
            if ((texture.Description.BindFlags & BindFlags.RenderTarget) == 0)
            {
                throw new InvalidOperationException("Texture was not created with RenderTarget bind flag");
            }
            var rtvDesc = new Vortice.Direct3D11.RenderTargetViewDescription
            {
                Format = texture.Description.Format,
                ViewDimension = Vortice.Direct3D11.RenderTargetViewDimension.Texture2D,
                Texture2D = new Vortice.Direct3D11.Texture2DRenderTargetView
                {
                    MipSlice = 0
                }
            };
            return Device.CreateRenderTargetView(texture, rtvDesc);
        }

        public static Vortice.Direct3D11.ID3D11Texture2D NewTexture_RenderTarget_Default(uint width, uint height)
        {
            try
            {
                var textureDesc = new Vortice.Direct3D11.Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, 
                    SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    Usage = Vortice.Direct3D11.ResourceUsage.Default, 
                    BindFlags = Vortice.Direct3D11.BindFlags.RenderTarget | Vortice.Direct3D11.BindFlags.ShaderResource,
                    CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.None,
                    MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
                };
                var texture = Device.CreateTexture2D(textureDesc);
                return texture;
            }
            catch (Exception ex)
            {
                throw;
            }
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

        public static Vortice.Mathematics.Color4 ToPremultipliedColor4_Vortice(System.Drawing.Color color)
        {
            float a = color.A / 255f;
            return new Vortice.Mathematics.Color4(
                (color.R / 255f) * a,
                (color.G / 255f) * a,
                (color.B / 255f) * a,
                a
            );
        }

        public static void DevicePresent()
        {
            if (DXGISwapChain == null || DXGISwapChain.NativePointer == nint.Zero)
                return;

            try
            {
                uint syncInterval = (uint)(Settings.FPSCap ? 1 : 0);
                DXGISwapChain.Present(syncInterval, Vortice.DXGI.PresentFlags.None);
            }
            catch (Exception ex)
            {
                DeviceLost = true;
                CMain.SaveError($"[Present] {ex}");
            }
        }

        public static void DeviceClear_Target(System.Drawing.Color color)
        {
            if (DeviceContext == null) return;

            bool resumeDrawing = _spriteDrawing;
            if (resumeDrawing)
            {
                Sprite_End();
            }

            try
            {
                var surface = CurrentSurface;
                if (surface == null || surface.NativePointer == nint.Zero)
                {
                    surface = MainSurface;
                    if (surface == null || surface.NativePointer == nint.Zero) return;
                    CurrentSurface = surface;
                }

                DeviceContext.ClearRenderTargetView(surface, ToPremultipliedColor4_Vortice(color));
            }
            finally
            {
                if (resumeDrawing)
                {
                    EnsureSpriteBegin();
                }
            }
        }

        public static void Sprite_Flush()
        {
            if (Sprite == null || !_spriteDrawing) return;

            Sprite.Flush(out ulong _, out ulong _);
            DeviceContext.Flush();
        }

        public static void Sprite_End()
        {
            if (Sprite == null || !_spriteDrawing) return;

            Sprite.EndDraw();
            _spriteDrawing = false;
        }

        private static void ForceEndSpriteDraw()
        {
            try
            {
                Sprite?.EndDraw();
            }
            catch
            {
            }
            finally
            {
                _spriteDrawing = false;
            }
        }

        private static bool TryRecreateFormHandle(string logPrefix)
        {
            if (Program.Form == null || Program.Form.IsDisposed)
                return false;

            try
            {
                var form = Program.Form;
                if (!form.IsHandleCreated)
                    form.CreateControl();

                nint oldHandle = form.Handle;
                if (oldHandle == nint.Zero)
                    return false;

                nint newHandle = (nint)form.ForceRecreateHandleForSwapChain();
                if (newHandle == nint.Zero)
                    return false;

                if (newHandle != oldHandle)
                    CMain.SaveError($"{logPrefix} HWND recreated: {oldHandle} -> {newHandle}");
                else
                    CMain.SaveError($"{logPrefix} HWND recreate attempted; handle unchanged: {oldHandle}");

                _swapChainHwnd = nint.Zero;
                try
                {
                    CMain.Graphics?.Dispose();
                    CMain.Graphics = form.CreateGraphics();
                }
                catch
                {
                }
                return true;
            }
            catch (Exception ex)
            {
                CMain.SaveError($"{logPrefix} HWND recreate failed: {ex.Message}");
            }

            return false;
        }

        public static void SpriteBegin_AlphaBlend()
        {
            if (D2D1Context != null)
            {
                D2D1Context.PrimitiveBlend = Vortice.Direct2D1.PrimitiveBlend.SourceOver;
            }
            EnsureSpriteBegin();
        }

        public static Matrix4x4 MatrixScaling0(float scaleX, float scaleY)
        {
            return Matrix4x4.CreateScale(scaleX, scaleY, 1.0f);
        }

        public static void SpriteTransform(Matrix4x4 matrix)
        {
            var bufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                ByteWidth = (uint)Marshal.SizeOf<Matrix4x4>(),
                CPUAccessFlags = CpuAccessFlags.Write
            };
            var constantBuffer = Device.CreateBuffer(bufferDesc);
            var dataStream = DeviceContext.Map(constantBuffer, MapMode.WriteDiscard);
            unsafe
            {
                var matrixPtr = (Matrix4x4*)dataStream.DataPointer;
                *matrixPtr = matrix; 
            }
            DeviceContext.Unmap(constantBuffer);
            DeviceContext.VSSetConstantBuffer(0, constantBuffer);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MatrixBuffer
        {
            public Vector4 param1;
            public Vector4 param2;
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
        }


        private static void CleanUp()
        {
            ForceEndSpriteDraw();

            if (D2D1Context != null)
            {
                try
                {
                    D2D1Context.Target = null;
                    D2D1Context.Flush(out ulong _, out ulong _);
                    D2D1Context.Transform = Matrix3x2.Identity;
                }
                catch
                {
                }
            }
            if (DeviceContext != null)
            {
                try
                {
                    DeviceContext.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView)null);
                    DeviceContext.UnsetRenderTargets();
                    DeviceContext.ClearState();
                    DeviceContext.Flush();
                }
                catch
                {
                }
            }

            TextBrush?.Dispose();
            TextBrush = null;

            _lineBrush?.Dispose();
            _lineBrush = null;

            foreach (var bitmap in _targetBitmaps.Values)
                bitmap.Dispose();
            _targetBitmaps.Clear();
            Sprite = null;
            _spriteDrawing = false;

            _alphaBlendState?.Dispose();
            _alphaBlendState = null;

            if (CurrentSurface != null && !ReferenceEquals(CurrentSurface, MainSurface))
            {
                CurrentSurface.Dispose();
            }
            CurrentSurface = null;

            MainSurface?.Dispose();
            MainSurface = null;

            DepthStencilView?.Dispose();
            DepthStencilView = null;
            DepthStencilTexture?.Dispose();
            DepthStencilTexture = null;

            DXGISurface?.Dispose();
            DXGISurface = null;
            BackBuffer?.Dispose();
            BackBuffer = null;

            if (PoisonDotBackground != null)
            {
                PoisonDotBackground.Dispose();
                PoisonDotBackground = null;
            }

            if (RadarTexture != null)
            {
                RadarTexture.Dispose();
                RadarTexture = null;
            }

            if (FloorTexture != null)
            {
                FloorTexture.Dispose();
                FloorTexture = null;
                try
                {
                    if (GameScene.Scene?.MapControl != null)
                        GameScene.Scene.MapControl.FloorValid = false;
                }
                catch
                {
                }

                if (FloorSurface != null)
                {
                    FloorSurface.Dispose();
                }

                FloorSurface = null;
            }

            if (LightTexture != null)
            {
                LightTexture.Dispose();
                LightTexture = null; 

                if (LightSurface != null)
                {
                    LightSurface.Dispose();
                }

                LightSurface = null;
            }

            if (Lights != null)
            {
                for (int i = 0; i < Lights.Count; i++)
                {
                    Lights[i].Dispose();
                    Lights[i] = null;
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
        }

        public static void Dispose()
        {
            CleanUp();

            NormalPixelShader?.Dispose();
            GrayScalePixelShader?.Dispose();
            MagicPixelShader?.Dispose();
            NormalPixelShader = null;
            GrayScalePixelShader = null;
            MagicPixelShader = null;

            try
            {
                if (_frameLatencyWaitableObject != nint.Zero)
                {
                    CloseHandle(_frameLatencyWaitableObject);
                    _frameLatencyWaitableObject = nint.Zero;
                }
            }
            catch { }

            try { DXGISwapChain?.SetFullscreenState(false, null); }
            catch { }

            DXGISwapChain?.Dispose();
            DXGISwapChain = null;
            _swapChainHwnd = nint.Zero;

            DxgiFactory?.Dispose();
            DxgiFactory = null;

            _dxgiInfoQueue?.Dispose();
            _dxgiInfoQueue = null;

            _gpuIdleQuery?.Dispose();
            _gpuIdleQuery = null;

            D2D1Context?.Dispose();
            D2D1Context = null;
            D2D1Device?.Dispose();
            D2D1Device = null;
            D2DFactory?.Dispose();
            D2DFactory = null;

            DwFactory?.Dispose();
            DwFactory = null;
            WicFactory?.Dispose();
            WicFactory = null;

            DeviceContext?.Dispose();
            DeviceContext = null;
            Device?.Dispose();
            Device = null;
        }

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

    public static class Direct2DTextureRenderer
    {
        public static void DrawTexture(
            ID2D1RenderTarget renderTarget,
            ID2D1Bitmap bitmap,
            Rectangle? sourceRect,
            System.Numerics.Vector3? position,
            Vortice.Mathematics.Color4 color)
        {
            var destRect = CalculateDestinationRect(
                bitmap.PixelSize,
                sourceRect,
                position);

            RawRectF? sourceRectF = sourceRect.HasValue
                ? new RawRectF(
                    sourceRect.Value.Left,
                    sourceRect.Value.Top,
                    sourceRect.Value.Right,
                    sourceRect.Value.Bottom)
                : null;

            float opacity = color.A;
            bool grayscale = DXManager.GrayScale;

            if (!grayscale && color.R == 1.0f && color.G == 1.0f && color.B == 1.0f)
            {
                renderTarget.DrawBitmap(
                    bitmap,
                    destRect,
                    opacity,
                    Vortice.Direct2D1.BitmapInterpolationMode.NearestNeighbor,
                    sourceRectF);
                return;
            }

            if (renderTarget is ID2D1DeviceContext deviceContext)
            {
                var alphaMode = bitmap.PixelFormat.AlphaMode == Vortice.DCommon.AlphaMode.Straight
                    ? Vortice.Direct2D1.ColorMatrixAlphaMode.Straight
                    : Vortice.Direct2D1.ColorMatrixAlphaMode.Premultiplied;

                Vortice.Mathematics.Matrix5x4 matrix;
                if (grayscale)
                {
                    const float lr = 0.299f;
                    const float lg = 0.587f;
                    const float lb = 0.114f;

                    matrix = new Vortice.Mathematics.Matrix5x4(
                        lr * color.R * opacity, lg * color.R * opacity, lb * color.R * opacity, 0,
                        lr * color.G * opacity, lg * color.G * opacity, lb * color.G * opacity, 0,
                        lr * color.B * opacity, lg * color.B * opacity, lb * color.B * opacity, 0,
                        0, 0, 0, opacity,
                        0, 0, 0, 0);
                }
                else
                {
                    matrix = new Vortice.Mathematics.Matrix5x4(
                        color.R * opacity, 0, 0, 0,
                        0, color.G * opacity, 0, 0,
                        0, 0, color.B * opacity, 0,
                        0, 0, 0, opacity,
                        0, 0, 0, 0);
                }

                using var colorMatrixEffect = new Vortice.Direct2D1.Effects.ColorMatrix(deviceContext)
                {
                    AlphaMode = alphaMode,
                    Matrix = matrix
                };
                colorMatrixEffect.SetInput(0, bitmap, true);

                var compositeMode = deviceContext.PrimitiveBlend == Vortice.Direct2D1.PrimitiveBlend.Add
                    ? Vortice.Direct2D1.CompositeMode.Plus
                    : Vortice.Direct2D1.CompositeMode.SourceOver;

                deviceContext.DrawImage(
                    colorMatrixEffect.Output,
                    new System.Numerics.Vector2(destRect.Left, destRect.Top),
                    sourceRectF,
                    Vortice.Direct2D1.InterpolationMode.NearestNeighbor,
                    compositeMode);
                return;
            }

            renderTarget.DrawBitmap(
                bitmap,
                destRect,
                opacity,
                Vortice.Direct2D1.BitmapInterpolationMode.NearestNeighbor,
                sourceRectF);
        }

        private static RawRectF CalculateDestinationRect(
            Vortice.Mathematics.SizeI textureSize,
            Rectangle? sourceRect,
            System.Numerics.Vector3? position)
        {
            int width = sourceRect.HasValue ? sourceRect.Value.Width : textureSize.Width;
            int height = sourceRect.HasValue ? sourceRect.Value.Height : textureSize.Height;

            float left = position.HasValue ? position.Value.X : 0;
            float top = position.HasValue ? position.Value.Y : 0;

            return new RawRectF(
                left,
                top,
                left + width,
                top + height);
        }
    }

}
