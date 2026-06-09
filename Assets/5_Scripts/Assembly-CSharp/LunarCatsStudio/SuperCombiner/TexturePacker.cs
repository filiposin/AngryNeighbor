using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class TexturePacker
	{
		public Material copyedMaterials;

		public Material copyedToSaveMaterials;

		private Dictionary<string, List<Texture2D>> texturesForAtlas = new Dictionary<string, List<Texture2D>>();

		public Dictionary<string, Texture2D> packedTextures = new Dictionary<string, Texture2D>();

		private int combinedIndex;

		public Dictionary<string, string> TexturePropertyNames = new Dictionary<string, string>
		{
			{ "_MainTex", "Diffuse" },
			{ "_BumpMap", "Normal" },
			{ "_SpecGlossMap", "Specular" },
			{ "_ParallaxMap", "Height" },
			{ "_OcclusionMap", "Occlusion" },
			{ "_EmissionMap", "Emission" },
			{ "_DetailMask", "Detail Mask" },
			{ "_DetailAlbedoMap", "Detail Diffuse" },
			{ "_DetailNormalMap", "Detail Normal" },
			{ "_MetallicGlossMap", "Metallic" },
			{ "_LightMap", "Light Map" }
		};

		private bool _hasEmission;

		private Color _emissionColor = Color.black;

		private List<string> customProperties = new List<string>();

		private Dictionary<int, TextureImportSettings> importedTextures = new Dictionary<int, TextureImportSettings>();

		private const int NO_TEXTURE_COLOR_SIZE = 256;

		private const int MAX_TEXTURE_SIZE = 16384;

		private CombinedResult combinedResult;

		public int CombinedIndex
		{
			get
			{
				return combinedIndex;
			}
			set
			{
				combinedIndex = value;
			}
		}

		public CombinedResult CombinedResult
		{
			set
			{
				combinedResult = value;
			}
		}

		public TexturePacker()
		{
			foreach (KeyValuePair<string, string> texturePropertyName in TexturePropertyNames)
			{
				texturesForAtlas.Add(texturePropertyName.Key, new List<Texture2D>());
			}
		}

		public void SetCustomPropertyNames(List<string> list)
		{
			foreach (string item in list)
			{
				if (!TexturePropertyNames.ContainsKey(item))
				{
					TexturePropertyNames.Add(item, item);
					customProperties.Add(item);
					texturesForAtlas.Add(item, new List<Texture2D>());
				}
			}
		}

		public Material GetCombinedMaterialToSave()
		{
			return copyedToSaveMaterials;
		}

		public void GenerateCopyedMaterialToSave()
		{
			Material material = new Material(copyedMaterials);
			copyedToSaveMaterials = material;
		}

		public void SetCopyedMaterial(Material mat)
		{
			copyedMaterials = mat;
		}

		public void ClearTextures()
		{
			packedTextures.Clear();
			texturesForAtlas.Clear();
			foreach (string customProperty in customProperties)
			{
				TexturePropertyNames.Remove(customProperty);
			}
			customProperties.Clear();
			foreach (KeyValuePair<string, string> texturePropertyName in TexturePropertyNames)
			{
				texturesForAtlas.Add(texturePropertyName.Key, new List<Texture2D>());
			}
			importedTextures.Clear();
			_hasEmission = false;
			_emissionColor = Color.black;
		}

		private Vector3 GetTextureSizeInAtlas(Vector2 inputTextureSize, float scaleX, float scaleY, string materialName)
		{
			Vector3 result = new Vector3(inputTextureSize.x * scaleX, inputTextureSize.y * scaleY, 1f);
			if (result.x >= 16384f || result.y >= 16384f)
			{
				int num = (int)Mathf.Max(Mathf.Ceil(result.x / (float)SystemInfo.maxTextureSize), Mathf.Ceil(result.y / (float)SystemInfo.maxTextureSize));
				result.Set(result.x / (float)num, result.y / (float)num, num);
				Debug.LogWarning("[Super Combiner] Textures in material '" + materialName + "' are being tiled and the total tiled size exceeds the maximum texture size for the current plateform (" + SystemInfo.maxTextureSize + "). All textures in this material will be shrunk by " + num + " to fit in the atlas. This could leads to a quality loss. Whenever possible, avoid combining tiled texture.");
			}
			return result;
		}

		private Texture2D CopyTexture(Texture2D texture, Rect materialUVBounds, Rect meshUVBounds, Material mat, Vector3 textureInAtlasSize, Vector2 targetTextureSize, bool isMainTexture)
		{
			int num = (int)Mathf.Sign(materialUVBounds.width);
			int num2 = (int)Mathf.Sign(materialUVBounds.height);
			float num3 = Mathf.Abs(materialUVBounds.width);
			float num4 = Mathf.Abs(materialUVBounds.height);
			Color color = Color.white;
			if (mat.HasProperty("_Color"))
			{
				color = mat.color;
			}
			bool flag = false;
			if (num3 != 1f || num4 != 1f || materialUVBounds.position != Vector2.zero)
			{
				flag = true;
			}
			if (!CheckTextureImportSettings(texture).isReadable)
			{
				Debug.LogError("[Super Combiner] The format of texture '" + texture.name + "' is not handled by Unity. Try manually setting 'Read/Write Enabled' parameter to true or converting this texture into a known format.");
				return CreateColoredTexture2D((int)textureInAtlasSize.x, (int)textureInAtlasSize.y, Color.white);
			}
			Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
			texture2D.name = texture.name;
			texture2D.SetPixels(texture.GetPixels());
			if (texture.width != (int)targetTextureSize.x || texture.height != (int)targetTextureSize.y)
			{
				TextureScale.Bilinear(texture2D, (int)targetTextureSize.x, (int)targetTextureSize.y);
				Debug.LogWarning("[Super Combiner] Texture '" + texture.name + "' will be scaled from " + GetStringTextureSize(texture.width, texture.height) + " to " + GetStringTextureSize(targetTextureSize.x, targetTextureSize.y) + " to match the size of the other textures in material '" + mat.name + "'");
			}
			Texture2D texture2D2 = new Texture2D((int)textureInAtlasSize.x, (int)textureInAtlasSize.y, texture2D.format, false);
			texture2D2.name = texture.name;
			if (textureInAtlasSize.z != 1f && textureInAtlasSize.z > 0f)
			{
				TextureScale.Bilinear(texture2D, texture2D.width / (int)textureInAtlasSize.z, texture2D.height / (int)textureInAtlasSize.z);
			}
			if (flag)
			{
				if (Mathf.Abs(textureInAtlasSize.x - (float)texture2D.width) > 1f || Mathf.Abs(textureInAtlasSize.y - (float)texture2D.height) > 1f)
				{
					Debug.Log("[Super Combiner] Texture '" + texture.name + "' is being tiled in the atlas because mesh using it has UVs out of [0, 1] bound. The tiled size is " + GetStringTextureSize(textureInAtlasSize.x, textureInAtlasSize.y) + ".");
				}
				int num5 = (int)(meshUVBounds.xMin * (float)texture2D.width * mat.mainTextureScale.x + mat.mainTextureOffset.x * (float)texture2D.width);
				int num6 = (int)(meshUVBounds.yMin * (float)texture2D.height * mat.mainTextureScale.y + mat.mainTextureOffset.y * (float)texture2D.height);
				int i = 0;
				int j = 0;
				if (num < 0 || num2 < 0 || (!color.Equals(Color.white) && isMainTexture))
				{
					for (i = 0; i < texture2D2.width; i++)
					{
						for (j = 0; j < texture2D2.height; j++)
						{
							texture2D2.SetPixel(i, j, texture2D.GetPixel(num * (i + num5) % texture2D.width, num2 * (j + num6) % texture2D.height) * color);
						}
					}
				}
				else
				{
					int num7 = 0;
					int num8 = 0;
					for (; i < texture2D2.width; i += num7)
					{
						int num9 = (num * (i + num5) % texture2D.width + texture2D.width) % texture2D.width;
						num7 = ((i + texture2D.width > texture2D2.width) ? (texture2D2.width - i) : texture2D.width);
						if (num9 + num7 > texture2D.width)
						{
							num7 = texture2D.width - num9;
						}
						for (; j < texture2D2.height; j += num8)
						{
							int num10 = (num2 * (j + num6) % texture2D.height + texture2D.height) % texture2D.height;
							num8 = ((j + texture2D.height > texture2D2.height) ? (texture2D2.height - j) : texture2D.height);
							if (num10 + num8 > texture2D.height)
							{
								num8 = texture2D.height - num10;
							}
							texture2D2.SetPixels(i, j, num7, num8, texture2D.GetPixels(num9, num10, num7, num8));
						}
						j = 0;
					}
				}
			}
			else if (color.Equals(Color.white) || !isMainTexture)
			{
				texture2D2.LoadRawTextureData(texture2D.GetRawTextureData());
			}
			else
			{
				for (int k = 0; k < texture2D2.width; k++)
				{
					for (int l = 0; l < texture2D2.height; l++)
					{
						texture2D2.SetPixel(k, l, texture2D.GetPixel(k, l) * color);
					}
				}
			}
			return texture2D2;
		}

		private Vector2 checkTexturesSize(Material mat, bool alignToSmallest)
		{
			Vector2 vector = Vector2.zero;
			foreach (string key in TexturePropertyNames.Keys)
			{
				if (!mat.HasProperty(key))
				{
					continue;
				}
				Texture texture = mat.GetTexture(key);
				if (!(texture != null) || ((float)texture.width == vector.x && (float)texture.height == vector.y))
				{
					continue;
				}
				if (alignToSmallest)
				{
					if (vector != Vector2.zero)
					{
						Debug.LogWarning("[Super Combiner] Material '" + mat.name + "' has various textures with different size! Textures in this material will be scaled to match the smallest one.\nTo avoid this, ensure to have all textures in a material of the same size. Try adjusting 'Max Size' in import settings.");
					}
					if (vector == Vector2.zero || (float)(texture.width * texture.height) < vector.x * vector.y)
					{
						vector = new Vector2(texture.width, texture.height);
					}
				}
				else
				{
					if (vector != Vector2.zero)
					{
						Debug.LogWarning("[Super Combiner] Material '" + mat.name + "' has various textures with different size! Textures in this material will be scaled to match the biggest one.\nTo avoid this, ensure to have all textures in a material of the same size. Try adjusting 'Max Size' in import settings.");
					}
					if (vector == Vector2.zero || (float)(texture.width * texture.height) > vector.x * vector.y)
					{
						vector = new Vector2(texture.width, texture.height);
					}
				}
			}
			if (vector == Vector2.zero)
			{
				vector = new Vector2(256f, 256f);
			}
			return vector;
		}

		public void SetTextures(Material mat, bool combineMaterials, Rect materialUVBounds, Rect meshUVBounds, float tilingFactor)
		{
			Vector2 vector = checkTexturesSize(mat, false);
			if (tilingFactor > 1f)
			{
				combinedResult.combinedMaterials[combinedIndex].scaleFactors.Add(tilingFactor);
				materialUVBounds.size = Vector2.Scale(materialUVBounds.size, Vector2.one * tilingFactor);
				meshUVBounds.position -= new Vector2(meshUVBounds.width * (tilingFactor - 1f) / 2f, meshUVBounds.height * (tilingFactor - 1f) / 2f);
			}
			else
			{
				combinedResult.combinedMaterials[combinedIndex].scaleFactors.Add(1f);
			}
			combinedResult.combinedMaterials[combinedIndex].meshUVBounds.Add(meshUVBounds);
			Vector3 textureInAtlasSize = GetTextureSizeInAtlas(vector, Mathf.Abs(materialUVBounds.width), Mathf.Abs(materialUVBounds.height), mat.name);
			foreach (KeyValuePair<string, List<Texture2D>> texturesForAtla in texturesForAtlas)
			{
				if (mat.HasProperty(texturesForAtla.Key))
				{
					Texture texture = mat.GetTexture(texturesForAtla.Key);
					if (texture != null)
					{
						Texture2D texture2D = CopyTexture((Texture2D)texture, materialUVBounds, meshUVBounds, mat, textureInAtlasSize, vector, texturesForAtla.Key.Equals("_MainTex"));
						textureInAtlasSize = new Vector2(texture2D.width, texture2D.height);
						texturesForAtla.Value.Add(texture2D);
						if (importedTextures.ContainsKey(texture.GetInstanceID()) && !importedTextures[texture.GetInstanceID()].isNormal)
						{
						}
					}
					else if (texturesForAtla.Key.Equals("_MainTex"))
					{
						texturesForAtla.Value.Add(CreateColoredTexture2D((int)textureInAtlasSize.x, (int)textureInAtlasSize.y, (!mat.HasProperty("_Color")) ? Color.white : mat.color));
						Debug.Log(string.Concat("[Super Combiner] Creating a colored texture ", (!mat.HasProperty("_Color")) ? Color.white : mat.color, " of size ", GetStringTextureSize(textureInAtlasSize.x, textureInAtlasSize.y), " for ", TexturePropertyNames[texturesForAtla.Key], " in material '", mat.name, "' because texture is missing."));
					}
					else if (texturesForAtla.Key.Equals("_EmissionMap") && mat.IsKeywordEnabled("_EMISSION") && mat.GetColor("_EmissionColor") != Color.black)
					{
						texturesForAtla.Value.Add(CreateColoredTexture2D((int)textureInAtlasSize.x, (int)textureInAtlasSize.y, Color.white));
						_hasEmission = true;
						_emissionColor = mat.GetColor("_EmissionColor");
						Debug.Log("[Super Combiner] Creating a white texture of size " + GetStringTextureSize(textureInAtlasSize.x, textureInAtlasSize.y) + " for " + TexturePropertyNames[texturesForAtla.Key] + " in material '" + mat.name + "' because texture is missing.");
					}
					else if (texturesForAtla.Value.Count > 0)
					{
						texturesForAtla.Value.Add(CreateColoredTexture2D((int)textureInAtlasSize.x, (int)textureInAtlasSize.y, DefaultColoredTexture.GetDefaultTextureColor(texturesForAtla.Key)));
						Debug.Log(string.Concat("[Super Combiner] Creating a colored texture ", DefaultColoredTexture.GetDefaultTextureColor(texturesForAtla.Key), " of size ", GetStringTextureSize(textureInAtlasSize.x, textureInAtlasSize.y), " for ", TexturePropertyNames[texturesForAtla.Key], " in material '", mat.name, "' because texture is missing."));
					}
				}
				else if (texturesForAtla.Key.Equals("_MainTex"))
				{
					texturesForAtla.Value.Add(CreateColoredTexture2D((int)vector.x, (int)vector.y, (!mat.HasProperty("_Color")) ? Color.white : mat.color));
					Debug.Log(string.Concat("[Super Combiner] Creating a colored texture ", DefaultColoredTexture.GetDefaultTextureColor(texturesForAtla.Key), " of size ", GetStringTextureSize(textureInAtlasSize.x, textureInAtlasSize.y), " for ", TexturePropertyNames[texturesForAtla.Key], " in material '", mat.name, "' because texture is missing."));
				}
				else if (texturesForAtla.Value.Count > 0)
				{
					Debug.LogWarning("[Super Combiner] Found materials with properties that don't match. Maybe you are trying to combine different shaders that don't share the same properties.");
				}
			}
		}

		private string GetStringTextureSize(float width, float height)
		{
			return (int)width + "x" + (int)height + " pixels";
		}

		private Texture2D CreateColoredTexture2D(int width, int height, Color color)
		{
			Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					texture2D.SetPixel(i, j, color);
				}
			}
			texture2D.Apply();
			return texture2D;
		}

		private TextureImportSettings CheckTextureImportSettings(Texture2D texture)
		{
			TextureImportSettings textureImportSettings = new TextureImportSettings
			{
				isReadable = true
			};
			if (!importedTextures.ContainsKey(texture.GetInstanceID()))
			{
				importedTextures.Add(texture.GetInstanceID(), textureImportSettings);
			}
			return textureImportSettings;
		}

		private void CheckTexturesConformity()
		{
			int num = 0;
			foreach (KeyValuePair<string, List<Texture2D>> texturesForAtla in texturesForAtlas)
			{
				if (texturesForAtla.Value.Count > 0)
				{
					num = Mathf.Max(num, texturesForAtla.Value.Count);
				}
			}
			if (num <= 0)
			{
				return;
			}
			foreach (KeyValuePair<string, List<Texture2D>> texturesForAtla2 in texturesForAtlas)
			{
				if (texturesForAtla2.Value.Count > 0 && texturesForAtla2.Value.Count < num)
				{
					int num2 = num - texturesForAtla2.Value.Count;
					for (int i = 0; i < num2; i++)
					{
						int width = texturesForAtlas["_MainTex"][i].width;
						int height = texturesForAtlas["_MainTex"][i].height;
						texturesForAtla2.Value.Insert(0, CreateColoredTexture2D(width, height, DefaultColoredTexture.GetDefaultTextureColor(texturesForAtla2.Key)));
						Debug.Log(string.Concat("[Super Combiner] Creating a colored texture ", DefaultColoredTexture.GetDefaultTextureColor(texturesForAtla2.Key), " of size ", GetStringTextureSize(width, height), " for ", TexturePropertyNames[texturesForAtla2.Key], " because texture is missing."));
					}
				}
			}
		}

		public void PackTextures(int textureAtlasSize, int atlasPadding, bool combineMaterials, string name)
		{
			CheckTexturesConformity();
			int num = 0;
			foreach (KeyValuePair<string, List<Texture2D>> texturesForAtla in texturesForAtlas)
			{
				if (texturesForAtla.Value.Count > 0)
				{
					Texture2D texture2D = new Texture2D(textureAtlasSize, textureAtlasSize, TextureFormat.RGBA32, false);
					texture2D.Reinitialize(textureAtlasSize, textureAtlasSize);
					Rect[] uvs = texture2D.PackTextures(texturesForAtla.Value.ToArray(), atlasPadding, textureAtlasSize);
					packedTextures.Add(texturesForAtla.Key, texture2D);
					if (texturesForAtla.Key.Equals("_MainTex"))
					{
						combinedResult.combinedMaterials[combinedIndex].uvs = uvs;
					}
				}
				num++;
			}
			if (!combineMaterials)
			{
				return;
			}
			Material material;
			if (combinedResult.originalMaterialList[combinedIndex].Count > 0)
			{
				material = new Material(combinedResult.originalMaterialList[combinedIndex][combinedResult.originalReferenceMaterial[combinedIndex]].material.shader);
				material.CopyPropertiesFromMaterial(combinedResult.originalMaterialList[combinedIndex][combinedResult.originalReferenceMaterial[combinedIndex]].material);
			}
			else
			{
				Debug.LogError("[Super Combiner] No reference material to create the combined material. A default standard shader material will be created");
				material = new Material(Shader.Find("Standard"));
			}
			material.mainTextureOffset = Vector2.zero;
			material.mainTextureScale = Vector2.one;
			material.color = Color.white;
			material.name = name + "_material";
			foreach (KeyValuePair<string, Texture2D> packedTexture in packedTextures)
			{
				material.SetTexture(packedTexture.Key, packedTexture.Value);
			}
			if (_hasEmission)
			{
				material.SetColor("_EmissionColor", _emissionColor);
				material.EnableKeyword("_EMISSION");
			}
			copyedMaterials = material;
			combinedResult.SetCombinedMaterial(material, combinedIndex, false);
		}

		public void SaveTextures(string folder, string name)
		{
		}

		public string GetTextureFilePathName(string folder, string sessionName, string textureName, int displayedIndex)
		{
			return folder + "/Textures/" + sessionName + "_" + textureName + "_" + displayedIndex + ".png";
		}
	}
}
