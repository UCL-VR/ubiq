using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Samples
{
    public class SceneInfo : MonoBehaviour
    {
        public Texture2D screenshot;

        public string base64image
        {
            get
            {
                var bytes = ImageConversion.EncodeToPNG(screenshot);
                return Convert.ToBase64String(bytes);
            }
        }

    }
}