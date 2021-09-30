using System;
using System.Collections;
using System.Collections.Generic;
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

    public interface IAvatarHintProvider<T>
    {
        T Provide ();
    }

    // Provides static access to body part positions and input to guide avatar
    // position/rotation and animation.
    public class AvatarHintPositionRotation : MonoBehaviour, IAvatarHintProvider<PositionRotation>
    {
        public AvatarHints.NodePosRot node;

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
        public enum NodePosRot
        {
            Head,
            LeftHand,
            RightHand,
            LeftWrist,
            RightWrist
        }

        public enum NodeFloat
        {
            LeftHandGrip,
            RightHandGrip
        }

        private static Dictionary<NodePosRot,IAvatarHintProvider<PositionRotation>> providersPosRot;
        private static Dictionary<NodeFloat, IAvatarHintProvider<float>> providersFloat;

        private static Dictionary<NodePosRot,IAvatarHintProvider<PositionRotation>> RequireProvidersPosRot ()
        {
            if (providersPosRot == null)
            {
                providersPosRot = new Dictionary<NodePosRot, IAvatarHintProvider<PositionRotation>>();
            }
            return providersPosRot;
        }

        private static Dictionary<NodeFloat, IAvatarHintProvider<float>> RequireProvidersFloat ()
        {
            if (providersFloat == null)
            {
                providersFloat = new Dictionary<NodeFloat, IAvatarHintProvider<float>>();
            }
            return providersFloat;
        }

        public static bool TryGet (NodePosRot node, out PositionRotation posRot)
        {
            RequireProvidersPosRot();

            if (providersPosRot.TryGetValue(node,out IAvatarHintProvider<PositionRotation> provider))
            {
                posRot = provider.Provide();
                return true;
            }
            posRot = PositionRotation.identity;
            return false;
        }
        public static bool TryGet (NodeFloat node, out float f)
        {
            RequireProvidersFloat();

            if (providersFloat.TryGetValue(node, out IAvatarHintProvider<float> provider))
            {
                f = provider.Provide();
                return true;
            }
            f = 0.0f;
            return false;
        }

        public static void AddProvider (NodePosRot node, IAvatarHintProvider<PositionRotation> provider)
        {
            RequireProvidersPosRot();

            if (providersPosRot.ContainsKey(node))
            {
                // Silently ignoring subsequent providers now as this can happen
                // on scene reload due to (temporary) multiple player prefabs
                // Debug.LogError("Multiple AvatarHint providers for node: " + node
                //     + ", but only one can be used at any time. Ignoring.");
                return;
            }

            providersPosRot[node] = provider;
        }

        public static void AddProvider (NodeFloat node, IAvatarHintProvider<float> provider)
        {
            RequireProvidersFloat();

            if (providersFloat.ContainsKey(node))
            {
                return;
            }

            providersFloat[node] = provider;
        }

        public static void RemoveProvider (NodePosRot node, IAvatarHintProvider<PositionRotation> provider)
        {
            RequireProvidersPosRot();

            if (providersPosRot.TryGetValue(node,out IAvatarHintProvider<PositionRotation> extProvider)
                && extProvider == provider)
            {
                providersPosRot.Remove(node);
            }
        }
        public static void RemoveProvider (NodeFloat node, IAvatarHintProvider<float> provider)
        {
            RequireProvidersFloat();

            if (providersFloat.TryGetValue(node, out IAvatarHintProvider<float> extProvider)
                && extProvider == provider)
            {
                providersFloat.Remove(node);
            }
        }
    }
}
