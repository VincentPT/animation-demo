using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animation
{
    public abstract class FrameController
    {
        public enum ControlEvent
        {
            Start,
            Play,
            Pause,
            Stop
        }

        protected ControlEvent currentEvent = ControlEvent.Stop;
        public ControlEvent RunningState { get => currentEvent; }
        public abstract double CurrentTime { get; }

        public abstract void AddAnimation(FrameAnimation frameAnimation, FrameHandler frameHandler);
        public abstract void RemoveAnimation(FrameAnimation frameAnimation);        
        public abstract void Start();
        public abstract void Play();
        public abstract void Pause();
        public abstract void Stop();
        public abstract void Clear();
    }
}
