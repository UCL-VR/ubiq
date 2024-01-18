using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Rooms
{
    [CreateAssetMenu(fileName = "Room Guid", menuName = "Ubiq/Room Guid", order = 1)]
    public class RoomGuid : ScriptableObject
    {
        public string Guid;

        public static implicit operator System.Guid(RoomGuid g) => System.Guid.Parse(g.Guid);
    }
}
