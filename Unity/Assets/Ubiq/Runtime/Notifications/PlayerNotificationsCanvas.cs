using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Ubiq.XR.Notifications
{
    /// <summary>
    /// Shows any notifications to the player
    /// </summary>
    public class PlayerNotificationsCanvas : MonoBehaviour
    {
        [Tooltip("The camera which will show notifications. Defaults to the camera with the MainCamera tag.")]
        public Camera notificationCamera;
        
        private Canvas canvas;
        private Text messages;
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
            if (!notificationCamera)
            {
                notificationCamera = Camera.main;
                if (!notificationCamera)
                {
                    Debug.LogWarning("No Camera supplied and no camera found " +
                                     " with the MainCamera tag in this" +
                                     " Unity scene. Notifications will not" +
                                     " be shown.");
                    enabled = false;
                    return;
                }
            }
            
            canvas = GetComponentInChildren<Canvas>(includeInactive:true);
            canvas.worldCamera = notificationCamera;
            canvas.gameObject.SetActive(false);
            
            messages = GetComponentInChildren<Text>(includeInactive:true);
        }

        // Update is called once per frame
        void Update()
        {
            canvas.gameObject.SetActive(true);

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
            messages.text = content;

            if(notifications.Count <= 0)
            {
                canvas.gameObject.SetActive(false);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            PlayerNotifications.OnNotification -= OnNotification;
        }
    }
}