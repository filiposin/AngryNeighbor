using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class MaterialToCombine
	{
		public Material material;

		public Rect uvBounds;

		public int combinedIndex;

		public int index;

		public Rect GetScaledAndOffsetedUVBounds()
		{
			Rect result = uvBounds;
			if (material.HasProperty("_MainTex"))
			{
				result.size = Vector2.Scale(result.size, material.mainTextureScale);
				result.position += material.mainTextureOffset;
			}
			return result;
		}
	}
}
