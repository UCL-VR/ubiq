using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples.Social
{
    public sealed class TextPageIndicator : PageIndicator
    {
        public Text pageText;
        public Text pageCountText;

        public override int capacity { get => int.MaxValue; protected set {} }
        public override int page { get; protected set; }
        public override int pageCount { get; protected set; }

        public override void SetPageIndication (int page, int pageCount)
        {
            if (this.pageCount != pageCount)
            {
                pageCountText.text = pageCount.ToString();
                this.pageCount = pageCount;
            }

            if (this.page != page)
            {
                pageText.text = (page+1).ToString();
                this.page = page;
            }
        }
    }
}
