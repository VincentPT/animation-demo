using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animation
{
    public class TimingFrameController : FrameController
    {
        internal sealed class AnimationControlNode
        {
            public KeyFramesAnimation Frames;
            public FrameHandler FrameHandler;
            public double StartTime;
        }

        Dictionary<FrameAnimation, AnimationControlNode> animationNodes = new Dictionary<FrameAnimation, AnimationControlNode>();
        private DateTime startTime = DateTime.MinValue;
        private DateTime stopTime = DateTime.MinValue;
        private DateTime pausingTime = DateTime.MinValue;
        private TimeSpan pausingDuration = TimeSpan.Zero;

        public override double CurrentTime
        {
            get
            {
                if(currentEvent == ControlEvent.Stop)
                {
                    return (stopTime - startTime - pausingDuration).TotalMilliseconds;
                }
                else if(currentEvent == ControlEvent.Play)
                {
                    return (DateTime.Now - startTime - pausingDuration).TotalMilliseconds;
                }
                else if (currentEvent == ControlEvent.Play)
                {
                    return (pausingTime - startTime - pausingDuration).TotalMilliseconds;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// return index of first frame that has delay time greater than duration
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="dutation"></param>
        /// <returns></returns>
        private static int UpperBound(KeyFrame[] frames, int first, int last, double dutation)
        {                      
            while (first < last)
            {
                var it = (first + last) >> 1;
                var time = frames[it].Delay;
                if(dutation >= time)
                {
                    first = it + 1;
                }
                else
                {
                    last = it - 1;
                }
            }

            return first;
        }

        private static int FindKeyFrame(KeyFrame[] frames, double duration)
        {
            return UpperBound(frames, 0, frames.Length, duration) - 1;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if(currentEvent == ControlEvent.Pause)
            {
                return;
            }           

            var t = CurrentTime;
            if (currentEvent == ControlEvent.Play)
            {
                foreach(var entry in animationNodes)
                {
                    var animationNode = entry.Value;
                    var originAnimationObject = entry.Key;
                    var duration = t - animationNode.StartTime;
                    var animationLength = originAnimationObject.Duration;
                    if (duration < animationLength || originAnimationObject.Loop)
                    {
                        int time = (int)(duration / animationLength);
                        duration -= animationLength * time;

                        var keyFrameIdx = FindKeyFrame(animationNode.Frames.KeyFrames, duration);
                        if(keyFrameIdx >= 0)
                        {
                            if (keyFrameIdx != originAnimationObject.CurrentFrameIndex)
                            {
                                originAnimationObject.CurrentFrameIndex = keyFrameIdx;
                                animationNode.FrameHandler.OnIncommingFrame(originAnimationObject.Stream);
                            }
                        }
                    }
                }
            }
        }

        #region animation controlling methods

        public override void AddAnimation(FrameAnimation frameAnimation, FrameHandler frameHandler)
        {
            var absoluteFrame = frameAnimation.AnimationSource.Clone();
            absoluteFrame.MakeAbsoluteTimingKeyFrame();

            var animationNode = new AnimationControlNode { Frames = absoluteFrame, FrameHandler = frameHandler, StartTime = CurrentTime };
            animationNodes[frameAnimation] = animationNode;
        }

        public override void Clear()
        {
            animationNodes.Clear();
        }

        public override void Play()
        {
            if(currentEvent == ControlEvent.Stop)
            {
                throw new System.Exception("Animation is not start");
            }
            if (currentEvent == ControlEvent.Pause || currentEvent == ControlEvent.Start)
            {
                currentEvent = ControlEvent.Play;
                pausingDuration += DateTime.Now - pausingTime;
            }
        }

        public override void Pause()
        {
            if (currentEvent == ControlEvent.Stop)
            {
                throw new System.Exception("Animation is not start");
            }
            if (currentEvent == ControlEvent.Play)
            {
                DoPause();
            }
        }

        public override void RemoveAnimation(FrameAnimation frameAnimation)
        {
            animationNodes.Remove(frameAnimation);
        }

        public override void Start()
        {
            System.Windows.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
            startTime = DateTime.Now;            
            pausingDuration = TimeSpan.Zero;
            stopTime = DateTime.MinValue;

            DoPause();
        }

        public override void Stop()
        {
            System.Windows.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
            stopTime = DateTime.Now;
            currentEvent = ControlEvent.Stop;
        }

        private void DoPause()
        {
            currentEvent = ControlEvent.Pause;
            pausingTime = DateTime.Now;
        }

        #endregion
    }
}
