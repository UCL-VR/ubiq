using Ubiq.Voip.Implementations;

namespace Ubiq.Voip.Factory
{
    public static class PeerConnectionImplFactory
    {
        public static IPeerConnectionImpl Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new Ubiq.Voip.Implementations.WebGL.WebPeerConnectionImpl();
#else
            return new Ubiq.Voip.Implementations.Dotnet.DotnetPeerConnectionImpl();
#endif
        }
    }
}