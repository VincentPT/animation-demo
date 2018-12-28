using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Animation
{
    public interface FrameHandler
    {
        void OnIncommingFrame(FrameStream frameStream);
    }
}
