﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Logging.Utf8Json;
using Ubiq.Logging.Utf8Json.Resolvers;

namespace Ubiq.Logging
{
    /// <summary>
    /// Pre-defined event types that will create log files with names instead of
    /// the tag value. Other tag values may be used however.
    /// </summary>
    public enum EventType : sbyte // sbyte because Unity's 'Everything' option sets the field to all -1 (all 1's in 2's complement)
    {
        Application = 1,
        Experiment = 2,
        Debug = 3,
        Info = 4
    }

    /// <summary>
    /// LogEmitters are used to create events and send them to LogCollector 
    /// instances. The LogCollector then ensures they are delivered to the 
    /// appropriate endpoint.
    /// LogEmitters are cheap, and it is expected that a single component may
    /// have multiple loggers for different types of event or for different 
    /// levels.
    /// </summary>
    public abstract class LogEmitter
    {
        internal LogCollector collector;
        private static bool initialised;

        public bool mirrorToConsole;

        public EventType EventType { get; set; }

        public LogEmitter(EventType type)
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
            return collector != null && collector.enabled;
        }

        /// <summary>
        /// Attempts to Register this emitter using the provided Component as the location to start searching for a Collector
        /// </summary>
        /// <param name="component"></param>
        protected void RegisterCollectorBy(MonoBehaviour component)
        {
            try
            {
                LogCollector.Find(component).Register(this);
            }
            catch (NullReferenceException)
            {
                Debug.LogWarning($"{GetType()} could not find a LogManager: any logs generated will be lost!");
            }
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
            collector.Push(ref writer);
        }

        protected virtual void WriteHeader(ref JsonWriter writer)
        {
            writer.Write("ticks", DateTime.Now.Ticks);
        }
    }

    /// <summary>
    /// A LogEmitter that includes a field "type" with the name of the class that emitted the log (the one the emitter itself is a member of).
    /// </summary>
    public abstract class TypedLogEmitter : LogEmitter
    {
        private string Type;

        public TypedLogEmitter(Type type, EventType eventType):base(eventType)
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
    /// The most common event logger that is designed to operate with Unity MonoBehaviours.
    /// </summary>
    public class ComponentLogEmitter : TypedLogEmitter
    {
        public ComponentLogEmitter(MonoBehaviour component, EventType type = EventType.Debug) : base(component.GetType(), type)
        {
            RegisterCollectorBy(component);
        }
    }

    /// <summary>
    /// An Event Logger for Objects that have a Network Identity
    /// </summary>
    public class ContextLogEmitter : TypedLogEmitter
    {
        private NetworkContext context;

        public ContextLogEmitter(NetworkContext context, EventType type = EventType.Debug) : base(context.component.GetType(), type)
        {
            this.context = context;
            RegisterCollectorBy(context.scene);
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
    public class ExperimentLogEmitter : LogEmitter
    {
        public ExperimentLogEmitter(MonoBehaviour component):base(EventType.Experiment)
        {
            RegisterCollectorBy(component);
        }
    }

    public class InfoLogEmitter : LogEmitter
    {
        public InfoLogEmitter(MonoBehaviour component) : base(EventType.Info)
        {
            RegisterCollectorBy(component);
        }
    }
}