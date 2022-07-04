using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    public class MovingAverage
    {
        float[] samples;
        int i;

        public MovingAverage(int N)
        {
            samples = new float[N];
            i = 0;
        }

        public void Update(float s)
        {
            i = (int)Mathf.Repeat(i, samples.Length);
            samples[i++] = s;
        }

        public float Mean()
        {
            float acc = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                acc += samples[i];
            }
            return acc / (float)samples.Length;
        }
    }
}