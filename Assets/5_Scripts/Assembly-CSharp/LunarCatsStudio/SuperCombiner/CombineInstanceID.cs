using System.Collections.Generic;
using UnityEngine;

namespace LunarCatsStudio.SuperCombiner
{
	public class CombineInstanceID
	{
		public List<CombineInstance> combineInstances = new List<CombineInstance>();

		public List<int> instancesID = new List<int>();

		public List<string> names = new List<string>();

		public void AddCombineInstance(int subMeshIndex, Mesh mesh, Matrix4x4 matrix, int instanceID, string name)
		{
			CombineInstance item = new CombineInstance
			{
				subMeshIndex = subMeshIndex,
				mesh = mesh,
				transform = matrix
			};
			combineInstances.Add(item);
			instancesID.Add(instanceID);
			names.Add(name);
		}

		public void AddRange(CombineInstanceID instances)
		{
			combineInstances.AddRange(instances.combineInstances);
			instancesID.AddRange(instances.instancesID);
			names.AddRange(instances.names);
		}

		public void Clear()
		{
			combineInstances.Clear();
			instancesID.Clear();
			names.Clear();
		}

		public int Count()
		{
			return combineInstances.Count;
		}
	}
}
