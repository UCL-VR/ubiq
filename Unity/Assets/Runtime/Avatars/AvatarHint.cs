using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Extensions;
using UnityEngine;

namespace Ubiq.Avatars
{
    [Serializable]
    public struct PositionRotation
    {
        public static PositionRotation identity
        {
            get
            {
                return new PositionRotation
                {
                    position = Vector3.zero,
                    rotation = Quaternion.identity
                };
            }
        }

        public Vector3 position;
        public Quaternion rotation;

        public PositionRotation(Transform transform, bool local = false)
        {
            if (local)
            {
                this.position = transform.localPosition;
                this.rotation = transform.localRotation;
            }
            else
            {
                this.position = transform.position;
                this.rotation = transform.rotation;
            }
        }

    }

    public interface IAvatarHintProvider
    {
        AvatarHints.Node Node { get; }
        PositionRotation Provide();
    }

    // Provides static access to body part positions and input to guide avatar
    // position/rotation and animation.
    public class AvatarHint : MonoBehaviour, IAvatarHintProvider
    {
        public AvatarHints.Node node;

        public AvatarHints.Node Node { get => node; }

        void OnEnable ()
        {
            AvatarHints.AddProvider(node,this);
        }

        void OnDisable ()
        {
            AvatarHints.RemoveProvider(node,this);
        }

        public PositionRotation Provide()
        {
            return new PositionRotation (transform);
        }
    }

    public static class AvatarHints
    {
        public enum Node : int
        {
            Head = 0,
            LeftHand = 1,
            RightHand = 2,
            LeftWrist = 3,
            RightWrist = 4
        }

        /// <summary>
        /// Returns the closest hint provider in a branch
        /// </summary>
        public static IAvatarHintProvider Find(AvatarHints.Node node, MonoBehaviour avatar)
        {
            return avatar.GetClosestPredicate<IAvatarHintProvider>(hp =>
            {
                return hp.Node == node;
            });
        }

        private static Dictionary<Node,IAvatarHintProvider> providers;

        private static Dictionary<Node,IAvatarHintProvider> RequireProviders ()
        {
            if (providers == null)
            {
                providers = new Dictionary<Node, IAvatarHintProvider>();
            }
            return providers;
        }

        public static bool TryGet (Node node, out PositionRotation posRot)
        {
            RequireProviders();

            if (providers.TryGetValue(node,out IAvatarHintProvider provider))
            {
                posRot = provider.Provide();
                return true;
            }
            posRot = PositionRotation.identity;
            return false;
        }

        public static void AddProvider (Node node, IAvatarHintProvider provider)
        {
            RequireProviders();

            if (providers.ContainsKey(node))
            {
                // Silently ignoring subsequent providers now as this can happen
                // on scene reload due to (temporary) multiple player prefabs
                // Debug.LogError("Multiple AvatarHint providers for node: " + node
                //     + ", but only one can be used at any time. Ignoring.");
                return;
            }

            providers[node] = provider;
        }

        public static void RemoveProvider (Node node, IAvatarHintProvider provider)
        {
            RequireProviders();

            if (providers.TryGetValue(node,out IAvatarHintProvider extProvider)
                && extProvider == provider)
            {
                providers.Remove(node);
            }
        }
    }
}
