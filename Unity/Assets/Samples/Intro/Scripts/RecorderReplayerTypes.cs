using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Ubiq.Messaging;

namespace RecorderReplayerTypes {

    /// <summary>
    /// A MessagePack contains all the messages (SingleMessages) that are recorded in one frame.
    /// </summary>
    public class MessagePack
    {
        public List<byte[]> messages;

        public void AddMessage(byte[] message)
        {
            messages.Add(message);
        }
        public MessagePack()
        {
            messages = new List<byte[]>();
            messages.Add(new byte[4]); // save space for size?
        }

        public MessagePack(byte[] messagePack) // 4 byte at beginning for size
        {
            messages = new List<byte[]>();
            messages.Add(new byte[] { messagePack[0], messagePack[1], messagePack[2], messagePack[3] });

            int i = 4;
            while (i < messagePack.Length) // error here!!!
            {
                int lengthMsg = BitConverter.ToInt32(messagePack, i);
                i += 4;
                byte[] msg = new byte[lengthMsg];
                Buffer.BlockCopy(messagePack, i, msg, 0, lengthMsg);
                messages.Add(msg);
                i += lengthMsg;
            }
        }

        public byte[] GetBytes()
        {
            byte[] toBytes = messages.SelectMany(a => a).ToArray();
            byte[] l = BitConverter.GetBytes(toBytes.Length - 4); // only need length of package not length of package + 4 byte of length
            toBytes[0] = l[0]; toBytes[1] = l[1]; toBytes[2] = l[2]; toBytes[3] = l[3];
            //int t = BitConverter.ToInt32(messages[0], 0);
            //if (BitConverter.IsLittleEndian)
            //    Array.Reverse(messages[0]);
            //t = BitConverter.ToInt32(messages[0], 0);
            return toBytes;

        }
    }
    // Kind of obsolete if I think about it... it just encapsulates the ReferenceCountedSceneGraphMessage
    public class SingleMessage
    {
        public byte[] message; // whole message including object and component ids
        public SingleMessage(byte[] message)
        {
            this.message = message;
        }
        public byte[] GetBytes()
        {
            byte[] bLength = BitConverter.GetBytes(message.Length);
            //if (BitConverter.IsLittleEndian)
            //    Array.Reverse(bLength);
            byte[] toBytes = new byte[bLength.Length + message.Length];
            Buffer.BlockCopy(bLength, 0, toBytes, 0, bLength.Length);
            Buffer.BlockCopy(message, 0, toBytes, bLength.Length, message.Length);
            return toBytes;
        }
    }

    public class ReplayedObjectProperties
    {
        public GameObject gameObject;
        public ObjectHider hider;
        public NetworkId id;
        public Dictionary<int, INetworkComponent> components = new Dictionary<int, INetworkComponent>();

    }

    [System.Serializable]
    public class RecordingInfo
    {
        public int[] listLengths;
        public int frames;
        public int avatarsAtStart;
        public int avatarNr;
        public List<NetworkId> objectids;
        public List<string> textures;
        public List<float> frameTimes;
        public List<int> pckgSizePerFrame;
        public List<int> idxFrameStart;

        public RecordingInfo(int frames, int avatarsAtStart, int avatarNr, List<NetworkId> objectids, List<string> textures, List<float> frameTimes, List<int> pckgSizePerFrame, List<int> idxFrameStart)
        {
            listLengths = new int[3] { frameTimes.Count, pckgSizePerFrame.Count, idxFrameStart.Count };
            this.frames = frames;
            this.avatarsAtStart = avatarsAtStart;
            this.avatarNr = avatarNr;
            this.objectids = objectids;
            this.textures = textures;
            this.frameTimes = frameTimes;
            this.pckgSizePerFrame = pckgSizePerFrame;
            this.idxFrameStart = idxFrameStart;
        }
    }
}