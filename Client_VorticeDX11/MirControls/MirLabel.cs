using Client.MirGraphics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using Vortice.WIC;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Client.MirControls
{
    public class MirLabel : MirControl
    {
        #region Auto Size
        private bool _autoSize;
        public bool AutoSize
        {
            get { return _autoSize; }
            set
            {
                if (_autoSize == value)
                    return;
                _autoSize = value;
                OnAutoSizeChanged(EventArgs.Empty);
            }
        }
        public event EventHandler AutoSizeChanged;
        private void OnAutoSizeChanged(EventArgs e)
        {
            TextureValid = false;
            GetSize();
            if (AutoSizeChanged != null)
                AutoSizeChanged.Invoke(this, e);
        }
        #endregion

        #region DrawFormat
        private TextFormatFlags _drawFormat;
        public TextFormatFlags DrawFormat
        {
            get { return _drawFormat; }
            set
            {
                _drawFormat = value;
                OnDrawFormatChanged(EventArgs.Empty);
            }
        }
        public event EventHandler DrawFormatChanged;
        private void OnDrawFormatChanged(EventArgs e)
        {
            TextureValid = false;

            if (DrawFormatChanged != null)
                DrawFormatChanged.Invoke(this, e);
        }
        #endregion

        #region Font
        private Font _font;
        public Font Font
        {
            get { return _font; }
            set
            {
                _font = ScaleFont(value);
                OnFontChanged(EventArgs.Empty);
            }
        }
        public event EventHandler FontChanged;
        private void OnFontChanged(EventArgs e)
        {
            TextureValid = false;

            GetSize();

            if (FontChanged != null)
                FontChanged.Invoke(this, e);
        }
        #endregion

        #region Out Line
        private bool _outLine;
        public bool OutLine
        {
            get { return _outLine; }
            set
            {
                if (_outLine == value)
                    return;
                _outLine = value;
                OnOutLineChanged(EventArgs.Empty);
            }
        }
        public event EventHandler OutLineChanged;
        private void OnOutLineChanged(EventArgs e)
        {
            TextureValid = false;
            GetSize();
            
            if (OutLineChanged != null)
                OutLineChanged.Invoke(this, e);
        }
        #endregion

        #region Out Line Colour
        private Color _outLineColour;
        public Color OutLineColour
        {
            get { return _outLineColour; }
            set
            {
                if (_outLineColour == value)
                    return;
                _outLineColour = value;
                OnOutLineColourChanged();
            }
        }
        public event EventHandler OutLineColourChanged;
        private void OnOutLineColourChanged()
        {
            TextureValid = false;

            if (OutLineColourChanged != null)
                OutLineColourChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Size

        private void GetSize()
        {
            if (!AutoSize)
                return;

            if (string.IsNullOrEmpty(_text))
                Size = Size.Empty;
            else
            {
                Size = TextRenderer.MeasureText(CMain.Graphics, Text, Font);
                //Size = new Size(Size.Width, Size.Height + 5);

                if (OutLine && Size != Size.Empty)
                    Size = new Size(Size.Width + 2, Size.Height + 2);
            }
        }
        #endregion

        #region Label
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value)
                    return;

                _text = value;
                OnTextChanged(EventArgs.Empty);
            }
        }
        public event EventHandler TextChanged;
        private void OnTextChanged(EventArgs e)
        {
            DrawControlTexture = !string.IsNullOrEmpty(Text);
            TextureValid = false;
            Redraw();

            GetSize();

            if (TextChanged != null)
                TextChanged.Invoke(this, e);
        }
        #endregion

        public MirLabel()
        {
            DrawControlTexture = true;
            _drawFormat = TextFormatFlags.WordBreak;

            _font = ScaleFont(new Font(Settings.FontName, 8F));
            _outLine = true;
            _outLineColour = Color.Black; 
            _text = string.Empty;

        }
        
        protected override unsafe void CreateTexture()
        {
            //CMain.SaveError(DXManager.PrintParentMethod());

            if (string.IsNullOrEmpty(Text))
                return;

            if (Size.Width == 0 || Size.Height == 0)
                return;

            if (TextureSize != Size)
                DisposeTexture();

            if (ControlTexture == null)
            {
                DXManager.ControlList.Add(this);
                //ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                //ControlTexture = DXManager.NewTexture_None_Managed((uint)Size.Width, (uint)Size.Height);
                TextureSize = Size;
            }

            ////DataRectangle stream = ControlTexture.LockRectangle(0, LockFlags.Discard);
            //var stream = DXManager.TextureLockRectangle_0Discard(ControlTexture);
            //using (Bitmap image = new Bitmap(Size.Width, Size.Height, Size.Width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, stream.DataPointer))
            //{
            //    using (Graphics graphics = Graphics.FromImage(image))
            //    {
            //        graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            //        graphics.CompositingQuality = CompositingQuality.HighQuality;
            //        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            //        graphics.TextContrast = 0;
            //        graphics.Clear(BackColour);

            //        if (OutLine)
            //        {
            //            TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 0, Size.Width, Size.Height), OutLineColour, DrawFormat);
            //            TextRenderer.DrawText(graphics, Text, Font, new Rectangle(0, 1, Size.Width, Size.Height), OutLineColour, DrawFormat);
            //            TextRenderer.DrawText(graphics, Text, Font, new Rectangle(2, 1, Size.Width, Size.Height), OutLineColour, DrawFormat);
            //            TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 2, Size.Width, Size.Height), OutLineColour, DrawFormat);
            //            TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 1, Size.Width, Size.Height), ForeColour, DrawFormat);

            //            //LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, this.Size.Width, this.Size.Height), Color.FromArgb(239, 243, 239), Color.White, LinearGradientMode.Vertical);
            //            ////graphics.DrawString(Text, Font, brush, 37, 9);
            //            ////graphics.DrawString(this.Text, this.Font, new SolidBrush(Color.Black), 39, 9, StringFormat.GenericDefault);
            //        }
            //        else
            //            TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 0, Size.Width, Size.Height), ForeColour, DrawFormat);
            //    }
            //}
            ////ControlTexture.UnlockRectangle(0);
            //DXManager.TextureUnlockRectangle(ControlTexture, 0);

            #region lyo：仿创建图片纹理

            using (Bitmap image = new Bitmap(Size.Width, Size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    //创建文字纹理内容
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.TextContrast = 0;
                    graphics.Clear(BackColour);
                    if (OutLine)
                    {
                        TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 0, Size.Width, Size.Height), OutLineColour, DrawFormat);
                        TextRenderer.DrawText(graphics, Text, Font, new Rectangle(0, 1, Size.Width, Size.Height), OutLineColour, DrawFormat);
                        TextRenderer.DrawText(graphics, Text, Font, new Rectangle(2, 1, Size.Width, Size.Height), OutLineColour, DrawFormat);
                        TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 2, Size.Width, Size.Height), OutLineColour, DrawFormat);
                        TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 1, Size.Width, Size.Height), ForeColour, DrawFormat);

                        //LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, this.Size.Width, this.Size.Height), Color.FromArgb(239, 243, 239), Color.White, LinearGradientMode.Vertical);
                        ////graphics.DrawString(Text, Font, brush, 37, 9);
                        ////graphics.DrawString(this.Text, this.Font, new SolidBrush(Color.Black), 39, 9, StringFormat.GenericDefault);
                    }
                    else
                    {
                        TextRenderer.DrawText(graphics, Text, Font, new Rectangle(1, 0, Size.Width, Size.Height), ForeColour, DrawFormat);
                    }

                    //if (ControlTexture == null)
                    //{
                    //    //重新创建纹理
                    //    DXManager.ControlList.Add(this);
                    //    var data = DXManager.GetBitmapPixelData(image);
                    //    var texDesc = new Vortice.Direct3D11.Texture2DDescription
                    //    {
                    //        Width = (uint)Size.Width,
                    //        Height = (uint)Size.Height,
                    //        MipLevels = 1,
                    //        ArraySize = 1,
                    //        Format = Vortice.DXGI.Format.B8G8R8A8_UNorm, // 对应A8R8G8B8格式
                    //        SampleDescription = new Vortice.DXGI.SampleDescription(1, 0),
                    //        Usage = Vortice.Direct3D11.ResourceUsage.Dynamic,// Pool.Managed等效配置
                    //        BindFlags = Vortice.Direct3D11.BindFlags.ShaderResource,// Usage.None默认绑定
                    //        CPUAccessFlags = Vortice.Direct3D11.CpuAccessFlags.Write,
                    //        //MiscFlags = Vortice.Direct3D11.ResourceOptionFlags.None,
                    //    };
                    //    var initData = new Vortice.Direct3D11.SubresourceData(Marshal.AllocHGlobal(data.Length), (uint)Size.Width * 4, 0);
                    //    try
                    //    {
                    //        Marshal.Copy(data, 0, initData.DataPointer, data.Length);
                    //        ControlTexture = DXManager.Device.CreateTexture2D(texDesc, new[] { initData });
                    //    }
                    //    finally
                    //    {
                    //        Marshal.FreeHGlobal(initData.DataPointer);
                    //    }
                    //    //ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                    //    //ControlTexture = DXManager.NewTexture_None_Managed((uint)Size.Width, (uint)Size.Height);
                    //    TextureSize = Size;
                    //}
                    //else
                    //{
                    //    //todo:复用纹理，内容注入

                    //}

                    nint datapoint = 0;
                    ControlTexture = DXManager.CreateTextureFromBytes(DXManager.GetBitmapPixelData(image), (uint)Size.Width, (uint)Size.Height, ref datapoint);
                }
            }

            #endregion

            #region lyo：Vortice实现

            //// 创建WIC渲染目标
            //using var wicBitmap = DXManager.WicFactory.CreateBitmap((uint)Size.Width, (uint)Size.Height, 
            //    Vortice.WIC.PixelFormat.Format32bppPBGRA,BitmapCreateCacheOption.CacheOnLoad);
            //// 创建D2D渲染目标
            //using var renderTarget = DXManager.D2DFactory.CreateWicBitmapRenderTarget(wicBitmap,new RenderTargetProperties());
            //// 创建渲染器实例
            //using var renderer = new Direct2DText(renderTarget);
            //// 创建纹理位图
            //using var textureBitmap = renderer.CreateTextureBitmap(new Size(Size.Width, Size.Height));
            //// 绘制文本
            //renderer.DrawTextToTexture(textureBitmap, Text, Font, ForeColour, BackColour, new Size(Size.Width, Size.Height), 
            //    DrawTextOptions.None, OutLine ? 1 : 0, OutLineColour);

            #endregion

            //DXManager.Sprite.Flush();
            DXManager.Sprite_Flush();

            TextureValid = true;

            //CMain.SaveError($"MirLabel.draw文字：{Text}");
        }

        #region Disposable
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            
            AutoSizeChanged = null;
            _autoSize = false;

            DrawFormatChanged = null;
            _drawFormat = 0;

            FontChanged = null;
            if (_font != null)
            {
                _font.Dispose();
                _font = null;
            }

            OutLineChanged = null;
            _outLine = false;

            OutLineColourChanged = null;
            _outLineColour = Color.Empty;

            TextChanged = null;
            _text = null;
        }
        #endregion

    }
}
