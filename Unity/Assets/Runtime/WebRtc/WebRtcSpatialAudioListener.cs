using UnityEngine;

/// <summary>
/// Implements spatialised audio for WebRtc native driven audio devices by modulating the volume according to the positions
/// relative to this listener
/// </summary>
public class WebRtcSpatialAudioListener : MonoBehaviour
{
    public static WebRtcSpatialAudioListener Active;

    private void Awake()
    {
        Active = this;
    }

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        Active = null;
    }
}
