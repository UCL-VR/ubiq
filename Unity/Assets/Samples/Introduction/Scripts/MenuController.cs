using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class MenuController : MonoBehaviour
    {
        [Serializable]
        public class Page
        {
            public GameObject page;
            public Button button;
        }

        public List<Page> pages;

        // Start is called before the first frame update
        void Start()
        {
            foreach (var item in pages)
            {
                item.button.onClick.AddListener(() =>
                {
                    SwapPage(item.page);
                });

            }

            if(pages.Count > 0)
            {
                SwapPage(pages[0].page);
            }
        }

        private void SwapPage(GameObject page)
        {
            foreach (var item in pages)
            {
                if(item.page == page)
                {
                    item.page.SetActive(true);
                }
                else
                {
                    item.page.SetActive(false);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}