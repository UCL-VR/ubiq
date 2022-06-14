using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Utilities.Coroutines
{
    public class Coroutines
    {
        public static IEnumerator Update(float period, Action action)
        {
            while(true)
            {
                action();
                yield return new WaitForSeconds(period);
            }
        }
    }
}