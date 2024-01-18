﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples.Social
{
    public class SetNameButton : MonoBehaviour
    {
        public DisplayNameManager nameManager;
        public Text nameText;

        // Expected to be called by a UI element
        public void SetName()
        {
            if (nameText && nameManager)
            {
                nameManager.SetDisplayName(nameText.text);
            }
        }
    }
}