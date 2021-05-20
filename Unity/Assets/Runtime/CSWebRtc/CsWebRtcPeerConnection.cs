// // using Pixiv.Cricket;
// // using Pixiv.Webrtc;
// using System;
// using System.Collections;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using Ubiq.Logging;
// using Ubiq.Messaging;
// using UnityEngine;
// using UnityEngine.Events;

// // using SIP

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using SIPSorcery.Net;
using Ubiq.Messaging;

// Debug
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia;
using System.Net;
using System.Threading;

namespace Ubiq.CsWebRtc
{
    [NetworkComponentId(typeof(CsWebRtcPeerConnection), 78)]
    public class CsWebRtcPeerConnection : MonoBehaviour, INetworkComponent, INetworkObject {

        // private class RTCPeerConnectionHandler : IDisposable
        // {
        //     private CancellationTokenSource cts;

        //     public RTCPeerConnectionHandler () {
        //         cts = new CancellationTokenSource();
        //     }

        //     public void Dispose()
        //     {
        //         cts.Cancel();
        //     }
        // }

        // Debug - this is not the way to do this. Probably mic endpoint (src) should be shared between peers
        // Adapted from WindowsAudioEndPoint.cs
        // https://github.com/sipsorcery-org/SIPSorceryMedia.Windows/blob/master/src/WindowsAudioEndPoint.cs
        public class DummyAudioEndPoint : IAudioSource, IAudioSink
        {
            public event EncodedSampleDelegate OnAudioSourceEncodedSample;
            public event RawAudioSampleDelegate OnAudioSourceRawSample;
            public event SourceErrorDelegate OnAudioSourceError;
            public event SourceErrorDelegate OnAudioSinkError;

            // private IAudioEncoder _audioEncoder;
            // private MediaFormatManager<AudioFormat> _audioFormatManager;

            private bool _disableSink;
            private bool _disableSource;

            public delegate void LogAvailableDelegate(string log);
            public event LogAvailableDelegate OnLogAvailable;

            private System.Timers.Timer timer;

            private List<AudioFormat> dummyFormats = new List<AudioFormat>();

            private void InitPlaybackDevice()
            {
                // try
                // {
                //     _waveOutEvent?.Stop();

                //     _waveSinkFormat = new WaveFormat(
                //         audioSinkSampleRate,
                //         DEVICE_BITS_PER_SAMPLE,
                //         DEVICE_CHANNELS);

                //     // Playback device.
                //     _waveOutEvent = new WaveOutEvent();
                //     _waveOutEvent.DeviceNumber = audioOutDeviceIndex;
                //     _waveProvider = new BufferedWaveProvider(_waveSinkFormat);
                //     _waveProvider.DiscardOnBufferOverflow = true;
                //     _waveOutEvent.Init(_waveProvider);
                // }
                // catch (Exception excp)
                // {
                //     logger.LogWarning(0, excp, "WindowsAudioEndPoint failed to initialise playback device.");
                //     OnAudioSinkError?.Invoke($"WindowsAudioEndPoint failed to initialise playback device. {excp.Message}");
                // }
            }

            private void InitRecordingDevice()
            {
                // if (WaveInEvent.DeviceCount > 0)
                // {
                //     if (WaveInEvent.DeviceCount > audioInDeviceIndex)
                //     {
                //         _waveInEvent = new WaveInEvent();
                //         _waveInEvent.BufferMilliseconds = AUDIO_SAMPLE_PERIOD_MILLISECONDS;
                //         _waveInEvent.NumberOfBuffers = INPUT_BUFFERS;
                //         _waveInEvent.DeviceNumber = audioInDeviceIndex;
                //         _waveInEvent.WaveFormat = _waveSourceFormat;
                //         _waveInEvent.DataAvailable += LocalAudioSampleAvailable;
                //     }
                //     else
                //     {
                //         logger.LogWarning($"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
                //         OnAudioSourceError?.Invoke($"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
                //     }
                // }
                // else
                // {
                //     logger.LogWarning("No audio capture devices are available.");
                //     OnAudioSourceError?.Invoke("No audio capture devices are available.");
                // }
            }

            public DummyAudioEndPoint(
                bool disableSource = false,
                bool disableSink = false)
            {
                timer = new System.Timers.Timer();
                timer.Elapsed += (obj, e) =>
                {
                    var unixTime = ((DateTimeOffset)(DateTime.Now)).ToUnixTimeSeconds();
                    LocalAudioSampleAvailable(unixTime.ToString());
                };
                timer.Interval = 5000;

                // logger = SIPSorcery.LogFactory.CreateLogger<WindowsAudioEndPoint>();
                // _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
                // _audioEncoder = audioEncoder;
                dummyFormats.Add(new AudioFormat(SDPWellKnownMediaFormatsEnum.G722));

                _disableSource = disableSource;
                _disableSink = disableSink;

                if (!_disableSink)
                {
                    InitPlaybackDevice();//_audioOutDeviceIndex, DefaultAudioPlaybackRate.GetHashCode());
                }

                if (!_disableSource)
                {
                    InitRecordingDevice();
                }

                timer.Start();
            }

            public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample)
            {
                throw new NotImplementedException();
            }

            public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
            {
                OnLogAvailable?.Invoke($"{remoteEndPoint} {ssrc} {seqnum} {timestamp} {payloadID} {marker} {System.Text.Encoding.ASCII.GetString(payload)}");
                // if (_waveProvider != null && _audioEncoder != null)
                // {
                //     var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
                //     byte[] pcmBytes = pcmSample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
                //     _waveProvider?.AddSamples(pcmBytes, 0, pcmBytes.Length);
                // }
            }

            private void OnTimedEvent (object src, System.Timers.ElapsedEventArgs e) {
                LocalAudioSampleAvailable(((DateTimeOffset)(DateTime.Now)).ToUnixTimeSeconds().ToString());
            }

            // Debug - just send an ASCII string
            public void LocalAudioSampleAvailable (string msg)
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(msg);
                OnAudioSourceEncodedSample?.Invoke((uint)bytes.Length,bytes);
            }

            public bool HasEncodedAudioSubscribers()
            {
                return true;
                // throw new NotImplementedException();
            }

            public bool IsAudioSourcePaused()
            {
                return !timer.Enabled;
                // throw new NotImplementedException();
            }

            public void RestrictFormats(Func<AudioFormat, bool> filter)
            {
                // throw new NotImplementedException();
            }

            public List<AudioFormat> GetAudioSourceFormats()
            {
                return dummyFormats;
                // throw new NotImplementedException();
            }

            public void SetAudioSourceFormat(AudioFormat audioFormat)
            {
                // throw new NotImplementedException();
            }

            public Task StartAudio() { timer.Start(); return Task.CompletedTask; }
            public Task PauseAudio() { timer.Stop(); return Task.CompletedTask; }
            public Task ResumeAudio() { timer.Start(); return Task.CompletedTask; }
            public Task CloseAudio() { timer.Stop(); return Task.CompletedTask; }

            public List<AudioFormat> GetAudioSinkFormats()
            {
                return dummyFormats;
                // throw new NotImplementedException();
            }

            public void SetAudioSinkFormat(AudioFormat audioFormat)
            {
                // throw new NotImplementedException();
            }

            public Task StartAudioSink() { return Task.CompletedTask; }
            public Task PauseAudioSink() { return Task.CompletedTask; }
            public Task ResumeAudioSink() { return Task.CompletedTask; }
            public Task CloseAudioSink() { return Task.CompletedTask; }
        }

        public struct PeerConnectionState
        {
            public string Peer;
            // public string LastMessageReceived;
            // public bool HasRemote;
            // public volatile PeerConnectionInterface.SignalingState SignalingState;
            // public volatile PeerConnectionInterface.IceConnectionState ConnectionState;
            // public volatile PeerConnectionInterface.IceGatheringState IceState;
        }

        public PeerConnectionState State;

        public NetworkId Id { get; set; }

        public void MakePolite(){polite = true;}
        public void AddLocalAudioSource(){}

        // SipSorcery Peer Connection
        private RTCPeerConnection rtcPeerConnection;
        private NetworkContext context;
        private ConcurrentQueue<Action> mainThreadActions;

        private bool polite;

        private Task initTask;

        private CancellationTokenSource cts;

        // Debug
        private const string STUN_URL = "stun:stun.l.google.com:19302";

        private void Awake ()
        {
            cts = new CancellationTokenSource();
            mainThreadActions = new ConcurrentQueue<Action>();
        }

        // private async Task SetupConnection()
        // {
        //     rtcPeerConnection = await
        // }

        private void Start ()
        {
            context = NetworkScene.Register(this);
            rtcPeerConnection = CreatePeerConnection().Result;
            if (!polite)
            {
                SendOffer();
            }

            initTask = Init();
        }

        private void OnDestroy()
        {
            if (rtcPeerConnection != null)
            {
                rtcPeerConnection.Dispose();
                rtcPeerConnection = null;
            }
        }

        private async Task Init ()
        {
            rtcPeerConnection = await CreatePeerConnection();
            if (!polite)
            {
                await SendOffer();
            }
        }

        private async Task SendOffer ()
        {
            var offer = rtcPeerConnection.createOffer();
            await rtcPeerConnection.setLocalDescription(offer);
            OnMainThread(() => Send("Offer",offer.toJSON()));
        }

        private Task<RTCPeerConnection> CreatePeerConnection()
        {
            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = STUN_URL } }
            };
            var pc = new RTCPeerConnection(config);

            // debug
            // var audioSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });
            var audioEp = new DummyAudioEndPoint();

            // audioSource.OnAudioSourceEncodedSample += pc.SendAudio;
            audioEp.OnAudioSourceEncodedSample += pc.SendAudio;

            MediaStreamTrack audioTrack = new MediaStreamTrack(audioEp.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);

            pc.addTrack(audioTrack);

            pc.OnAudioFormatsNegotiated += (formats) => audioEp.SetAudioSourceFormat(formats[0]);//.First());

            pc.onconnectionstatechange += async (state) =>
            {
                OnMainThread(() => Debug.Log($"Peer connection state change to {state}."));

                if (state == RTCPeerConnectionState.connected)
                {
                    await audioEp.StartAudio();
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    await audioEp.CloseAudio();
                }
            };
            pc.onicecandidate += (iceCandidate) =>
            {
                if (pc.signalingState == RTCSignalingState.have_remote_offer ||
                    pc.signalingState == RTCSignalingState.stable)
                {
                    OnMainThread(() => Send("IceCandidate",iceCandidate.toJSON()));
                }
            };
            pc.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) =>
            {
                //logger.LogDebug($"RTP {media} pkt received, SSRC {rtpPkt.Header.SyncSource}.");
                if (media == SDPMediaTypesEnum.audio)
                {
                    audioEp.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber, rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
                }
            };

            // Diagnostics.
            pc.OnReceiveReport += (re, media, rr) => OnMainThread(
                () => Debug.Log($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}"));
            pc.OnSendReport += (media, sr) => OnMainThread(
                () => Debug.Log($"RTCP Send for {media}\n{sr.GetDebugSummary()}"));
            pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => OnMainThread(
                () => Debug.Log($"STUN {msg.Header.MessageType} received from {ep}."));
            pc.oniceconnectionstatechange += (state) => OnMainThread(
                () => Debug.Log($"ICE connection state change to {state}."));

            // Debug
            audioEp.OnLogAvailable += (msg) => OnMainThread(() => Debug.Log(msg));

            return Task.FromResult(pc);
        }

        private void OnMainThread(Action Action)
        {
            mainThreadActions.Enqueue(Action);
        }

        private void Update()
        {
            while (mainThreadActions.TryDequeue(out Action action))
            {
                action();
            }
        }

        [Serializable]
        private struct Message
        {
            public string type;
            public string args;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
        {
            // try
            // {
            //     OnRtcMessage(data.FromJson<Message>());
            // }
            // catch (Exception e)
            // {
            //     debug.Log("Exception", e.Message);
            //     return;
            // }
            var msg = data.FromJson<Message>();

            switch(msg.type)
            {
                case "Offer":
                    // var offer = JsonUtility.FromJson<RTCSessionDescriptionInit>(msg.args);
                    if (RTCSessionDescriptionInit.TryParse(msg.args,out RTCSessionDescriptionInit offer))
                    {
                        Debug.Log($"Got remote SDP, type {offer.type}");

                        var result = rtcPeerConnection.setRemoteDescription(offer);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            Debug.Log($"Failed to set remote description, {result}.");
                            rtcPeerConnection.Close("Failed to set remote description");
                        }
                        else
                        {
                            if(rtcPeerConnection.signalingState == RTCSignalingState.have_remote_offer)
                            {
                                var answerSdp = rtcPeerConnection.createAnswer();
                                rtcPeerConnection.setLocalDescription(answerSdp);

                                Debug.Log($"Sending SDP answer");
                                //logger.LogDebug(answerSdp.sdp);

                                Send("Offer", answerSdp.toJSON());
                            }
                        }
                    }
                    break;
                case "IceCandidate":
                    // var candidate = JsonUtility.FromJson<RTCIceCandidateInit>(msg.args);
                    //
                    if (RTCIceCandidateInit.TryParse(msg.args,out RTCIceCandidateInit candidate))
                    {
                        Debug.Log($"Got remote Ice Candidate, uri {candidate.candidate}");
                        rtcPeerConnection.addIceCandidate(candidate);
                    }
                    break;
            }
        }

        private void Send(string type, string args)
        {
            context.SendJson(new Message()
            {
                type = type,
                args = args
            });
        }
    }
}