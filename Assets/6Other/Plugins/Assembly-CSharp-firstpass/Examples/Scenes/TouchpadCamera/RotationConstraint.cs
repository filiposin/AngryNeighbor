using UnityEngine;

namespace Examples.Scenes.TouchpadCamera
{
	public class RotationConstraint : MonoBehaviour
	{
		public float Min = -15f;

		public float Max = 15f;

		private Transform _transformCache;

		private Quaternion _minQuaternion;

		private Quaternion _maxQuaternion;

		private Vector3 _rotateAround;

		private float _range;

		private void Awake()
		{
			_transformCache = base.transform;
			_rotateAround = Vector3.right;
			Quaternion quaternion = Quaternion.AngleAxis(_transformCache.localRotation.eulerAngles[0], _rotateAround);
			_minQuaternion = quaternion * Quaternion.AngleAxis(Min, _rotateAround);
			_maxQuaternion = quaternion * Quaternion.AngleAxis(Max, _rotateAround);
			_range = Max - Min;
		}

		private void LateUpdate()
		{
			Quaternion localRotation = _transformCache.localRotation;
			Quaternion a = Quaternion.AngleAxis(localRotation.eulerAngles[0], _rotateAround);
			float num = Quaternion.Angle(a, _minQuaternion);
			float num2 = Quaternion.Angle(a, _maxQuaternion);
			if (!(num <= _range) || !(num2 <= _range))
			{
				Vector3 eulerAngles = localRotation.eulerAngles;
				if (num > num2)
				{
					eulerAngles[0] = _maxQuaternion.eulerAngles[0];
				}
				else
				{
					eulerAngles[0] = _minQuaternion.eulerAngles[0];
				}
				_transformCache.localEulerAngles = eulerAngles;
			}
		}
	}
}
