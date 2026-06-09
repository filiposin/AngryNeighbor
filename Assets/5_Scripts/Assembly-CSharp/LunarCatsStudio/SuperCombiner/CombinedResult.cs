using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class CombinedResult : ScriptableObject
	{
		public bool showCombinedMaterials;

		public bool showCombinedMeshes;

		public List<CombinedMaterial> combinedMaterials = new List<CombinedMaterial>();

		public List<Dictionary<int, MaterialToCombine>> originalMaterialList = new List<Dictionary<int, MaterialToCombine>>();

		public Dictionary<int, int> originalReferenceMaterial = new Dictionary<int, int>();

		public List<List<GameObject>> combinedGameObjectFromMeshList = new List<List<GameObject>>();

		public List<List<GameObject>> combinedGameObjectFromSkinnedMeshList = new List<List<GameObject>>();

		public List<MeshCombined> meshResults = new List<MeshCombined>();

		public int materialCombinedCount;

		public int combinedMaterialCount;

		public int meshesCombinedCount;

		public int skinnedMeshesCombinedCount;

		public int totalVertexCount;

		public int subMeshCount;

		public TimeSpan duration;

		public void Clear()
		{
			if (originalMaterialList != null)
			{
				for (int i = 0; i < originalMaterialList.Count; i++)
				{
					originalMaterialList[i].Clear();
				}
				originalMaterialList.Clear();
			}
			originalReferenceMaterial.Clear();
			materialCombinedCount = 0;
			combinedMaterials.Clear();
			combinedMaterialCount = 0;
			meshesCombinedCount = 0;
			skinnedMeshesCombinedCount = 0;
			totalVertexCount = 0;
			subMeshCount = 0;
			meshResults.Clear();
			combinedGameObjectFromMeshList.Clear();
			combinedGameObjectFromSkinnedMeshList.Clear();
		}

		public void SetCombinedMaterial(Material mat, int combinedIndex, bool isOriginal)
		{
			if (combinedIndex < combinedMaterials.Count)
			{
				combinedMaterials[combinedIndex].material = mat;
				if (!isOriginal)
				{
					Material material = combinedMaterials[combinedIndex].material;
					material.name = material.name + "_" + combinedMaterialCount;
				}
				combinedMaterials[combinedIndex].displayedIndex = combinedMaterialCount;
				combinedMaterials[combinedIndex].isOriginalMaterial = isOriginal;
			}
			combinedMaterialCount++;
		}

		public void AddNewCombinedMaterial()
		{
			combinedMaterials.Add(new CombinedMaterial());
		}

		public void AddMaterialToCombine(MaterialToCombine mat, int combinedIndex)
		{
			if (!originalReferenceMaterial.ContainsKey(combinedIndex))
			{
				originalReferenceMaterial.Add(combinedIndex, mat.material.GetInstanceID());
			}
			mat.index = originalMaterialList[combinedIndex].Count;
			originalMaterialList[combinedIndex].Add(mat.material.GetInstanceID(), mat);
		}

		public void AddCombinedMesh(Mesh combinedMesh, CombineInstanceID combineInstanceID, int combinedIndex)
		{
			MeshCombined meshCombined = new MeshCombined();
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < combineInstanceID.combineInstances.Count; i++)
			{
				if (!meshCombined.instanceIds.Contains(combineInstanceID.instancesID[i]))
				{
					num += combineInstanceID.combineInstances[i].mesh.vertexCount;
					num2 += combineInstanceID.combineInstances[i].mesh.triangles.Length;
					meshCombined.names.Add(combineInstanceID.names[i]);
					meshCombined.instanceIds.Add(combineInstanceID.instancesID[i]);
					meshCombined.indexes.Add(new CombineInstanceIndexes(combineInstanceID.combineInstances[i].mesh, num, num2));
				}
			}
			meshResults.Add(meshCombined);
		}

		public int FindCorrespondingMaterialIndex(Material matToFind, int combinedIndex)
		{
			if (combinedIndex < originalMaterialList.Count && originalMaterialList[combinedIndex].ContainsKey(matToFind.GetInstanceID()))
			{
				return originalMaterialList[combinedIndex][matToFind.GetInstanceID()].index;
			}
			Debug.LogWarning(string.Concat("[Super Combiner] Material ", matToFind, " was not found in list ", combinedIndex));
			return 0;
		}

		public Material GetCombinedMaterial(Material sourceMaterial)
		{
			for (int i = 0; i < originalMaterialList.Count; i++)
			{
				if (originalMaterialList[i].ContainsKey(sourceMaterial.GetInstanceID()))
				{
					return combinedMaterials[i].material;
				}
			}
			Debug.LogWarning("[Super Combiner] Could not find combined material associated with " + sourceMaterial.name);
			return null;
		}

		public int GetCombinedIndex(Material sourceMaterial)
		{
			for (int i = 0; i < originalMaterialList.Count; i++)
			{
				if (originalMaterialList[i].ContainsKey(sourceMaterial.GetInstanceID()))
				{
					return i;
				}
			}
			Debug.LogWarning("[Super Combiner] Could not find combined material associated with " + sourceMaterial.name);
			return 0;
		}

		public int GetCombinedIndexCount()
		{
			return originalMaterialList.Count;
		}
	}
}
