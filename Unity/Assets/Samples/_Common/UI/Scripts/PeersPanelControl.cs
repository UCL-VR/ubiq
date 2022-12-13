using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using Ubiq.Avatars;
using Ubiq.Voip;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Ubiq.Samples
{
    public class PeersPanelControl : MonoBehaviour
    {
        // Not generic for compatibility with Unity serialization
        [Serializable]
        public class StagedIconFloat
        {
            [Serializable]
            public class Stage
            {
                public bool enabled;
                public Sprite sprite;
                public Color color;
                public float threshold;
            }
            public Image image;
            public List<Stage> stages;

            public void Update (float value)
            {
                for (int i = stages.Count-1; i >= 0; i++)
                {
                    if (value >= stages[i].threshold)
                    {
                        image.sprite = stages[i].sprite;
                        image.color = stages[i].color;
                        image.enabled = stages[i].enabled;
                        return;
                    }
                }

                // No appropriate stage identified
                image.sprite = null;
                image.color = Color.white;
                image.enabled = false;
            }
        }

        // Not generic for compatibility with Unity serialization
        [Serializable]
        public class StagedIconInt
        {
            [Serializable]
            public class Stage
            {
                public bool enabled;
                public Sprite sprite;
                public Color color;
                public int threshold;
            }
            public Image image;
            public List<Stage> stages;

            public void Update (int value)
            {
                for (int i = stages.Count-1; i >= 0; i--)
                {
                    if (value >= stages[i].threshold)
                    {
                        image.sprite = stages[i].sprite;
                        image.color = stages[i].color;
                        image.enabled = stages[i].enabled;
                        return;
                    }
                }

                // No appropriate stage identified
                image.sprite = null;
                image.color = Color.white;
                image.enabled = false;
            }
        }

        public Text meText;
        public Text peerName;
        public StagedIconFloat voipVolumeIndicator;
        public StagedIconFloat latencyIndicator;
        public StagedIconInt voipConnectionIndicator;
        public Slider voipVolumeSlider;

        [System.Serializable]
        public class BindEvent : UnityEvent<RoomClient, IPeer> { };
        public BindEvent OnBind;

        private VoipPeerConnectionManager peerConnectionManager;
        // private VoipPeerConnection
        private IPeer peer;
        private bool isMe;
        private RoomClient roomClient;

        private void ClearBinding ()
        {
            peer = null;
            roomClient = null;
        }

        private void OnDisable ()
        {
            ClearBinding();
        }

        private void Update ()
        {
            if (!roomClient)
            {
                return;
            }

            peerName.text = peer["ubiq.samples.social.name"] ?? "(unnamed)";

            if (meText)
            {
                meText.gameObject.SetActive(isMe);
            }

            // Hide volume slider for local user
            voipVolumeSlider.gameObject.SetActive(!isMe);

            if (peerConnectionManager)
            {
                var peerConnection = peerConnectionManager.GetPeerConnection(peer.uuid);
                if (peerConnection)
                {
                    // var audioSink = peerConnection.audioSink;

                    var stats  = peerConnection.GetLastFramePlaybackStats();
                    var volume = stats.volume / stats.samples;
                    voipVolumeIndicator.Update(volume);
                    voipConnectionIndicator.Update((int)peerConnection.peerConnectionState);

                    // if (!isMe && audioSink is IOutputVolume)
                    // {
                    //     (audioSink as IOutputVolume).Volume = voipVolumeSlider.value;
                    // }
                    // else
                    // {
                    //     voipVolumeSlider.value = 1.0f;
                    // }
                }
            }
            else
            {
                // No peer connection manager - we haven't connected yet
                var state = VoipPeerConnection.IceConnectionState.disconnected;
                voipConnectionIndicator.Update((int)state);
            }

            // TODO latency
        }

        public void Bind(RoomClient client, IPeer peer, bool isMe)
        {
            ClearBinding();

            this.roomClient = client;
            this.peer = peer;
            this.isMe = isMe;
            this.peerConnectionManager = VoipPeerConnectionManager.Find(this);

            OnBind.Invoke(client,peer);
        }


    }


}