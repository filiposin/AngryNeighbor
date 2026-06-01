using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	[Serializable]
	public class MeshCombined
	{
		public List<string> names = new List<string>();

		public List<int> instanceIds = new List<int>();

		public List<CombineInstanceIndexes> indexes = new List<CombineInstanceIndexes>();

		public bool showMeshCombined;

		public Mesh RemoveMesh(int instanceID, Mesh mesh)
		{
			if (instanceIds.Contains(instanceID))
			{
				int num = instanceIds.IndexOf(instanceID);
				Vector3[] vertices = mesh.vertices;
				Vector3[] array = new Vector3[mesh.vertexCount - indexes[num].vertexCount];
				int[] triangles = mesh.triangles;
				int[] array2 = new int[triangles.Length - indexes[num].triangleCount];
				Vector4[] tangents = mesh.tangents;
				Vector4[] array3 = new Vector4[mesh.tangents.Length - indexes[num].vertexCount];
				Vector2[] uv = mesh.uv;
				Vector2[] array4 = new Vector2[array.Length];
				Vector2[] uv2 = mesh.uv2;
				Vector2[] array5 = new Vector2[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					if (i < indexes[num].firstVertexIndex)
					{
						array[i] = vertices[i];
						array4[i] = uv[i];
						array5[i] = uv2[i];
						array3[i] = tangents[i];
					}
					else
					{
						array[i] = vertices[i + indexes[num].vertexCount];
						array4[i] = uv[i + indexes[num].vertexCount];
						array5[i] = uv2[i + indexes[num].vertexCount];
						array3[i] = tangents[i + indexes[num].vertexCount];
					}
				}
				for (int j = 0; j < array2.Length; j++)
				{
					if (j < indexes[num].firstTriangleIndex)
					{
						array2[j] = triangles[j];
					}
					else
					{
						array2[j] = triangles[j + indexes[num].triangleCount] - indexes[num].vertexCount;
					}
				}
				for (int k = num; k < indexes.Count; k++)
				{
					indexes[k].MoveIndexes(indexes[num].vertexCount, indexes[num].triangleCount);
				}
				indexes.RemoveAt(num);
				instanceIds.RemoveAt(num);
				names.RemoveAt(num);
				mesh.Clear();
				mesh.vertices = new List<Vector3>(array).ToArray();
				mesh.SetTriangles(array2, 0);
				mesh.tangents = array3;
				mesh.uv = array4;
				mesh.uv2 = array5;
				mesh.RecalculateBounds();
				mesh.RecalculateNormals();
			}
			return mesh;
		}
	}
}
