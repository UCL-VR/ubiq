using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class NewRoomMenuControl : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => GetComponentInParent<RoomsMenuController>().Create());
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}