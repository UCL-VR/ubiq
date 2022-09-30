using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Rooms.Spatial
{
    public class SpatialState
    {
        public Guid Shard;
        public Guid Member;
        public List<Guid> Observed;

        public SpatialState()
        {
            Observed = new List<Guid>();
            Shard = Guid.NewGuid(); // To prevent coding mistakes dumping everyone in the same room...
        }
    }

    /// <summary>
    /// Provides the Spatial Partition for a scene. An instance of this must be placed on an object in the Scene.
    /// </summary>
    /// <remarks>
    /// A partition is defined per-Unity Scene (not, for example, per Peer)
    /// </remarks>
    public abstract class SpatialPartition : MonoBehaviour
    {
        private static SpatialPartition singleton;

        public static SpatialPartition ScenePartition
        {
            get { return singleton; }
        }

        private void Awake()
        {
            singleton = this;
        }

        /// <summary>
        /// For a given position, return the rooms that the position is in (member) and the rooms adjacent to it (observers)
        /// in the SpatialState object.
        /// Uses the Shard parameter of the state, and returns whether or not the state has changed.
        /// </summary>
        public abstract void GetRooms(Vector3 position, SpatialState state);
    }
}