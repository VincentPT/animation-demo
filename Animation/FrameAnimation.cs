using System.Windows.Media;

namespace Animation
{
    public class FrameAnimation
    {
        sealed class AnimationStream : FrameStream
        {
            public ImageSource currentFrame;
            public override ImageSource CurrentFrame => currentFrame;
        }

        private AnimationStream animationStream = new AnimationStream();
        public FrameStream Stream { get => animationStream; }

        public KeyFrame currentFrame;
        public KeyFrame CurrentFrame { get => currentFrame; }

        public bool Loop { get; set; }
        public double Duration { get => animationSource == null ? 0 : animationSource.Duration; }
        public int CurrentFrameIndex
        {
            get => currentFrameIndex;
            set
            {
                var index = value;
                if (index < 0 || index >= KeyFrames.Length)
                {
                    throw new System.IndexOutOfRangeException("frame index " + index + " is out of range [0, " + KeyFrames.Length + "]");
                }
                currentFrameIndex = index;
                UpdateFrame();
            }
        }

        private int currentFrameIndex = 0;
        private KeyFramesAnimation animationSource;
        public KeyFramesAnimation AnimationSource
        {
            get => animationSource;
            protected set
            {
                animationSource = value;
                currentFrameIndex = 0;
                KeyFrames = animationSource == null ? null : animationSource.KeyFrames;
            }
        }
        private KeyFrame[] KeyFrames;

        public int FrameCount { get => animationSource == null ? 0 : animationSource.KeyFrames.Length; }

        public FrameAnimation(KeyFramesAnimation source)
        {
            AnimationSource = source;
            Loop = false;
        }

        private void UpdateFrame()
        {
            currentFrame = KeyFrames[currentFrameIndex];
            animationStream.currentFrame = currentFrame.Frame;
        }

        public void NextFrame()
        {
            currentFrameIndex++;
            if (currentFrameIndex >= KeyFrames.Length)
            {
                if (Loop == false) return;
                currentFrameIndex = 0;
            }            
            UpdateFrame();
        }

        public void PrevFrame()
        {
            currentFrameIndex--;
            if (currentFrameIndex <= 0)
            {
                if (Loop == false) return;
                currentFrameIndex = FrameCount - 1;
            }
            
            UpdateFrame();
        }
    }
}
