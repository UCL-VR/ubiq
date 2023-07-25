namespace Ubiq.Voip.Implementations
{
    public static class PeerConnectionImplFactory
    {
        public static IPeerConnectionImpl Create()
        {
            return new Ubiq.Voip.Implementations.Web.PeerConnectionImpl();
        }
    }
}
