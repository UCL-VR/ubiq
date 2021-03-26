using System;
using System.Collections.Generic;
using System.Text;

namespace Ubiq.Networking
{
    namespace JmBucknall.Structures
    {
        public interface IPriorityConverter<P>
        {
            int Convert(P priority);
            int PriorityCount { get; }
        }
    }
}