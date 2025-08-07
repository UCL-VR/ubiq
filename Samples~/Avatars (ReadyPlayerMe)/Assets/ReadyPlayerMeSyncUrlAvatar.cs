using Ubiq.Rooms;
using Ubiq.ReadyPlayerMe;
using UnityEngine;

public class ReadyPlayerMeSyncUrlAvatar : MonoBehaviour
{
    private Ubiq.Avatars.Avatar avatar;
    private UbiqReadyPlayerMeLoader loader;

    private const string KEY = "avatars.readyplayerme.url"; 
    
    private void Start()
    {
        loader = GetComponentInChildren<UbiqReadyPlayerMeLoader>();
        avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        if (!avatar.IsLocal)
        {
            avatar.OnPeerUpdated.AddListener(Peer_OnPeerUpdated);
        }
    }
    
    private void OnDestroy()
    {
        if (avatar)
        {
            avatar.OnPeerUpdated.RemoveListener(Peer_OnPeerUpdated);
        }
    }
    
    public void SetUrl(string url)
    {
        if (!avatar.IsLocal)
        {
            Debug.LogWarning("SetUrl should not be called on remote avatars.");
        }
        
        // Can only set properties through RoomClient.Me, because an Avatar's 
        // peer could be remote.
        var roomClient = GetComponentInParent<RoomClient>();
        if (!roomClient)
        {
            Debug.LogWarning("Could not SetUrl - RoomClient could not be found.");
        }
        
        roomClient.Me[KEY] = url;
        UpdateLoader(url);
    }
    
    private void Peer_OnPeerUpdated(IPeer peer)
    {
        UpdateLoader(peer[KEY]);
    }
    
    private void UpdateLoader(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            // Loader expects null rather than empty string if no avatar
            // requested.
            url = null;
        }
        
        if (loader)
        {
            loader.Load(url);
        }
    }
}
