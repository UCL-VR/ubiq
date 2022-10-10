using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Messaging;

namespace Ubiq.Samples.Social
{
    [RequireComponent(typeof(PagePanel))]
    public class AvatarPagePanelController : MonoBehaviour
    {
        public AvatarTextureCatalogue catalogue;
        public int maxPages = 4;
        public int entriesPerPage = 3;
        public GameObject controlTemplate;
        public Transform controlsRoot;

        private List<AvatarPagePanelControl> controls = new List<AvatarPagePanelControl>();

        private PagePanel pagePanel;
        private NetworkScene networkScene;

        private void Awake ()
        {
            pagePanel = GetComponent<PagePanel>();
        }

        private void OnEnable()
        {
            UpdateOptions();
            pagePanel.onPageChanged.AddListener(PagePanel_OnPageChanged);
        }

        private void OnDisable()
        {
            if (pagePanel)
            {
                pagePanel.onPageChanged.RemoveListener(PagePanel_OnPageChanged);
            }
        }

        private AvatarPagePanelControl InstantiateControl () {
            var go = GameObject.Instantiate(controlTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<AvatarPagePanelControl>();
        }

        private void PagePanel_OnPageChanged(int page, int pageCount)
        {
            UpdateOptions();
        }

        private void UpdateOptions()
        {
            var optionCount = Mathf.Min(entriesPerPage*maxPages,catalogue.Count);
            pagePanel.SetPageCount(optionCount/entriesPerPage);

            int controlI = 0;
            for (int optionI = 0; optionI < optionCount; controlI++, optionI++)
            {
                if (controls.Count <= controlI)
                {
                    controls.Add(InstantiateControl());
                }

                controls[controlI].Bind(SetTexture,catalogue.Get(optionI));
            }

            while (controls.Count > controlI) {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }

            var startOptionI = pagePanel.page * entriesPerPage;
            var endOptionI = pagePanel.page * entriesPerPage + entriesPerPage-1;

            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].gameObject.SetActive(i >= startOptionI && i <= endOptionI);
            }
        }

        private void SetTexture(Texture2D texture)
        {
            if (!networkScene)
            {
                networkScene = NetworkScene.Find(this);
                if (!networkScene)
                {
                    return;
                }
            }

            var avatar = networkScene.GetComponentInChildren<AvatarManager>().LocalAvatar;
            var texturedAvatar = avatar.GetComponent<TexturedAvatar>();
            if (texturedAvatar)
            {
                texturedAvatar.SetTexture(texture);
            }
        }
    }
}
