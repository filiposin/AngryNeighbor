using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	[Serializable]
	public class CombinedMaterial
	{
		public Material material;

		public Rect[] uvs;

		public Rect[] uvs2;

		public List<float> scaleFactors = new List<float>();

		public List<Rect> meshUVBounds = new List<Rect>();

		public bool isOriginalMaterial;

		public int displayedIndex;

		public bool showCombinedMaterial;

		public bool showUVs;

		public bool showMeshUVBounds;
	}
}
