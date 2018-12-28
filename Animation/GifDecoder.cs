using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Animation.Gif
{
    public static class GifDecodeHelper
    {
        private struct Int32Size
        {
            public Int32Size(int width, int height) : this()
            {
                Width = width;
                Height = height;
            }

            public int Width { get; private set; }
            public int Height { get; private set; }
        }

        private enum FrameDisposalMethod
        {
            None = 0,
            DoNotDispose = 1,
            RestoreBackground = 2,
            RestorePrevious = 3
        }

        private class FrameMetadata
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            // delay time in milisecond
            public Double Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
        }

        private static T GetQueryOrDefault<T>(this BitmapMetadata metadata, string query, T defaultValue)
        {
            if (metadata.ContainsQuery(query))
                return (T)Convert.ChangeType(metadata.GetQuery(query), typeof(T));
            return defaultValue;
        }

        private static T GetQueryOrNull<T>(this BitmapMetadata metadata, string query)
            where T : class
        {
            if (metadata.ContainsQuery(query))
                return metadata.GetQuery(query) as T;
            return null;
        }

        private static Int32Size GetFullSize(BitmapDecoder decoder)
        {            
            int width = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Width", 0);
            int height = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Height", 0);
            return new Int32Size(width, height);
        }

        private static FrameMetadata GetFrameMetadata(BitmapFrame frame)
        {
            var metadata = (BitmapMetadata)frame.Metadata;
            // default is 100 miliseconds
            var delay = 100.0;
            var metadataDelay = metadata.GetQueryOrDefault("/grctlext/Delay", 10);
            if (metadataDelay != 0)
                delay = metadataDelay * 10.0;
            var disposalMethod = (FrameDisposalMethod)metadata.GetQueryOrDefault("/grctlext/Disposal", 0);
            var frameMetadata = new FrameMetadata
            {
                Left = metadata.GetQueryOrDefault("/imgdesc/Left", 0),
                Top = metadata.GetQueryOrDefault("/imgdesc/Top", 0),
                Width = metadata.GetQueryOrDefault("/imgdesc/Width", frame.PixelWidth),
                Height = metadata.GetQueryOrDefault("/imgdesc/Height", frame.PixelHeight),
                Delay = delay,
                DisposalMethod = disposalMethod
            };
            return frameMetadata;
        }

        private static bool IsFullFrame(FrameMetadata metadata, Int32Size fullSize)
        {
            return metadata.Left == 0
                   && metadata.Top == 0
                   && metadata.Width == fullSize.Width
                   && metadata.Height == fullSize.Height;
        }

        private static BitmapSource MakeFrame(
            Int32Size fullSize,
            BitmapSource rawFrame, FrameMetadata metadata,
            BitmapSource baseFrame)
        {
            if (baseFrame == null && IsFullFrame(metadata, fullSize))
            {
                // No previous image to combine with, and same size as the full image
                // Just return the frame as is
                return rawFrame;
            }

            DrawingVisual visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                if (baseFrame != null)
                {
                    var fullRect = new Rect(0, 0, fullSize.Width, fullSize.Height);
                    context.DrawImage(baseFrame, fullRect);
                }

                var rect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                context.DrawImage(rawFrame, rect);
            }
            var bitmap = new RenderTargetBitmap(
                fullSize.Width, fullSize.Height,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var result = new WriteableBitmap(bitmap);

            if (result.CanFreeze && !result.IsFrozen)
                result.Freeze();
            return result;
        }

        private static BitmapSource ClearArea(BitmapSource frame, FrameMetadata metadata)
        {
            DrawingVisual visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var fullRect = new Rect(0, 0, frame.PixelWidth, frame.PixelHeight);
                var clearRect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                var clip = Geometry.Combine(
                    new RectangleGeometry(fullRect),
                    new RectangleGeometry(clearRect),
                    GeometryCombineMode.Exclude,
                    null);
                context.PushClip(clip);
                context.DrawImage(frame, fullRect);
            }

            var bitmap = new RenderTargetBitmap(
                    frame.PixelWidth, frame.PixelHeight,
                    frame.DpiX, frame.DpiY,
                    PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var result = new WriteableBitmap(bitmap);

            if (result.CanFreeze && !result.IsFrozen)
                result.Freeze();
            return result;
        }

        public static KeyFramesAnimation DecodeFrames(string path)
        {
            var decoder = new GifBitmapDecoder(new Uri(path, UriKind.RelativeOrAbsolute) , BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            var fullSize = GetFullSize(decoder);
            int index = 0;
            int frameCount = decoder.Frames.Count;
            var rawFrames = decoder.Frames;

            KeyFramesAnimation keyFramesAnimation = new KeyFramesAnimation() { FrameWidth = fullSize.Width, FrameHeight = fullSize.Height };
            KeyFrame[] keyFrames = new KeyFrame[rawFrames.Count];

            double totalDuration = 0;
            double delay = 0;
            BitmapSource baseFrame = null;
            for(int i = 0; i < rawFrames.Count; i++)
            {
                var rawFrame = rawFrames[index];
                var metadata = GetFrameMetadata(rawFrame);

                var frame = MakeFrame(fullSize, rawFrame, metadata, baseFrame);
                var keyFrame = new KeyFrame(frame, delay);
                keyFrames[i] = keyFrame;
                delay = metadata.Delay;
                totalDuration += delay;

                switch (metadata.DisposalMethod)
                {
                    case FrameDisposalMethod.None:
                    case FrameDisposalMethod.DoNotDispose:
                        baseFrame = frame;
                        break;
                    case FrameDisposalMethod.RestoreBackground:
                        if (IsFullFrame(metadata, fullSize))
                        {
                            baseFrame = null;
                        }
                        else
                        {
                            baseFrame = ClearArea(frame, metadata);
                        }
                        break;
                    case FrameDisposalMethod.RestorePrevious:
                        // Reuse same base frame
                        break;
                }

                index++;
            }
            
            keyFramesAnimation.Duration = totalDuration;
            keyFramesAnimation.KeyFrames = keyFrames;

            return keyFramesAnimation;
        }

        public static Task<KeyFramesAnimation> DecodeFramesAysnc(string path)
        {
            return Task.Run<KeyFramesAnimation>(() => DecodeFrames(path));
        }
    }
}
