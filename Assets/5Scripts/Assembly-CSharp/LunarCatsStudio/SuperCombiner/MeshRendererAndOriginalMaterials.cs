using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	internal class MeshRendererAndOriginalMaterials
	{
		public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

		public List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

		public List<Material[]> originalMaterials = new List<Material[]>();

		public List<Material[]> originalskinnedMeshMaterials = new List<Material[]>();

		public List<GameObject> splittedGameObject = new List<GameObject>();
	}
}
