using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;
using System.Linq;
using Ubiq.XR;

namespace Ubiq.XR
{
    [RequireComponent(typeof(LineRenderer))]
    public class TeleportRay : MonoBehaviour
    {
        [Serializable]
        public class TeleportEvent : UnityEvent<Vector3>
        { }

        public TeleportEvent OnTeleport;

        [HideInInspector]
        public Vector3 teleportLocation;

        [HideInInspector]
        public bool teleportLocationValid;

        public bool isTeleporting;

        private new LineRenderer renderer;

        private readonly float range = 8f;
        private readonly float curve = 20f;
        private readonly int segments = 50;

        private Color validColour = new Color(0f, 1f, 0f, 0.4f);
        private Color collisionColour = new Color(1f, 1f, 0f, 0.4f);
        private Color invalidColour = new Color(1f, 0f, 0f, 0.4f);

        private void Awake()
        {
            renderer = GetComponent<LineRenderer>();
            renderer.useWorldSpace = true;

            if (OnTeleport == null)
            {
                OnTeleport = new TeleportEvent();
            }
        }

        private void Start()
        {
            foreach (IPrimaryButtonProvider item in GetComponentsInParent<MonoBehaviour>().Where(c => c is IPrimaryButtonProvider))
            {
                item.PrimaryButtonPress.AddListener(UpdateTeleport);
            }
        }

        public void UpdateTeleport(bool teleporterActivation)
        {
            if(teleporterActivation)
            {
                isTeleporting = true;
            }
            else
            {
                if(teleportLocationValid)
                {
                    OnTeleport.Invoke(teleportLocation);
                }

                isTeleporting = false;
            }
        }

        private void Update()
        {
            if(isTeleporting)
            {
                ComputeArc();
                renderer.enabled = true;
            }
            else
            {
                renderer.enabled = false;
            }
        }

        private void ComputeArc()
        {
            // Compute an arc using Eulers method, inspired by Valve's teleporter.

            teleportLocationValid = false;
            renderer.sharedMaterial.color = invalidColour;

            var positions = new Vector3[segments];

            RaycastHit raycasthitinfo;

            var numsegments = 1;
            var step = 1f / segments;
            var dy = Vector3.down * curve;
            var velocity = transform.forward * range;
            positions[0] = transform.position;
            for (int i = 1; i < segments; i++)
            {
                positions[i] = positions[i - 1] + (velocity * step);
                velocity += (dy * step);

                numsegments++;

                if (Physics.Linecast(positions[i-1], positions[i], out raycasthitinfo))
                {
                    if (raycasthitinfo.collider.CompareTag("Teleport"))
                    {
                        positions[i] = raycasthitinfo.point;
                        teleportLocation = raycasthitinfo.point;
                        teleportLocationValid = true;
                        break;
                    }

                    renderer.sharedMaterial.color = collisionColour;
                }
            }

            renderer.positionCount = numsegments;
            renderer.SetPositions(positions);

            renderer.startWidth = 0.01f;
            renderer.endWidth = 0.01f;

            if (teleportLocationValid)
            {
                renderer.sharedMaterial.color = validColour;
            }
        }
    }
}
