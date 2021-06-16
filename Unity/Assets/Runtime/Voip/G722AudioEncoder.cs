using System;
using System.Collections.Generic;
using System.Linq;
using SIPSorceryMedia.Abstractions;
using SIPSorcery.Media;

namespace Ubiq.Voip
{
    public class G722AudioEncoder
    {
        private const int G722_BIT_RATE = 64000; // G722 sampling rate is 16KHz with bits per sample of 16.

        private G722Codec g722Codec;
        private G722CodecState g722CodecState;

        // public int Encode(byte[] encoded, short[] pcm)
        // {
        //     if (g722Codec == null)
        //     {
        //         g722Codec = new G722Codec();
        //         g722CodecState = new G722CodecState(G722_BIT_RATE, G722Flags.None);
        //     }

        //     var requiredEncodedByteCount = pcm.Length / 2;
        //     if (encoded.Length < requiredEncodedByteCount)
        //     {
        //         return -1;
        //     }

        //     var encodedByteCount = g722Codec.Encode(g722CodecState, encoded,
        //         pcm, pcm.Length);
        //     return encodedByteCount;
        // }

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

        // public short[] Resample(short[] pcm, int inRate, int outRate)
        // {
        //     if (inRate == outRate)
        //     {
        //         return pcm;
        //     }
        //     else if (inRate == 8000 && outRate == 16000)
        //     {
        //         // Crude up-sample to 16Khz by doubling each sample.
        //         return pcm.SelectMany(x => new short[] { x, x }).ToArray();
        //     }
        //     else if (inRate == 8000 && outRate == 48000)
        //     {
        //         // Crude up-sample to 48Khz by 6x each sample. This sounds bad, use for testing only.
        //         return pcm.SelectMany(x => new short[] { x, x, x, x, x, x }).ToArray();
        //     }
        //     else if (inRate == 16000 && outRate == 8000)
        //     {
        //         // Crude down-sample to 8Khz by skipping every second sample.
        //         return pcm.Where((x, i) => i % 2 == 0).ToArray();
        //     }
        //     else if (inRate == 16000 && outRate == 48000)
        //     {
        //         // Crude up-sample to 48Khz by 3x each sample. This sounds bad, use for testing only.
        //         return pcm.SelectMany(x => new short[] { x, x, x }).ToArray();
        //     }
        //     else
        //     {
        //         throw new ApplicationException($"Sorry don't know how to re-sample PCM from {inRate} to {outRate}. Pull requests welcome!");
        //     }
        // }
    }
}