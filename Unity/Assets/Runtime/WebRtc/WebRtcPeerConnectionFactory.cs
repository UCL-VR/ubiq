using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pixiv.Cricket;
using Pixiv.Rtc;
using Pixiv.Webrtc;
using System;
using UnityEngine.Android;
using Ubiq.Networking;

namespace Ubiq.WebRtc
{
    public class WebRtcPeerConnectionFactory : MonoBehaviour
    {
        private DisposablePeerConnectionFactoryInterface factory;
        private WebRtcThreads threads;
        private AudioDeviceModule adm;
        private List<WebRtcPeerConnection> pcs;

        public List<ConnectionDefinition> turnServers;
        public string turnUser;
        public string turnPassword;

        public AudioDeviceHelper playoutHelper;
        public AudioDeviceHelper recordingHelper;

        public bool ready
        {
            get
            {
                return factory != null;
            }
        }

        private void Awake()
        {
            // First we run through some Android specifics. Android requires explicit permission requests before the audio context can be created.
            // Beware that failing these will not necessarily report permission errors, but rather device creation errors.
            // Additionally, the library prefers the audio in communication mode, so we switch that too.

            if(Application.platform == RuntimePlatform.Android)
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                {
                    Permission.RequestUserPermission(Permission.Microphone);
                }

                try
                {
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio");
                    int mode1 = audioManager.Call<Int32>("getMode");
                    //audioManager.Call("setMode", 3); // 3 is Communication Mode
                    int mode2 = audioManager.Call<Int32>("getMode");

                    Debug.Log($"Android Audio Mode changed from {mode1} to {mode2}");
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }

            threads = WebRtcThreads.Acquire();

            // the default audio device module must be created on the worker thread
            adm = AudioDeviceModuleFactory.CreateDefault(threads.threads[1]); // by convention 1 is the worker (see call below)

            // adm is now initialised

            factory = PeerConnectionFactory.Create(
                threads.threads[0],
                threads.threads[1],
                threads.threads[2], // This is the main signalling thread
                adm,
                AudioEncoderFactory.CreateBuiltin(),
                AudioDecoderFactory.CreateBuiltin(),
                VideoEncoderFactory.CreateBuiltin(),
                VideoDecoderFactory.CreateBuiltin(),
                null,
                null);

            playoutHelper = new AudioDeviceHelper(PlayoutDevices, ChangePlayoutDevice);
            recordingHelper = new AudioDeviceHelper(RecordingDevices, ChangeRecordingDevice);

            pcs = new List<WebRtcPeerConnection>();
        }

        private void ChangePlayoutDevice(ushort device)
        {
            adm.StopPlayout();
            adm.StopRecording(); // even if we only change the playout device, we must restart the recording because the buffers must be in sync for echo cancellation!
            adm.SetPlayoutDevice(device);
            adm.InitPlayout();
            adm.InitRecording();
            adm.StartPlayout();
            adm.StartRecording();
        }

        private void ChangeRecordingDevice(ushort device)
        {
            adm.StopPlayout();
            adm.StopRecording();
            adm.SetRecordingDevice(device);
            adm.InitPlayout();
            adm.InitRecording();
            adm.StartPlayout();
            adm.StartRecording();
        }

        public IEnumerable<AudioDeviceModule.AudioDeviceName> PlayoutDevices
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    yield break; // On Android, the audio device is set via the Android API only
                }
                for (int i = 0; i < adm.PlayoutDevices(); i++)
                {
                    yield return adm.GetPlayoutDeviceName((ushort)i);
                }
            }
        }

        public IEnumerable<AudioDeviceModule.AudioDeviceName> RecordingDevices
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    yield break; // On Android, the audio device is set via the Android API only
                }
                for (int i = 0; i < adm.RecordingDevices(); i++)
                {
                    yield return adm.GetRecordingDeviceName((ushort)i);
                }
            }
        }

        public DisposablePeerConnectionInterface CreatePeerConnection(PeerConnectionInterface.RtcConfiguration config, WebRtcPeerConnection observer)
        {
            var dependencies = new PeerConnectionDependencies();
            dependencies.Observer = new DisposablePeerConnectionObserver(observer);

            var pc = factory.CreatePeerConnection(config, dependencies); // keep track of the peerconnections as they must be destroyed before the threads
            pcs.Add(observer);

            return pc;
        }

        public IAudioSourceInterface CreateAudioSource()
        {
            return factory.CreateAudioSource();
        }

        public IMediaStreamTrackInterface CreateAudioTrack(string label, IAudioSourceInterface source)
        {
            return factory.CreateAudioTrack(label, source);
        }

        /// <summary>
        /// Special class to help manage selecting audio devices. The media stack doesn't know which device is chosen by default,
        /// so the options will permutate depending on what has been selected when.
        /// </summary>
        public class AudioDeviceHelper
        {
            public List<string> names;
            public int selected;
            public Action<ushort> ChangeDevice;

            public event Action OnOptionsChanged;
            public event Action OnDeviceChanged;

            public AudioDeviceHelper(IEnumerable<AudioDeviceModule.AudioDeviceName> devices, Action<ushort> changeDevice)
            {
                names = new List<string>();
                names.Add("Default");
                names.AddRange(devices.Select(d => d.name));
                selected = 0;
                this.ChangeDevice = changeDevice;
            }

            public void Select(int i)
            {
                if (i != selected)
                {
                    if (names.Contains("Default"))
                    {
                        names.Remove("Default");
                        i--;
                        if (OnOptionsChanged != null)
                        {
                            OnOptionsChanged.Invoke();
                        }
                    }
                    selected = i;
                    ChangeDevice((ushort)selected);
                    if (OnDeviceChanged != null)
                    {
                        OnDeviceChanged.Invoke();
                    }
                }
            }

            public void Select(string name)
            {
                throw new System.NotImplementedException();
            }
        }

        private IEnumerable<PeerConnectionInterface.IceServer> GetIceServers()
        {
            yield return new PeerConnectionInterface.IceServer()
            {
                Uri = "stun:stun.l.google.com:19302"
            };

            foreach (var item in turnServers)
            {
                yield return new PeerConnectionInterface.IceServer()
                {
                    Uri = "turn:" + item.send_to_ip + ":" + item.send_to_port,
                    Username = turnUser,
                    Password = turnPassword
                };
            }

        }

        internal void GetRtcConfiguration(Action<PeerConnectionInterface.RtcConfiguration> OnConfiguration)
        {
            var config = new PeerConnectionInterface.RtcConfiguration();
            config.SdpSemantics = SdpSemantics.UnifiedPlan;
            config.Servers = GetIceServers().ToArray();
            OnConfiguration.Invoke(config);
        }

        private void OnDestroy()
        {
            foreach (var item in pcs)
            {
                item.Dispose();
            }
            factory.Dispose();
            factory = null;
            threads.Release();
        }
    }
}