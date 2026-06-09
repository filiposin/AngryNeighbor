using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class SubmeshSplitter
	{
		public static MeshRenderer SplitSubmeshes(MeshFilter meshFilter, int[] submehesIndex, int index)
		{
			if (submehesIndex.Length == 0)
			{
				Debug.LogError(string.Concat("[Super Combiner] Could not split submeshes of mesh ", meshFilter, " because indexes is null"));
				return null;
			}
			if (meshFilter.sharedMesh == null)
			{
				Debug.LogError(string.Concat("[Super Combiner] Could not split submeshes of mesh ", meshFilter, " because it has no mesh"));
				return null;
			}
			Mesh sharedMesh = meshFilter.sharedMesh;
			Material[] sharedMaterials = meshFilter.GetComponent<MeshRenderer>().sharedMaterials;
			Material[] array = new Material[submehesIndex.Length];
			for (int i = 0; i < submehesIndex.Length; i++)
			{
				array[i] = sharedMaterials[submehesIndex[0]];
			}
			Mesh newMesh_p = CreateMeshFromSubmesh(sharedMesh, submehesIndex, index);
			GameObject gameObject = GenerateGameObject(meshFilter.transform, false, meshFilter.gameObject.name, newMesh_p, array);
			return gameObject.GetComponent<MeshRenderer>();
		}

		public static SkinnedMeshRenderer SplitSubmeshes(SkinnedMeshRenderer skinnedMesh, int[] submehesIndex, int index)
		{
			if (submehesIndex.Length == 0)
			{
				Debug.LogError(string.Concat("[Super Combiner] Could not split submeshes of mesh ", skinnedMesh, " because indexes is null"));
				return null;
			}
			if (skinnedMesh.sharedMesh == null)
			{
				Debug.LogError(string.Concat("[Super Combiner] Could not split submeshes of mesh ", skinnedMesh, " because it has no mesh"));
				return null;
			}
			Mesh sharedMesh = skinnedMesh.sharedMesh;
			Material[] sharedMaterials = skinnedMesh.sharedMaterials;
			Material[] array = new Material[submehesIndex.Length];
			for (int i = 0; i < submehesIndex.Length; i++)
			{
				array[i] = sharedMaterials[submehesIndex[0]];
			}
			Mesh newMesh_p = CreateMeshFromSubmesh(sharedMesh, submehesIndex, index);
			GameObject gameObject = GenerateGameObject(skinnedMesh.transform, true, skinnedMesh.gameObject.name, newMesh_p, array);
			return gameObject.GetComponent<SkinnedMeshRenderer>();
		}

		private static Mesh CreateMeshFromSubmesh(Mesh mesh, int[] submehesIndex, int index)
		{
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector4[] tangents = mesh.tangents;
			Color[] colors = mesh.colors;
			Vector2[] uv = mesh.uv;
			Vector2[] uv2 = mesh.uv2;
			Mesh mesh2 = new Mesh();
			mesh2.name = mesh.name + "_" + index;
			mesh2.vertices = vertices;
			mesh2.normals = normals;
			mesh2.tangents = tangents;
			mesh2.colors = colors;
			mesh2.uv = uv;
			mesh2.uv2 = uv2;
			for (int i = 0; i < submehesIndex.Length; i++)
			{
				int[] triangles = mesh.GetTriangles(submehesIndex[i]);
				int[] indices = mesh.GetIndices(submehesIndex[i]);
				MeshTopology topology = mesh.GetTopology(submehesIndex[i]);
				mesh2.SetIndices(indices, topology, i);
				mesh2.SetTriangles(triangles, i);
			}
			return mesh2;
		}

		private static GameObject GenerateGameObject(Transform parent, bool skinnedMesh, string name_p, Mesh newMesh_p, Material[] mat)
		{
			GameObject gameObject = new GameObject(name_p);
			gameObject.transform.SetParent(parent);
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = Vector3.one;
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.mesh = newMesh_p;
			if (skinnedMesh)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
				skinnedMeshRenderer.materials = mat;
			}
			else
			{
				MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
				meshRenderer.materials = mat;
			}
			return gameObject;
		}
	}
}
