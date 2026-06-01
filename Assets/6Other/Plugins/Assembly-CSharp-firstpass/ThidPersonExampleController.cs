using CnControls;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThidPersonExampleController : MonoBehaviour
{
	public float MovementSpeed = 10f;

	private Transform _mainCameraTransform;

	private Transform _transform;

	private CharacterController _characterController;

	private void OnEnable()
	{
		_mainCameraTransform = Camera.main.GetComponent<Transform>();
		_characterController = GetComponent<CharacterController>();
		_transform = GetComponent<Transform>();
	}

	public void Update()
	{
		Vector3 direction = new Vector3(CnInputManager.GetAxis("Horizontal"), CnInputManager.GetAxis("Vertical"));
		Vector3 vector = Vector3.zero;
		if (direction.sqrMagnitude > 0.001f)
		{
			vector = _mainCameraTransform.TransformDirection(direction);
			vector.y = 0f;
			vector.Normalize();
			_transform.forward = vector;
		}
		vector += Physics.gravity;
		_characterController.Move(vector * Time.deltaTime);
	}
}
