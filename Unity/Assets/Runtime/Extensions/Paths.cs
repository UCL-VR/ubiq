using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Extensions.Utilities
{
    public class Paths
    {
        /// <summary>
        /// Returns a unique fully qualified filename based on a guid. The filename has no extension.
        /// </summary>
        /// <returns></returns>
        public static string NewPersistentFileName()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, System.Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Returns a unique fully qualified filename based on the prefix and the current timestamp.
        /// </summary>
        public static string NewPersistentFileName(string prefix, string extension)
        {
            int postfix = 0;
            string filename = PersistentFilename(prefix, postfix, extension);
            while (System.IO.File.Exists(filename))
            {
                filename = PersistentFilename(prefix, postfix++, extension);
            }
            return filename;
        }

        /// <summary>
        /// Returns a fully qualified path to the file in the Persistent Data folder.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string PersistentFileName(string filename)
        {
            return System.IO.Path.Combine(Application.persistentDataPath, filename);
        }

        private static string PersistentFilename(string prefix, int postfix, string extension)
        {
            return System.IO.Path.Combine(Application.persistentDataPath, $"{prefix}_log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{postfix}.{extension}");
        }
    }
}
