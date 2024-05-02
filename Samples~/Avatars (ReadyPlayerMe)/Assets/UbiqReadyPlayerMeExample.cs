#if READYPLAYERME_0_0_0_OR_NEWER
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using ReadyPlayerMe.AvatarLoader;
using Ubiq.Avatars;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Ubiq.ReadyPlayerMe
{
    // Quick sample script to show loading status and visualize avatars
    public class UbiqReadyPlayerMeExample : AvatarHintProvider
    {
        private UbiqReadyPlayerMeLoader loader;
        private Text argsText;
        private AvatarManager avatarManager;
        
        const string HEAD_POSITION_NODE = "HeadPosition";
        const string HEAD_ROTATION_NODE = "HeadRotation";
        const string LEFT_HAND_POSITION_NODE = "LeftHandPosition";
        const string LEFT_HAND_ROTATION_NODE = "LeftHandRotation";
        const string RIGHT_HAND_POSITION_NODE = "RightHandPosition";
        const string RIGHT_HAND_ROTATION_NODE = "RightHandRotation";
        
        const string EXPLAINER_SUFFIX = "\n\nSee README in sample folder for instructions on changing model or using in Ubiq";
        
        private void Start()
        {
            var canvas = GameObject.Find("Canvas");
            canvas.transform.Find("Edit Mode").gameObject.SetActive(false);
            canvas.transform.Find("Play Mode").gameObject.SetActive(true);
        
            argsText = canvas.transform.Find("Play Mode/Status Args Text").GetComponent<Text>();
            argsText.text = "Waiting for avatar to load...";
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
            
            if (!avatarManager)
            {
                avatarManager = FindObjectOfType<AvatarManager>();
                if (avatarManager)
                {
                    avatarManager.hints.SetProvider(HEAD_POSITION_NODE,AvatarHints.Type.Vector3,this);
                    avatarManager.hints.SetProvider(HEAD_ROTATION_NODE,AvatarHints.Type.Quaternion,this);
                    avatarManager.hints.SetProvider(LEFT_HAND_POSITION_NODE,AvatarHints.Type.Vector3,this);
                    avatarManager.hints.SetProvider(LEFT_HAND_ROTATION_NODE,AvatarHints.Type.Quaternion,this);
                    avatarManager.hints.SetProvider(RIGHT_HAND_POSITION_NODE,AvatarHints.Type.Vector3,this);
                    avatarManager.hints.SetProvider(RIGHT_HAND_ROTATION_NODE,AvatarHints.Type.Quaternion,this);
                }
            }
            
            if (loader && loader.isLoaded)
            {
                Camera.main.transform.RotateAround(Vector3.zero,Vector3.up,Time.deltaTime*45);
            }
        }

        public override Vector3 ProvideVector3(string node)
        {
            return node switch
            {
                HEAD_POSITION_NODE => new Vector3(0.0f,1.3f,0.0f),
                LEFT_HAND_POSITION_NODE => new Vector3(-0.25f,0.8f,0.2f),
                RIGHT_HAND_POSITION_NODE => new Vector3(0.25f,0.8f,0.2f),
                _ => default
            };
        }
        
        public override Quaternion ProvideQuaternion(string node)
        {
            return node switch
            {
                HEAD_POSITION_NODE => Quaternion.identity,
                LEFT_HAND_POSITION_NODE => Quaternion.identity,
                RIGHT_HAND_POSITION_NODE => Quaternion.identity,
                _ => default
            };
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