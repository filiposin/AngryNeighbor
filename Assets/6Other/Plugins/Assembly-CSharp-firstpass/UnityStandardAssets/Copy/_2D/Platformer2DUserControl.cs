using CnControls;
using UnityEngine;

namespace UnityStandardAssets.Copy._2D
{
	[RequireComponent(typeof(PlatformerCharacter2D))]
	public class Platformer2DUserControl : MonoBehaviour
	{
		private PlatformerCharacter2D m_Character;

		private bool m_Jump;

		private void Awake()
		{
			m_Character = GetComponent<PlatformerCharacter2D>();
		}

		private void Update()
		{
			if (!m_Jump)
			{
				m_Jump = CnInputManager.GetButtonDown("Jump");
			}
		}

		private void FixedUpdate()
		{
			float axis = CnInputManager.GetAxis("Horizontal");
			m_Character.Move(axis, m_Jump);
			m_Jump = false;
		}
	}
}
