using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Ubiq.Messaging
{
    public static class MessagingExtensions
    {
        public static INetworkObject GetNetworkObjectInChildren(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is INetworkObject).FirstOrDefault() as INetworkObject;
        }

        private static MD5 hash;

        public static ushort GetPortableHashCode(this string str)
        {
            if(hash == null)
            {
                hash = MD5.Create();
            }
            var hashed = hash.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToUInt16(hashed, 0);
        }
    }
}