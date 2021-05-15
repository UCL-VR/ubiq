using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Logging.Utf8Json;
using Ubiq.Logging.Utf8Json.Resolvers;

namespace Ubiq.Logging
{
    [Flags]
    public enum EventType : sbyte // sbyte because Unity's 'Everything' option sets the field to all -1 (all 1's in 2's complement)
    {
        Application = 1,
        User = 2
    }

    /// <summary>
    /// EventLoggers are used to create events and send them to LogManager instances. The LogManager then ensures they
    /// are delivered to the appropriate endpoint.
    /// EventLoggers are cheap, and it is expected that a single component may have multiple loggers for different types
    /// of event or different levels.
    /// </summary>
    public abstract class EventLogger
    {
        internal LogManager manager;
        private static bool initialised;

        public bool mirrorToConsole;

        public EventType EventType { get; set; }

        public EventLogger(EventType type)
        {
            EventType = type;
            if (!initialised)
            {
                CompositeResolver.Register(UbiqResolver.Instance);
                initialised = true;
            }
            mirrorToConsole = false;
        }

        private bool ShouldLog()
        {
            return manager != null && (manager.Listen & EventType) > 0;
        }

        public void Log(string Event)
        {
            if(ShouldLog())
            {
                var writer = BeginWriter();
                writer.Write("event", Event);
                EndWriter(ref writer);
            }
            if(mirrorToConsole)
            {
                Debug.Log(string.Format("{0}", Event));
            }
        }

        public void Log<T1>(string Event, T1 arg1)
        {
            if (ShouldLog())
            {
                var writer = BeginWriter();
                writer.Write("event", Event);
                writer.Write("arg1", arg1);
                EndWriter(ref writer);
            }
            if (mirrorToConsole)
            {
                Debug.Log(String.Format("{0} {1}", Event, arg1));
            }
        }

        public void Log<T1, T2>(string Event, T1 arg1, T2 arg2)
        {
            if (ShouldLog())
            {
                var writer = BeginWriter();
                writer.Write("event", Event);
                writer.Write("arg1", arg1);
                writer.Write("arg2", arg2);
                EndWriter(ref writer);
            }
            if (mirrorToConsole)
            {
                Debug.Log(String.Format("{0} {1}", Event, arg1));
            }
        }

        public void Log<T1, T2, T3>(string Event, T1 arg1, T2 arg2, T3 arg3)
        {
            if (ShouldLog())
            {
                var writer = BeginWriter();
                writer.Write("event", Event);
                writer.Write("arg1", arg1);
                writer.Write("arg2", arg2);
                writer.Write("arg3", arg3);
                EndWriter(ref writer);
            }
            if (mirrorToConsole)
            {
                Debug.Log(String.Format("{0} {1} {2} {3}", Event, arg1, arg2, arg3));
            }
        }

        public void Log<T1, T2, T3, T4>(string Event, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (ShouldLog())
            {
                var writer = BeginWriter();
                writer.Write("event", Event);
                writer.Write("arg1", arg1);
                writer.Write("arg2", arg2);
                writer.Write("arg3", arg3);
                writer.Write("arg4", arg4);
                EndWriter(ref writer);
            }
            if (mirrorToConsole)
            {
                Debug.Log(String.Format("{0} {1} {2} {3} {4}", Event, arg1, arg2, arg3, arg4));
            }
        }

        protected JsonWriter BeginWriter()
        {
            var writer = new JsonWriter();
            writer.Tag = (byte)EventType;
            writer.Begin();
            WriteHeader(ref writer);
            return writer;
        }

        protected void EndWriter(ref JsonWriter writer)
        {
            writer.End();
            manager.Push(ref writer);
        }

        protected virtual void WriteHeader(ref JsonWriter writer)
        {
            writer.Write("ticks", DateTime.Now.Ticks);
        }
    }

    public abstract class TypedEventLogger : EventLogger
    {
        private string Type;

        public TypedEventLogger(Type type, EventType eventType):base(eventType)
        {
            Type = type.FullName;
        }

        protected override void WriteHeader(ref JsonWriter writer)
        {
            base.WriteHeader(ref writer);
            writer.Write("type", Type);
        }
    }


    /// <summary>
    /// The most common event logger that is designed to operate with Ubiq Network or Component MonoBehaviours.
    /// </summary>
    public class ComponentEventLogger : TypedEventLogger
    {
        public ComponentEventLogger(MonoBehaviour component):base(component.GetType(), EventType.Application)
        {
            try
            {
                LogManager.Find(component).Register(this);
            } catch (NullReferenceException)
            { 
            }
        }
    }

    /// <summary>
    /// An Event Logger for Objects that have a Network Identity
    /// </summary>
    public class ContextEventLogger : TypedEventLogger
    {
        private NetworkContext context;

        public ContextEventLogger(NetworkContext context) : base(context.component.GetType(), EventType.Application)
        {
            this.context = context;
            try
            {
                LogManager.Find(context.scene).Register(this);
            }
            catch (NullReferenceException)
            {
            }
        }

        protected override void WriteHeader(ref JsonWriter writer)
        {
            base.WriteHeader(ref writer);
            writer.Write("sceneid", context.scene.Id);
            writer.Write("objectid", context.networkObject.Id);
            writer.Write("componentid", context.componentId);
        }
    }

    /// <summary>
    /// An Event Logger for user-defined events such as measurements in an experiment. (This still requires a MonoBehaviour refrence from which to find the LogManager).
    /// </summary>
    public class UserEventLogger : EventLogger
    {
        public UserEventLogger(MonoBehaviour component):base(EventType.User)
        {
            try
            {
                LogManager.Find(component).Register(this);
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning("UserEventLogger could not find a LogManager: any logs generated will be lost!");
            }
        }
    }

}
