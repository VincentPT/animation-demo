using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Animation
{
    public abstract class FrameStream
    {
        public string Name { get; set; }
        public abstract ImageSource CurrentFrame { get;}
    }
}
