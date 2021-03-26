using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Packing 
{
    public static void GetBytes(int value, byte[] bytes, int offset)
    {
        bytes[offset + 0] = (byte)(value & 0xFF);
        bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    public static int GetInt(byte[] bytes, int offset)
    {
        int v0 = bytes[offset + 0];
        int v1 = bytes[offset + 1];
        int v2 = bytes[offset + 2];
        int v3 = bytes[offset + 3];
        int value = v0;
        value = value | (v1 << 8);
        value = value | (v2 << 16);
        value = value | (v3 << 24);
        return value;
    }
}
