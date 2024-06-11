using SIPSorcery.Media;

namespace Ubiq.Voip.Implementations.Dotnet
{
    public class G722AudioEncoder
    {
        private const int G722_BIT_RATE = 64000; // G722 sampling rate is 16KHz with bits per sample of 16.

        private G722Codec g722Codec;
        private G722CodecState g722CodecState;

        public static int GetEncodedByteLength (int pcmLength)
        {
            return pcmLength / 2;
        }

        public void Encode(byte[] outputBuffer, short[] pcm)
        {
            if (g722Codec == null)
            {
                g722Codec = new G722Codec();
                g722CodecState = new G722CodecState(G722_BIT_RATE, G722Flags.None);
            }

            g722Codec.Encode(g722CodecState,outputBuffer,pcm,pcm.Length);
        }

        public byte[] Encode(short[] pcm)
        {
            if (g722Codec == null)
            {
                g722Codec = new G722Codec();
                g722CodecState = new G722CodecState(G722_BIT_RATE, G722Flags.None);
            }

            var requiredEncodedByteCount = pcm.Length / 2;
            var enc = new byte[requiredEncodedByteCount];

            g722Codec.Encode(g722CodecState,enc,pcm,pcm.Length);
            return enc;
        }
    }
}