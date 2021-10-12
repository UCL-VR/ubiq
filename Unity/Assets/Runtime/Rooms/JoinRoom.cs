using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Rooms
{
    /// <summary>
    /// Joins a Room based on a GUID.
    /// </summary>
    public class JoinRoom : MonoBehaviour
    {
        public string Guid;

        // Start is called before the first frame update
        void Start()
        {
            if (Guid.Length > 0)
            {
                try
                {
                    GetComponent<RoomClient>().Join(new Guid(Guid));
                }
                catch (FormatException)
                {
                    Debug.LogError($"The Room Guid {Guid} is not in the correct format");
                }
                catch (OverflowException)
                {
                    Debug.LogError($"The Room Guid {Guid} is not in the correct format");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}