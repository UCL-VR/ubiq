using System.Collections;
using System.Collections.Generic;
using Ubiq.WebRtc;
using UnityEngine;
using UnityEngine.UI;

public class AudioTestController : MonoBehaviour
{
    public Text stateText;
    public WebRtcPeerConnection pc;
    public WebRtcPeerConnectionFactory factory;
    public Button startButton;
    public Dropdown playoutDevice;
    public Dropdown recordingDevice;

    private void Awake()
    {
        startButton.onClick.AddListener(() => { startButton.interactable = false; });
        startButton.onClick.AddListener(() => { pc.AddLocalAudioSource(); });
    }

    private void Start()
    {
        new AudioHelperBindings(factory.playoutHelper, playoutDevice);
        new AudioHelperBindings(factory.recordingHelper, recordingDevice);
    }

    public class AudioHelperBindings
    {
        WebRtcPeerConnectionFactory.AudioDeviceHelper helper;
        Dropdown dropdown;

        public AudioHelperBindings(WebRtcPeerConnectionFactory.AudioDeviceHelper helper, Dropdown dropdown)
        {
            this.helper = helper;
            this.dropdown = dropdown;

            helper.OnOptionsChanged += Helper_OnOptionsChanged;
            helper.OnDeviceChanged += Helper_OnDeviceChanged;

            Helper_OnOptionsChanged();

            dropdown.onValueChanged.AddListener(helper.Select);
        }

        private void Helper_OnDeviceChanged()
        {
            dropdown.SetValueWithoutNotify(helper.selected);
        }

        private void Helper_OnOptionsChanged()
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(helper.names);
        }
    }


    // Update is called once per frame
    void Update()
    {
        stateText.text = "Connection State: " + pc.State.ConnectionState.ToString() + " " + pc.State.SignalingState.ToString();
    }
}

