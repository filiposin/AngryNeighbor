using System;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	[Serializable]
	public class CombineInstanceIndexes
	{
		public int firstVertexIndex;

		public int vertexCount;

		public int firstTriangleIndex;

		public int triangleCount;

		public bool showCombinedInstanceIndex;

		public CombineInstanceIndexes(Mesh mesh, int vertexIndex, int trianglesIndex)
		{
			vertexCount = mesh.vertexCount;
			firstVertexIndex = vertexIndex;
			triangleCount = mesh.triangles.Length;
			firstTriangleIndex = trianglesIndex;
		}

		public void MoveIndexes(int vertexOffset_p, int triangleOffset_p)
		{
			firstVertexIndex -= vertexOffset_p;
			firstTriangleIndex -= triangleOffset_p;
		}
	}
}
