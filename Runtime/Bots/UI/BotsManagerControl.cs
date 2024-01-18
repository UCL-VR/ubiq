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
        public Text ProcessIdText;
        public Gradient FpsGradient;

        [NonSerialized]
        public BotManagerProxy proxy;

        private float lastRefreshTime;

        public void SetProxy(BotManagerProxy proxy)
        {
            this.proxy = proxy;
            InstanceIdText.text = proxy.Guid;
            ProcessIdText.text = proxy.Pid;
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

        void Update()
        {
            if(proxy != null)
            {
                NumberOfBotsText.text = proxy.NumBots.ToString();
                FpsText.text = proxy.Fps.ToString();
                FpsText.color = FpsGradient.Evaluate(proxy.Fps * 0.01f);
            }
        }
    }
}