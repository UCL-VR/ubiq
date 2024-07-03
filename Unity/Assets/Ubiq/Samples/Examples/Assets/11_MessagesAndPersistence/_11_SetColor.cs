using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Examples
{
    public class _11_SetColor : MonoBehaviour
    {
        public Color32 color;
        public _11_ColorMessager messager;

        public void SetColor()
        {
            messager.SetColor(color);
        }
    }
}