using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples.Social
{
    public abstract class PageIndicator : MonoBehaviour
    {
        public abstract int capacity { get; protected set; }
        public abstract int page { get; protected set; }
        public abstract int pageCount { get; protected set; }
        public abstract void SetPageIndication (int page, int pageCount);
    }
}
