using UnityEngine;
using UnityEngine.EventSystems;

namespace CnControls
{
	public class Dpad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IEventSystemHandler
	{
		public DpadAxis[] DpadAxis;

		public Camera CurrentEventCamera { get; set; }

		public void OnPointerDown(PointerEventData eventData)
		{
			CurrentEventCamera = eventData.pressEventCamera ?? CurrentEventCamera;
			DpadAxis[] dpadAxis = DpadAxis;
			foreach (DpadAxis dpadAxis2 in dpadAxis)
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(dpadAxis2.RectTransform, eventData.position, CurrentEventCamera))
				{
					dpadAxis2.Press(eventData.position, CurrentEventCamera, eventData.pointerId);
				}
			}
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			DpadAxis[] dpadAxis = DpadAxis;
			foreach (DpadAxis dpadAxis2 in dpadAxis)
			{
				dpadAxis2.TryRelease(eventData.pointerId);
			}
		}
	}
}
