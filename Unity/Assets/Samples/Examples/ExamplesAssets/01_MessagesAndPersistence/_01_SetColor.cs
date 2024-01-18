using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples
{
    public class _01_SetColor : MonoBehaviour
    {
        public Color32 color;
        public _01_ColorMessager messager;

        public void SetColor()
        {
            messager.SetColor(color);
        }
    }
}