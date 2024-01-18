using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Logging
{
    public struct LogCollectorMessage
    {
        public LogCollectorMessage(ReferenceCountedSceneGraphMessage message)
        {
            this.buffer = message;
        }

        private ReferenceCountedSceneGraphMessage buffer;
        private const int headerLength = 2;

        public enum MessageType : int
        {
            Command = 0x1,
            Event = 0x2,
            Ping = 0x3
        }

        public MessageType Type
        {
            get
            {
                return (MessageType)Header[0];
            }
            set
            {
                Header[0] = (byte)value;
            }
        }

        public byte Tag
        {
            get
            {
                return Header[1];
            }
            set
            {
                Header[1] = value;
            }
        }

        private Span<byte> Header
        {
            get
            {
                try
                {
                    return new Span<byte>(buffer.bytes, buffer.start, headerLength);
                }catch(Exception e)
                {
                    Debug.LogException(e);
                    throw e;
                }
            }
        }

        public Span<byte> Bytes
        {
            get
            {
                return new Span<byte>(buffer.bytes, buffer.start + headerLength, buffer.length - headerLength);
            }
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(buffer.bytes, buffer.start + headerLength, buffer.length - headerLength);
        }

        public T FromJson<T>()
        {
            return JsonUtility.FromJson<T>(ToString());
        }

        public static ReferenceCountedSceneGraphMessage Rent(ReadOnlySpan<byte> bytes, byte tag)
        {
            var message = new LogCollectorMessage(ReferenceCountedSceneGraphMessage.Rent(bytes.Length + headerLength));
            message.Type = MessageType.Event;
            message.Tag = tag;
            bytes.CopyTo(message.Bytes);
            return message.buffer;
        }

        private static ReferenceCountedSceneGraphMessage RentTypedJsonMessage<T>(T msg, MessageType type)
        {
            var str = JsonUtility.ToJson(msg);
            var strBytes = Encoding.UTF8.GetBytes(str);
            var message = new LogCollectorMessage(ReferenceCountedSceneGraphMessage.Rent(strBytes.Length + headerLength));
            message.Type = type;
            new Span<byte>(strBytes).CopyTo(message.Bytes);
            return message.buffer;
        }

        public static ReferenceCountedSceneGraphMessage RentCommandMessage<T>(T msg)
        {
            return RentTypedJsonMessage(msg, MessageType.Command);
        }

        public static ReferenceCountedSceneGraphMessage RentPingMessage<T>(T msg)
        {
            return RentTypedJsonMessage(msg, MessageType.Ping);
        }
    }
}