using UnityEngine;
using System;
using Ubiq.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ubiq.Messaging
{
    public interface INetworkComponent
    {
        /// <summary>
        /// Process a message directed at this object. Use the data directly in the implementation. Once the call returns the data in message will be undefined.
        /// If necessary, Acquire can be called after which message may be stored, and after which Release must be called when it is done with.
        /// Release does not have to be called if the message is processed entirely within the implementation.
        /// </summary>
        void ProcessMessage(ReferenceCountedSceneGraphMessage message);
    }

    /// <summary>
    /// Wraps a reference counted message for when interacting with the scene graph bus
    /// </summary>
    // this is a wrapper rather than a subclass, because the networking code will allocated reference counted objects, with no knowledge of the sgb
    public struct ReferenceCountedSceneGraphMessage : IReferenceCounter
    {
        internal const int header = 10;
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

        public ushort componentid
        {
            get
            {
                return System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(new Span<byte>(buffer.bytes, buffer.start + NetworkId.Size, 2));
            }
            set
            {
                System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buffer.bytes, buffer.start + NetworkId.Size, 2), value);
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
            // expected header length is 4 bytes for the node id and 4 bytes for the entity id
            return new ReferenceCountedSceneGraphMessage(MessagePool.Shared.Rent(length + ReferenceCountedSceneGraphMessage.header));
        }

        public static ReferenceCountedSceneGraphMessage Rent(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var msg = Rent(bytes.Length);
            Array.Copy(bytes, 0, msg.bytes, msg.start, bytes.Length);
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

        public static int Size = 8;

        public NetworkId(UInt32 id)
        {
            a = 0;
            b = id;
        }

        public NetworkId(string id)
        {
            ulong numeric;
            id = id.Replace("-", "");
            if(ulong.TryParse(id, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out numeric))
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
            a = 0;
            b = 0;
            Debug.LogException(new ArgumentException($"Invalid Network Id Format {id}"));
        }

        public NetworkId(byte[] buffer, int offset)
        {
            a = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset + 0, 4));
            b = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset + 4, 4));
        }

        public override string ToString()
        {
            return b.ToString("x8") + "-" + a.ToString("x8");
        }

        public void ToBytes(byte[] buffer, int offset)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer, offset + 0, 4), a);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer, offset + 4, 4), b);
        }

        public bool Equals(NetworkId other)
        {
            return a == other.a;
        }

        public bool Equals(NetworkId x, NetworkId y)
        {
            return x.a == y.a;
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
    }

    public interface INetworkObject
    {
        NetworkId Id { get; }
    }
}
