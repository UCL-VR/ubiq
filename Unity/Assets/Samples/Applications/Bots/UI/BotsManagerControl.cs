using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples.Bots.UI
{
    public class BotsManagerControl : MonoBehaviour
    {
        public Text NumberOfBotsText;
        public Text InstanceIdText;
        public Text FpsText;
        public Button AddBotsButton;
        public InputField AddBotsInputField;

        [NonSerialized]
        public BotManagerProxy proxy;

        private float lastRefreshTime;

        public void SetProxy(BotManagerProxy proxy)
        {
            this.proxy = proxy;
            InstanceIdText.text = proxy.Guid;
        }

        private void Awake()
        {
            AddBotsButton.onClick.AddListener(() =>
            {
                try
                {
                    proxy.AddBots(int.Parse(AddBotsInputField.text));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });

            lastRefreshTime = Time.time;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(proxy != null)
            {
                NumberOfBotsText.text = proxy.NumBots.ToString();
                FpsText.text = proxy.Fps.ToString();
            }            
        }
    }
}