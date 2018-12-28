using System;

namespace Animation
{
    public interface Dispatcher
    {
        void Invoke(Delegate method, TimeSpan timeout, params object[] args);
    }
}
