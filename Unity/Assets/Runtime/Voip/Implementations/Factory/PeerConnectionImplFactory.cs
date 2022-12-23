using Ubiq.Voip.Implementations;

namespace Ubiq.Voip.Factory
{
    public static class PeerConnectionImplFactory
    {
        public static IPeerConnectionImpl Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR && UNITY_2021_3_OR_NEWER
            return new Ubiq.Voip.Implementations.Web.WebPeerConnectionImpl();
#else
            return new Ubiq.Voip.Implementations.Dotnet.DotnetPeerConnectionImpl();
#endif
        }
    }
}