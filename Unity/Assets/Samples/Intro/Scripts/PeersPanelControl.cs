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
        [Serializable]
        public class StagedIcon<T> where T : IComparable<T>
        {
            [Serializable]
            public class Stage
            {
                public Sprite sprite;
                public Color color;
                public T threshold;
            }
            public Image image;
            public List<Stage> stages;

            public void Update (T value)
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    if (value.CompareTo(stages[i].threshold) >= 0)
                    {
                        image.sprite = stages[i].sprite;
                        image.color = stages[i].color;
                    }
                }
            }
        }
        // For compatibility with Unity serialization
        [Serializable]
        public class StagedIconFloat //: StagedIcon<float> {}
        {
            [Serializable]
            public class Stage
            {
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
                        return;
                    }
                }

                // No appropriate stage identified
                image.sprite = null;
                image.color = Color.white;
            }
        }

        [Serializable]
        public class StagedIconInt //: // StagedIcon<float> {}
        {
            [Serializable]
            public class Stage
            {
                public Sprite sprite;
                public Color color;
                public int threshold;
            }
            public Image image;
            public List<Stage> stages;

            public void Update (int value)
            {
                for (int i = stages.Count-1; i >= 0; i++)
                {
                    if (value >= stages[i].threshold)
                    {
                        image.sprite = stages[i].sprite;
                        image.color = stages[i].color;
                        return;
                    }
                }

                // No appropriate stage identified
                image.sprite = null;
                image.color = Color.white;
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

            var name = peer["ubiq.samples.intro.name"];
            if (name == null)
            {
                name = "(unnamed)";
            }
            this.peerName.text = name;

            if (meText)
            {
                meText.gameObject.SetActive(isMe);
            }

            if (peerConnectionManager)
            {
                var peerConnection = peerConnectionManager.GetPeerConnection(peer.UUID);
                if (peerConnection)
                {
                    var volume = peerConnection.audioSink.lastFrameStats.volume;
                    voipVolumeIndicator.Update(volume);
                    voipConnectionIndicator.Update((int)peerConnection.peerConnectionState);
                    peerConnection.audioSink.unityAudioSource.volume = voipVolumeSlider.value;
                }
            }

            // TODO latency
        }

        public void Bind(RoomClient client, IPeer peer, bool isMe)
        {
            ClearBinding();

            this.peer = peer;
            this.isMe = isMe;
            this.peerConnectionManager = VoipPeerConnectionManager.Find(this);

            OnBind.Invoke(client,peer);
        }


    }


}