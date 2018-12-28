using System.Windows.Media;

namespace Animation
{
    public class KeyFrame
    {
        public ImageSource Frame;
        // relative time in miliseconds
        public double Delay;

        public KeyFrame(ImageSource frame, double delay)
        {
            Frame = frame;
            Delay = delay;
        }

        public KeyFrame Clone()
        {
            return new KeyFrame(Frame, Delay);
        }
    }

    public class KeyFramesAnimation
    {
        public KeyFrame[] KeyFrames { get; set; }
        public double Duration { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }

        public KeyFramesAnimation Clone()
        {
            KeyFramesAnimation absoluteTimmingKeyFrame = new KeyFramesAnimation();

            KeyFrame[] frames = new KeyFrame[KeyFrames.Length];
            for(int i = 0; i < frames.Length; i++)
            {
                frames[i] = KeyFrames[i].Clone();
            }

            absoluteTimmingKeyFrame.FrameWidth = FrameWidth;
            absoluteTimmingKeyFrame.FrameHeight = FrameHeight;
            absoluteTimmingKeyFrame.Duration = Duration;
            absoluteTimmingKeyFrame.KeyFrames = frames;
            return absoluteTimmingKeyFrame;
        }

        public void MakeAbsoluteTimingKeyFrame()
        {
            double absoluteTime = 0;
            for(int i = 0; i < KeyFrames.Length; i++)
            {
                absoluteTime += KeyFrames[i].Delay;
                KeyFrames[i].Delay = absoluteTime;
            }
        }
    }
}
