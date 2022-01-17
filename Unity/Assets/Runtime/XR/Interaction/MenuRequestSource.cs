using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.XR
{
    /// <summary>
    /// Provides an interface to allow subscribers to interact with a menu without being aware of underlying platform.
    /// Could be invoked by keyboard, by menu button presses or events in the virtual environment.
    /// </summary>
    public class MenuRequestSource : MonoBehaviour
    {
        [System.Serializable]
        public class RequestEvent : UnityEvent<GameObject> { };

        public RequestEvent OnRequest;

        public void Request(GameObject requester)
        {
            OnRequest.Invoke(requester);
        }
    }
}
