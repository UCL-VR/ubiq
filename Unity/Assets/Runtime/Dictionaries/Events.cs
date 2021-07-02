using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq
{
    /// <summary>
    /// A UnityEvent that can fire for existing items in a list, compensating for race conditions in initialisation between sources and their sinks
    /// </summary>
    public class ListEvent<T> : UnityEvent<T>
    {
        private IEnumerable<T> existing;

        public void AddListener(UnityAction<T> method, bool runExisting)
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

        public void SetList(IEnumerable<T> existing)
        {
            this.existing = existing;
            foreach (var item in existing)
            {
                base.Invoke(item);
            }
        }
    }
}