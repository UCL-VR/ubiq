namespace Ubiq.Voip.Implementations
{
    public static class PeerConnectionImplFactory
    {
        public static IPeerConnectionImpl Create()
        {
#if UNITY_WEBRTC || UNITY_WEBRTC_NO_VULKAN_HOOK
            return new Ubiq.Voip.Implementations.Unity.PeerConnectionImpl();
#else
            return new Ubiq.Voip.Implementations.NullPeerConnectionImpl();
#endif
        }
    }
}
