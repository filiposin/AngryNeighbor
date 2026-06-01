using UnityEngine;

namespace UnityStandardAssets.Copy._2D
{
	public class PlatformerCharacter2D : MonoBehaviour
	{
		[SerializeField]
		private float m_MaxSpeed = 10f;

		[SerializeField]
		private float m_JumpForce = 400f;

		[SerializeField]
		private bool m_AirControl;

		[SerializeField]
		private LayerMask m_WhatIsGround;

		private Transform m_GroundCheck;

		private const float k_GroundedRadius = 0.2f;

		private bool m_Grounded;

		private Animator m_Anim;

		private Rigidbody2D m_Rigidbody2D;

		private bool m_FacingRight = true;

		private void Awake()
		{
			m_GroundCheck = base.transform.Find("GroundCheck");
			m_Anim = GetComponent<Animator>();
			m_Rigidbody2D = GetComponent<Rigidbody2D>();
		}

		private void FixedUpdate()
		{
			m_Grounded = false;
			Collider2D[] array = Physics2D.OverlapCircleAll(m_GroundCheck.position, 0.2f, m_WhatIsGround);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].gameObject != base.gameObject)
				{
					m_Grounded = true;
				}
			}
			m_Anim.SetBool("Ground", m_Grounded);
		}

		public void Move(float move, bool jump)
		{
			if (m_Grounded || m_AirControl)
			{
				m_Anim.SetFloat("Speed", Mathf.Abs(move));
				m_Rigidbody2D.velocity = new Vector2(move * m_MaxSpeed, m_Rigidbody2D.velocity.y);
				if (move > 0f && !m_FacingRight)
				{
					Flip();
				}
				else if (move < 0f && m_FacingRight)
				{
					Flip();
				}
			}
			if (m_Grounded && jump && m_Anim.GetBool("Ground"))
			{
				m_Grounded = false;
				m_Anim.SetBool("Ground", false);
				m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
			}
		}

		private void Flip()
		{
			m_FacingRight = !m_FacingRight;
			Vector3 localScale = base.transform.localScale;
			localScale.x *= -1f;
			base.transform.localScale = localScale;
		}
	}
}
