using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Ubiq.XR
{
    public class XRControl : MonoBehaviour
    {
        public bool enableXROnStartup;

        void Start()
        {
#if UNITY_ANDROID
        enableXROnStartup = true;
#endif
            if (enableXROnStartup)
            {
                InitialiseXR();
            }
        }

        public void InitialiseXR()
        {
            StartCoroutine(StartSubsystems());
        }

        IEnumerator StartSubsystems()
        {
            if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                yield break;
            }
            else
            {
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
            }
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }


        // Update is called once per frame
        void Update()
        {

        }
    }
}