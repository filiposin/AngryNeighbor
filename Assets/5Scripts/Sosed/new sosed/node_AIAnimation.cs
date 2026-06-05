using UnityEngine;

public class node_AIAnimation : MonoBehaviour
{
	[Tooltip("If true, ik compatable models will look at target when in chase mode.")]
	public bool useIK;

	[Tooltip("use this to adjust where AI looks. By default ai will look at the center of its target, raise the offset value to raise where the agent looks")]
	public float lookOffset = 2f;

	private Animator m_Animator;

	private node_AIMovement aiM;

	private Rigidbody m_Rigidbody;

	private void Start()
	{
		aiM = base.transform.GetComponent<node_AIMovement>();
		m_Animator = GetComponent<Animator>();
		m_Rigidbody = GetComponent<Rigidbody>();
	}

	private void OnAnimatorIK()
	{
		if (useIK && aiM.chase)
		{
			m_Animator.SetLookAtWeight(1f);
			m_Animator.SetLookAtPosition(new Vector3(aiM.target.transform.position.x, aiM.target.transform.position.y + lookOffset, aiM.target.transform.position.z));
		}
	}

	public void OnAnimatorMove()
	{
		// if (Time.deltaTime > 0f)
		// {
		// 	Vector3 velocity = m_Animator.deltaPosition * aiM.speedSettings.m_MoveSpeedMultiplier / Time.deltaTime;
		// 	velocity.y = m_Rigidbody.velocity.y;
		// 	m_Rigidbody.velocity = velocity;
		// }
	}

	public void UpdateAnimator(float fAmount, float tAmount, float smooth, float animSpeed)
	{
		m_Animator.SetFloat("Forward", fAmount, smooth, Time.deltaTime);
		m_Animator.SetFloat("Turn", tAmount, smooth, Time.deltaTime);
		m_Animator.speed = animSpeed;
	}
}
