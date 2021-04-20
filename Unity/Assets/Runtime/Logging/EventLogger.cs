using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Ubiq.Logging
{
    /// <summary>
    /// EventLoggers are used to create events and send them to LogManager instances. The LogManager then ensures they
    /// are delivered to the appropriate endpoint.
    /// EventLoggers are cheap, and it is expected that a single component may have multiple loggers for different types
    /// of event or different levels.
    /// </summary>
    public abstract class EventLogger
    {
        private string Type;
        internal LogManager manager;

        private static bool initialised;

        public EventLogger(Type type)
        {
            Type = type.FullName;
            if (!initialised)
            {
                CompositeResolver.Register(UbiqResolver.Instance);
                initialised = true;
            }
        }

        private bool ShouldLog()
        {
            return manager != null;
        }

        public void Log(string Event)
        {
            if(ShouldLog())
            {
                var writer = BeginWriter();
                writer.Write("event", Event);
                EndWriter(ref writer);
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
        }

        protected JsonWriter BeginWriter()
        {
            var writer = new JsonWriter();
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
            writer.Write("type", Type);
            writer.Write("ticks", DateTime.Now.Ticks);
        }
    }

    /// <summary>
    /// The most common event logger that is designed to operate with Ubiq Network or Component MonoBehaviours.
    /// </summary>
    public class ComponentEventLogger : EventLogger
    {
        public ComponentEventLogger(MonoBehaviour component):base(component.GetType())
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
    public class ContextEventLogger : EventLogger
    {
        private NetworkContext context;

        public ContextEventLogger(NetworkContext context) : base(context.component.GetType())
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

}
