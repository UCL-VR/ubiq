using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

#if NETSTANDARD
using System.Runtime.CompilerServices;
#endif

namespace Ubiq.Logging.Utf8Json.Internal
{
    /// <summary>
    /// The Dynamic Array pool is a wrapper around a regular array pool that is designed to allow
    /// expanding rented arrays with its EnsureCapacity method. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DynamicArrayPool<T>
    {
        const int ArrayMaxSize = 0x7FFFFFC7; // https://msdn.microsoft.com/en-us/library/system.array
        private System.Buffers.ArrayPool<T> arrayPool = System.Buffers.ArrayPool<T>.Shared;
        private int startingLength;

        public static DynamicArrayPool<T> Shared { get; private set; } = new DynamicArrayPool<T>(1);

        public DynamicArrayPool(int startingLength)
        {
            this.startingLength = startingLength;
        }

        public T[] Rent()
        {
            var buffer = arrayPool.Rent(startingLength);
            return buffer;
        }

        public void Return(T[] array)
        {
            arrayPool.Return(array);
        }

        public void EnsureCapacity(ref T[] buffer, int offset, int appendLength)
        {
            var newLength = offset + appendLength;

            // If null(most case fisrt time) fill byte.
            if (buffer == null)
            {
                buffer = arrayPool.Rent(newLength);
                return;
            }

            // like MemoryStream.EnsureCapacity
            var current = buffer.Length;
            if (newLength > current)
            {
                var newSize = unchecked((newLength * 2));
                if (newSize < 0) // overflow
                {
                    newSize = ArrayMaxSize;
                }

                if(newSize < 256)
                {
                    newSize = 256;
                }

                if(newSize < newLength || current == ArrayMaxSize)
                {
                    throw new InvalidOperationException("buffer[] size reached maximum size of array(0x7FFFFFC7), can not write to single buffer[]. Details: https://msdn.microsoft.com/en-us/library/system.array");
                }

                var newBuffer = arrayPool.Rent(newLength);
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, offset);
                arrayPool.Return(buffer);
                buffer = newBuffer;
            }
        }
    }

    public class BinaryUtil
    {
        private static DynamicArrayPool<byte> pool = new DynamicArrayPool<byte>(32768);

#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void EnsureCapacity(ref byte[] bytes, int offset, int appendLength)
        {
            pool.EnsureCapacity(ref bytes, offset, appendLength);
        }

        public static void Release(ref byte[] buffer)
        {
            pool.Return(buffer);
            buffer = null;
        }

        // Buffer.BlockCopy version of Array.Resize
#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void FastResize(ref byte[] array, int newSize)
        {
            if (newSize < 0) throw new ArgumentOutOfRangeException("newSize");

            byte[] array2 = array;
            if (array2 == null)
            {
                array = new byte[newSize];
                return;
            }

            if (array2.Length != newSize)
            {
                byte[] array3 = new byte[newSize];
                Buffer.BlockCopy(array2, 0, array3, 0, (array2.Length > newSize) ? newSize : array2.Length);
                array = array3;
            }
        }

#if NETSTANDARD
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static
#if NETSTANDARD
            unsafe
#endif
            byte[] FastCloneWithResize(byte[] src, int newSize)
        {
            if (newSize < 0) throw new ArgumentOutOfRangeException("newSize");
            if (src.Length < newSize) throw new ArgumentException("length < newSize");

            if (src == null) return new byte[newSize];

            byte[] dst = new byte[newSize];

#if NETSTANDARD && !NET45
            fixed (byte* pSrc = &src[0])
            fixed (byte* pDst = &dst[0])
            {
                Buffer.MemoryCopy(pSrc, pDst, dst.Length, newSize);
            }
#else
            Buffer.BlockCopy(src, 0, dst, 0, newSize);
#endif

            return dst;
        }
    }
}
