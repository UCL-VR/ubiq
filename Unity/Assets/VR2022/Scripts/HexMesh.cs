using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	public Mesh hexMesh;
	List<Vector3> vertices;
	List<int> triangles;

    HexCell cell;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		hexMesh.name = "Hex Mesh";
		vertices = new List<Vector3>();
		triangles = new List<int>();

		// tag = "Teleport";

		MeshCollider collider = gameObject.AddComponent<MeshCollider>();
		collider.sharedMesh = hexMesh;
		collider.enabled = true;
	}


    public void Triangulate(HexCell c, HexGrid hexGrid)
    {
        cell = c;
        hexMesh.Clear();
		vertices.Clear();
		triangles.Clear();

        Vector3 center = cell.transform.localPosition;
        for(int i = 0; i < 6; i++)
        {
            AddTriangle(Vector3.zero,
			            hexGrid.corners[i],
			            hexGrid.corners[i+1]);
        }

        hexMesh.vertices = vertices.ToArray();
		hexMesh.triangles = triangles.ToArray();
		hexMesh.RecalculateNormals();

        // var renderer = GetComponent<MeshRenderer>();
        // renderer.enabled = false;
    }

    void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) 
    {
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}
}
