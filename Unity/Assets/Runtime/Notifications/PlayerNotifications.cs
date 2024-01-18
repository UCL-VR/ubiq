using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.XR.Notifications
{
    /// <summary>
    /// The PlayerNotifications class maintains a global callback allowing any component to give
    /// important notifications or warnings to Players in the scene.
    /// Any component can register to receive notifications, allowing full control over how they are
    /// displayed. The PlayerNotificationsCanvas component is used on the default Player prefab.
    /// PlayerNotifications should be used only when absolutely required - gamebreaking conditions or
    /// similar - as they cannot be dismissed by the user, and the caller has no control over how they 
    /// are displayed.
    /// </summary>
    /// <remarks>
    /// Notifications are only sent locally.
    /// </remarks>
    public static class PlayerNotifications
    {
        public delegate void NotificationDelegate(Notification notification);

        public static event NotificationDelegate OnNotification;

        public static T Show<T>(T notification) where T : Notification
        {
            if(OnNotification != null)
            {
                OnNotification.Invoke(notification);
            }
            return notification;
        }

        public static void Delete<T>(ref T notification) where T: Notification
        {
            if (notification != null)
            {
                notification.Delete();
                notification = null;
            }
        }
    }

    public class Notification
    {
        public virtual string Message { get; protected set; }
        public bool Deleted;

        public void Delete()
        {
            Deleted = true;
        }

        public Notification(string message)
        {
            Message = message;
        }

        public Notification()
        {
        }
    }

}
