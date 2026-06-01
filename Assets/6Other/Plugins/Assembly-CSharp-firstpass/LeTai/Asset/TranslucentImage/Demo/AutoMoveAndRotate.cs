using System;
using UnityEngine;

namespace LeTai.Asset.TranslucentImage.Demo
{
	public class AutoMoveAndRotate : MonoBehaviour
	{
		[Serializable]
		public class Vector3andSpace
		{
			public Vector3 value;

			public Space space = Space.Self;
		}

		public Vector3andSpace moveUnitsPerSecond;

		public Vector3andSpace rotateDegreesPerSecond;

		public bool ignoreTimescale;

		public bool lateUpdate;

		private float m_LastRealTime;

		private void Start()
		{
			m_LastRealTime = Time.realtimeSinceStartup;
		}

		private void Update()
		{
			if (!lateUpdate)
			{
				DoWork();
			}
		}

		private void LateUpdate()
		{
			if (lateUpdate)
			{
				DoWork();
			}
		}

		private void DoWork()
		{
			float num = Time.deltaTime;
			if (ignoreTimescale)
			{
				num = Time.realtimeSinceStartup - m_LastRealTime;
				m_LastRealTime = Time.realtimeSinceStartup;
			}
			base.transform.Translate(moveUnitsPerSecond.value * num, moveUnitsPerSecond.space);
			base.transform.Rotate(rotateDegreesPerSecond.value * num, moveUnitsPerSecond.space);
		}
	}
}
