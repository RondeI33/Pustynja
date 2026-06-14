using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DesertScene
{
    public static class DesertMarchingCubes
    {
        private static readonly Vector3Int[] CubeCornerOffsets =
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, 1, 1),
            new Vector3Int(0, 1, 1)
        };

        private static readonly int[,] Tetrahedra =
        {
            { 0, 5, 1, 6 },
            { 0, 1, 2, 6 },
            { 0, 2, 3, 6 },
            { 0, 3, 7, 6 },
            { 0, 7, 4, 6 },
            { 0, 4, 5, 6 }
        };

        private static readonly int[,] TetrahedronEdges =
        {
            { 0, 1 },
            { 0, 2 },
            { 0, 3 },
            { 1, 2 },
            { 1, 3 },
            { 2, 3 }
        };

        public static Mesh BuildMesh(
            DesertScalarField field,
            Vector2 terrainSize,
            float heightMultiplier,
            int resolution,
            float isoLevel,
            Vector2 textureTiling,
            bool smoothTileEdges)
        {
            terrainSize.x = Mathf.Max(1f, terrainSize.x);
            terrainSize.y = Mathf.Max(1f, terrainSize.y);
            heightMultiplier = Mathf.Max(0.1f, heightMultiplier);
            resolution = Mathf.Clamp(resolution, 4, 96);

            int xCells = resolution;
            int zCells = resolution;
            int yCells = Mathf.Clamp(Mathf.CeilToInt(resolution * heightMultiplier / Mathf.Max(terrainSize.x, terrainSize.y)), 4, resolution);

            Vector3 cellSize = new Vector3(
                terrainSize.x / xCells,
                heightMultiplier / yCells,
                terrainSize.y / zCells);

            Vector3 origin = new Vector3(-terrainSize.x * 0.5f, 0f, -terrainSize.y * 0.5f);
            MeshBuilder builder = new MeshBuilder(terrainSize, textureTiling);

            Vector3[] cornerPositions = new Vector3[8];
            float[] cornerDensities = new float[8];

            for (int x = 0; x < xCells; x++)
            {
                for (int y = 0; y < yCells; y++)
                {
                    for (int z = 0; z < zCells; z++)
                    {
                        Vector3 cubeOrigin = origin + new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z);

                        for (int i = 0; i < CubeCornerOffsets.Length; i++)
                        {
                            Vector3Int offset = CubeCornerOffsets[i];
                            Vector3 position = cubeOrigin + Vector3.Scale((Vector3)offset, cellSize);
                            cornerPositions[i] = position;
                            cornerDensities[i] = field.SampleDensity(position);
                        }

                        for (int tetrahedron = 0; tetrahedron < Tetrahedra.GetLength(0); tetrahedron++)
                            PolygonizeTetrahedron(cornerPositions, cornerDensities, tetrahedron, isoLevel, builder);
                    }
                }
            }

            Mesh mesh = new Mesh
            {
                name = "Generated Desert Terrain",
                indexFormat = builder.Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };

            mesh.SetVertices(builder.Vertices);
            mesh.SetUVs(0, builder.Uvs);
            mesh.SetTriangles(builder.Triangles, 0);
            mesh.RecalculateNormals();
            if (smoothTileEdges)
                SmoothTileEdgeNormals(mesh, terrainSize);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void SmoothTileEdgeNormals(Mesh mesh, Vector2 terrainSize)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            AverageOppositeEdgeNormals(vertices, normals, true, terrainSize);
            AverageOppositeEdgeNormals(vertices, normals, false, terrainSize);

            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.Normalize(Vector3.Lerp(Vector3.up, normals[i], 0.65f));

            mesh.normals = normals;
        }

        private static void AverageOppositeEdgeNormals(Vector3[] vertices, Vector3[] normals, bool xAxis, Vector2 terrainSize)
        {
            float min = xAxis ? -terrainSize.x * 0.5f : -terrainSize.y * 0.5f;
            float max = xAxis ? terrainSize.x * 0.5f : terrainSize.y * 0.5f;
            float tolerance = Mathf.Max(0.001f, (max - min) * 0.0001f);
            Dictionary<EdgeNormalKey, List<int>> edgeGroups = new Dictionary<EdgeNormalKey, List<int>>();

            for (int i = 0; i < vertices.Length; i++)
            {
                float edgeCoordinate = xAxis ? vertices[i].x : vertices[i].z;
                if (Mathf.Abs(edgeCoordinate - min) > tolerance && Mathf.Abs(edgeCoordinate - max) > tolerance)
                    continue;

                EdgeNormalKey key = xAxis
                    ? new EdgeNormalKey(vertices[i].y, vertices[i].z)
                    : new EdgeNormalKey(vertices[i].x, vertices[i].y);

                if (!edgeGroups.TryGetValue(key, out List<int> indices))
                {
                    indices = new List<int>();
                    edgeGroups.Add(key, indices);
                }

                indices.Add(i);
            }

            foreach (List<int> indices in edgeGroups.Values)
            {
                if (indices.Count < 2)
                    continue;

                Vector3 normal = Vector3.zero;
                for (int i = 0; i < indices.Count; i++)
                    normal += normals[indices[i]];

                normal.Normalize();

                for (int i = 0; i < indices.Count; i++)
                    normals[indices[i]] = normal;
            }
        }

        private static void PolygonizeTetrahedron(
            Vector3[] cubePositions,
            float[] cubeDensities,
            int tetrahedronIndex,
            float isoLevel,
            MeshBuilder builder)
        {
            Vector3[] positions = new Vector3[4];
            float[] densities = new float[4];
            bool[] inside = new bool[4];

            int insideCount = 0;
            for (int i = 0; i < 4; i++)
            {
                int cubeIndex = Tetrahedra[tetrahedronIndex, i];
                positions[i] = cubePositions[cubeIndex];
                densities[i] = cubeDensities[cubeIndex];
                inside[i] = densities[i] >= isoLevel;

                if (inside[i])
                    insideCount++;
            }

            if (insideCount == 0 || insideCount == 4)
                return;

            Vector3[] crossings = new Vector3[4];
            int crossingCount = 0;

            for (int edge = 0; edge < TetrahedronEdges.GetLength(0); edge++)
            {
                int a = TetrahedronEdges[edge, 0];
                int b = TetrahedronEdges[edge, 1];

                if (inside[a] == inside[b])
                    continue;

                crossings[crossingCount] = Interpolate(positions[a], positions[b], densities[a], densities[b], isoLevel);
                crossingCount++;
            }

            if (crossingCount < 3)
                return;

            Vector3 normalHint = CalculateNormalHint(positions, inside);
            SortPolygon(crossings, crossingCount, normalHint);

            AddOrientedTriangle(crossings[0], crossings[1], crossings[2], normalHint, builder);

            if (crossingCount == 4)
                AddOrientedTriangle(crossings[0], crossings[2], crossings[3], normalHint, builder);
        }

        private static Vector3 Interpolate(Vector3 a, Vector3 b, float valueA, float valueB, float isoLevel)
        {
            float difference = valueB - valueA;
            if (Mathf.Abs(difference) < 0.00001f)
                return (a + b) * 0.5f;

            float t = Mathf.Clamp01((isoLevel - valueA) / difference);
            return Vector3.Lerp(a, b, t);
        }

        private static Vector3 CalculateNormalHint(Vector3[] positions, bool[] inside)
        {
            Vector3 insideCenter = Vector3.zero;
            Vector3 outsideCenter = Vector3.zero;
            int insideCount = 0;
            int outsideCount = 0;

            for (int i = 0; i < positions.Length; i++)
            {
                if (inside[i])
                {
                    insideCenter += positions[i];
                    insideCount++;
                }
                else
                {
                    outsideCenter += positions[i];
                    outsideCount++;
                }
            }

            if (insideCount > 0)
                insideCenter /= insideCount;

            if (outsideCount > 0)
                outsideCenter /= outsideCount;

            Vector3 normal = outsideCenter - insideCenter;
            return normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        }

        private static void SortPolygon(Vector3[] points, int count, Vector3 normal)
        {
            Vector3 center = Vector3.zero;
            for (int i = 0; i < count; i++)
                center += points[i];

            center /= count;

            Vector3 axisA = Vector3.Cross(normal, Vector3.up);
            if (axisA.sqrMagnitude < 0.0001f)
                axisA = Vector3.Cross(normal, Vector3.right);

            axisA.Normalize();
            Vector3 axisB = Vector3.Cross(normal, axisA).normalized;
            float[] angles = new float[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = points[i] - center;
                angles[i] = Mathf.Atan2(Vector3.Dot(direction, axisB), Vector3.Dot(direction, axisA));
            }

            for (int i = 0; i < count - 1; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (angles[j] >= angles[i])
                        continue;

                    (angles[i], angles[j]) = (angles[j], angles[i]);
                    (points[i], points[j]) = (points[j], points[i]);
                }
            }
        }

        private static void AddOrientedTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normalHint, MeshBuilder builder)
        {
            Vector3 triangleNormal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(triangleNormal, normalHint) < 0f)
                builder.AddTriangle(a, c, b);
            else
                builder.AddTriangle(a, b, c);
        }

        private sealed class MeshBuilder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            private readonly Dictionary<VertexKey, int> vertexLookup = new Dictionary<VertexKey, int>();
            private readonly Vector2 terrainSize;
            private readonly Vector2 textureTiling;

            public MeshBuilder(Vector2 terrainSize, Vector2 textureTiling)
            {
                this.terrainSize = terrainSize;
                this.textureTiling = textureTiling;
            }

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                Triangles.Add(AddVertex(a));
                Triangles.Add(AddVertex(b));
                Triangles.Add(AddVertex(c));
            }

            private int AddVertex(Vector3 position)
            {
                VertexKey key = new VertexKey(position);

                if (vertexLookup.TryGetValue(key, out int existingIndex))
                    return existingIndex;

                int index = Vertices.Count;
                vertexLookup.Add(key, index);
                Vertices.Add(position);

                float u = (position.x / terrainSize.x + 0.5f) * textureTiling.x;
                float v = (position.z / terrainSize.y + 0.5f) * textureTiling.y;
                Uvs.Add(new Vector2(u, v));

                return index;
            }
        }

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            private readonly int x;
            private readonly int y;
            private readonly int z;

            public VertexKey(Vector3 position)
            {
                const float precision = 10000f;
                x = Mathf.RoundToInt(position.x * precision);
                y = Mathf.RoundToInt(position.y * precision);
                z = Mathf.RoundToInt(position.z * precision);
            }

            public bool Equals(VertexKey other)
            {
                return x == other.x && y == other.y && z == other.z;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = x;
                    hash = hash * 397 ^ y;
                    hash = hash * 397 ^ z;
                    return hash;
                }
            }
        }

        private readonly struct EdgeNormalKey : IEquatable<EdgeNormalKey>
        {
            private readonly int a;
            private readonly int b;

            public EdgeNormalKey(float a, float b)
            {
                const float precision = 10000f;
                this.a = Mathf.RoundToInt(a * precision);
                this.b = Mathf.RoundToInt(b * precision);
            }

            public bool Equals(EdgeNormalKey other)
            {
                return a == other.a && b == other.b;
            }

            public override bool Equals(object obj)
            {
                return obj is EdgeNormalKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (a * 397) ^ b;
                }
            }
        }

    }
}
