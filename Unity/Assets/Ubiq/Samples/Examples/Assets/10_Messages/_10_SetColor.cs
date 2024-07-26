using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Examples
{
    public class _10_SetColor : MonoBehaviour
    {
        public Color32 color;
        public _10_Messager messager;

        public void SetColor()
        {
            messager.SetColor(color);
        }
    }
}