using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Ubiq.Samples.Social
{
    public class PagePanel : MonoBehaviour
    {
        public List<PageIndicator> pageIndicators;
        public Button prevPageButton;
        public Button nextPageButton;
        public GameObject buttonsRoot;

        public bool buttonsWrap = true;

        public int page { get; private set; }
        public int pageCount { get; private set; }

        [System.Serializable]
        public class PageChangedEvent : UnityEvent<int,int> { };
        public PageChangedEvent onPageChanged;

        private PageIndicator indicator;

        private void Start()
        {
            SetPageCount(pageCount,force:true);
            SetPage(page,force:true);
        }

        private void SetPageCount(int pageCount, bool force)
        {
            if (!force && this.pageCount == pageCount)
            {
                return;
            }

            // Enable the first indicator with enough capacity
            // If only one page, hide indicator
            var selected = pageCount <= 1;
            for (int i = 0; i < pageIndicators.Count; i++)
            {
                if (!selected && pageIndicators[i].capacity >= pageCount)
                {
                    selected = true;
                    pageIndicators[i].gameObject.SetActive(true);
                    indicator = pageIndicators[i];
                }
                else
                {
                    pageIndicators[i].gameObject.SetActive(false);
                }
            }

            // If only one page, hide buttons
            buttonsRoot.SetActive(pageCount > 1);

            this.pageCount = pageCount;
            SetPage(page,force:true);
        }

        private void SetPage(int page, bool force)
        {
            if (pageCount == 0)
            {
                this.page = -1;
                return;
            }

            page = page % pageCount;
            if (page < 0)
            {
                page += pageCount;
            }

            prevPageButton.interactable = buttonsWrap || page != 0;
            nextPageButton.interactable = buttonsWrap || page != pageCount-1;


            if (this.page != page)
            {
                this.page = page;
                onPageChanged.Invoke(page,pageCount);
            }

            if (indicator)
            {
                indicator.SetPageIndication(page,pageCount);
            }
        }

        public void SetPageCount(int pageCount)
        {
            SetPageCount(pageCount,force:false);
        }

        public void SetPage(int page)
        {
            SetPage(page,force:false);
        }

        public void NextPage()
        {
            SetPage(page+1);
        }

        public void PrevPage()
        {
            SetPage(page-1);
        }
    }
}
