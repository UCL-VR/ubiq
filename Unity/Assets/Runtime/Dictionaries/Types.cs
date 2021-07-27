using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Dictionaries
{
    /// <summary>
    /// An observable string dictionary that can be serialised to Json by the in-built Unity serialisation.
    /// </summary>
    [Serializable]
    public class SerializableDictionary : IEnumerable<KeyValuePair<string,string>>
    {
        [SerializeField]
        private List<string> keys = new List<string>();

        [SerializeField]
        private List<string> values = new List<string>();

        private bool isUpdated = false;

        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IEnumerable<KeyValuePair<string, string>> other)
        {
            foreach (var item in other)
            {
                keys.Add(item.Key);
                values.Add(item.Value);
            }
        }

        public string this[string key]
        {
            get
            {
                var index = keys.IndexOf(key);
                if (index < 0)
                {
                    return null;
                }
                else
                {
                    return values[index];
                }
            }
            set
            {
                var index = keys.IndexOf(key);
                if (index < 0)
                {
                    if (value == null || value == "")
                    {
                        return;
                    }

                    keys.Add(key);
                    values.Add(value);
                    isUpdated = true;
                }
                else
                {
                    if (value == null || value == "")
                    {
                        keys.RemoveAt(index);
                        values.RemoveAt(index);
                        isUpdated = true;
                    }
                    else
                    {
                        if(values[index] != value)
                        {
                            values[index] = value;
                            isUpdated = true;
                        }
                    }
                }
            }
        }

        public bool IsUpdated()
        {
            if(isUpdated)
            {
                isUpdated = false;
                return true;
            }
            return false;
        }

        public bool Update(string key, string value)
        {
            this[key] = value;
            return IsUpdated();
        }

        public bool Update(IEnumerable<KeyValuePair<string,string>> other)
        {
            foreach (var item in other)
            {
                this[item.Key] = item.Value;
            }
            return IsUpdated();
        }

        public bool Update(SerializableDictionary other)
        {
            return Update(other.Enumerator);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            for (int i = 0; i < keys.Count; i++)
            {
                yield return new KeyValuePair<string, string>(keys[i], values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<KeyValuePair<string, string>> Enumerator
        {
            get
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    yield return new KeyValuePair<string, string>(keys[i], values[i]);
                }
            }
        }


    }

}