using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples
{
    public class _00_SetColor : MonoBehaviour
    {
        public Color32 color;
        public _00_Messager messager;

        public void SetColor()
        {
            messager.SetColor(color);
        }
    }
}