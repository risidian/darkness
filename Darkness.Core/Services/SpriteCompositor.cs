using Darkness.Core.Interfaces;
using SkiaSharp;

namespace Darkness.Core.Services
{
    public class SpriteCompositor : ISpriteCompositor
    {
        public byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight)
        {
            using var composite = new SKBitmap(sheetWidth, sheetHeight);
            using var canvas = new SKCanvas(composite);
            canvas.Clear(SKColors.Transparent);

            foreach (var stream in layerStreams)
            {
                using var layerBitmap = SKBitmap.Decode(stream);
                if (layerBitmap != null)
                {
                    canvas.DrawBitmap(layerBitmap, 0, 0);
                }
            }

            using var image = SKImage.FromBitmap(composite);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale)
        {
            using var sheet = SKBitmap.Decode(spriteSheetPng);

            int scaledWidth = frameWidth * scale;
            int scaledHeight = frameHeight * scale;

            using var frameBitmap = new SKBitmap(scaledWidth, scaledHeight);
            using var canvas = new SKCanvas(frameBitmap);
            canvas.Clear(SKColors.Transparent);

            var destRect = new SKRect(0, 0, scaledWidth, scaledHeight);
            var sourceRectF = new SKRect(frameX, frameY, frameX + frameWidth, frameY + frameHeight);
#pragma warning disable CS0618 // SKPaint.FilterQuality is deprecated in SkiaSharp 3.x but DrawBitmap has no SKSamplingOptions overload
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.None };
            canvas.DrawBitmap(sheet, sourceRectF, destRect, paint);
#pragma warning restore CS0618

            using var image = SKImage.FromBitmap(frameBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}
