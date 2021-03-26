using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ubiq.TimeManagement
{
    public class TimeManager : MonoBehaviour
    {
        // By convention, all time values of type long are in microseconds from the Windows Epoch.

        public long gameTime;
        public long physicsTime;
        public long deltaTime;

        private long processTime;

        public bool controlPhysics = true;

        public enum TimeMode
        {
            LocalTime,
            SystemTime
        }

        public TimeMode mode;

        private void Awake()
        {
            if (controlPhysics)
            {
                Physics.autoSimulation = false;
                Physics.autoSyncTransforms = false;
            }
        }

        void Start()
        {
            processTime = 0;
            UpdateGameTime();

            if (controlPhysics)
            {
                physicsTime = gameTime;
            }
        }

        private void FixedUpdate()
        {
            processTime += fixedDeltaTime;

            if (!controlPhysics)
            {
                physicsTime += fixedDeltaTime;
            }

            UpdateGameTime();

            if (controlPhysics)
            {
                Physics.SyncTransforms();

                // For stability reasons, PhysX cannot update over a period longer than TIme.maximumDeltaTime, but also not smaller than 8 ms (empirical figure).
                // We therefore keep a seperate 'physics clock' in sync with our game time to control the updates.
                while ((physicsTime - gameTime) < fixedDeltaTime)
                {
                    var timestep = ToSeconds(gameTime - physicsTime);
                    timestep = Mathf.Clamp(timestep, UnityEngine.Time.fixedDeltaTime, UnityEngine.Time.maximumDeltaTime);
                    Physics.Simulate(timestep);
                    physicsTime += FromSeconds(timestep);
                }
            }
        }

        private void Update()
        {
            UpdateGameTime();
        }

        private void UpdateGameTime()
        {
            var previousGameTime = gameTime;
            switch (mode)
            {
                case TimeMode.LocalTime:
                    gameTime = processTime;
                    break;
                case TimeMode.SystemTime:
                    gameTime = SystemTime;
                    break;
                default:
                    break;
            }
            deltaTime = gameTime - previousGameTime;
        }

        public long fixedDeltaTime
        {
            get
            {
                return (long)(UnityEngine.Time.fixedDeltaTime * 1e6f);
            }
        }

        public static long FromSeconds(float seconds)
        {
            return (long)(seconds * 1e6f);
        }

        public static float ToSeconds(long microseconds)
        {
            return (float)microseconds / 1e6f;
        }

        /// <summary>
        /// The current timeestamp to the best precision we have. If the mode is set to gametime this will be quantised to the update loop period. Otherwise it returns the system time.
        /// </summary>
        public long Time
        {
            get
            {
                switch (mode)
                {
                    case TimeMode.LocalTime:
                        return gameTime;
                    case TimeMode.SystemTime:
                        return SystemTime;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static long SystemTime
        {
            get
            {
#if UNITY_STANDALONE_WIN
                return HighResolutionDateTime.UtcNow.ToMicroseconds();
#elif UNITY_EDITOR_WIN
                return HighResolutionDateTime.UtcNow.ToMicroseconds();
#else
                return FromSeconds(UnityEngine.Time.time); // until we get better clocks this is just the game time
#endif
            }
        }

        public enum Epochs
        {
            Day,
        }

        public long Epoch(Epochs epoch)
        {
            switch (mode)
            {
                case TimeMode.LocalTime:
                    return 0;
            }

            switch (epoch)
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                case Epochs.Day:
                    return HighResolutionDateTime.ToMicroseconds(DateTime.Today);
#endif
                default:

                    throw new NotImplementedException();
            }
        }

    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    //https://manski.net/2014/07/high-resolution-clock-in-csharp/
    public static class HighResolutionDateTime
    {
        public static bool IsAvailable { get; private set; }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        public static DateTime UtcNow
        {
            get
            {
                if (!IsAvailable)
                {
                    throw new InvalidOperationException("High resolution clock isn't available.");
                }
                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);
                return DateTime.FromFileTimeUtc(filetime);
            }
        }

        static HighResolutionDateTime()
        {
            try
            {
                long filetime; GetSystemTimePreciseAsFileTime(out filetime);
                IsAvailable = true;
            }
            catch (EntryPointNotFoundException) // Not running Windows 8 or higher.
            {
                IsAvailable = false;
            }
        }

        public static double ToSeconds(this DateTime time)
        {
            return time.Ticks / 10e6; //https://docs.microsoft.com/en-us/dotnet/api/system.datetime.ticks?view=netframework-4.8
        }

        public static long ToMicroseconds(this DateTime time)
        {
            return time.Ticks / 10;
        }
    }
#endif

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    public static class HighResolutionDateTime
    {
        public static long ToMicroseconds(this DateTime time)
        {
            return time.Ticks / 10;
        }
    }
#endif

}