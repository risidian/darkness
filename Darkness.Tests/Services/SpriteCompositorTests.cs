using Darkness.Core.Services;
using SkiaSharp;

namespace Darkness.Tests.Services
{
    public class SpriteCompositorTests
    {
        private readonly SpriteCompositor _compositor = new();

        private Stream CreateTestLayerStream(int width, int height, SKColor color)
        {
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(color);
            var stream = new MemoryStream();
            bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void CompositeLayers_SingleLayer_ReturnsSameDimensions()
        {
            using var layer = CreateTestLayerStream(832, 1344, SKColors.Red);
            var result = _compositor.CompositeLayers(new List<Stream> { layer }, 832, 1344);

            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            using var resultBitmap = SKBitmap.Decode(result);
            Assert.Equal(832, resultBitmap.Width);
            Assert.Equal(1344, resultBitmap.Height);
        }

        [Fact]
        public void CompositeLayers_TwoLayers_TopLayerOverlaps()
        {
            using var bottom = CreateTestLayerStream(64, 64, SKColors.Red);
            using var top = CreateTestLayerStream(64, 64, SKColors.Blue);

            var result = _compositor.CompositeLayers(new List<Stream> { bottom, top }, 64, 64);

            using var resultBitmap = SKBitmap.Decode(result);
            var pixel = resultBitmap.GetPixel(32, 32);
            Assert.Equal(SKColors.Blue, pixel);
        }

        [Fact]
        public void CompositeLayers_TransparentTopLayer_ShowsBottom()
        {
            using var bottom = CreateTestLayerStream(64, 64, SKColors.Red);
            using var top = CreateTestLayerStream(64, 64, SKColors.Transparent);

            var result = _compositor.CompositeLayers(new List<Stream> { bottom, top }, 64, 64);

            using var resultBitmap = SKBitmap.Decode(result);
            var pixel = resultBitmap.GetPixel(32, 32);
            Assert.Equal(SKColors.Red, pixel);
        }

        [Fact]
        public void CompositeLayers_EmptyList_ReturnsTransparentSheet()
        {
            var result = _compositor.CompositeLayers(new List<Stream>(), 64, 64);

            using var resultBitmap = SKBitmap.Decode(result);
            Assert.Equal(64, resultBitmap.Width);
            var pixel = resultBitmap.GetPixel(0, 0);
            Assert.Equal(0, pixel.Alpha);
        }

        [Fact]
        public void ExtractFrame_ReturnsScaledFrame()
        {
            using var bitmap = new SKBitmap(128, 128);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            using var redPaint = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(0, 0, 64, 64, redPaint);
            using var bluePaint = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(64, 0, 64, 64, bluePaint);

            var sheetPng = new MemoryStream();
            bitmap.Encode(sheetPng, SKEncodedImageFormat.Png, 100);
            var sheetBytes = sheetPng.ToArray();

            var result = _compositor.ExtractFrame(sheetBytes, 0, 0, 64, 64, 4);

            using var frame = SKBitmap.Decode(result);
            Assert.Equal(256, frame.Width);
            Assert.Equal(256, frame.Height);
            Assert.Equal(SKColors.Red, frame.GetPixel(128, 128));
        }
    }
}
