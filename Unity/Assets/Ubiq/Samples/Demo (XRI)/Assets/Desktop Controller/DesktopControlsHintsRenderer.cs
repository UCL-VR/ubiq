using Org.BouncyCastle.Asn1.IsisMtt.X509;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Ubiq.Geometry.Query;

namespace Ubiq.Samples
{
    [ExecuteInEditMode]
    public class DesktopControlsHintsRenderer : MonoBehaviour
    {
        public Mesh Mesh;
        public Material Black;
        public Material White;

#pragma warning disable 0108
        private CanvasRenderer renderer;
#pragma warning restore 0108

        private Vector3[] corners;

        private Mesh mesh;

        private void Awake()
        {
            renderer = GetComponent<CanvasRenderer>();
        }

        void Update()
        {
            if (renderer.hasMoved)
            {
                var rect = transform as RectTransform;

                if (corners == null)
                {
                    corners = new Vector3[4];
                }

                rect.GetLocalCorners(corners);

                var width = corners[3].x - corners[0].x;
                var height = corners[1].y - corners[0].y;

                var meshWidth = Mesh.bounds.size.x;
                var meshHeight = Mesh.bounds.size.y;

                var scaleX = meshWidth / width;
                var scaleY = meshHeight / height;

                var scale = Mathf.Max(scaleX, scaleY);

                var vertices = Mesh.vertices;

                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = vertices[i] / scale;
                    vertices[i].z = 0;
                }

                var mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.subMeshCount = 2;
                mesh.SetTriangles(Mesh.GetTriangles(0), 0);
                mesh.SetTriangles(Mesh.GetTriangles(1), 1);

                CanvasRenderer cr = GetComponent<CanvasRenderer>();
                cr.materialCount = 2;
                cr.SetMaterial(Black, 0);
                cr.SetMaterial(White, 1);
                cr.SetMesh(mesh);
            }
        }
    }
}