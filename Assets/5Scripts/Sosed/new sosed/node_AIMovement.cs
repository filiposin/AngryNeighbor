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
		nodeWander = 1,
		wayPoints = 2,
		guard = 3
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
	public class NodeWanderOptions
	{
		[Tooltip("If true, the agent will only wander to nodes with the group name defined in the 'Node Group Name' setting")]
		public bool useNodeGroup;

		[Tooltip("If 'Use Node Group' is true, the name of the node group the agent can wander through")]
		public string nodeGroupName;

		[Tooltip("If true, the agent will wander through its list of nodes in order, good for patrol type behaviour. If false, the agent will pick a random node each time, good for search behavior")]
		public bool followSequence;

		[Tooltip("minimum time agent will pause before picking a new node destination")]
		public float nodeWanderPauseTimeLow = 6f;

		[Tooltip("max time agent will pause before picking a new node destination")]
		public float nodeWanderPauseTimeHigh = 6f;
	}

	[Serializable]
	public class WayPointOptions
	{
		[Tooltip("minimum amount of time AI will pause when it reaches a wayPoint")]
		public float patrolStopTimeLow = 4f;

		[Tooltip("max amount of time AI will pause when it reaches a wayPoint")]
		public float patrolStopTimeHigh = 8f;

		[Tooltip("this allows you to specify unique stop times for each wayPoint. set to false if not sure.")]
		public bool useCustomWPTime;

		[Tooltip("if useCustomWPTime is true, use this to set the stop times.")]
		public int[] wayPointStopTime;

		[Tooltip("this allows AI to trigger scripts at custom wayPoints")]
		public bool useWayPointScript;

		[Tooltip("the list of wayPoints this AI will patrol")]
		public Transform[] wayPoints;
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
	public NodeWanderOptions nodeWanderSettings = new NodeWanderOptions();

	[SerializeField]
	public WayPointOptions wayPointSettings = new WayPointOptions();

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
	public Transform targetWayPoint;

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

	private bool moveToWayPoint;

	private bool returnToGuardSpot;

	private bool timerReset;

	private bool useWander;

	private bool useWayPoints;

	private bool useNodeWander;

	private bool useGuard;

	private bool wokenUp;

	private bool wanderSpotFound;

	private float isInFront;

	private float guardSpotDistance;

	private float m_TurnAmount;

	private float m_ForwardAmount;

	private float nodeGoalDistance;

	private float wayPointDistance;

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

	private int wayPointIndex;

	private List<Transform> myNodes = new List<Transform>();

	private RaycastHit hit;

	private Rigidbody m_Rigidbody;

	private Transform goalNode;

	private Transform guardSpot;

	private Transform wanderSpot;

	private Vector3 rotationTarget;

	private Coroutine trailCheck;

	private Coroutine playerGone;

	private Coroutine locatePlayer;

	private Coroutine nodeWanderCoroutine;

	private bool signal;

	private float ignorePlayerUntilTime;

	private void Start()
	{
		if (GetComponent<node_AIAnimation>() != null)
		{
			myAnim = GetComponent<node_AIAnimation>();
		}
		// if (GameObject.FindGameObjectWithTag("GameController") != null)
		// {
		// 	nManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<node_NodeManager>();
		// }
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
		case AIType.wayPoints:
			useWayPoints = true;
			break;
		case AIType.nodeWander:
			useNodeWander = true;
			break;
		case AIType.guard:
			useGuard = true;
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
				useGuard = true;
			}
			if (useNodeWander)
			{
				if (!nodeWanderSettings.followSequence)
				{
					goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count - 1)];
				}
				if (nodeWanderSettings.followSequence)
				{
					goalNode = myNodes[0];
					nodeWanderSequenceID = 0;
				}
				agent.speed = speedSettings.walkSpeed;
				moveToGoalNode = true;
				agent.SetDestination(goalNode.position);
			}
		}
		if (useWayPoints)
		{
			if (!wayPointSettings.useWayPointScript && wayPointSettings.wayPoints.Length != 0)
			{
				targetWayPoint = wayPointSettings.wayPoints[0];
				wayPointIndex = 0;
				agent.speed = speedSettings.walkSpeed;
				if (wayPointSettings.wayPointStopTime.Length != wayPointSettings.wayPoints.Length)
				{
					wayPointSettings.useCustomWPTime = false;
				}
				moveToWayPoint = true;
				agent.SetDestination(targetWayPoint.position);
			}
			if (wayPointSettings.useWayPointScript)
			{
				Debug.LogWarning("node_ConditionalWayPointHandler missing. Using wander mode.");
				useWander = true;
				moveToWayPoint = false;
				useWayPoints = false;
				wayPointSettings.useCustomWPTime = false;
			}
			else if (wayPointSettings.wayPoints.Length == 0)
			{
				Debug.LogWarning("no way points found. Agent set to wander mode.");
				useWander = true;
				moveToWayPoint = false;
				useWayPoints = false;
				wayPointSettings.useCustomWPTime = false;
			}
			if (wayPointSettings.useWayPointScript)
			{
				wayPointSettings.useCustomWPTime = false;
			}
		}
		if (useWander)
		{
			moveToWayPoint = false;
			useWayPoints = false;
			useGuard = false;
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
				Debug.Log("valid wander spot could not be found. Agent using guard mode.");
				useWander = false;
				useGuard = true;
			}
		}
		if (useGuard)
		{
			GameObject gameObject2 = new GameObject();
			gameObject2.name = base.name + " guard Spot";
			gameObject2.transform.position = base.transform.position;
			gameObject2.transform.rotation = base.transform.rotation;
			guardSpot = gameObject2.transform;
			agent.speed = 0f;
		}
		setKinematic(true);
		agent.updateRotation = false; // Отключаем вращение агента, будем крутить скриптом
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
		if (useGuard && returnToGuardSpot)
		{
			guardSpotDistance = Vector3.Distance(base.transform.position, guardSpot.position);
			if (guardSpotDistance <= agent.stoppingDistance + advancedSettings.stopDistanceAdjust && agent.speed != 0f)
			{
				guardSpotHit();
			}
			if (advancedSettings.debugTarget)
			{
				Debug.DrawLine(detectionSettings.myEyes.position, guardSpot.position, advancedSettings.debugColor);
			}
		}
		if (moveToWayPoint)
		{
			if (!wayPointSettings.useWayPointScript)
			{
				wayPointDistance = Vector3.Distance(base.transform.position, targetWayPoint.position);
			}
			if (!wayPointSettings.useWayPointScript && wayPointDistance <= agent.stoppingDistance + advancedSettings.stopDistanceAdjust && agent.speed != 0f)
			{
				StartCoroutine(wayPointHit(UnityEngine.Random.Range(wayPointSettings.patrolStopTimeLow, wayPointSettings.patrolStopTimeHigh), false));
			}
			if (advancedSettings.debugTarget)
			{
				Debug.DrawLine(detectionSettings.myEyes.position, targetWayPoint.position, advancedSettings.debugColor);
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

			if (chaseAudio != null && !chaseAudio.isPlaying)
			{
				chaseAudio.volume = maxAudioVolume;
				chaseAudio.Play();
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

	public void guardSpotHit()
	{
		agent.speed = 0f;
		returnToGuardSpot = false;
		rotationTarget = guardSpot.forward;
		adjustRotation = true;
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
		// Пусто. Мы больше не крутим модельку myBody отдельно, так как это ломает анимации (бег боком).
		// Вращение к игроку теперь обрабатывается через Move(..., forceTurnToPlayer: true).
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
			// Маппинг скорости для старого аниматора (0 = стоит, 0.5 = ходьба, 1 = бег)
			// У NavMeshAgent скорости 2.0 и 3.5, а аниматору нужны 0.5 и 1.0
			float animForward = 0f;
			if (desiredSpeed > 0.1f || agent.velocity.magnitude > 0.1f)
			{
				if (agent.speed <= speedSettings.walkSpeed + 0.1f)
				{
					animForward = 0.5f; // Анимация ходьбы
				}
				else
				{
					animForward = 1.0f; // Анимация бега
				}
			}
			
			// Если движемся вперед (localMove.z > 0), задаем расчитанную скорость
			m_ForwardAmount = localMove.z > 0.1f ? animForward : 0f;
		}
		
		ApplyExtraTurnRotation();
		
		if (myAnim != null)
		{
			// Вместо сложных боковых анимаций (m_TurnAmount), передаем только скорость вперед, 
			// так как боковые убрали. Для этого передаём m_TurnAmount = 0
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
		
		float wait = 3.5f; // default
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
		moveToWayPoint = false;
		moveToWanderSpot = false;
		returnToGuardSpot = false;
		moveToGoalNode = false;
		cautious = false;
		adjustRotation = false;
		canSeePlayer = playerInSight;
		agent.speed = speedSettings.chaseSpeed;
		chase = true;
		StartCoroutine("updatePlayerPos");
	}

	private IEnumerator playerEscaped()
	{
		CancelInvoke();
		agent.speed = 0f;
		chase = false;
		attackOk = false;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			chaseAudio.Stop();
		}

		campLastKnownSpot = true;
		markerSettings.myMark.GetComponent<Renderer>().material.color = markerSettings.markerUnawareColor;
		yield return new WaitForSeconds(detectionSettings.campTime);
		campLastKnownSpot = false;
		cautious = false;
		if (useNodeWander)
		{
			if (!nodeWanderSettings.followSequence)
			{
				goalNode = myNodes[UnityEngine.Random.Range(0, myNodes.Count - 1)];
			}
			if (nodeWanderSettings.followSequence)
			{
				goalNode = myNodes[nodeWanderSequenceID];
			}
			moveToGoalNode = true;
			agent.SetDestination(goalNode.position);
		}
		if (useWayPoints && !wayPointSettings.useWayPointScript)
		{
			moveToWayPoint = true;
			StartCoroutine(wayPointHit(UnityEngine.Random.Range(wayPointSettings.patrolStopTimeLow, wayPointSettings.patrolStopTimeHigh), true));
		}
		if (useWander)
		{
			moveToWanderSpot = true;
		}
		if (useGuard)
		{
			returnToGuardSpot = true;
			agent.SetDestination(guardSpot.position);
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
		
		// Главный Rigidbody должен быть isKinematic = true, чтобы NavMeshAgent нормально работал и не прыгал
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

		// Raycast downwards to find the actual floor beneath the player
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
			// Sample nearest navmesh strictly on the floor we found
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
				agent.Warp(node.transform.position); // Force snap position to prevent sliding
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
		
		// Fallback brute force: ensure all SMRs on the body are enabled when reverting
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
			chaseAudio.Stop();
		}

		agent.speed = 0f;
		agent.isStopped = true;
		ignorePlayerUntilTime = Time.time + safeZoneIgnoreTime;
		StartCoroutine(SafeZoneResume());
	}

	private IEnumerator SafeZoneResume()
	{
		yield return new WaitForSeconds(safeZoneIgnoreTime);
		agent.isStopped = false;
		agent.speed = speedSettings.walkSpeed;
	}

	public void StopHunt(float ignoreTime)
	{
		StopAllCoroutines();
		CancelInvoke();
		chase = false;
		attackOk = false;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			chaseAudio.Stop();
		}

		agent.isStopped = true;
		agent.speed = 0f;
		ignorePlayerUntilTime = Time.time + ignoreTime;
	}

	public void ResetAfterCatch(float ignoreTime)
	{
		chase = false;
		attackOk = false;
		canSeePlayer = false;
		cautious = false;
		ignorePlayerUntilTime = Time.time + ignoreTime;
		
		if (chaseAudio != null && chaseAudio.isPlaying)
		{
			chaseAudio.Stop();
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
			Debug.Log("valid wander spot could not be found. Agent using guard mode.");
			useWander = false;
			useGuard = true;
			GameObject gameObject = new GameObject();
			gameObject.name = base.name + " guard Spot";
			gameObject.transform.position = base.transform.position;
			gameObject.transform.rotation = base.transform.rotation;
			guardSpot = gameObject.transform;
			agent.speed = 0f;
		}
		agent.speed = speedSettings.walkSpeed;
	}

	public IEnumerator wayPointHit(float wayPointWaitTime, bool resumePatrol)
	{
		agent.speed = 0f;
		if (wayPointSettings.useCustomWPTime)
		{
			yield return new WaitForSeconds(wayPointSettings.wayPointStopTime[wayPointIndex]);
		}
		if (!resumePatrol)
		{
			wayPointIndex++;
		}
		if (wayPointIndex >= wayPointSettings.wayPoints.Length)
		{
			wayPointIndex = 0;
		}
		if (!wayPointSettings.useCustomWPTime && !wayPointSettings.useWayPointScript)
		{
			yield return new WaitForSeconds(wayPointWaitTime);
		}
		if (!wayPointSettings.useWayPointScript)
		{
			targetWayPoint = wayPointSettings.wayPoints[wayPointIndex];
			agent.SetDestination(targetWayPoint.position);
		}
		if (wayPointSettings.useWayPointScript)
		{
			// Not supported
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
