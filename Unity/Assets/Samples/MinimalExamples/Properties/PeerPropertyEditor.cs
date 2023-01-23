using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System;
using System.Linq;

namespace Ubiq.Samples.Single.Properties
{
    [RequireComponent(typeof(RoomClient))]
    public class PeerPropertyEditor : MonoBehaviour, IList
    {
        public string this[string key]
        {
            get => peer[key];
            set => peer[key] = value;
        }

        public KeyValuePair<string, string> this[int index]
        {
            get
            {
                return peer.ToArray()[index];
            }
            set
            {
                peer[value.Key] = value.Value;
            }
        }

        object IList.this[int index] { get => this[index]; set => this[index] = (KeyValuePair<string, string>)value; }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        int ICollection.Count
        {
            get
            {
                if (peer != null)
                {
                    return peer.ToArray().Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        int IList.Add(object value)
        {
            var pair = (KeyValuePair<string, string>)value;
            peer[pair.Key] = pair.Value;
            return 0;
        }

        void IList.Clear()
        {
            foreach (var item in peer)
            {
                peer[item.Key] = null;
            }
        }

        bool IList.Contains(object value)
        {
            var pair = (KeyValuePair<string, string>)value;
            return peer[pair.Key] == pair.Value;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            peer.ToArray().CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return peer.GetEnumerator();
        }

        int IList.IndexOf(object value)
        {
            var pair = (KeyValuePair<string, string>)value;
            int i = 0;
            foreach (var item in peer)
            {
                if(item.Key == pair.Key && item.Value == pair.Value)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        void IList.Insert(int index, object value)
        {
            var pair = (KeyValuePair<string, string>)value;
            peer[pair.Key] = pair.Value;
        }

        void IList.Remove(object value)
        {
            var pair = (KeyValuePair<string, string>)value;
            peer[pair.Key] = null;
        }

        void IList.RemoveAt(int index)
        {
            var key = this[index].Key;
            peer[key] = null;
        }

        ILocalPeer peer;

        // Start is called before the first frame update
        void Start()
        {
            peer = GetComponent<RoomClient>().Me;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}