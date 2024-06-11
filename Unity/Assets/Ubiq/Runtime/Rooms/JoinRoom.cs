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
        public RoomGuid Guid;

        private void Start()
        {
            if (Guid.Guid.Length > 0)
            {
                try
                {
                    RoomClient.Find(this).Join(Guid);
                }
                catch (NullReferenceException)
                {
                    Debug.LogWarning("No RoomClient found");
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
    }
}