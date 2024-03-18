using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ubiq.Guids
{
    public class Guids
    {
        private static SHA1 sha1 = SHA1.Create();

        /// <summary>
        /// Creates a V5 GUID, which is an SHA1 hash of a piece of data, under a Namespace.
        /// </summary>
        public static Guid Generate(Guid Namespace, Vector2Int Name) //Todo: so much garbage, why is C# making this so hard!?
        {
            var Bytes = new Byte[24];
            Span<byte> NamespaceBytes = new Span<byte>(Bytes, 0, 16);
            Span<byte> NameBytes = new Span<byte>(Bytes, 16, 8);
            Namespace.ToByteArray().CopyTo(NamespaceBytes);
            MemoryMarshal.Cast<Vector2, byte>(new ReadOnlySpan<Vector2>(new Vector2[] { Name })).CopyTo(NameBytes);

            var Hash = sha1.ComputeHash(Bytes);

            return Generate(Hash);
        }

        public static Guid Generate(Guid Namespace, string Name)
        {
            var NumNameBytes = System.Text.UTF8Encoding.UTF8.GetByteCount(Name);

            var Bytes = new Byte[16 + NumNameBytes];

            Span<byte> NamespaceBytes = new Span<byte>(Bytes, 0, 16);
            Span<byte> NameBytes = new Span<byte>(Bytes, 16, NumNameBytes);
            Namespace.ToByteArray().CopyTo(NamespaceBytes);
            System.Text.UTF8Encoding.UTF8.GetBytes(Name).CopyTo(NameBytes);

            var Hash = sha1.ComputeHash(Bytes);

            return Generate(Hash);
        }

        /// <summary>
        /// Generates a V5 GUID, which packages an SHA1 Hash.
        /// </summary>
        /// <param name="SHA1Hash"></param>
        /// <returns></returns>
        public static Guid Generate(ReadOnlySpan<byte> SHA1Hash)
        {
            var GuidBytes = new byte[16];
            SHA1Hash.Slice(0,16).CopyTo(new Span<byte>(GuidBytes));

            // Set high-nibble to 5 to indicate type 5
            // Note that the order of the byte groups for the .NET GUID constructor are reversed for the first three groups
            // https://docs.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-5.0
            // This isn't a problem here, but will be if we try to use the same method on other platforms.
            GuidBytes[7] &= 0x0F;
            GuidBytes[7] |= 0x50;

            //set upper two bits to "10"
            GuidBytes[8] &= 0x3F;
            GuidBytes[8] |= 0x80;

            return new Guid(GuidBytes);
        }
    }
}
