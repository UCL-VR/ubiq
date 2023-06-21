using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Voip;

// This sample uses Unity's WebRtc solution, which is for native builds only.
// To use video in the browser, use the browser's native stack.
#if !UNITY_WEBGL
    using Unity.WebRTC;
#endif

public class StreamingSampleCamera : MonoBehaviour
{
    public Camera Camera;

    private VoipPeerConnectionManager manager;
#if !UNITY_WEBGL
    private void Awake()
    {
        manager = GetComponent<VoipPeerConnectionManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        manager.OnPeerConnection.AddListener(OnPeerConnection);
    }

    private void OnPeerConnection(VoipPeerConnection component)
    {
        var track = Camera.CaptureStreamTrack(1280, 720);
        var pc = (component.Implementation as Ubiq.Voip.Implementations.Unity.PeerConnectionImpl).PeerConnection;
        pc.AddTrack(track);
        StartCoroutine(WebRTC.Update());
    }
#else
    private void Awake()
    {
        Debug.LogError("The current target is WebGL - this sample will not start in WebGL");
    }
#endif

}
