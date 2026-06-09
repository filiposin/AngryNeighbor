using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class ObjSaver
	{
		public static int StartIndex;

		public static void SaveObjFile(GameObject obj, bool makeSubmeshes, string floderPath)
		{
			string name = obj.name;
			string filename = floderPath + "/" + name + ".obj";
			StartIndex = 0;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("#" + name + ".obj\n#" + DateTime.Now.ToLongDateString() + "\n#" + DateTime.Now.ToLongTimeString() + "\n#-------\n\n");
			Transform transform = obj.transform;
			Vector3 position = transform.position;
			transform.position = Vector3.zero;
			if (!makeSubmeshes)
			{
				stringBuilder.Append("g ").Append(transform.name).Append("\n");
			}
			stringBuilder.Append(processTransform(transform, makeSubmeshes));
			WriteToFile(stringBuilder.ToString(), filename);
			transform.position = position;
			StartIndex = 0;
		}

		private static void WriteToFile(string s, string filename)
		{
			using (StreamWriter streamWriter = new StreamWriter(filename))
			{
				streamWriter.Write(s);
			}
		}

		private static string processTransform(Transform t, bool makeSubmeshes)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("#" + t.name + "\n#-------\n");
			if (makeSubmeshes)
			{
				stringBuilder.Append("g ").Append(t.name).Append("\n");
			}
			MeshFilter component = t.GetComponent<MeshFilter>();
			SkinnedMeshRenderer component2 = t.GetComponent<SkinnedMeshRenderer>();
			if ((bool)component)
			{
				stringBuilder.Append(MeshToString(component.sharedMesh, component.GetComponent<Renderer>().sharedMaterials, t));
			}
			if ((bool)component2)
			{
				stringBuilder.Append(MeshToString(component2.sharedMesh, component2.sharedMaterials, t));
			}
			for (int i = 0; i < t.childCount; i++)
			{
				stringBuilder.Append(processTransform(t.GetChild(i), makeSubmeshes));
			}
			return stringBuilder.ToString();
		}

		public static string MeshToString(Mesh m, Material[] mats, Transform t)
		{
			Quaternion localRotation = t.localRotation;
			int num = 0;
			if (!m)
			{
				return "####Error####";
			}
			StringBuilder stringBuilder = new StringBuilder();
			Vector3[] vertices = m.vertices;
			foreach (Vector3 position in vertices)
			{
				Vector3 vector = t.TransformPoint(position);
				num++;
				stringBuilder.Append(string.Format("v {0} {1} {2}\n", vector.x, vector.y, 0f - vector.z));
			}
			stringBuilder.Append("\n");
			Vector3[] normals = m.normals;
			foreach (Vector3 vector2 in normals)
			{
				Vector3 vector3 = localRotation * vector2;
				stringBuilder.Append(string.Format("vn {0} {1} {2}\n", 0f - vector3.x, 0f - vector3.y, vector3.z));
			}
			stringBuilder.Append("\n");
			Vector2[] uv = m.uv;
			for (int k = 0; k < uv.Length; k++)
			{
				Vector3 vector4 = uv[k];
				stringBuilder.Append(string.Format("vt {0} {1}\n", vector4.x, vector4.y));
			}
			for (int l = 0; l < m.subMeshCount; l++)
			{
				stringBuilder.Append("\n");
				stringBuilder.Append("usemtl ").Append(mats[l].name).Append("\n");
				stringBuilder.Append("usemap ").Append(mats[l].name).Append("\n");
				int[] triangles = m.GetTriangles(l);
				for (int n = 0; n < triangles.Length; n += 3)
				{
					stringBuilder.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n", triangles[n] + 1 + StartIndex, triangles[n + 1] + 1 + StartIndex, triangles[n + 2] + 1 + StartIndex));
				}
			}
			StartIndex += num;
			return stringBuilder.ToString();
		}
	}
}
