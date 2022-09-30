using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Guids;
using UnityEngine;

namespace Ubiq.Rooms.Spatial
{
    public class HexGridPartition : SpatialPartition
    {
        public int CellSize;
        public int NumNeighbours;

        // The Axial Coordinate definition and conversions are taken from https://www.redblobgames.com/grids/hexagons/

        // These are the Basis Vectors for the Axial Coordinate System. They correspond to the primary axes of the hex grid, at 0, 60 degrees.
        // They are perpendicular to the flat edges.
        static Vector3 bq = Vector3.right * Mathf.Sqrt(3);
        static Vector3 br = Quaternion.Euler(0, 60, 0) * bq;

        // These are the Basis Vectors, scaled to match the actual cell size 
        Vector3 Q => bq * CellSize;
        Vector3 R => br * CellSize;

        private List<Vector2Int> cellsList = new List<Vector2Int>(); // A temporary list for use by GetRooms to avoid allocating a new one each call.

        // These are points that define the corners of a Unit Hexagon in Cartesian Space.
        // The last corner is duplicated to make it easier to draw a complete hexagon through a series of lines
        static Vector3[] Corners = { 
            Quaternion.Euler(0, 30,  0) * Vector3.right,
            Quaternion.Euler(0, 90,  0) * Vector3.right,
            Quaternion.Euler(0, 150, 0) * Vector3.right,
            Quaternion.Euler(0, 210, 0) * Vector3.right,
            Quaternion.Euler(0, 270, 0) * Vector3.right,
            Quaternion.Euler(0, 330, 0) * Vector3.right,
            Quaternion.Euler(0, 30,  0) * Vector3.right
        };

        // The directions go clockwise starting from the Q basis vector (right)
        static Vector2[] HexDirections = { 
            new Vector2(1,0),
            new Vector2(1,-1),
            new Vector2(0,-1),
            new Vector2(-1,0),
            new Vector2(-1,1),
            new Vector2(0,1)
        };

        public override void GetRooms(Vector3 position, SpatialState state)
        {
            var origin = PointToCell(position);

            state.Member = Guids.Guids.Generate(state.Shard, origin);

            cellsList.Clear();
            state.Observed.Clear();
            HexNeighbours(origin, NumNeighbours, cellsList);
            foreach (var cell in cellsList)
            {
                state.Observed.Add(Guids.Guids.Generate(state.Shard, cell));
            }
        }

        /// <summary>
        /// Returns the position of the cell in Cartesian Coordinates
        /// </summary>
        private Vector3 GetHexagonCenter(Vector2 cell)
        {
            return cell.x * Q + cell.y * R;
        }

        /// <summary>
        /// Returns the Cell in Cube Coordinates containing the Position in Cartesian Space
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2Int PointToCell(Vector3 position)
        {
            return RoundHex(PointToHex(position));
        }

        /// <summary>
        /// Returns the Cube Coordinates of the Position in Cartesian Space
        /// </summary>
        private Vector2 PointToHex(Vector3 position)
        {
            var q = (Mathf.Sqrt(3) / 3f * position.x - 1f / 3f * -position.z) / CellSize;
            var r = -(2f / 3f * position.z) / CellSize;
            return new Vector2(q, r);
        }

        private Vector2 HexNeighbour(Vector2 hex, int direction)
        {
            return hex + HexDirections[direction];
        }

        /// <summary>
        /// Returns all the cells within a given radius (a spiral) in Cube Coordinates. This does not include the origin cell.
        /// </summary>
        private void HexNeighbours(Vector2 hex, int radius, List<Vector2Int> neighbours)
        {
            for (int i = 1; i <= radius; i++)
            {
                HexRing(hex, i, neighbours);
            }
        }

        private void HexRing(Vector2 center, int radius, List<Vector2Int> neighbours)
        {
            var start = center + HexDirections[4] * radius;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < radius; j++)
                {
                    neighbours.Add(new Vector2Int((int)start.x, (int)start.y));
                    start = HexNeighbour(start, i);
                }
            }
        }

        private Vector2Int RoundHex(Vector2 hex)
        {
            var x = hex.x;
            var z = hex.y;
            var y = -x - z;

            var rx = Mathf.Round(x);
            var ry = Mathf.Round(y);
            var rz = Mathf.Round(z);

            var x_diff = Mathf.Abs(rx - x);
            var y_diff = Mathf.Abs(ry - y);
            var z_diff = Mathf.Abs(rz - z);

            if (x_diff > y_diff && x_diff > z_diff)
            {
                rx = -ry - rz;
            }
            else if (y_diff > z_diff)
            { 
                ry = -rx - rz;
            }
            else
            {
                rz = -rx - ry;
            }

            return new Vector2Int((int)rx, (int)rz);
        }

        private List<Vector2Int> GizmoCells = new List<Vector2Int>();

        private void OnDrawGizmos()
        {
            // Draw the basis vectors
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Vector3.zero, Q);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, R);

            Gizmos.color = Color.green;

            GizmoCells.Clear();

            HexNeighbours(Vector2.zero, 10, GizmoCells);

            foreach (var item in GizmoCells)
            {
                DrawHexGizmo(item);
            }

            var cell = PointToHex(transform.position);
            Gizmos.DrawSphere(GetHexagonCenter(cell), 0.1f);
            Gizmos.color = Color.red;
            DrawHexGizmo(RoundHex(cell));
        }

        /// <summary>
        /// Draw the Hex cell at the location given in Cube Coordinates
        /// </summary>
        private void DrawHexGizmo(Vector2 cell)
        {
            for (int i = 0; i < Corners.Length - 1; i++)
            {
                var Offset = GetHexagonCenter(cell);

                Gizmos.DrawSphere(Offset, 0.1f);

                var From = (Corners[i] * CellSize) + Offset;
                var To = (Corners[i + 1] * CellSize) + Offset;
                Gizmos.DrawLine(From, To);
            }
        }
    }
}
