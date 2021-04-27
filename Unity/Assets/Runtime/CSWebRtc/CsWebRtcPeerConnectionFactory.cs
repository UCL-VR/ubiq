
namespace Ubiq.CsWebRtc
{
    public class CsWebRtcPeerConnectionFactory : MonoBehaviour
    {
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
                    audioManager.Call("setMode", 3); // 3 is Communication Mode
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
    }
}