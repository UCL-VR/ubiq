using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.XR.Notifications
{
    /// <summary>
    /// Shows any notifications to the player
    /// </summary>
    public class PlayerNotificationsCanvas : MonoBehaviour
    {
        public Canvas Canvas;
        public Text Messages;

        private List<Notification> notifications;
        private List<Notification> deleted;

        private void Awake()
        {
            notifications = new List<Notification>();
            deleted = new List<Notification>();
            PlayerNotifications.OnNotification += OnNotification;
        }

        void OnNotification(Notification notification)
        {
            notifications.Add(notification);
            enabled = true;
        }

        // Start is called before the first frame update
        void Start()
        {
            Canvas.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            Canvas.gameObject.SetActive(true);

            foreach (var item in notifications)
            {
                if(item.Deleted)
                {
                    deleted.Add(item);
                }
            }

            foreach (var item in deleted)
            {
                notifications.Remove(item);
            }

            deleted.Clear();

            string content = "";
            foreach (var item in notifications)
            {
                if (content.Length > 0)
                {
                    content += "\n";
                }
                content += item.Message;
            }
            Messages.text = content;

            if(notifications.Count <= 0)
            {
                Canvas.gameObject.SetActive(false);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            PlayerNotifications.OnNotification -= OnNotification;
        }
    }
}