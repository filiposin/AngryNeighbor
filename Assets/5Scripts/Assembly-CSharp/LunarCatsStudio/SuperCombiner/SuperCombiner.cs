using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class SuperCombiner : MonoBehaviour
	{
		public enum CombineStatesList
		{
			Uncombined = 0,
			Combining = 1,
			CombinedMaterials = 2,
			Combined = 3
		}

		public List<int> TextureAtlasSizes = new List<int> { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

		public List<string> TextureAtlasSizesNames = new List<string> { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };

		public CombineStatesList combiningState;

		public List<TexturePacker> texturePackers = new List<TexturePacker>();

		public MeshCombiner meshCombiner = new MeshCombiner();

		public bool _showInstructions = true;

		public bool _showCombineSettings;

		public bool _showMeshSettings;

		public bool _showTextureSettings = true;

		public bool _showAdditionalParameters;

		public bool _showMeshResults;

		public bool _showOriginalMaterials;

		public bool _showCombinedAtlas;

		public bool _showCombinedMaterials;

		public bool _showCombinedMesh;

		public bool _showSaveOptions;

		public bool _showMultiMaterials;

		public string sessionName = "combinedSession";

		public bool combineAtRuntime;

		public int textureAtlasSize = 1024;

		public List<string> customTextureProperies = new List<string>();

		public float tilingFactor = 1f;

		public int atlasPadding;

		public bool combineMeshes;

		public int meshOutput;

		public int maxVerticesCount = 65534;

		public bool combineMaterials = true;

		public GameObject targetGameObject;

		public bool multipleMaterialsMode;

		public List<Material> multiMaterials0 = new List<Material>();

		public List<Material> multiMaterials1 = new List<Material>();

		public List<Material> multiMaterials2 = new List<Material>();

		public List<Material> multiMaterials3 = new List<Material>();

		public List<Material> multiMaterials4 = new List<Material>();

		public List<Material> multiMaterials5 = new List<Material>();

		public List<Material> multiMaterials6 = new List<Material>();

		public List<Material> multiMaterials7 = new List<Material>();

		public List<Material> multiMaterials8 = new List<Material>();

		public List<Material> multiMaterials9 = new List<Material>();

		public List<Material> multiMaterials10 = new List<Material>();

		public List<Material> multiMaterialsAllOthers = new List<Material>();

		public List<List<Material>> multiMaterialsList = new List<List<Material>>();

		public int _multiMaterialsCount;

		private List<List<MaterialToCombine>> materialsToCombine = new List<List<MaterialToCombine>>();

		public bool savePrefabs = true;

		public bool saveMeshObj;

		public bool saveMeshFbx;

		public bool saveMaterials = true;

		public bool saveTextures = true;

		public string folderDestination = "Assets/SuperCombiner/Combined";

		public List<MeshRenderer> meshList = new List<MeshRenderer>();

		public List<SkinnedMeshRenderer> skinnedMeshList = new List<SkinnedMeshRenderer>();

		public Dictionary<int, string> uniqueCombinedMeshId = new Dictionary<int, string>();

		public Dictionary<int, string> copyMeshId = new Dictionary<int, string>();

		public List<GameObject> toSavePrefabList = new List<GameObject>();

		public List<MeshRenderer> toSaveObjectList = new List<MeshRenderer>();

		public List<Mesh> toSaveMeshList = new List<Mesh>();

		public List<SkinnedMeshRenderer> toSaveSkinnedObjectList = new List<SkinnedMeshRenderer>();

		public GameObject targetParentForCombinedGameObjects;

		private DateTime timeStart;

		public CombinedResult combinedResult;

		private SuperCombiner()
		{
		}

		private void Start()
		{
			if (combineAtRuntime)
			{
				CombineChildren();
			}
		}

		public void FindMeshesToCombine()
		{
			meshList = FindEnabledMeshes(base.transform);
			skinnedMeshList = FindEnabledSkinnedMeshes(base.transform);
		}

		public void CombineChildren()
		{
			timeStart = DateTime.Now;
			combiningState = CombineStatesList.Combining;
			FindMeshesToCombine();
			Combine(meshList, skinnedMeshList);
		}

		public bool CombineMaterials(List<MeshRenderer> meshesToCombine, List<SkinnedMeshRenderer> skinnedMeshesToCombine)
		{
			InitializeMultipleMaterialElements();
			if (combinedResult == null)
			{
				combinedResult = (CombinedResult)ScriptableObject.CreateInstance(typeof(CombinedResult));
			}
			List<MaterialToCombine> list = FindEnabledMaterials(meshesToCombine, skinnedMeshesToCombine);
			combinedResult.materialCombinedCount = list.Count;
			foreach (MaterialToCombine item in list)
			{
				bool flag = false;
				for (int i = 0; i < multiMaterialsList.Count; i++)
				{
					if (multiMaterialsList[i].Contains(item.material))
					{
						materialsToCombine[i].Add(item);
						flag = true;
					}
				}
				if (!flag)
				{
					materialsToCombine[materialsToCombine.Count - 1].Add(item);
				}
			}
			int num = 0;
			for (int j = 0; j < materialsToCombine.Count; j++)
			{
				combinedResult.originalMaterialList.Add(new Dictionary<int, MaterialToCombine>());
				combinedResult.AddNewCombinedMaterial();
				if ((j == multiMaterialsList.Count && materialsToCombine[j].Count > 0) || (j < multiMaterialsList.Count && multiMaterialsList[j].Count > 0))
				{
					TexturePacker texturePacker = new TexturePacker();
					texturePacker.CombinedResult = combinedResult;
					texturePacker.CombinedIndex = j;
					TexturePacker texturePacker2 = texturePacker;
					texturePacker2.SetCustomPropertyNames(customTextureProperies);
					texturePackers.Add(texturePacker2);
					foreach (MaterialToCombine item2 in materialsToCombine[j])
					{
						combinedResult.AddMaterialToCombine(item2, j);
						texturePacker2.SetTextures(item2.material, combineMaterials, item2.GetScaledAndOffsetedUVBounds(), item2.uvBounds, tilingFactor);
						num++;
					}
					if (materialsToCombine[j].Count == 0)
					{
						if (multiMaterialsList[j].Count == 0)
						{
							Debug.LogWarning("[Super Combiner] Source materials group " + j + " is empty. Skipping this combine process");
						}
						else
						{
							Debug.LogWarning("[Super Combiner] Cannot combined materials for group " + j + " because none of the material were found in the list of game objects to combine");
						}
					}
					else if (materialsToCombine[j].Count == 1)
					{
						Debug.Log("[Super Combiner] Only one material found for group " + j + ", skipping combine material process and keep this material for the combined mesh.");
						combinedResult.SetCombinedMaterial(materialsToCombine[j][0].material, j, true);
						combinedResult.combinedMaterials[j].uvs = new Rect[1];
						combinedResult.combinedMaterials[j].uvs[0] = new Rect(0f, 0f, 1f, 1f);
						texturePacker2.SetCopyedMaterial(materialsToCombine[j][0].material);
					}
					else
					{
						texturePacker2.PackTextures(textureAtlasSize, atlasPadding, combineMaterials, sessionName);
					}
				}
				else
				{
					texturePackers.Add(null);
				}
			}
			combiningState = CombineStatesList.CombinedMaterials;
			return false;
		}

		public void SetTargetParentForCombinedGameObject()
		{
			if (targetGameObject == null)
			{
				targetParentForCombinedGameObjects = new GameObject(sessionName);
				targetParentForCombinedGameObjects.transform.parent = base.transform;
				targetParentForCombinedGameObjects.transform.localPosition = Vector3.zero;
			}
			else
			{
				targetParentForCombinedGameObjects = targetGameObject;
			}
		}

		public void CombineMeshes(List<MeshRenderer> meshesToCombine, List<SkinnedMeshRenderer> skinnedMeshesToCombine, Transform parent)
		{
			meshCombiner.CombinedResult = combinedResult;
			combinedResult.meshesCombinedCount = meshesToCombine.Count;
			combinedResult.skinnedMeshesCombinedCount = skinnedMeshesToCombine.Count;
			if (combineMeshes && meshesToCombine.Count + skinnedMeshesToCombine.Count < 1)
			{
				if (meshesToCombine.Count == 0)
				{
					UnCombine();
				}
				return;
			}
			meshCombiner.SetParameters(maxVerticesCount, sessionName);
			if (combineMeshes)
			{
				List<MeshRendererAndOriginalMaterials> meshRenderersByCombineIndex = GetMeshRenderersByCombineIndex(meshesToCombine, skinnedMeshesToCombine, targetParentForCombinedGameObjects.transform);
				for (int i = 0; i < combinedResult.GetCombinedIndexCount(); i++)
				{
					combinedResult.combinedGameObjectFromMeshList.Add(new List<GameObject>());
					combinedResult.combinedGameObjectFromSkinnedMeshList.Add(new List<GameObject>());
					if (combinedResult.originalMaterialList[i].Count > 0)
					{
						if (meshOutput == 0)
						{
							combinedResult.combinedGameObjectFromMeshList[i] = meshCombiner.CombineToMeshes(meshRenderersByCombineIndex[i].meshRenderers, meshRenderersByCombineIndex[i].skinnedMeshRenderers, parent, i);
							foreach (GameObject item in combinedResult.combinedGameObjectFromMeshList[i])
							{
								uniqueCombinedMeshId.Add(item.GetComponent<MeshFilter>().sharedMesh.GetInstanceID(), item.name);
							}
						}
						else
						{
							combinedResult.combinedGameObjectFromSkinnedMeshList[i] = meshCombiner.CombineToSkinnedMeshes(meshRenderersByCombineIndex[i].meshRenderers, meshRenderersByCombineIndex[i].skinnedMeshRenderers, parent, i);
							foreach (GameObject item2 in combinedResult.combinedGameObjectFromSkinnedMeshList[i])
							{
								uniqueCombinedMeshId.Add(item2.GetComponent<SkinnedMeshRenderer>().sharedMesh.GetInstanceID(), item2.name);
							}
						}
						if (combinedResult.combinedGameObjectFromMeshList.Count + combinedResult.combinedGameObjectFromSkinnedMeshList.Count == 0)
						{
							Debug.LogError("[Super Combiner] No mesh could be combined");
						}
					}
					for (int j = 0; j < meshRenderersByCombineIndex[i].splittedGameObject.Count; j++)
					{
						UnityEngine.Object.DestroyImmediate(meshRenderersByCombineIndex[i].splittedGameObject[j]);
					}
				}
			}
			else
			{
				CopyGameObjectsHierarchy(parent);
				List<MeshRenderer> meshRenderers = FindEnabledMeshes(parent);
				List<SkinnedMeshRenderer> skinnedMeshRenderers = FindEnabledSkinnedMeshes(parent);
				List<MeshRendererAndOriginalMaterials> meshRenderersByCombineIndex2 = GetMeshRenderersByCombineIndex(meshRenderers, skinnedMeshRenderers, null);
				for (int k = 0; k < combinedResult.GetCombinedIndexCount(); k++)
				{
					combinedResult.combinedGameObjectFromMeshList.Add(new List<GameObject>());
					combinedResult.combinedGameObjectFromSkinnedMeshList.Add(new List<GameObject>());
					if (meshRenderersByCombineIndex2[k].meshRenderers.Count > 0)
					{
						combinedResult.combinedGameObjectFromMeshList[k].AddRange(GenerateTransformedGameObjects(parent, meshRenderersByCombineIndex2[k].meshRenderers));
					}
					if (meshRenderersByCombineIndex2[k].skinnedMeshRenderers.Count > 0)
					{
						combinedResult.combinedGameObjectFromSkinnedMeshList[k].AddRange(GenerateTransformedGameObjects(parent, meshRenderersByCombineIndex2[k].skinnedMeshRenderers));
					}
					if (combinedResult.originalMaterialList[k].Count > 1)
					{
						for (int l = 0; l < meshRenderersByCombineIndex2[k].meshRenderers.Count; l++)
						{
							GenerateUVs(meshRenderersByCombineIndex2[k].meshRenderers[l].GetComponent<MeshFilter>().sharedMesh, meshRenderersByCombineIndex2[k].originalMaterials[l], meshRenderersByCombineIndex2[k].meshRenderers[l].name, k);
						}
						for (int m = 0; m < meshRenderersByCombineIndex2[k].skinnedMeshRenderers.Count; m++)
						{
							GenerateUVs(meshRenderersByCombineIndex2[k].skinnedMeshRenderers[m].sharedMesh, meshRenderersByCombineIndex2[k].originalskinnedMeshMaterials[m], meshRenderersByCombineIndex2[k].skinnedMeshRenderers[m].name, k);
						}
					}
				}
			}
			combiningState = CombineStatesList.Combined;
			DisableRenderers(meshList, skinnedMeshList);
		}

		private List<MeshRendererAndOriginalMaterials> GetMeshRenderersByCombineIndex(List<MeshRenderer> meshRenderers, List<SkinnedMeshRenderer> skinnedMeshRenderers, Transform parent)
		{
			List<MeshRendererAndOriginalMaterials> list = new List<MeshRendererAndOriginalMaterials>();
			List<List<int>> list2 = new List<List<int>>();
			if (combinedResult.originalMaterialList.Count == 0)
			{
				Debug.LogError("[Super Combiner] List of materials to combine has been lost. Try to uncombine and combine again.");
				return list;
			}
			for (int i = 0; i < combinedResult.originalMaterialList.Count; i++)
			{
				list.Add(new MeshRendererAndOriginalMaterials());
				list2.Add(new List<int>());
			}
			foreach (MeshRenderer meshRenderer2 in meshRenderers)
			{
				Material[] sharedMaterials = meshRenderer2.sharedMaterials;
				combinedResult.subMeshCount += sharedMaterials.Length - 1;
				for (int j = 0; j < sharedMaterials.Length; j++)
				{
					if (sharedMaterials[j] != null)
					{
						int combinedIndex = combinedResult.GetCombinedIndex(sharedMaterials[j]);
						list2[combinedIndex].Add(j);
					}
					else
					{
						Debug.LogWarning("[Super Combiner] MeshRenderer of '" + meshRenderer2.name + "' has some missing material references.");
					}
				}
				bool flag = false;
				for (int k = 0; k < combinedResult.originalMaterialList.Count; k++)
				{
					if (list2[k].Count > 0)
					{
						if (list2[k].Count < sharedMaterials.Length)
						{
							MeshRenderer meshRenderer = SubmeshSplitter.SplitSubmeshes(meshRenderer2.GetComponent<MeshFilter>(), list2[k].ToArray(), k);
							list[k].meshRenderers.Add(meshRenderer);
							list[k].originalMaterials.Add(meshRenderer.sharedMaterials);
							list[k].splittedGameObject.Add(meshRenderer.gameObject);
							flag = true;
						}
						else
						{
							list[k].meshRenderers.Add(meshRenderer2);
							list[k].originalMaterials.Add(meshRenderer2.sharedMaterials);
						}
					}
				}
				if (flag && parent == null)
				{
					UnityEngine.Object.DestroyImmediate(meshRenderer2.GetComponent<MeshFilter>());
					UnityEngine.Object.DestroyImmediate(meshRenderer2);
				}
				for (int l = 0; l < combinedResult.originalMaterialList.Count; l++)
				{
					list2[l].Clear();
				}
			}
			foreach (SkinnedMeshRenderer skinnedMeshRenderer2 in skinnedMeshRenderers)
			{
				Material[] sharedMaterials2 = skinnedMeshRenderer2.sharedMaterials;
				combinedResult.subMeshCount += sharedMaterials2.Length - 1;
				for (int m = 0; m < sharedMaterials2.Length; m++)
				{
					if (sharedMaterials2[m] != null)
					{
						int combinedIndex2 = combinedResult.GetCombinedIndex(sharedMaterials2[m]);
						list2[combinedIndex2].Add(m);
					}
					else
					{
						Debug.LogWarning("[Super Combiner] SkinnedMeshRenderer of '" + skinnedMeshRenderer2.name + "' has some missing material references.");
					}
				}
				bool flag2 = false;
				for (int n = 0; n < combinedResult.originalMaterialList.Count; n++)
				{
					if (list2[n].Count > 0)
					{
						if (list2[n].Count < sharedMaterials2.Length)
						{
							SkinnedMeshRenderer skinnedMeshRenderer = SubmeshSplitter.SplitSubmeshes(skinnedMeshRenderer2, list2[n].ToArray(), n);
							list[n].skinnedMeshRenderers.Add(skinnedMeshRenderer);
							list[n].originalskinnedMeshMaterials.Add(skinnedMeshRenderer.sharedMaterials);
							list[n].splittedGameObject.Add(skinnedMeshRenderer.gameObject);
							flag2 = true;
						}
						else
						{
							list[n].skinnedMeshRenderers.Add(skinnedMeshRenderer2);
							list[n].originalskinnedMeshMaterials.Add(skinnedMeshRenderer2.sharedMaterials);
						}
					}
				}
				if (flag2 && parent == null)
				{
					UnityEngine.Object.DestroyImmediate(skinnedMeshRenderer2.GetComponent<MeshFilter>());
					UnityEngine.Object.DestroyImmediate(skinnedMeshRenderer2);
				}
				for (int num = 0; num < combinedResult.originalMaterialList.Count; num++)
				{
					list2[num].Clear();
				}
			}
			return list;
		}

		public void Combine(List<MeshRenderer> meshesToCombine, List<SkinnedMeshRenderer> skinnedMeshesToCombine)
		{
			if (combiningState == CombineStatesList.Uncombined)
			{
				timeStart = DateTime.Now;
				combiningState = CombineStatesList.Combining;
			}
			Debug.Log("[Super Combiner] Start processing...");
			if (!CombineMaterials(meshesToCombine, skinnedMeshesToCombine))
			{
				SetTargetParentForCombinedGameObject();
				CombineMeshes(meshesToCombine, skinnedMeshesToCombine, targetParentForCombinedGameObjects.transform);
				combiningState = CombineStatesList.Combined;
				combinedResult.duration = DateTime.Now - timeStart;
				Debug.Log("[Super Combiner] Successfully combined game objects!\nExecution time is " + combinedResult.duration);
			}
		}

		private void InitializeMultipleMaterialElements()
		{
			if (multipleMaterialsMode)
			{
				multiMaterialsList.Add(multiMaterials0);
				multiMaterialsList.Add(multiMaterials1);
				multiMaterialsList.Add(multiMaterials2);
				multiMaterialsList.Add(multiMaterials3);
				multiMaterialsList.Add(multiMaterials4);
				multiMaterialsList.Add(multiMaterials5);
				multiMaterialsList.Add(multiMaterials6);
				multiMaterialsList.Add(multiMaterials7);
				multiMaterialsList.Add(multiMaterials8);
				multiMaterialsList.Add(multiMaterials9);
				multiMaterialsList.Add(multiMaterials10);
			}
			for (int i = 0; i < multiMaterialsList.Count + 1; i++)
			{
				materialsToCombine.Add(new List<MaterialToCombine>());
			}
		}

		private void CopyGameObjectsHierarchy(Transform parent)
		{
			Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>();
			Transform[] array = componentsInChildren;
			foreach (Transform transform in array)
			{
				if (transform.parent == base.transform && transform != parent)
				{
					GameObject gameObject = InstantiateCopy(transform.gameObject, false);
					gameObject.transform.SetParent(parent);
				}
			}
		}

		private void GenerateUVs(Mesh mesh, Material[] originalMaterials, string objectName, int combinedIndex)
		{
			int[] array = new int[originalMaterials.Length];
			for (int i = 0; i < originalMaterials.Length; i++)
			{
				Material matToFind = originalMaterials[i];
				array[i] = combinedResult.FindCorrespondingMaterialIndex(matToFind, combinedIndex);
			}
			if (!meshCombiner.GenerateUV(mesh, array, combinedResult.combinedMaterials[combinedIndex].scaleFactors.ToArray(), objectName, combinedIndex))
			{
				UnCombine();
			}
		}

		private void EnableRenderers(List<MeshRenderer> meshes, List<SkinnedMeshRenderer> skinnedMeshes)
		{
			foreach (MeshRenderer mesh in meshes)
			{
				if (mesh != null)
				{
					mesh.gameObject.SetActive(true);
				}
			}
			foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
			{
				if (skinnedMesh != null)
				{
					skinnedMesh.gameObject.SetActive(true);
				}
			}
		}

		private void DisableRenderers(List<MeshRenderer> meshes, List<SkinnedMeshRenderer> skinnedMeshes)
		{
			foreach (MeshRenderer mesh in meshes)
			{
				if (mesh != null && mesh.gameObject != targetGameObject)
				{
					mesh.gameObject.SetActive(false);
				}
			}
			foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
			{
				if (skinnedMesh != null && skinnedMesh.gameObject != targetGameObject)
				{
					skinnedMesh.gameObject.SetActive(false);
				}
			}
		}

		private List<GameObject> GenerateTransformedGameObjects(Transform parent, List<MeshRenderer> originalMeshRenderer)
		{
			List<GameObject> list = new List<GameObject>();
			for (int i = 0; i < originalMeshRenderer.Count; i++)
			{
				Mesh mesh = meshCombiner.copyMesh(originalMeshRenderer[i].GetComponent<MeshFilter>().sharedMesh, string.Empty);
				if (originalMeshRenderer[i].GetComponent<Renderer>().sharedMaterial != null)
				{
					uniqueCombinedMeshId.Add(mesh.GetInstanceID(), originalMeshRenderer[i].GetComponent<MeshFilter>().sharedMesh.GetInstanceID().ToString() + originalMeshRenderer[i].GetComponent<Renderer>().sharedMaterial.GetInstanceID() + mesh.name);
				}
				else
				{
					uniqueCombinedMeshId.Add(mesh.GetInstanceID(), originalMeshRenderer[i].GetComponent<MeshFilter>().sharedMesh.GetInstanceID() + mesh.name);
				}
				copyMeshId[originalMeshRenderer[i].GetComponent<MeshFilter>().sharedMesh.GetInstanceID()] = uniqueCombinedMeshId[mesh.GetInstanceID()];
				originalMeshRenderer[i].GetComponent<MeshFilter>().sharedMesh = mesh;
				if (combineMaterials)
				{
					Material[] sharedMaterials = originalMeshRenderer[i].GetComponent<Renderer>().sharedMaterials;
					Material[] array = new Material[sharedMaterials.Length];
					for (int j = 0; j < array.Length; j++)
					{
						array[j] = combinedResult.GetCombinedMaterial(sharedMaterials[j]);
					}
					originalMeshRenderer[i].GetComponent<Renderer>().sharedMaterials = array;
				}
				list.Add(originalMeshRenderer[i].gameObject);
			}
			return list;
		}

		private List<GameObject> GenerateTransformedGameObjects(Transform parent, List<SkinnedMeshRenderer> originalSkinnedMeshRenderer)
		{
			List<GameObject> list = new List<GameObject>();
			for (int i = 0; i < originalSkinnedMeshRenderer.Count; i++)
			{
				Mesh mesh = meshCombiner.copyMesh(originalSkinnedMeshRenderer[i].GetComponent<SkinnedMeshRenderer>().sharedMesh, string.Empty);
				if (originalSkinnedMeshRenderer[i].GetComponent<Renderer>().sharedMaterial != null)
				{
					uniqueCombinedMeshId.Add(mesh.GetInstanceID(), originalSkinnedMeshRenderer[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.GetInstanceID().ToString() + originalSkinnedMeshRenderer[i].GetComponent<Renderer>().sharedMaterial.GetInstanceID() + mesh.name);
				}
				else
				{
					uniqueCombinedMeshId.Add(mesh.GetInstanceID(), originalSkinnedMeshRenderer[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.GetInstanceID() + mesh.name);
				}
				copyMeshId[originalSkinnedMeshRenderer[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.GetInstanceID()] = uniqueCombinedMeshId[mesh.GetInstanceID()];
				originalSkinnedMeshRenderer[i].GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
				if (combineMaterials)
				{
					Material[] sharedMaterials = originalSkinnedMeshRenderer[i].GetComponent<Renderer>().sharedMaterials;
					Material[] array = new Material[sharedMaterials.Length];
					for (int j = 0; j < array.Length; j++)
					{
						array[j] = combinedResult.GetCombinedMaterial(sharedMaterials[j]);
					}
					originalSkinnedMeshRenderer[i].GetComponent<SkinnedMeshRenderer>().sharedMaterials = array;
				}
				list.Add(originalSkinnedMeshRenderer[i].gameObject);
			}
			return list;
		}

		private GameObject InstantiateCopy(GameObject original, bool deleteChidren = true)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(original);
			gameObject.transform.parent = original.transform.parent;
			gameObject.transform.localPosition = original.transform.localPosition;
			gameObject.transform.localRotation = original.transform.localRotation;
			gameObject.transform.localScale = original.transform.localScale;
			gameObject.name = original.name;
			if (deleteChidren)
			{
				foreach (Transform item in gameObject.transform)
				{
					UnityEngine.Object.DestroyImmediate(item.gameObject);
				}
			}
			return gameObject;
		}

		private List<MeshCollider> FindEnabledMeshColliders(Transform parent)
		{
			MeshCollider[] componentsInChildren = parent.GetComponentsInChildren<MeshCollider>();
			List<MeshCollider> list = new List<MeshCollider>();
			MeshCollider[] array = componentsInChildren;
			foreach (MeshCollider meshCollider in array)
			{
				if (meshCollider.sharedMesh != null)
				{
					list.Add(meshCollider);
				}
			}
			return list;
		}

		private List<MeshRenderer> FindEnabledMeshes(Transform parent)
		{
			MeshFilter[] componentsInChildren = parent.GetComponentsInChildren<MeshFilter>();
			List<MeshRenderer> list = new List<MeshRenderer>();
			MeshFilter[] array = componentsInChildren;
			foreach (MeshFilter meshFilter in array)
			{
				if (meshFilter.sharedMesh != null)
				{
					MeshRenderer component = meshFilter.GetComponent<MeshRenderer>();
					if (component != null && component.enabled && component.sharedMaterials.Length > 0)
					{
						list.Add(component);
					}
				}
			}
			return list;
		}

		private List<SkinnedMeshRenderer> FindEnabledSkinnedMeshes(Transform parent)
		{
			SkinnedMeshRenderer[] componentsInChildren = parent.GetComponentsInChildren<SkinnedMeshRenderer>();
			List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
			SkinnedMeshRenderer[] array = componentsInChildren;
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in array)
			{
				if (skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.enabled && skinnedMeshRenderer.sharedMaterials.Length > 0)
				{
					list.Add(skinnedMeshRenderer);
				}
			}
			return list;
		}

		private List<MaterialToCombine> FindEnabledMaterials(List<MeshRenderer> meshes, List<SkinnedMeshRenderer> skinnedMeshes)
		{
			Dictionary<int, MaterialToCombine> dictionary = new Dictionary<int, MaterialToCombine>();
			foreach (MeshRenderer mesh in meshes)
			{
				Rect uVBounds = getUVBounds(mesh.GetComponent<MeshFilter>().sharedMesh.uv);
				Material[] sharedMaterials = mesh.sharedMaterials;
				foreach (Material material in sharedMaterials)
				{
					if (material != null)
					{
						int instanceID = material.GetInstanceID();
						if (!dictionary.ContainsKey(instanceID))
						{
							MaterialToCombine materialToCombine = new MaterialToCombine();
							materialToCombine.material = material;
							materialToCombine.uvBounds = uVBounds;
							dictionary.Add(instanceID, materialToCombine);
						}
						else
						{
							Rect maxRect = getMaxRect(dictionary[instanceID].uvBounds, uVBounds);
							MaterialToCombine materialToCombine2 = dictionary[instanceID];
							materialToCombine2.uvBounds = maxRect;
							dictionary[instanceID] = materialToCombine2;
						}
					}
				}
			}
			foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
			{
				Rect uVBounds2 = getUVBounds(skinnedMesh.sharedMesh.uv);
				Material[] sharedMaterials2 = skinnedMesh.sharedMaterials;
				foreach (Material material2 in sharedMaterials2)
				{
					if (material2 != null)
					{
						int instanceID2 = material2.GetInstanceID();
						if (!dictionary.ContainsKey(instanceID2))
						{
							MaterialToCombine materialToCombine3 = new MaterialToCombine();
							materialToCombine3.material = material2;
							materialToCombine3.uvBounds = uVBounds2;
							dictionary.Add(instanceID2, materialToCombine3);
						}
						else
						{
							Rect maxRect2 = getMaxRect(dictionary[instanceID2].uvBounds, uVBounds2);
							MaterialToCombine materialToCombine4 = dictionary[instanceID2];
							materialToCombine4.uvBounds = maxRect2;
							dictionary[instanceID2] = materialToCombine4;
						}
					}
				}
			}
			return new List<MaterialToCombine>(dictionary.Values);
		}

		private Rect getUVBounds(Vector2[] uvs)
		{
			Rect result = new Rect(0f, 0f, 1f, 1f);
			for (int i = 0; i < uvs.Length; i++)
			{
				if (uvs[i].x < 0f && uvs[i].x < result.xMin)
				{
					result.xMin = uvs[i].x;
				}
				if (uvs[i].x > 1f && uvs[i].x > result.xMax)
				{
					result.xMax = uvs[i].x;
				}
				if (uvs[i].y < 0f && uvs[i].y < result.yMin)
				{
					result.yMin = uvs[i].y;
				}
				if (uvs[i].y > 1f && uvs[i].y > result.yMax)
				{
					result.yMax = uvs[i].y;
				}
			}
			return result;
		}

		private Rect getMaxRect(Rect uv1, Rect uv2)
		{
			return new Rect
			{
				xMin = Math.Min(uv1.xMin, uv2.xMin),
				yMin = Math.Min(uv1.yMin, uv2.yMin),
				xMax = Math.Max(uv1.xMax, uv2.xMax),
				yMax = Math.Max(uv1.yMax, uv2.yMax)
			};
		}

		public void UnCombine()
		{
			EnableRenderers(meshList, skinnedMeshList);
			if (targetParentForCombinedGameObjects == targetGameObject && combinedResult != null)
			{
				for (int i = 0; i < combinedResult.GetCombinedIndexCount(); i++)
				{
					if (combinedResult.combinedGameObjectFromMeshList.Count <= i)
					{
						continue;
					}
					foreach (GameObject item in combinedResult.combinedGameObjectFromMeshList[i])
					{
						UnityEngine.Object.DestroyImmediate(item);
					}
					foreach (GameObject item2 in combinedResult.combinedGameObjectFromSkinnedMeshList[i])
					{
						UnityEngine.Object.DestroyImmediate(item2);
					}
				}
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(targetParentForCombinedGameObjects);
			}
			texturePackers.Clear();
			materialsToCombine.Clear();
			multiMaterialsList.Clear();
			meshCombiner.Clear();
			meshList.Clear();
			skinnedMeshList.Clear();
			uniqueCombinedMeshId.Clear();
			copyMeshId.Clear();
			toSavePrefabList.Clear();
			toSaveObjectList.Clear();
			toSaveMeshList.Clear();
			toSaveSkinnedObjectList.Clear();
			if (combinedResult != null)
			{
				combinedResult.Clear();
			}
			combiningState = CombineStatesList.Uncombined;
			Debug.Log("[Super Combiner] Successfully uncombined game objects.");
		}

		private List<Transform> GetFirstLevelChildren(Transform parent)
		{
			List<Transform> list = new List<Transform>();
			for (int i = 0; i < parent.transform.childCount; i++)
			{
				list.Add(parent.transform.GetChild(i));
			}
			return list;
		}

		public void Save()
		{
		}
	}
}
