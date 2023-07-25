using SIPSorcery.Media;

namespace Ubiq.Voip.Implementations.Dotnet
{
    public class G722AudioDecoder
    {
        private const int G722_BIT_RATE = 64000; // G722 sampling rate is 16KHz with bits per sample of 16.

        private G722Codec g722Codec;
        private G722CodecState g722CodecState;

        public short[] Decode(byte[] encoded)
        {
            if (g722Codec == null)
            {
                g722Codec = new G722Codec();
                g722CodecState = new G722CodecState(G722_BIT_RATE, G722Flags.None);
            }

            var requiredDecodedSampleCount = encoded.Length * 2;
            var pcm = new short[requiredDecodedSampleCount];

            g722Codec.Decode(g722CodecState,pcm,encoded,encoded.Length);
            return pcm;
        }
    }
}