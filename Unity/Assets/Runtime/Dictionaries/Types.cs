using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Dictionaries
{
    // on local client set:
    // for remote peer dicts, no sets allowed, ever
    // for local peer dict, add to log, apply immediately
    // for room dict, add to log, but never apply

    // listen to network - on recv remote sets:
    // for remote peer dict, apply
    // for local peer dict, ignore (should never recv)
    // for room dict, apply

    public class PropertyCollection : IEnumerable<KeyValuePair<string,string>>
    {
        private Dictionary<string,string> _tmpDict = new Dictionary<string, string>();
        private Dictionary<string,string> dict = new Dictionary<string, string>();
        public IEnumerable<string> keys => dict.Keys;
        public IEnumerable<string> values => dict.Values;

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string>>)dict).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)dict).GetEnumerator();
        }

        public string this[string key]
        {
            get
            {
                if (dict.TryGetValue(key,out var value))
                {
                    return value;
                }
                return string.Empty;
            }
        }

        public void Clear()
        {
            dict.Clear();
        }

        public bool Set (string key, string value)
        {
            if (value == null || value == string.Empty)
            {
                // Remove key
                if (dict.ContainsKey(key))
                {
                    dict.Remove(key);
                    return true;
                }
            }

            if (dict.TryGetValue(key, out var existing)
                && existing == value)
            {
                return false;
            }

            dict[key] = value;
            return true;
        }

        public bool Set (List<string> keys, List<string> values)
        {
            _tmpDict.Clear();
            for(int i = 0; i < keys.Count; i++)
            {
                if (i >= values.Count)
                {
                    break;
                }

                _tmpDict.Add(keys[i],values[i]);
            }

            if (!_tmpDict.Equals(dict))
            {
                var swap = dict;
                dict = _tmpDict;
                _tmpDict = swap;
                return true;
            }

            return false;
        }

        public bool Append (List<string> keys, List<string> values)
        {
            var modified = false;
            for (int i = 0; i < keys.Count; i++)
            {
                if (i >= values.Count)
                {
                    break;
                }
                if(Set(keys[i], values[i]))
                {
                    modified = true;
                }
            }
            return modified;
        }
    }
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