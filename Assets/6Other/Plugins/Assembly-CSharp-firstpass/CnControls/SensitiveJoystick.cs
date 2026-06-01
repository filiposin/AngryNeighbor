using UnityEngine;
using UnityEngine.EventSystems;

namespace CnControls
{
	public class SensitiveJoystick : SimpleJoystick
	{
		public AnimationCurve SensitivityCurve = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));

		public override void OnDrag(PointerEventData eventData)
		{
			base.OnDrag(eventData);
			float value = HorizintalAxis.Value;
			float value2 = VerticalAxis.Value;
			float num = Mathf.Sign(value);
			float num2 = Mathf.Sign(value2);
			HorizintalAxis.Value = num * SensitivityCurve.Evaluate(num * value);
			VerticalAxis.Value = num2 * SensitivityCurve.Evaluate(num2 * value2);
		}
	}
}
