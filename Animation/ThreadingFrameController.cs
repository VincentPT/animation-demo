using System.Collections.Generic;
using System.Threading;
using System;

namespace Animation
{
    /// <summary>
    /// control how to play animations
    /// currently, support only for animations that loop forever
    /// </summary>
    public class ThreadingFrameController : FrameController
    {
        internal sealed class AnimationControlNode
        {
            public FrameAnimation FrameAnimation;
            public FrameHandler FrameHandler;
            public double StartTime;
        }

        sealed class Frame
        {
            public AnimationControlNode AnimationNode;
            public double Delay;
            public bool Played;
        }
        private DateTime startTime = DateTime.MinValue;
        public override double CurrentTime {
            get => (DateTime.Now - startTime).TotalMilliseconds;
        }

        sealed class UIDispatcher : Dispatcher
        {
            System.Windows.Threading.Dispatcher dispatcher;
            public UIDispatcher()
            {
                dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            }
            public void Invoke(Delegate method, TimeSpan timeout, params object[] args)
            {
                dispatcher.Invoke(method, timeout, args);
            }
        }

        public Dispatcher UserDispatcher { get; set; }

        LinkedList<Frame> frames = new LinkedList<Frame>();
        List<AnimationControlNode> animationNodes = new List<AnimationControlNode>();
        object syncObject = new object();
        Dispatcher dispatcher;
        
        Thread playThread;
        AutoResetEvent addAnimationEvent = new AutoResetEvent(false);
        AutoResetEvent hasControlEvent = new AutoResetEvent(false);

        private static void OnFramesChange(LinkedList<Frame> frames)
        {
            foreach(var frame in frames)
            {
                // check if a frame on the right time to play
                if (frame.Delay <= 0)
                {
                    var animationNode = frame.AnimationNode;
                    var frameHandler = animationNode.FrameHandler;
                    var animation = animationNode.FrameAnimation;
                    var frameStream = animation.Stream;

                    // invoke handler to play the frame
                    frameHandler.OnIncommingFrame(frameStream);

                    frame.Played = true;
                }
            }
        }

        private delegate void FrameChangeDelegate(LinkedList<Frame> frames);
        private static FrameChangeDelegate OnFrameChangesDelagate = OnFramesChange;
        
        private void RunAnimations()
        {
            WaitHandle[] waitHandles = { hasControlEvent, addAnimationEvent };
            while (currentEvent != ControlEvent.Stop && frames.Count == 0)
            {
                WaitHandle.WaitAny(waitHandles);
            }

            if (currentEvent == ControlEvent.Stop) return;

            double delay = 0;
            TimeSpan invokeTimeout = TimeSpan.FromMilliseconds(50);
            var frameDelay = TimeSpan.FromMilliseconds(delay);           

            startTime = DateTime.Now;
            while (currentEvent != ControlEvent.Stop)
            {
                while (currentEvent == ControlEvent.Pause)
                {
                    hasControlEvent.WaitOne();
                }

                var t1 = DateTime.Now;

                lock (syncObject) {
                    dispatcher.Invoke(OnFrameChangesDelagate, invokeTimeout, frames);

                    // remove frames that animation was end after frames was played
                    // and update the frame that its was played
                    for (var node = frames.First; node != null;)
                    {
                        var frame = node.Value;
                        var animationNode = frame.AnimationNode;
                        var animation = animationNode.FrameAnimation;

                        // check if a frame is played
                        if (frame.Played)
                        {
                            if (animation.CurrentFrameIndex == animation.FrameCount - 1)
                            {
                                frame.Delay = animation.CurrentFrame.Delay;
                                animation.NextFrame();
                            }
                            else
                            {
                                animation.NextFrame();
                                frame.Delay = animation.CurrentFrame.Delay;
                            }
                            frame.Played = false;
                        }

                        // remove animation that was out of its life time
                        if (animation.FrameCount <= animation.CurrentFrameIndex && animation.Loop == false)
                        {
                            var tempNode = node;
                            node = node.Next;
                            frames.Remove(tempNode);
                        }
                        else
                        {
                            node = node.Next;
                        }
                    }                    

                    // find min delay time to delay in the next turn
                    delay = Double.MaxValue;
                    foreach (var frame in frames)
                    {                        
                        if (delay > frame.Delay)
                        {
                            delay = frame.Delay;
                        }
                    }
                    if (delay < Double.MaxValue - 1.0f)
                    {
                        frameDelay = TimeSpan.FromMilliseconds(delay);
                    }
                    else
                    {
                        frameDelay = TimeSpan.FromMilliseconds(0);
                    }

                    // after the delay time was found
                    // update the delay time of all frame
                    // because this engine will using the delay time to sleep
                    // then after wake up the all current frame must be reduce its delay time by the sleeping time
                    foreach (var frame in frames)
                    {                        
                        frame.Delay -= delay;
                    }
                }

                var t2 = DateTime.Now;
                var processDuration = t2 - t1;
                var frameDelayLeft = frameDelay - processDuration;

                if(frameDelayLeft > TimeSpan.Zero)
                {
                    int eventIndex = WaitHandle.WaitAny(waitHandles, frameDelayLeft);

                    // check if while the loop waiting for the next frame, a animation is added...
                    if(eventIndex < waitHandles.Length && waitHandles[eventIndex] == addAnimationEvent)
                    {
                        lock (syncObject)
                        {
                            // then we need to play the new animation immediately
                            // but before do that, we need to reduce delay time for the waiting frames
                            var t3 = DateTime.Now;
                            var sleptTime = (t3 - t2).TotalMilliseconds;
                            foreach (var frame in frames)
                            {
                                frame.Delay -= sleptTime;
                            }
                        }
                    }
                }
            }

            currentEvent = ControlEvent.Stop;
        }

        #region animation controlling methods

        /// <summary>        
        /// Add an animation into the controller
        /// </summary>
        /// <param name="frameAnimation">animation object</param>
        /// <param name="frameHandler">frame change handler</param>
        /// <param name="target">tag object</param>
        public override void AddAnimation(FrameAnimation frameAnimation, FrameHandler frameHandler)
        {
            lock (syncObject)
            {
                var animationNode = new AnimationControlNode { FrameAnimation = frameAnimation, FrameHandler = frameHandler, StartTime = CurrentTime };
                animationNodes.Add(animationNode);

                if (frameAnimation.FrameCount == 0) return;
                frames.AddLast(new Frame { AnimationNode = animationNode, Delay = 0, Played = false });
                addAnimationEvent.Set();
            }
        }

        public override void RemoveAnimation(FrameAnimation frameAnimation)
        {
            lock (syncObject)
            {
                // remove animation node that has same animation
                animationNodes.RemoveAll(x => x.FrameAnimation == frameAnimation);

                // remove animation from current frame
                for (var node = frames.First; node != null;)
                {
                    var frame = node.Value;
                    var animationNode = frame.AnimationNode;
                    var animation = animationNode.FrameAnimation;

                    if (animation == frameAnimation)
                    {
                        var tempNode = node;
                        node = node.Next;
                        frames.Remove(tempNode);
                    }
                    else
                    {
                        node = node.Next;
                    }
                }
            }
        }

        public override void Start()
        {
            if (playThread != null)
            {
                return;
            }
            currentEvent = ControlEvent.Start;            
            dispatcher = UserDispatcher == null ? new UIDispatcher() : UserDispatcher;
            playThread = new Thread(new ThreadStart(RunAnimations));
            playThread.Name = "Animation worker thread";
            currentEvent = ControlEvent.Pause;
            playThread.Start();
        }

        public override void Play()
        {
            if (currentEvent == ControlEvent.Stop)
            {
                throw new System.Exception("Animation is not start");
            }
            if (currentEvent == ControlEvent.Pause || currentEvent == ControlEvent.Start)
            {
                currentEvent = ControlEvent.Play;
                hasControlEvent.Set();
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
                currentEvent = ControlEvent.Pause;
                hasControlEvent.Set();
            }
        }

        public override void Stop()
        {
            if (playThread == null)
            {
                return;
            }
            currentEvent = ControlEvent.Stop;
            dispatcher = null;
            hasControlEvent.Set();
            playThread.Join();
            playThread = null;
        }

        public override void Clear()
        {
            Stop();
            frames.Clear();
            animationNodes.Clear();
        }

        #endregion
    }
}
