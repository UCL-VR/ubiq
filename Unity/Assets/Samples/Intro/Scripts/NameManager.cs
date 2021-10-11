using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System.Text;

namespace Ubiq.Samples.Social
{
    public class NameManager : MonoBehaviour
    {
        [System.Serializable]
        public class WrappedList
        {
            public List<string> list;
        }

        public bool autoName;
        public bool persistName;
        public List<WrappedList> nameComponents;
        public string delimiter = " ";

        private RoomClient roomClient;

        private void Awake()
        {
            roomClient = GetComponentInParent<RoomClient>();
        }

        private void Start()
        {
            if (!autoName)
            {
                return;
            }

            var name = null as string;
            if (persistName)
            {
                name = LoadPersistentName();
            }
            if (string.IsNullOrEmpty(name))
            {
                name = GenerateName();
            }
            SetName(name);
        }

        private string LoadPersistentName ()
        {
            return PlayerPrefs.GetString("ubiq.samples.social.name", "");
        }

        private string GenerateName ()
        {
            if (nameComponents == null || nameComponents.Count == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            var delimiterNeeded = false;
            for (int i = 0; i < nameComponents.Count; i++)
            {
                var options = nameComponents[i].list;
                if (options.Count == 0)
                {
                    continue;
                }

                if (delimiterNeeded)
                {
                    sb.Append(delimiter);
                }

                sb.Append(options[UnityEngine.Random.Range(0,options.Count)]);
                delimiterNeeded = true;
            }

            return sb.ToString();
        }

        public void SetName (string name)
        {
            roomClient.Me["ubiq.samples.social.name"] = name;

            if (persistName)
            {
                PlayerPrefs.SetString("ubiq.samples.social.name",name);
            }
        }
    }
}