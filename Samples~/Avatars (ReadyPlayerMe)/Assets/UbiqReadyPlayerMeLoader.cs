#if READYPLAYERME_0_0_0_OR_NEWER
using ReadyPlayerMe.AvatarLoader;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.ReadyPlayerMe
{
    public class UbiqReadyPlayerMeLoader : MonoBehaviour
    {
        public AvatarConfig avatarConfig;
        public bool loadOnStart;
        
        public string loadedUrl => isLoaded ? avatarUrl : null;
        public CompletionEventArgs loadedArgs => avatarArgs;
        public bool isLoaded => avatar != null;
        
        public class FailureEvent : UnityEvent<FailureEventArgs> {}
        public class CompletedEvent : UnityEvent<CompletionEventArgs> {}
        
        public FailureEvent failed = new FailureEvent();
        public CompletedEvent completed = new CompletedEvent();
        
        [SerializeField] private string avatarUrl = "https://api.readyplayer.me/v1/avatars/632d65e99b4c6a4352a9b8db.glb";
        private GameObject avatar;
        private CompletionEventArgs avatarArgs;
        private AvatarObjectLoader loader;
        
        private void Start()
        {
            if (loadOnStart)
            {
                Load(avatarUrl);
            }
        }
        
        private void OnDestroy()
        {
            Unload();
        }
        
        public void Unload()
        {
            if (avatar)
            {
                Destroy(avatar);
                avatar = null;
                avatarArgs = null;
            }
            
            if (loader != null)
            {
                loader.OnFailed -= Failed;
                loader.OnCompleted -= Completed;
                loader.Cancel();
                loader = null;
            }
        }
        
        public void Load(string url, bool clean = false)
        {
            if (!clean && loadedUrl == url)
            {
                return;
            }
        
            Unload();
        
            avatarUrl = url;
        
            if (url != null)
            {
                loader = new AvatarObjectLoader();
                loader.AvatarConfig = avatarConfig;
                loader.OnFailed += Failed;
                loader.OnCompleted += Completed;
                loader.LoadAvatar(avatarUrl);
            }
        }
        
        private void Failed(object sender, FailureEventArgs args)
        {
            Debug.LogError($"{args.Type} - {args.Message}");
            failed.Invoke(args);
        }
        
        private void Completed(object sender, CompletionEventArgs args)
        {
            avatarArgs = args;
        
            avatar = args.Avatar;
            avatar.transform.parent = transform;
            avatar.transform.localPosition = Vector3.zero;
            avatar.transform.localRotation = Quaternion.identity;
        
            AvatarAnimatorHelper.SetupAnimator(args.Metadata.BodyType, avatar);
        
            // Rename because otherwise the RPM loader will delete us if anyone
            // tries to use the same avatar
            args.Avatar.name = $"rpm-{args.Avatar.name}";
        
            completed.Invoke(args);
        }
    }
}
#endif
