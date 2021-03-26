using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Ubiq.Messaging;
using UnityEngine;

public class IdGenerator 
{
    private static System.Random random = new System.Random();

    public static NetworkId GenerateUnique()
    {
        using (var stream = new MemoryStream(2048))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(System.Environment.MachineName);
                writer.Write(System.Environment.UserName);
                writer.Write(System.Environment.OSVersion);
                writer.Write(System.Environment.CurrentManagedThreadId);
                writer.Write(System.Environment.ExitCode);
                writer.Write(System.Environment.TickCount);
                writer.Write(System.Environment.Version.ToString());
                writer.Write(System.Environment.WorkingSet);
                writer.Write(System.DateTime.Now.Ticks);
                writer.Write(random.NextDouble());
                writer.Write(random.NextDouble());
                writer.Write(random.NextDouble());
                writer.Write(random.NextDouble());
                writer.Write(random.NextDouble());

                writer.Flush();

                using (var sha1 = new SHA1Managed())
                {
                    return new NetworkId(sha1.ComputeHash(stream.ToArray()), 0);
                }
            }
        }
    }

    public static NetworkId GenerateFromName(string name)
    {
        using (var stream = new MemoryStream(1024))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(name);
                using (var sha1 = new SHA1Managed())
                {
                    return new NetworkId(sha1.ComputeHash(stream.ToArray()), 0);
                }
            }
        }
    }
}
