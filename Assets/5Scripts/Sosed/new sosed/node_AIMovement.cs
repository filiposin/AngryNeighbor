using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class node_AIMovement : MonoBehaviour
{
	public enum AIType
	{
		wander = 0,
		nodeWander = 1
	}

	[Serializable]
	public class WanderOptions
	{
		[Tooltip("minimum time agent will pause before picking a new wander destination")]
		public float wanderPauseTimeLow = 3f;

		[Tooltip("max time agent will pause before picking a new wander destination")]
		public float wanderPauseTimeHigh = 6f;

		[Tooltip("max distance from current position agent will wander")]
		public float wanderRadius = 25f;
	}

	[Serializable]
	public class SpeedOptions
	{
		[Tooltip("speed to chase target")]
		public float chaseSpeed = 1f;

		[Tooltip("normal speed for agent, used during its non chase behaviors")]
		public float walkSpeed = 0.5f;

		[Tooltip("Helps character turn faster")]
		public float m_MovingTurnSpeed = 360f;

		[Tooltip("Helps character turn faster")]
		public float m_StationaryTurnSpeed = 180f;

		[Tooltip("Multiplies the agents move speed. Best left at 1")]
		public float m_MoveSpeedMultiplier = 1f;

		[Tooltip("Multiplies the agents animation speed. Best left at 1")]
		public float m_AnimSpeedMultiplier = 1f;

		[Tooltip("Adjust the speed the agent looks at the player")]
		public float rotateSpeed = 7f;

		[Tooltip("Used to smooth out movement transistions. Higher values are smoother but less responsive")]
		public float smoothMove = 0.2f;
	}

	[Serializable]
	public class DistanceOptions
	{
		[Tooltip("Max distance agent will stop chasing player. Set a higher distance for ranged enemies, and a short distance for melee enemies, for example.")]
		public float stoppingDistanceHigh = 7f;

		[Tooltip("Minimum distance agent will stop chasing player. Set both stopping distance settings if you want the enemy to stop chasing the player at a set distance. With a melee character for instance, you would probably want both settings to be 0.5 or 1.")]
		public float stoppingDistanceLow = 2.5f;

		[Tooltip("the maximum distance at which the enemy will attempt to attack player")]
		public float attackingDistance = 100f;

		[Tooltip("the distance at which a enemy will detect a player, regardless of position.")]
		public float personalAwarenessDistance = 3f;
	}

	[Serializable]
	public class MarkerOptions
	{
		[Tooltip("Optional object used to ID AI on players HUD. typically a pointer or some other object above enemy head.")]
		public GameObject myMark;

		[Tooltip("activates AI marker when it notices player. ")]
		public bool markActiveOnAlert = true;

		[Tooltip("marker color when AI is completely unaware of player")]
		public Color markerUnawareColor = Color.green;

		[Tooltip("color when player is in range of AI. if AI makes visual contact with Player, it could notice player")]
		public Color markerCautionColor = Color.yellow;

		[Tooltip("color when AI is chasing/attacking player")]
		public Color markerAttackColor = Color.red;
	}

	[Serializable]
	public class DetectionOptions
	{
		[Tooltip("toggles rather the AI can detect enemies all around him. If false, AI can only detect player in front.")]
		public bool omniVision;

		[Tooltip("default distance the AI can see")]
		public float normalVisionDistance = 40f;

		[Tooltip("how far the AI can see when pursuing player")]
		public float pursueVisionDistance = 100f;

		[HideInInspector]
		public float visionDistance = 40f;

		[Tooltip("the fastest the AI can react to seeing the player")]
		public float reactionTimeLow = 0.5f;

		[Tooltip("the slowest the AI can react to seeing the player")]
		public float reactionTimeHigh = 5f;

		[Tooltip("Affects how distance from player changes reaction time")]
		public float reactionTimeSmooth = 0.15f;

		[Tooltip("how long an AI will search/chase a player after loosing sight ")]
		public float searchTime = 10f;

		[Tooltip("how long an AI will campout at players last known location ")]
		public float campTime = 2f;

		[Tooltip("if true, the agent will alert other agents while chasing the player.")]
		public bool alertOtherAgents = true;

		[Tooltip("Adjust where the agent looks at the player. By default, agents will look at the center of the player, but the center of the player may be behind cover. Raising this value would have the agent look for the players head instead.")]
		public float heightOffset;

		[Tooltip("transform used to search for player. Typically set around AI models eye height")]
		public Transform myEyes;
	}

	[Serializable]
	public class AdvancedOptions
	{
		[Tooltip("If true, instead of chasing an enemy this agent will maintain its position and attack")]
		public bool holdGround;

		[Tooltip("How long it takes in between instances the agent can be woken up/alerted by other enemies ")]
		public float wakeUpTimeOut = 60f;

		[Tooltip("If true, a line will be drawn in the editor between the agent and its current target ")]
		public bool debugTarget;

		[Tooltip("If debugTarget is true, the color of the line between the agent and its current target ")]
		public Color debugColor;

		[Tooltip("How frequent the players position is updated. Lower value is better quality but higher performance cost")]
		public float playerUpdateInterval = 0.1f;

		[Tooltip("Adjust how close an agent needs to get to its target to trigger the script that it has arrived at its destination")]
		[Range(0f, 1f)]
		public float stopDistanceAdjust = 0.5f;
	}

	public AIType aiType;

	[Tooltip("Main Interaction layers. Select all enemy, player and level geometry layers. Typically only 'default', 'player' and 'enemy' need to be selected.")]
	public LayerMask interactionLayers;

	[Tooltip("this layer is used for agents to communicate. Typical use is to alert other agents to chase the player, for instance. Typically just the 'Enemy layer")]
	public LayerMask enemyLayers;

	[Tooltip("Drag in your character model here.")]
	public Transform myBody;

	[SerializeField]
	private WanderOptions wanderSettings = new WanderOptions();

	[SerializeField]
	public SpeedOptions speedSettings = new SpeedOptions();

	[SerializeField]
	private DistanceOptions distanceSettings = new DistanceOptions();

	[SerializeField]
	private MarkerOptions markerSettings = new MarkerOptions();

	[SerializeField]
	public DetectionOptions detectionSettings = new DetectionOptions();

	[SerializeField]
	public AdvancedOptions advancedSettings = new AdvancedOptions();

	[SerializeField]
	public List<Transform> patrolNodes = new List<Transform>();

	[SerializeField]
	public bool usePatrolNodes = true;

	[Tooltip("If true, the agent will wander through its list of nodes in order. If false, random.")]
	public bool followSequence = false;

	[SerializeField]
	public float safeZoneIgnoreTime = 3f;

	[Header("Audio")]
	public AudioSource chaseAudio;
	public float maxAudioVolume = 1f;

	[HideInInspector]
	public bool canSeePlayer;

	[HideInInspector]
	public bool chase;

	[HideInInspector]
	public bool attackOk;

	[HideInInspector]
	public bool pauseMovement;

	[HideInInspector]
	public Transform target;

	[HideInInspector]
	public float distance;

	[HideInInspector]
	public float stoppingDistance;

	[HideInInspector]
	public NavMeshAgent agent;

	private bool adjustRotation;

	private bool cautious;

	private bool campLastKnownSpot;

	private bool checkingForPlayer;

	private bool lostTrailCheck;

	private bool moveToGoalNode;

	private bool moveToWanderSpot;

	private bool timerReset;

	private bool useWander;

	private bool useNodeWander;

	private bool wokenUp;

	private bool wanderSpotFound;

	private float isInFront;

	private float m_TurnAmount;

	private float m_ForwardAmount;

	private float nodeGoalDistance;

	private float wanderSpotDistance;

	private float step;

	private float reactionTime;

	private float startTime;

	private float tempReactionTime;

	private float tempFloater;

	private float xOffset;

	private float zOffset;

	private node_AIAnimation myAnim;

	private PatrolNode lastPatrolNode;

	private bool patrolEffectsApplied;

	private int findWanderSpotTimeOutMax = 1000;

	private int timeOutClock;

	private int nodeWanderSequenceID;

	private List<Transform> myNodes = new List<Transform>();

	private RaycastHit hit;

	private Rigidbody m_Rigidbody;

	private Transform goalNode;

	private Transform wanderSpot;

	private Vector3 rotationTarget;

	private Coroutine trailCheck;

	private Coroutine playerGone;

	private Coroutine locatePlayer;

	private Coroutine nodeWanderCoroutine;

	private Coroutine audioFadeCoroutine;

	private bool signal;

	private float ignorePlayerUntilTime;

	private void Start()
	{
		if (GetComponent<node_AIAnimation>() != null)
		{
			myAnim = GetComponent<node_AIAnimation>();
		}
		if ((bool)GameObject.FindGameObjectWithTag("Player"))
		{
			target = GameObject.FindGameObjectWithTag("Player").transform;
		}
		agent = GetComponent<NavMeshAgent>();
		if ((bool)markerSettings.myMark)
		{
			markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerUnawareColor;
		}
		stoppingDistance = UnityEngine.Random.Range(distanceSettings.stoppingDistanceLow, distanceSettings.stoppingDistanceHigh);
		detectionSettings.visionDistance = detectionSettings.normalVisionDistance;
		startTime = Time.time;
		switch (aiType)
		{
		case AIType.wander:
			useWander = true;
			break;
		case AIType.nodeWander:
			useNodeWander = true;
			break;
		}
		if (useNodeWander)
		{
			myNodes.Clear();
			if (usePatrolNodes && patrolNodes != null && patrolNodes.Count > 0)
			{
				foreach (Transform node in patrolNodes)
				{
					if (node != null)
					{
						myNodes.Add(node);
					}
				}
			}
			if (myNodes.Count == 0)
			{
				Debug.LogWarning("No suitable Nodes");
				useNodeWander = false;
				useWander = true;
			}
			if (useNodeWander)
			{
				if (!followSequence)
				{
					goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count - 1)];
				}
				if (followSequence)
				{
					goalNode = myNodes[0];
					nodeWanderSequenceID = 0;
				}
				agent.speed = speedSettings.walkSpeed;
				moveToGoalNode = true;
				agent.SetDestination(goalNode.position);
			}
		}
		if (useWander)
		{
			useNodeWander = false;
			agent.speed = speedSettings.walkSpeed;
			GameObject gameObject = new GameObject();
			gameObject.name = base.name + " wander target";
			wanderSpot = gameObject.transform;
			while (!wanderSpotFound && timeOutClock < findWanderSpotTimeOutMax)
			{
				Vector3 result;
				if (RandomPoint(base.transform.position, wanderSettings.wanderRadius, out result))
				{
					NavMeshPath navMeshPath = new NavMeshPath();
					agent.CalculatePath(result, navMeshPath);
					if (navMeshPath.status == NavMeshPathStatus.PathPartial || navMeshPath.status == NavMeshPathStatus.PathInvalid)
					{
						wanderSpotFound = false;
						Debug.Log("partial or invalid path. fix navmesh");
						timeOutClock++;
					}
					else
					{
						wanderSpot.position = result;
						agent.SetDestination(wanderSpot.position);
						moveToWanderSpot = true;
						wanderSpotFound = true;
					}
				}
				else
				{
					wanderSpotFound = false;
					timeOutClock++;
				}
			}
			if (timeOutClock >= findWanderSpotTimeOutMax)
			{
				Debug.Log("valid wander spot could not be found. Agent using nodeWander mode.");
				useWander = false;
				useNodeWander = true;
			}
		}
		setKinematic(true);
		agent.updateRotation = false;
		agent.updatePosition = true;
		
		m_Rigidbody = GetComponent<Rigidbody>();
		if (m_Rigidbody != null)
		{
			m_Rigidbody.isKinematic = true;
			m_Rigidbody.useGravity = false;
		}
	}

	private void Update()
	{
		if (target != null)
		{
			distance = Vector3.Distance(base.transform.position, target.position);
		}
		reactionTime = Mathf.Clamp(distance * detectionSettings.reactionTimeSmooth, detectionSettings.reactionTimeLow, detectionSettings.reactionTimeHigh);
		if (useNodeWander && (bool)goalNode && moveToGoalNode)
		{
			nodeGoalDistance = Vector3.Distance(base.transform.position, goalNode.position);
			if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + advancedSettings.stopDistanceAdjust && agent.speed != 0f)
			{
				moveToGoalNode = false;
				nodeWanderCoroutine = StartCoroutine(nodeWanderHit());
			}
			if (advancedSettings.debugTarget)
			{
				Debug.DrawLine(detectionSettings.myEyes.position, goalNode.position, advancedSettings.debugColor);
			}
		}
		if (moveToWanderSpot)
		{
			wanderSpotDistance = Vector3.Distance(base.transform.position, new Vector3(wanderSpot.position.x, base.transform.position.y, wanderSpot.position.z));
			if (wanderSpotDistance <= agent.stoppingDistance + advancedSettings.stopDistanceAdjust && agent.speed != 0f)
			{
				StartCoroutine(wanderSpotHit(UnityEngine.Random.Range(wanderSettings.wanderPauseTimeLow, wanderSettings.wanderPauseTimeHigh), false));
			}
			if (advancedSettings.debugTarget)
			{
				Debug.DrawLine(detectionSettings.myEyes.position, new Vector3(wanderSpot.position.x, base.transform.position.y, wanderSpot.position.z), advancedSettings.debugColor);
			}
		}
		bool allowDetection = Time.time >= ignorePlayerUntilTime;
		if (allowDetection && target != null && distance <= distanceSettings.personalAwarenessDistance && !chase && playerVisible())
		{
			pursuePlayer(true);
		}
		if (allowDetection && distance < detectionSettings.visionDistance && !chase)
		{
			if (!cautious)
			{
				if ((bool)markerSettings.myMark)
				{
					markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerCautionColor;
				}
				cautious = true;
			}
			if (!detectionSettings.omniVision && target != null)
			{
				isInFront = Vector3.Dot(target.position - base.transform.position, base.transform.forward);
				if (isInFront > 0f && !chase && Physics.Raycast(detectionSettings.myEyes.position, new Vector3(target.position.x, target.position.y + detectionSettings.heightOffset, target.position.z) - detectionSettings.myEyes.position, out hit, detectionSettings.visionDistance, interactionLayers) && hit.collider.gameObject.layer == target.gameObject.layer && !checkingForPlayer)
				{
					activateMark();
					if (locatePlayer != null)
					{
						StopCoroutine(locatePlayer);
					}
					locatePlayer = StartCoroutine(detectPlayer());
				}
			}
			if (detectionSettings.omniVision)
			{
				if (playerVisible())
				{
					pursuePlayer(true);
				}
				else
				{
					pursuePlayer(false);
				}
			}
		}
		if (allowDetection && distance > detectionSettings.visionDistance && cautious && !checkingForPlayer)
		{
			cautious = false;
			if ((bool)markerSettings.myMark)
			{
				markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerUnawareColor;
			}
		}
		if (allowDetection && checkingForPlayer)
		{
			if (!timerReset)
			{
				startTime = Time.time;
				timerReset = true;
			}
			if (cautious && isInFront > 0f && playerVisible())
			{
				if ((bool)myBody)
				{
					lookAtPlayer();
				}
				tempFloater = Mathf.Lerp(0f, 1f, (Time.time - startTime) / tempReactionTime);
				if ((bool)markerSettings.myMark)
				{
					markerSettings.myMark.GetComponent<Renderer>().material.color = Color.Lerp(markerSettings.markerCautionColor, markerSettings.markerAttackColor, tempFloater);
				}
			}
		}
		if (distance <= stoppingDistance && chase && playerVisible() && agent.speed != 0f)
		{
			agent.speed = 0f;
		}
		if (distance > stoppingDistance && chase && agent.speed != speedSettings.chaseSpeed)
		{
			agent.speed = speedSettings.chaseSpeed;
		}
		if ((distance > distanceSettings.attackingDistance && attackOk) || (lostTrailCheck && attackOk))
		{
			attackOk = false;
		}
		if (distance <= distanceSettings.attackingDistance && !attackOk && chase)
		{
			attackOk = true;
		}
		if (chase)
		{
			if (patrolEffectsApplied)
			{
				if (nodeWanderCoroutine != null)
				{
					StopCoroutine(nodeWanderCoroutine);
					nodeWanderCoroutine = null;
				}
				RevertPatrolNodeEffects();
			}

			if (chaseAudio != null)
			{
				if (audioFadeCoroutine != null)
				{
					StopCoroutine(audioFadeCoroutine);
					audioFadeCoroutine = null;
				}
				if (!chaseAudio.isPlaying)
				{
					chaseAudio.Play();
				}
				chaseAudio.volume = maxAudioVolume;
			}

			if (advancedSettings.debugTarget)
			{
				Debug.DrawLine(detectionSettings.myEyes.position, target.position, advancedSettings.debugColor);
			}
			if (!playerVisible())
			{
				canSeePlayer = false;
				if (!lostTrailCheck)
				{
					lostTrailCheck = true;
					if (trailCheck != null)
					{
						StopCoroutine(trailCheck);
					}
					trailCheck = StartCoroutine(lostTrailTimeOut());
				}
			}
			else
			{
				canSeePlayer = true;
				if (trailCheck != null)
				{
					StopCoroutine(trailCheck);
				}
				lostTrailCheck = false;
			}
		}

		bool forceTurnToPlayer = false;
		if (chase && canSeePlayer && distance <= stoppingDistance)
		{
			forceTurnToPlayer = true;
		}

		if (forceTurnToPlayer)
		{
			Vector3 dirToPlayer = target.position - base.transform.position;
			dirToPlayer.y = 0;
			if (dirToPlayer.magnitude > 0.1f)
			{
				Move(dirToPlayer.normalized, true);
			}
			else
			{
				Move(Vector3.zero, true);
			}
		}
		else if (agent.remainingDistance > agent.stoppingDistance)
		{
			Move(agent.desiredVelocity, false);
		}
		else
		{
			Move(Vector3.zero, false);
		}
		if (adjustRotation)
		{
			Vector3 forward = Vector3.RotateTowards(base.transform.forward, rotationTarget, step, 0f);
			base.transform.rotation = Quaternion.LookRotation(forward);
			if (Vector3.Angle(base.transform.forward, rotationTarget) < 1f)
			{
				adjustRotation = false;
			}
		}
		step = speedSettings.rotateSpeed * Time.deltaTime;
	}

	public void activateMark()
	{
		if ((bool)markerSettings.myMark && !markerSettings.myMark.GetComponent<Renderer>().enabled)
		{
			markerSettings.myMark.GetComponent<Renderer>().enabled = true;
		}
	}

	private void alertOthers()
	{
		Collider[] array = Physics.OverlapSphere(base.transform.position, 7f, enemyLayers);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].tag == "Enemy" && array[i].gameObject != base.gameObject)
			{
				array[i].SendMessageUpwards("wakeUp", SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	private void ApplyExtraTurnRotation()
	{
		float num = Mathf.Lerp(speedSettings.m_StationaryTurnSpeed, speedSettings.m_MovingTurnSpeed, m_ForwardAmount);
		base.transform.Rotate(0f, m_TurnAmount * num * Time.deltaTime, 0f);
	}

	private IEnumerator detectPlayer()
	{
		agent.speed = 0f;
		tempReactionTime = reactionTime;
		checkingForPlayer = true;
		yield return new WaitForSeconds(tempReactionTime);
		if (playerVisible())
		{
			pursuePlayer(true);
		}
		if (!chase && cautious && !campLastKnownSpot)
		{
			if ((bool)markerSettings.myMark)
			{
				markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerCautionColor;
			}
			agent.speed = speedSettings.walkSpeed;
		}
		checkingForPlayer = false;
		timerReset = false;
	}

	private IEnumerator lostTrailTimeOut()
	{
		yield return new WaitForSeconds(detectionSettings.searchTime);
		if (!canSeePlayer)
		{
			if (playerGone != null)
			{
				StopCoroutine(playerGone);
			}
			playerGone = StartCoroutine(playerEscaped());
		}
		lostTrailCheck = false;
	}

	public void lookAtPlayer()
	{
	}

	public void Move(Vector3 move, bool forceTurnToPlayer)
	{
		float desiredSpeed = move.magnitude;

		if (move.magnitude > 1f)
		{
			move.Normalize();
		}
		
		Vector3 localMove = base.transform.InverseTransformDirection(move);
		m_TurnAmount = Mathf.Atan2(localMove.x, localMove.z);
		
		if (forceTurnToPlayer)
		{
			m_ForwardAmount = 0f;
		}
		else
		{
			float animForward = 0f;
			if (desiredSpeed > 0.1f || agent.velocity.magnitude > 0.1f)
			{
				if (chase)
				{
					animForward = 1.0f;
				}
				else
				{
					animForward = 0.5f;
				}
			}
			
			m_ForwardAmount = localMove.z > 0.1f ? animForward : 0f;
		}
		
		ApplyExtraTurnRotation();
		
		if (myAnim != null)
		{
			myAnim.UpdateAnimator(m_ForwardAmount, 0f, speedSettings.smoothMove, speedSettings.m_AnimSpeedMultiplier);
		}
	}

	private IEnumerator nodeWanderHit()
	{
		agent.speed = 0f;
		PatrolNode data = null;
		if ((bool)goalNode)
		{
			data = goalNode.GetComponent<PatrolNode>();
		}
		
		RevertPatrolNodeEffects();
		ApplyPatrolNodeEffects(data);
		
		float wait = 3.5f;
		if (data != null)
		{
			wait = (data.nodeType == PatrolNode.NodeType.Special) ? data.freezeSeconds : data.normalStandSeconds;
		}
		
		yield return new WaitForSeconds(wait);
		
		RevertPatrolNodeEffects();
		
		goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count)];
		moveToGoalNode = true;
		agent.SetDestination(goalNode.position);
		agent.speed = speedSettings.walkSpeed;
	}

	public bool playerVisible()
	{
		bool result = false;
		if (Physics.Raycast(detectionSettings.myEyes.position, new Vector3(target.position.x, target.position.y + detectionSettings.heightOffset, target.position.z) - detectionSettings.myEyes.position, out hit, detectionSettings.visionDistance, interactionLayers) && hit.collider.gameObject.layer == target.gameObject.layer)
		{
			result = true;
		}
		return result;
	}

	public void pursuePlayer(bool playerInSight)
	{
		if (playerGone != null)
		{
			StopCoroutine(playerGone);
		}
		if ((bool)markerSettings.myMark)
		{
			markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerAttackColor;
			activateMark();
		}
		if (advancedSettings.holdGround)
		{
			stoppingDistance = distance;
		}
		if (detectionSettings.alertOtherAgents)
		{
			InvokeRepeating("alertOthers", 0.1f, UnityEngine.Random.Range(1f, 1.5f));
		}
		moveToWanderSpot = false;
		moveToGoalNode = false;
		cautious = false;
		adjustRotation = false;
		canSeePlayer = playerInSight;
		agent.speed = speedSettings.chaseSpeed;
		chase = true;
		StartCoroutine("updatePlayerPos");
	}

	private IEnumerator FadeOutAudio()
	{
		if (chaseAudio == null) yield break;
		
		float startVolume = chaseAudio.volume;
		float fadeDuration = 1.5f;
		float timer = 0f;
		
		while (timer < fadeDuration)
		{
			timer += Time.deltaTime;
			chaseAudio.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
			yield return null;
		}
		
		chaseAudio.volume = 0f;
		chaseAudio.Stop();
		audioFadeCoroutine = null;
	}

	private IEnumerator playerEscaped()
	{
		CancelInvoke();
		agent.speed = 0f;
		chase = false;
		attackOk = false;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			if (audioFadeCoroutine != null)
			{
				StopCoroutine(audioFadeCoroutine);
			}
			audioFadeCoroutine = StartCoroutine(FadeOutAudio());
		}

		campLastKnownSpot = true;
		markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerUnawareColor;
		yield return new WaitForSeconds(detectionSettings.campTime);
		campLastKnownSpot = false;
		cautious = false;
		if (useNodeWander)
		{
			if (!followSequence)
			{
				goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count - 1)];
			}
			if (followSequence)
			{
				goalNode = myNodes[nodeWanderSequenceID];
			}
			moveToGoalNode = true;
			agent.SetDestination(goalNode.position);
		}
		if (useWander)
		{
			moveToWanderSpot = true;
		}
		if (useWander)
		{
			StartCoroutine(wanderSpotHit(UnityEngine.Random.Range(wanderSettings.wanderPauseTimeLow, wanderSettings.wanderPauseTimeHigh), false));
		}
		agent.speed = speedSettings.walkSpeed;
	}

	private bool RandomPoint(Vector3 center, float range, out Vector3 result)
	{
		for (int i = 0; i < 30; i++)
		{
			Vector3 sourcePosition = center + UnityEngine.Random.insideUnitSphere * range;
			NavMeshHit navMeshHit;
			if (NavMesh.SamplePosition(sourcePosition, out navMeshHit, 1f, -1))
			{
				result = navMeshHit.position;
				return true;
			}
		}
		result = Vector3.zero;
		return false;
	}

	private void setKinematic(bool newValue)
	{
		Component[] componentsInChildren = GetComponentsInChildren(typeof(Rigidbody));
		Component[] array = componentsInChildren;
		foreach (Component component in array)
		{
			if (component.gameObject != base.gameObject)
			{
				(component as Rigidbody).isKinematic = newValue;
			}
		}
		
		Rigidbody mainRb = GetComponent<Rigidbody>();
		if (mainRb != null)
		{
			mainRb.isKinematic = true;
			mainRb.useGravity = false;
		}
	}

	private IEnumerator updatePlayerPos()
	{
		Vector3 dest = target.position;

		RaycastHit[] hits = Physics.RaycastAll(target.position + Vector3.up * 0.5f, Vector3.down, 10f, interactionLayers);
		float closestDist = float.MaxValue;
		bool foundFloor = false;
		Vector3 floorPoint = target.position;
		
		foreach (var h in hits)
		{
			if (h.collider.gameObject == target.gameObject) continue;
			
			if (h.distance < closestDist)
			{
				closestDist = h.distance;
				floorPoint = h.point;
				foundFloor = true;
			}
		}

		if (foundFloor)
		{
			NavMeshHit navHit;
			if (NavMesh.SamplePosition(floorPoint, out navHit, 3f, NavMesh.AllAreas))
			{
				dest = navHit.position;
			}
		}
		else
		{
			NavMeshHit navHit;
			if (NavMesh.SamplePosition(target.position - new Vector3(0, 1f, 0), out navHit, 3f, NavMesh.AllAreas))
			{
				dest = navHit.position;
			}
		}

		agent.SetDestination(dest);
		yield return new WaitForSeconds(advancedSettings.playerUpdateInterval);
		if (chase)
		{
			StartCoroutine("updatePlayerPos");
		}
	}

	private void ApplyPatrolNodeEffects(PatrolNode node)
	{
		if (node == null || patrolEffectsApplied)
		{
			return;
		}
		if (node.nodeType == PatrolNode.NodeType.Special && node.rotateOnArrive)
		{
			Transform rt = node.rotateTarget ? node.rotateTarget : base.transform;
			rt.rotation = Quaternion.Euler(0f, node.targetYRotation, 0f);
			if (rt == base.transform)
			{
				adjustRotation = false;
				rotationTarget = rt.forward;
				agent.Warp(node.transform.position);
			}
		}
		if (node.disableOnArrive != null)
		{
			foreach (Transform t in node.disableOnArrive)
			{
				if (t != null) PatrolNode.SetSMRActiveOnTransform(t, false);
			}
		}
		if (node.enableOnArrive != null)
		{
			foreach (Transform t2 in node.enableOnArrive)
			{
				if (t2 != null) PatrolNode.SetSMRActiveOnTransform(t2, true);
			}
		}
		if (node.disableGameObjectOnArrive != null)
		{
			foreach (GameObject go in node.disableGameObjectOnArrive)
			{
				if (go != null) PatrolNode.SetActiveOnGameObject(go, false);
			}
		}
		if (node.enableGameObjectOnArrive != null)
		{
			foreach (GameObject go2 in node.enableGameObjectOnArrive)
			{
				if (go2 != null) PatrolNode.SetActiveOnGameObject(go2, true);
			}
		}
		if (node.hasSound && node.specialAudio)
		{
			node.specialAudio.Play();
		}
		lastPatrolNode = node;
		patrolEffectsApplied = true;
	}

	private void RevertPatrolNodeEffects()
	{
		if (!patrolEffectsApplied || lastPatrolNode == null)
		{
			return;
		}
		
		PatrolNode node = lastPatrolNode;
		patrolEffectsApplied = false;
		lastPatrolNode = null;

		if (node.disableOnArrive != null)
		{
			foreach (Transform t in node.disableOnArrive)
			{
				if (t != null) PatrolNode.SetSMRActiveOnTransform(t, true);
			}
		}
		if (node.enableOnArrive != null)
		{
			foreach (Transform t2 in node.enableOnArrive)
			{
				if (t2 != null) PatrolNode.SetSMRActiveOnTransform(t2, false);
			}
		}
		if (node.disableGameObjectOnArrive != null)
		{
			foreach (GameObject go in node.disableGameObjectOnArrive)
			{
				if (go != null) PatrolNode.SetActiveOnGameObject(go, true);
			}
		}
		if (node.enableGameObjectOnArrive != null)
		{
			foreach (GameObject go2 in node.enableGameObjectOnArrive)
			{
				if (go2 != null) PatrolNode.SetActiveOnGameObject(go2, false);
			}
		}
		if (node.hasSound && node.specialAudio)
		{
			node.specialAudio.Stop();
		}
		
		var allSmrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
		foreach (var smr in allSmrs)
		{
			smr.enabled = true;
		}
	}

	public bool IsChasingPlayer()
	{
		return chase;
	}

	public void LosePlayerToSafeZone()
	{
		if (!chase)
		{
			ignorePlayerUntilTime = Time.time + safeZoneIgnoreTime;
			return;
		}
		StopAllCoroutines();
		CancelInvoke();
		chase = false;
		attackOk = false;
		canSeePlayer = false;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			if (audioFadeCoroutine != null)
			{
				StopCoroutine(audioFadeCoroutine);
			}
			audioFadeCoroutine = StartCoroutine(FadeOutAudio());
		}

		agent.speed = 0f;
		agent.isStopped = true;
		ignorePlayerUntilTime = Time.time + safeZoneIgnoreTime;
		
		Animator anim = GetComponent<Animator>();
		if (anim == null) anim = GetComponentInChildren<Animator>();
		if (anim != null) anim.SetTrigger("E_Lost");

		StartCoroutine(SafeZoneResume());
	}

	private IEnumerator SafeZoneResume()
	{
		yield return new WaitForSeconds(safeZoneIgnoreTime);
		agent.isStopped = false;
		agent.speed = speedSettings.walkSpeed;
		cautious = false;

		if (useNodeWander && myNodes.Count > 0)
		{
			if (!followSequence)
			{
				goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count)];
			}
			else
			{
				goalNode = myNodes[nodeWanderSequenceID];
			}
			moveToGoalNode = true;
			agent.SetDestination(goalNode.position);
		}
		else if (useWander)
		{
			moveToWanderSpot = true;
		}
	}

	public void StopHunt(float ignoreTime, bool immediateAudioStop = false)
	{
		StopAllCoroutines();
		CancelInvoke();
		chase = false;
		attackOk = false;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			if (audioFadeCoroutine != null)
			{
				StopCoroutine(audioFadeCoroutine);
			}
			if (immediateAudioStop)
			{
				chaseAudio.Stop();
			}
			else
			{
				audioFadeCoroutine = StartCoroutine(FadeOutAudio());
			}
		}

		agent.isStopped = true;
		agent.ResetPath();
		agent.velocity = Vector3.zero;
		agent.speed = 0f;
		ignorePlayerUntilTime = Time.time + ignoreTime;
	}

	public void ResetAfterCatch(float ignoreTime, bool immediateAudioStop = false)
	{
		chase = false;
		attackOk = false;
		canSeePlayer = false;
		cautious = false;
		ignorePlayerUntilTime = Time.time + ignoreTime;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			if (audioFadeCoroutine != null)
			{
				StopCoroutine(audioFadeCoroutine);
			}
			if (immediateAudioStop)
			{
				chaseAudio.Stop();
			}
			else
			{
				audioFadeCoroutine = StartCoroutine(FadeOutAudio());
			}
		}

		agent.isStopped = false;
		agent.speed = speedSettings.walkSpeed;

		if (useNodeWander && myNodes.Count > 0)
		{
			goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count)];
			moveToGoalNode = true;
			agent.SetDestination(goalNode.position);
		}
	}

	public void wakeUp()
	{
		if (!chase && !lostTrailCheck && !wokenUp)
		{
			if (playerVisible())
			{
				pursuePlayer(true);
			}
			else
			{
				pursuePlayer(false);
			}
			StartCoroutine("wakeUpTimeOut");
		}
	}

	private IEnumerator wakeUpTimeOut()
	{
		wokenUp = true;
		yield return new WaitForSeconds(advancedSettings.wakeUpTimeOut);
		wokenUp = false;
	}

	private IEnumerator wanderSpotHit(float pointWaitTime, bool resumePatrol)
	{
		agent.speed = 0f;
		yield return new WaitForSeconds(pointWaitTime);
		wanderSpotFound = false;
		timeOutClock = 0;
		while (!wanderSpotFound && timeOutClock < findWanderSpotTimeOutMax)
		{
			Vector3 point;
			if (RandomPoint(base.transform.position, wanderSettings.wanderRadius, out point))
			{
				NavMeshPath navMeshPath = new NavMeshPath();
				agent.CalculatePath(point, navMeshPath);
				if (navMeshPath.status == NavMeshPathStatus.PathPartial || navMeshPath.status == NavMeshPathStatus.PathInvalid)
				{
					wanderSpotFound = false;
					Debug.Log("partial or invalid path. fix navmesh");
					timeOutClock++;
				}
				else
				{
					wanderSpot.position = point;
					agent.SetDestination(wanderSpot.position);
					moveToWanderSpot = true;
					wanderSpotFound = true;
				}
			}
			else
			{
				wanderSpotFound = false;
				timeOutClock++;
			}
		}
		if (timeOutClock >= findWanderSpotTimeOutMax)
		{
			Debug.Log("valid wander spot could not be found. Agent using nodeWander mode.");
			useWander = false;
			useNodeWander = true;
		}
		agent.speed = speedSettings.walkSpeed;
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.name == "bang")
		{
			detectionSettings.omniVision = true;
			signal = true;
		}
	}

	private void FixedUpdate()
	{
		if (signal)
		{
			signal = false;
			detectionSettings.omniVision = false;
		}
	}
}
