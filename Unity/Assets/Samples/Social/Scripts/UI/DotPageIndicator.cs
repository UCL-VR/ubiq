using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples.Social
{
    public sealed class DotPageIndicator : PageIndicator
    {
        public HorizontalLayoutGroup layoutGroup;
        public List<Image> dots;
        public Color currentDotColor;
        public Color dotColor;

        public override int capacity { get => dots != null ? dots.Count : 0; protected set {} }
        public override int page { get; protected set; }
        public override int pageCount { get; protected set; }

        public override void SetPageIndication (int page, int pageCount)
        {
            if (pageCount > capacity)
            {
                Debug.LogWarning("Cannot indicate more pages than we have dots");
                return;
            }

            if (page < 0 || page >= pageCount)
            {
                Debug.LogWarning("No page at that index");
                return;
            }

            if (this.pageCount != pageCount)
            {
                for (int i = 0; i < dots.Count; i++)
                {
                    dots[i].gameObject.SetActive(i < pageCount);
                }

                // Todo: Fix spacing for our number of dots
                // var layoutTransform = layoutGroup.GetComponent<RectTransform>();
                // layoutTransform.ForceUpdateRectTransforms();
                // var rect = layoutTransform.rect;
                // var dotWidth = dots[0].GetComponent<RectTransform>().rect.width;
                // layoutGroup.spacing = (rect.width - pageCount * dotWidth)/(2 * (pageCount-1));

                this.pageCount = pageCount;
            }

            if (this.page != page)
            {
                for (int i = 0; i < dots.Count; i++)
                {
                    dots[i].color = dotColor;
                }
                dots[page].color = currentDotColor;

                this.page = page;
            }
        }
    }
}
