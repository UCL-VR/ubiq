using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Logging
{
    public struct LogManagerMessage
    {
        public LogManagerMessage(ReferenceCountedSceneGraphMessage message)
        {
            this.buffer = message;
        }

        private ReferenceCountedSceneGraphMessage buffer;
        private const int headerLength = 2;

        public Span<byte> Header
        {
            get
            {
                return new Span<byte>(buffer.bytes, buffer.start, headerLength);
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

        public static ReferenceCountedSceneGraphMessage Rent(ReadOnlySpan<byte> bytes, byte tag)
        {
            var message = new LogManagerMessage(ReferenceCountedSceneGraphMessage.Rent(bytes.Length + headerLength));
            message.Header[0] = 0x1;
            message.Header[1] = tag;
            bytes.CopyTo(message.Bytes);
            return message.buffer;
        }

        public static ReferenceCountedSceneGraphMessage Rent(string str)
        {
            var strBytes = Encoding.UTF8.GetBytes(str);
            var message = new LogManagerMessage(ReferenceCountedSceneGraphMessage.Rent(strBytes.Length + headerLength));
            message.Header[0] = 0x2;
            new Span<byte>(strBytes).CopyTo(message.Bytes);
            return message.buffer;
        }
    }
}