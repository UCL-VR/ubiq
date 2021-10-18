using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq
{
    /// <summary>
    /// A UnityEvent that can fire for existing items in a list, compensating for race conditions in initialisation between sources and their sinks
    /// </summary>
    public class ExistingListEvent<T> : UnityEvent<T>
    {
        private IEnumerable<T> existing;

        public void AddListener(UnityAction<T> method, bool runExisting = true)
        {
            base.AddListener(method);
            if (existing != null && runExisting)
            {
                foreach (var item in existing)
                {
                    method(item);
                }
            }
        }

        public void SetExisting(IEnumerable<T> existing)
        {
            this.existing = existing;
            foreach (var item in existing)
            {
                base.Invoke(item);
            }
        }
    }

    /// <summary>
    /// A UnityEvent that can fire for an existing item, compensating for race conditions in initialisation between sources and their sinks
    /// </summary>
    public class ExistingEvent<T> : UnityEvent<T>
    {
        private T existing;

        public void AddListener(UnityAction<T> method, bool runExisting = true)
        {
            base.AddListener(method);
            if (existing != null && runExisting)
            {
                method(existing);
            }
        }

        public void SetExisting(T existing)
        {
            this.existing = existing;
        }
    }

}