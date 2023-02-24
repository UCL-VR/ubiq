using UnityEngine;
using System;
using Ubiq.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine.SceneManagement;

namespace Ubiq.Messaging
{
    /// <summary>
    /// Wraps a reference counted message for when interacting with the scene graph bus
    /// </summary>
    /// <remarks>
    /// This is a wrapper rather than a subclass, because the networking code will allocate reference counted objects, with no knowledge of the sgb
    /// </remarks>
    public struct ReferenceCountedSceneGraphMessage : IReferenceCounter
    {
        internal const int header = 8; // 8 Bytes for the ObjectId
        internal ReferenceCountedMessage buffer;

        public ReferenceCountedSceneGraphMessage(ReferenceCountedMessage buffer)
        {
            this.buffer = buffer;
            start = buffer.start + header;
            length = buffer.length - header;
        }

        public int start
        {
            get;
            private set;
        }

        public int length
        {
            get;
            private set;
        }

        public byte[] bytes
        {
            get
            {
                return buffer.bytes;
            }
        }

        public Span<byte> data
        {
            get
            {
                return new Span<byte>(bytes, start, length);
            }
        }

        public NetworkId objectid
        {
            get
            {
                return new NetworkId(buffer.bytes, buffer.start);
            }
            set
            {
                value.ToBytes(buffer.bytes, buffer.start);
            }
        }

        public void Acquire()
        {
            buffer.Acquire();
        }

        public void Release()
        {
            buffer.Release();
        }

        public static ReferenceCountedSceneGraphMessage Rent(int length)
        {
            var msg = new ReferenceCountedSceneGraphMessage(MessagePool.Shared.Rent(length + header));
            msg.objectid = new NetworkId(0);
            return msg;
        }

        public static ReferenceCountedSceneGraphMessage Rent(string content)
        {
            var msg = Rent(Encoding.UTF8.GetByteCount(content));
            Encoding.UTF8.GetBytes(content, 0, content.Length, msg.bytes, msg.start);
            return msg;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(bytes, start, length);
        }

        public T FromJson<T>()
        {
            return JsonUtility.FromJson<T>(this.ToString());
        }
    }

    /// <summary>
    /// This instance holds the identity of a Network Object - like a passport. The NetworkId may change at runtime.
    /// Ids can be unique on the network, or shared, depending on whether messages should be one-to-one or one-to-many
    /// between instances.
    /// </summary>
    [Serializable]
    public struct NetworkId : IEquatable<NetworkId>, IEqualityComparer<NetworkId>
    {
        [SerializeField]
        private UInt32 a;
        [SerializeField]
        private UInt32 b;

        public const int Size = 8;

        public NetworkId(UInt32 id)
        {
            a = 0;
            b = id;
        }

        public NetworkId(string id)
        {
            a = 0;
            b = 0;

            if (id == null)
            {
                return;
            }

            ulong numeric;
            id = id.Replace("-", "");
            if (ulong.TryParse(id, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out numeric))
            {
                a = (uint)(numeric >> 32);
                b = (uint)(numeric & 0xffffffffL);
                return;
            }
            if (ulong.TryParse(id, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.CurrentCulture, out numeric))
            {
                a = (uint)(numeric >> 32);
                b = (uint)(numeric & 0xffffffffL);
                return;
            }

            Debug.LogException(new ArgumentException($"Invalid Network Id Format {id}"));
        }

        public NetworkId(byte[] buffer, int offset)
        {
            a = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset + 0, 4));
            b = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset + 4, 4));
        }

        public override string ToString()
        {
            return a.ToString("x8") + "-" + b.ToString("x8");
        }

        public void ToBytes(byte[] buffer, int offset)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer, offset + 0, 4), a);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer, offset + 4, 4), b);
        }

        public bool Equals(NetworkId other)
        {
            return a == other.a && b == other.b;
        }

        public bool Equals(NetworkId x, NetworkId y)
        {
            return x.a == y.a && x.b == y.b;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public override bool Equals(object obj)
        {
            return Equals((NetworkId)obj);
        }

        public int GetHashCode(NetworkId obj)
        {
            return obj.a.GetHashCode();
        }

        public bool Valid
        {
            get
            {
                return b != 0;
            }
        }

        public static bool operator ==(NetworkId a, NetworkId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(NetworkId a, NetworkId b)
        {
            return !a.Equals(b);
        }

        public static NetworkId Unique()
        {
            return IdGenerator.GenerateUnique();
        }

        public static implicit operator bool(NetworkId id)
        {
            return (id.a != 0 || id.b != 0);
        }

        public static NetworkId Null
        {
            get
            {
                return new NetworkId(0);
            }
        }

        public static NetworkId Create(NetworkId nameSpace, string service)
        {
            // Quick Hash..

            NetworkId id;
            id.a = nameSpace.a;
            id.b = nameSpace.b;
            var bytes = Encoding.UTF8.GetBytes(service);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i % 2 != 0)
                {
                    id.a = id.a * bytes[i];
                }
                else
                {
                    id.b = id.b * bytes[i];
                }
            }

            return id;
        }

        public static NetworkId Create(NetworkId nameSpace, uint service)
        {
            NetworkId id;
            id.a = nameSpace.a;
            id.b = unchecked(nameSpace.b + service);
            return id;
        }

        public static NetworkId Create(NetworkId nameSpace, NetworkId service)
        {
            NetworkId id;
            id.a = unchecked(nameSpace.a * service.b + service.a);
            id.b = unchecked(nameSpace.b * service.a + service.b);
            return id;
        }

        /// <summary>
        /// Create a NetworkId based on the name and location of the Object in 
        /// the scene graph.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static NetworkId Create(MonoBehaviour component)
        {
            var address = SceneGraphHelper.GetUniqueAddress(component);
            using (var sha1 = new SHA1Managed())
            {
                return new NetworkId(sha1.ComputeHash(Encoding.UTF8.GetBytes(address)), 0);
            }
        }
    }

    public static class SceneGraphHelper
    {
        private static Dictionary<NetworkScene, SceneAddress> networkSceneAddresses = new Dictionary<NetworkScene, SceneAddress>();

        private class SceneAddress
        {
            public List<Node> Ancestors = new List<Node>();
        }

        private class Node
        {
            public Transform Transform;
            public int Index;
            public bool Common;

            public string GetName()
            {
                if(Common)
                {
                    return "0";
                }
                else
                {
                    return Transform.name + Index;
                }
            }
        }

        private static IEnumerable<Transform> GetSiblings(Transform transform)
        {
            if (transform.parent != null)
            {
                foreach (Transform sibling in transform.parent)
                {
                    yield return sibling;
                }
            }
            else
            {
                foreach (Transform sibling in SceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.transform))
                {
                    yield return sibling;
                }
            }
        }

        private static void GetAncestors(Transform transform, List<Node> ancestors)
        {
            var node = new Node();
            node.Transform = transform;
            node.Index = 0;

            foreach (Transform sibling in GetSiblings(node.Transform))
            {
                if (sibling == transform)
                {
                    break;
                }
                if (sibling.name == transform.name)
                {
                    node.Index++;
                }
            }

            ancestors.Add(node);
            if (transform.parent != null)
            {
                GetAncestors(transform.parent, ancestors);
            }
        }

        private static string GetComponentIdentifier(MonoBehaviour component)
        {
            var index = 0;
            foreach (var item in component.gameObject.GetComponents(component.GetType()))
            {
                if(item == component)
                {
                    break;
                }
                index++;
            }
            return $"{component.GetType()}_{index}";
        }

        public static string GetUniqueAddress(Transform component)
        {
            // This method uses chain coding to store the address of an object
            // in the graph as a series of numbers indicating the sibling
            // indices of an object's ancestors, identifying it uniquely
            // within the graph instance in a portable way.

            // Unique Id's are always relative to the NetworkScene, so first
            // find the closest scene and its cached address, if we have it.

            var scene = NetworkScene.Find(component);

            if (!scene)
            {
                throw new KeyNotFoundException("Networked Component must be able to find a NetworkScene. Is there one in your Scene?");
            }

            if (!networkSceneAddresses.ContainsKey(scene))
            {
                var node = new SceneAddress();
                GetAncestors(scene.transform, node.Ancestors);
                node.Ancestors.Reverse();
                networkSceneAddresses.Add(scene, node);
            }
            var sceneAddress = networkSceneAddresses[scene];

            // Find our ancestors all the way to the root of the scene

            var ancestors = new List<Node>();
            GetAncestors(component.transform, ancestors);
            ancestors.Reverse();

            var root = ancestors[0];

            // If we are in a forest, attempt to find the common ancestor

            if (sceneAddress.Ancestors.Count > 1)
            {
                for (int i = 0; i < Mathf.Min(ancestors.Count, sceneAddress.Ancestors.Count); i++)
                {
                    if (ancestors[i].Transform == sceneAddress.Ancestors[i].Transform)
                    {
                        root = ancestors[i];
                        root.Common = true;
                    }
                }
            }

            // Now we can resolve the list of nodes to unique breadcrumbs

            var address = "root";
            for (int i = ancestors.IndexOf(root); i < ancestors.Count; i++)
            {
                if (!ancestors[i].Common)
                {
                    address += "/" + ancestors[i].GetName();
                }
            }

            return address;
        }

        /// <summary>
        /// Returns a string, unique in a forest, identifying the Component.
        /// </summary>
        public static string GetUniqueAddress(MonoBehaviour component)
        {
            return GetUniqueAddress(component.transform) + "/" + GetComponentIdentifier(component);
        }
    }



}
