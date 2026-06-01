using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class MeshCombiner
	{
		private int maxVerticesCount = 65534;

		private string sessionName = string.Empty;

		private Dictionary<string, BlendShapeFrame> blendShapes = new Dictionary<string, BlendShapeFrame>();

		private int vertexOffset;

		private CombinedResult combinedResult;

		public CombinedResult CombinedResult
		{
			set
			{
				combinedResult = value;
			}
		}

		public void SetParameters(int maxVertices_p, string sessionName_p)
		{
			maxVerticesCount = maxVertices_p;
			sessionName = sessionName_p;
		}

		public void Clear()
		{
			blendShapes.Clear();
			vertexOffset = 0;
		}

		public List<GameObject> CombineToMeshes(List<MeshRenderer> meshRenderers, List<SkinnedMeshRenderer> skinnedMeshRenderers, Transform parent, int combinedIndex)
		{
			List<GameObject> list = new List<GameObject>();
			CombineInstanceID combineInstanceID = new CombineInstanceID();
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < meshRenderers.Count; i++)
			{
				Mesh mesh = copyMesh(meshRenderers[i].GetComponent<MeshFilter>().sharedMesh, meshRenderers[i].GetInstanceID().ToString());
				num += meshRenderers[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
				if (num > maxVerticesCount)
				{
					list.Add(CreateCombinedMeshGameObject(combineInstanceID, parent, num2, combinedIndex));
					combineInstanceID.Clear();
					num2++;
					num = meshRenderers[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;
				}
				Matrix4x4 matrix = parent.transform.worldToLocalMatrix * meshRenderers[i].transform.localToWorldMatrix;
				combineInstanceID.AddRange(CreateCombinedInstances(mesh, meshRenderers[i].sharedMaterials, meshRenderers[i].gameObject.GetInstanceID(), meshRenderers[i].gameObject.name, matrix, combinedIndex));
			}
			for (int j = 0; j < skinnedMeshRenderers.Count; j++)
			{
				Mesh mesh2 = copyMesh(skinnedMeshRenderers[j].sharedMesh, skinnedMeshRenderers[j].GetInstanceID().ToString());
				vertexOffset += mesh2.vertexCount;
				num += skinnedMeshRenderers[j].sharedMesh.vertexCount;
				if (num > maxVerticesCount && combineInstanceID.Count() > 0)
				{
					list.Add(CreateCombinedMeshGameObject(combineInstanceID, parent, num2, combinedIndex));
					combineInstanceID.Clear();
					num2++;
					num = skinnedMeshRenderers[j].sharedMesh.vertexCount;
				}
				Matrix4x4 matrix2 = parent.transform.worldToLocalMatrix * skinnedMeshRenderers[j].transform.localToWorldMatrix;
				combineInstanceID.AddRange(CreateCombinedInstances(mesh2, skinnedMeshRenderers[j].sharedMaterials, skinnedMeshRenderers[j].GetInstanceID(), skinnedMeshRenderers[j].gameObject.name, matrix2, combinedIndex));
			}
			if (combineInstanceID.Count() > 0)
			{
				list.Add(CreateCombinedMeshGameObject(combineInstanceID, parent, num2, combinedIndex));
			}
			return list;
		}

		public List<GameObject> CombineToSkinnedMeshes(List<MeshRenderer> meshRenderers, List<SkinnedMeshRenderer> skinnedMeshRenderers, Transform parent, int combinedIndex)
		{
			List<GameObject> list = new List<GameObject>();
			CombineInstanceID combineInstanceID = new CombineInstanceID();
			int num = 0;
			int num2 = 0;
			List<BoneWeight> list2 = new List<BoneWeight>();
			List<Transform> list3 = new List<Transform>();
			List<Matrix4x4> list4 = new List<Matrix4x4>();
			Dictionary<int, Transform> dictionary = new Dictionary<int, Transform>();
			Dictionary<int, Transform> dictionary2 = new Dictionary<int, Transform>();
			int num3 = 0;
			for (int i = 0; i < skinnedMeshRenderers.Count; i++)
			{
				Transform[] bones = skinnedMeshRenderers[i].bones;
				foreach (Transform transform in bones)
				{
					if (!dictionary.ContainsKey(transform.GetInstanceID()))
					{
						dictionary.Add(transform.GetInstanceID(), transform);
					}
				}
			}
			Transform[] array = FindRootBone(dictionary);
			for (int k = 0; k < array.Length; k++)
			{
				GameObject gameObject = new GameObject("rootBone" + k);
				gameObject.transform.position = array[k].position;
				gameObject.transform.parent = parent;
				gameObject.transform.localPosition -= array[k].localPosition;
				gameObject.transform.localRotation = Quaternion.identity;
				GameObject gameObject2 = InstantiateCopy(array[k].gameObject);
				gameObject2.transform.position = array[k].position;
				gameObject2.transform.rotation = array[k].rotation;
				gameObject2.transform.parent = gameObject.transform;
				gameObject2.AddComponent<MeshRenderer>();
				GetOrignialToNewBonesCorrespondancy(array[k], gameObject2.transform, dictionary2);
			}
			Animator[] componentsInChildren = parent.parent.GetComponentsInChildren<Animator>();
			foreach (Animator animator in componentsInChildren)
			{
				Transform[] componentsInChildren2 = animator.GetComponentsInChildren<Transform>();
				Transform transform2 = FindTransformForAnimator(componentsInChildren2, array, animator);
				if (transform2 != null)
				{
					CopyAnimator(animator, dictionary2[transform2.GetInstanceID()].parent.gameObject);
				}
			}
			for (int m = 0; m < skinnedMeshRenderers.Count; m++)
			{
				Mesh mesh = copyMesh(skinnedMeshRenderers[m].sharedMesh, skinnedMeshRenderers[m].GetInstanceID().ToString());
				vertexOffset += mesh.vertexCount;
				num += skinnedMeshRenderers[m].sharedMesh.vertexCount;
				if (num > maxVerticesCount && combineInstanceID.Count() > 0)
				{
					GameObject gameObject3 = CreateCombinedSkinnedMeshGameObject(combineInstanceID, parent, num2, combinedIndex);
					SkinnedMeshRenderer component = gameObject3.GetComponent<SkinnedMeshRenderer>();
					AssignParametersToSkinnedMesh(component, list3, list2, list4);
					list.Add(gameObject3);
					num3 = 0;
					combineInstanceID.Clear();
					num2++;
					num = skinnedMeshRenderers[m].sharedMesh.vertexCount;
				}
				BoneWeight[] boneWeights = skinnedMeshRenderers[m].sharedMesh.boneWeights;
				BoneWeight[] array2 = boneWeights;
				foreach (BoneWeight boneWeight in array2)
				{
					BoneWeight item = boneWeight;
					item.boneIndex0 += num3;
					item.boneIndex1 += num3;
					item.boneIndex2 += num3;
					item.boneIndex3 += num3;
					list2.Add(item);
				}
				num3 += skinnedMeshRenderers[m].bones.Length;
				Transform[] bones2 = skinnedMeshRenderers[m].bones;
				Transform[] array3 = bones2;
				foreach (Transform transform3 in array3)
				{
					list3.Add(dictionary2[transform3.GetInstanceID()]);
					list4.Add(transform3.worldToLocalMatrix * parent.transform.localToWorldMatrix);
				}
				Matrix4x4 matrix = parent.transform.worldToLocalMatrix * skinnedMeshRenderers[m].transform.localToWorldMatrix;
				combineInstanceID.AddRange(CreateCombinedInstances(mesh, skinnedMeshRenderers[m].sharedMaterials, skinnedMeshRenderers[m].GetInstanceID(), skinnedMeshRenderers[m].gameObject.name, matrix, combinedIndex));
			}
			if (combineInstanceID.Count() > 0)
			{
				GameObject gameObject4 = CreateCombinedSkinnedMeshGameObject(combineInstanceID, parent, num2, combinedIndex);
				SkinnedMeshRenderer component2 = gameObject4.GetComponent<SkinnedMeshRenderer>();
				AssignParametersToSkinnedMesh(component2, list3, list2, list4);
				list.Add(gameObject4);
			}
			return list;
		}

		private void AssignParametersToSkinnedMesh(SkinnedMeshRenderer skin, List<Transform> bones, List<BoneWeight> boneWeights, List<Matrix4x4> bindposes)
		{
			if (boneWeights.Count > 0)
			{
				for (int i = boneWeights.Count; i < skin.sharedMesh.vertexCount; i++)
				{
					boneWeights.Add(boneWeights[0]);
				}
			}
			skin.bones = bones.ToArray();
			skin.sharedMesh.boneWeights = boneWeights.ToArray();
			skin.sharedMesh.bindposes = bindposes.ToArray();
			skin.sharedMesh.RecalculateBounds();
			skin.sharedMesh.RecalculateNormals();
			bones.Clear();
			boneWeights.Clear();
			bindposes.Clear();
			vertexOffset = 0;
		}

		private void CopyAnimator(Animator anim, GameObject target)
		{
			if (target.GetComponentsInChildren<Animator>().Length == 0)
			{
				Animator animator = target.AddComponent(typeof(Animator)) as Animator;
				if (animator != null)
				{
					animator.applyRootMotion = anim.applyRootMotion;
					animator.avatar = anim.avatar;
					animator.updateMode = anim.updateMode;
					animator.cullingMode = anim.cullingMode;
					animator.runtimeAnimatorController = anim.runtimeAnimatorController;
				}
			}
		}

		private Transform FindTransformForAnimator(Transform[] children, Transform[] rootBones, Animator anim)
		{
			foreach (Transform transform in children)
			{
				for (int j = 0; j < rootBones.Length; j++)
				{
					if (transform.Equals(rootBones[j]))
					{
						return rootBones[j];
					}
				}
			}
			return null;
		}

		private void GetOrignialToNewBonesCorrespondancy(Transform rootBone, Transform newRootBone, Dictionary<int, Transform> originToNewBoneMap)
		{
			Transform[] componentsInChildren = rootBone.GetComponentsInChildren<Transform>();
			Transform[] componentsInChildren2 = newRootBone.GetComponentsInChildren<Transform>();
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				if (!originToNewBoneMap.ContainsKey(componentsInChildren[i].GetInstanceID()))
				{
					originToNewBoneMap.Add(componentsInChildren[i].GetInstanceID(), componentsInChildren2[i]);
				}
				else
				{
					Debug.LogWarning("[Super Combiner] Found duplicated root bone: " + componentsInChildren[i]);
				}
			}
		}

		private Transform[] FindRootBone(Dictionary<int, Transform> bones)
		{
			List<Transform> list = new List<Transform>();
			List<Transform> list2 = new List<Transform>(bones.Values);
			if (list2.Count == 0)
			{
				return list.ToArray();
			}
			Transform transform = list2.ToArray()[0];
			while (transform.parent != null)
			{
				if (bones.ContainsKey(transform.parent.GetInstanceID()))
				{
					transform = transform.parent;
					continue;
				}
				list.Add(transform.parent);
				Transform[] componentsInChildren = transform.parent.GetComponentsInChildren<Transform>();
				Transform[] array = componentsInChildren;
				foreach (Transform transform2 in array)
				{
					bones.Remove(transform2.GetInstanceID());
					if (transform2 != transform.parent && list.Contains(transform2))
					{
						list.Remove(transform2);
					}
				}
				Transform[] array2 = new List<Transform>(bones.Values).ToArray();
				if (array2.Length > 0)
				{
					transform = array2[0];
					continue;
				}
				break;
			}
			return list.ToArray();
		}

		private GameObject InstantiateCopy(GameObject original)
		{
			GameObject gameObject = Object.Instantiate(original);
			gameObject.transform.parent = original.transform.parent;
			gameObject.transform.localPosition = original.transform.localPosition;
			gameObject.transform.localRotation = original.transform.localRotation;
			gameObject.transform.localScale = original.transform.localScale;
			gameObject.name = original.name;
			SkinnedMeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (SkinnedMeshRenderer obj in componentsInChildren)
			{
				Object.DestroyImmediate(obj);
			}
			return gameObject;
		}

		private CombineInstanceID CreateCombinedInstances(Mesh mesh, Material[] sharedMaterials, int instanceID, string name, Matrix4x4 matrix, int combinedIndex)
		{
			CombineInstanceID combineInstanceID = new CombineInstanceID();
			int[] array = new int[mesh.subMeshCount];
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				if (i < sharedMaterials.Length)
				{
					Material matToFind = sharedMaterials[i];
					array[i] = combinedResult.FindCorrespondingMaterialIndex(matToFind, combinedIndex);
					continue;
				}
				Debug.LogWarning("[Super Combiner] Mesh '" + mesh.name + "' has " + mesh.subMeshCount + " submeshes but only " + sharedMaterials.Length + " material(s) assigned");
				break;
			}
			combinedResult.subMeshCount += mesh.subMeshCount - 1;
			if (combinedResult.originalMaterialList[combinedIndex].Count > 1)
			{
				GenerateUV(mesh, array, combinedResult.combinedMaterials[combinedIndex].scaleFactors.ToArray(), name, combinedIndex);
			}
			for (int j = 0; j < mesh.subMeshCount; j++)
			{
				combineInstanceID.AddCombineInstance(j, mesh, matrix, instanceID, name);
			}
			return combineInstanceID;
		}

		private GameObject CreateCombinedSkinnedMeshGameObject(CombineInstanceID instances, Transform parent, int number, int combinedIndex)
		{
			GameObject gameObject = new GameObject(sessionName + number);
			SkinnedMeshRenderer skinnedMeshRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
			skinnedMeshRenderer.sharedMaterial = combinedResult.combinedMaterials[combinedIndex].material;
			skinnedMeshRenderer.sharedMesh = new Mesh();
			skinnedMeshRenderer.sharedMesh.name = sessionName + "_" + combinedResult.combinedMaterials[combinedIndex].displayedIndex + "_mesh" + number;
			skinnedMeshRenderer.sharedMesh.CombineMeshes(instances.combineInstances.ToArray(), true, true);
			foreach (BlendShapeFrame value in blendShapes.Values)
			{
				Vector3[] array = new Vector3[skinnedMeshRenderer.sharedMesh.vertexCount];
				Vector3[] array2 = new Vector3[skinnedMeshRenderer.sharedMesh.vertexCount];
				Vector3[] array3 = new Vector3[skinnedMeshRenderer.sharedMesh.vertexCount];
				for (int i = 0; i < value.deltaVertices.Length; i++)
				{
					array.SetValue(value.deltaVertices[i], i + value.vertexOffset);
					array2.SetValue(value.deltaNormals[i], i + value.vertexOffset);
					array3.SetValue(value.deltaTangents[i], i + value.vertexOffset);
				}
				skinnedMeshRenderer.sharedMesh.AddBlendShapeFrame(value.shapeName, value.frameWeight, array, array2, array3);
			}
			gameObject.transform.SetParent(parent);
			gameObject.transform.localPosition = Vector3.zero;
			combinedResult.totalVertexCount += skinnedMeshRenderer.sharedMesh.vertexCount;
			combinedResult.AddCombinedMesh(skinnedMeshRenderer.sharedMesh, instances, combinedIndex);
			return gameObject;
		}

		public GameObject CreateCombinedMeshGameObject(CombineInstanceID instances, Transform parent, int number, int combinedIndex)
		{
			GameObject gameObject;
			MeshFilter meshFilter;
			MeshRenderer meshRenderer;
			if (number == 0 && parent.GetComponent<MeshFilter>() != null && parent.GetComponent<MeshRenderer>() != null)
			{
				gameObject = parent.gameObject;
				meshFilter = parent.GetComponent<MeshFilter>();
				meshRenderer = parent.GetComponent<MeshRenderer>();
			}
			else
			{
				gameObject = new GameObject(sessionName + "_" + combinedResult.combinedMaterials[combinedIndex].displayedIndex + "_" + number.ToString());
				meshFilter = gameObject.AddComponent<MeshFilter>();
				meshRenderer = gameObject.AddComponent<MeshRenderer>();
				gameObject.transform.SetParent(parent);
				gameObject.transform.localPosition = Vector3.zero;
			}
			meshRenderer.sharedMaterial = combinedResult.combinedMaterials[combinedIndex].material;
			meshFilter.mesh = new Mesh();
			meshFilter.sharedMesh.name = sessionName + "_" + combinedResult.combinedMaterials[combinedIndex].displayedIndex + "_mesh" + number;
			meshFilter.sharedMesh.CombineMeshes(instances.combineInstances.ToArray());
			combinedResult.totalVertexCount += meshFilter.sharedMesh.vertexCount;
			combinedResult.AddCombinedMesh(meshFilter.sharedMesh, instances, combinedIndex);
			return gameObject;
		}

		public bool GenerateUV(Mesh targetMesh, int[] textureIndex, float[] scaleFactors, string objectName, int combinedIndex)
		{
			int num = targetMesh.subMeshCount;
			if (num > textureIndex.Length)
			{
				Debug.LogWarning("[SuperCombiner] GameObject '" + objectName + "' has submeshes with no material assigned");
				num = textureIndex.Length;
			}
			Vector2[] uv = targetMesh.uv;
			Vector2[] uv2 = targetMesh.uv2;
			Vector2[] array = new Vector2[uv.Length];
			Vector2[] array2 = new Vector2[uv2.Length];
			Rect[] array3 = new Rect[num];
			if (array.Length > 0)
			{
				for (int i = 0; i < num; i++)
				{
					int[] triangles = targetMesh.GetTriangles(i);
					if (textureIndex[i] < combinedResult.combinedMaterials[combinedIndex].uvs.Length)
					{
						array3[i] = combinedResult.combinedMaterials[combinedIndex].uvs[textureIndex[i]];
						Rect rect = new Rect(array3[i].position, array3[i].size);
						float num2 = scaleFactors[textureIndex[i]];
						if (num2 > 1f)
						{
							rect.size = Vector2.Scale(rect.size, Vector2.one / num2);
							rect.position += new Vector2(array3[i].width * (1f - 1f / num2) / 2f, array3[i].height * (1f - 1f / num2) / 2f);
						}
						foreach (int num3 in triangles)
						{
							array[num3] = uv[num3];
							array[num3].x -= combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].xMin;
							array[num3].y -= combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].yMin;
							if (combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].width != 0f && combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].width != 1f)
							{
								array[num3].Scale(new Vector2(1f / combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].width, 1f));
							}
							if (combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].height != 0f && combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].height != 1f)
							{
								array[num3].Scale(new Vector2(1f, 1f / combinedResult.combinedMaterials[combinedIndex].meshUVBounds[textureIndex[i]].height));
							}
							array[num3].Scale(rect.size);
							array[num3] += rect.position;
						}
					}
					else
					{
						Debug.LogError("[Super Combiner] Texture index exceed packed texture size");
					}
				}
			}
			else
			{
				Debug.LogWarning("[Super Combiner] Object " + objectName + " doesn't have uv, combine process may be incorrect. Add uv map with a 3d modeler tool.");
			}
			targetMesh.uv = array;
			if (uv2 != null && uv2.Length > 0 && combinedResult.combinedMaterials[combinedIndex].uvs2 != null && combinedResult.combinedMaterials[combinedIndex].uvs2.Length > 0)
			{
				for (int k = 0; k < uv2.Length; k++)
				{
					array2[uv2.Length + k] = new Vector2(uv2[k].x * combinedResult.combinedMaterials[combinedIndex].uvs2[textureIndex[0]].width + combinedResult.combinedMaterials[combinedIndex].uvs2[textureIndex[0]].x, uv2[k].y * combinedResult.combinedMaterials[combinedIndex].uvs2[textureIndex[0]].height + combinedResult.combinedMaterials[combinedIndex].uvs2[textureIndex[0]].y);
				}
				targetMesh.uv2 = array2;
			}
			return true;
		}

		public Mesh copyMesh(Mesh mesh, string id = "")
		{
			Mesh mesh2 = new Mesh();
			mesh2.vertices = mesh.vertices;
			mesh2.normals = mesh.normals;
			mesh2.uv = mesh.uv;
			mesh2.uv2 = mesh.uv2;
			mesh2.uv3 = mesh.uv3;
			mesh2.uv4 = mesh.uv4;
			mesh2.triangles = mesh.triangles;
			mesh2.tangents = mesh.tangents;
			mesh2.subMeshCount = mesh.subMeshCount;
			mesh2.bindposes = mesh.bindposes;
			mesh2.boneWeights = mesh.boneWeights;
			mesh2.bounds = mesh.bounds;
			mesh2.colors32 = mesh.colors32;
			mesh2.name = mesh.name;
			for (int i = 0; i < mesh.subMeshCount; i++)
			{
				mesh2.SetIndices(mesh.GetIndices(i), mesh.GetTopology(i), i);
			}
			if (mesh.blendShapeCount > 0)
			{
				Vector3[] array = new Vector3[mesh.vertexCount];
				Vector3[] array2 = new Vector3[mesh.vertexCount];
				Vector3[] array3 = new Vector3[mesh.vertexCount];
				for (int j = 0; j < mesh.blendShapeCount; j++)
				{
					for (int k = 0; k < mesh.GetBlendShapeFrameCount(j); k++)
					{
						if (!blendShapes.ContainsKey(mesh.GetBlendShapeName(j) + id))
						{
							mesh.GetBlendShapeFrameVertices(j, k, array, array2, array3);
							mesh2.AddBlendShapeFrame(mesh.GetBlendShapeName(j), mesh.GetBlendShapeFrameWeight(j, k), array, array2, array3);
							blendShapes.Add(mesh.GetBlendShapeName(j) + id, new BlendShapeFrame(mesh.GetBlendShapeName(j) + id, mesh.GetBlendShapeFrameWeight(j, k), array, array2, array3, vertexOffset));
						}
					}
				}
			}
			return mesh2;
		}

		private void CopyNewMeshesByCombine(Mesh original, Mesh destination)
		{
			int subMeshCount = original.subMeshCount;
			CombineInstance[] array = new CombineInstance[subMeshCount];
			for (int i = 0; i < subMeshCount; i++)
			{
				array[i] = default(CombineInstance);
				array[i].subMeshIndex = i;
				array[i].mesh = original;
				array[i].transform = Matrix4x4.identity;
			}
			destination.CombineMeshes(array, false);
		}
	}
}
