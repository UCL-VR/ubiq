#if READYPLAYERME_0_0_0_OR_NEWER
using UnityEngine;
using UnityEngine.UI;
using ReadyPlayerMe.AvatarLoader;
using Ubiq.Avatars;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Pose = UnityEngine.Pose;

namespace Ubiq.ReadyPlayerMe
{
    /// <summary>
    /// Quick sample script to show loading status and visualize avatars.
    /// </summary>
    public class UbiqReadyPlayerMeExample : MonoBehaviour
    {
        private class HeadAndHandsInput : IHeadAndHandsInput
        {
            public int priority => 0;
            public bool active => owner.isActiveAndEnabled;
            public InputVar<Pose> head => 
                new (new Pose(new Vector3(0.0f,1.3f,0.0f),Quaternion.identity));
            public InputVar<Pose> leftHand =>
                new (new Pose(new Vector3(-0.25f,0.8f,0.2f),Quaternion.identity));
            public InputVar<Pose> rightHand =>
                new (new Pose(new Vector3(0.25f,0.8f,0.2f),Quaternion.identity));
            public InputVar<float> leftGrip => InputVar<float>.invalid;
            public InputVar<float> rightGrip => InputVar<float>.invalid;
            
            private UbiqReadyPlayerMeExample owner;
            
            public HeadAndHandsInput(UbiqReadyPlayerMeExample owner)
            {
                this.owner = owner;
            }
        }
                
        private UbiqReadyPlayerMeLoader loader;
        private Text argsText;
        private AvatarManager avatarManager;
        
        const string EXPLAINER_SUFFIX = "\n\nSee README in sample folder for instructions on changing model or using in Ubiq";
        
        private IHeadAndHandsInput headAndHandsInput;
        
        private void Start()
        {
            var canvas = GameObject.Find("Canvas");
            canvas.transform.Find("Edit Mode").gameObject.SetActive(false);
            canvas.transform.Find("Play Mode").gameObject.SetActive(true);
        
            argsText = canvas.transform.Find("Play Mode/Status Args Text").GetComponent<Text>();
            argsText.text = "Waiting for avatar to load...";
            
            if (!avatarManager)
            {
                avatarManager = FindObjectOfType<AvatarManager>();
                if (!avatarManager)
                {
                    Debug.LogWarning("No NetworkScene could be found in this Unity scene. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            headAndHandsInput = new HeadAndHandsInput(this);
            avatarManager.input.Add(headAndHandsInput);
        }
        
        private void Update()
        {
            if (!loader)
            {
                loader = FindObjectOfType<UbiqReadyPlayerMeLoader>();
                if (loader)
                {
                    loader.completed.AddListener(Loader_Completed);
                    loader.failed.AddListener(Loader_Failed);
                }
            }
            
            if (loader && loader.isLoaded)
            {
                Camera.main.transform.RotateAround(Vector3.zero,Vector3.up,Time.deltaTime*45);
            }
        }
        
        private void OnDestroy()
        {
            if (loader)
            {
                loader.completed.RemoveListener(Loader_Completed);
                loader.failed.RemoveListener(Loader_Failed);
            }
        }
        
        private void Loader_Completed(CompletionEventArgs args)
        {
            argsText.text = args.Url + EXPLAINER_SUFFIX;
        }
        
        private void Loader_Failed(FailureEventArgs args)
        {
            argsText.text = args.Message;
        }
    }
}
#endif