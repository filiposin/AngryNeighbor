using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class CombinedMeshModification : MonoBehaviour
	{
		[Tooltip("Reference to the combinedResult file")]
		public CombinedResult combinedResult;

		[Tooltip("Reference to the MeshFilter in which the combined mesh is attached to")]
		public MeshFilter meshFilter;

		private CombinedResult currentCombinedResult;

		private void Awake()
		{
			currentCombinedResult = Object.Instantiate(combinedResult);
		}

		public void RemoveFromCombined(GameObject gameObject)
		{
			RemoveFromCombined(gameObject.GetInstanceID());
		}

		public void RemoveFromCombined(int instanceID)
		{
			if (meshFilter == null)
			{
				Debug.LogWarning("[Super Combiner] MeshFilter is not set, please assign MeshFilter parameter before trying to remove a part of it's mesh");
				return;
			}
			bool flag = false;
			foreach (MeshCombined meshResult in currentCombinedResult.meshResults)
			{
				if (meshResult.instanceIds.Contains(instanceID))
				{
					Debug.Log("[Super Combiner] Removing object '" + instanceID + "' from combined mesh");
					meshFilter.mesh = meshResult.RemoveMesh(instanceID, meshFilter.mesh);
					flag = true;
				}
			}
			if (!flag)
			{
				Debug.LogWarning("[Super Combiner] Could not remove object '" + instanceID + "' because it was not found");
			}
		}
	}
}
