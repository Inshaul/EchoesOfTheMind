using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GhostAIController : MonoBehaviour
{
    public enum GhostState { HuntStart, Patrolling, ChasingPlayer, HearingPlayer }
    public GhostState currentState = GhostState.HuntStart;

    public Transform eyePoint;

    private NavMeshAgent agent;
    private Transform player;

    [Header("Roaming Settings")]
    public float roamRadius = 100f;
    public float roamDelay = 5f;
    private float nextRoamTime = 0f;

    [Header("Vision Settings")]
    public float visionRange = 40f;
    public float fieldOfView = 60f;
    public float verticalFieldOfView = 45f;

    [Header("Catch Settings")]
    public Transform catchTeleportLocation;
    public float catchDistance = 2f;

    [Header("Mic Detection")]
    public ScreamDetector screamDetector; // assign in Inspector

    [Header("Blinking Settings")]
    public float minBlinkTime = 1f;
    public float maxBlinkTime = 2f;
    public SkinnedMeshRenderer ghostRenderer;

    [Header("Teleport Settings")]
    public bool allowTeleportation = false;
    public float teleportCooldown = 20f;
    private float nextTeleportTime = 0f;
    public float teleportDistanceFromPlayer = 25f;

    [Header("Chase/Loss Settings")]
    public float lostSightGrace = 5f;
    private float lostSightTimer = 0f;

    public AudioSource audioSource;
    public Animator animator;

    [Header("Misc")]
    public bool isJumpScareGhost = false;

    private Coroutine _blinkCo; // store to stop cleanly

    void Start()
    {
        agent  = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!player)
        {
            Debug.LogError("❌ Player not found in scene!");
            enabled = false;
            return;
        }

        // Delay hunt start slightly
        Invoke(nameof(StartPatrolling), 2f);
    }

    void OnEnable()
    {
        if (_blinkCo == null) _blinkCo = StartCoroutine(BlinkRoutine());

        // 🔊 subscribe to mic events
        if (screamDetector != null)
        {
            screamDetector.OnLoudTalk += HandlePlayerLoudTalk;
            screamDetector.OnScream   += HandlePlayerScream;
        }
    }

    void OnDisable()
    {
        if (_blinkCo != null) { StopCoroutine(_blinkCo); _blinkCo = null; }

        if (screamDetector != null)
        {
            screamDetector.OnLoudTalk -= HandlePlayerLoudTalk;
            screamDetector.OnScream   -= HandlePlayerScream;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case GhostState.Patrolling:
                RandomRoam();
                DetectPlayerBySight();
                DetectMicInput();        // legacy: switch to HearingPlayer during patrol if they talk
                TryTeleportNearPlayer(); // optional hunt spice
                break;

            case GhostState.ChasingPlayer:
                ChasePlayer();
                break;

            case GhostState.HearingPlayer:
                MoveTowardPlayerSound();
                break;
        }
    }

    void TryTeleportNearPlayer()
    {
        if (!allowTeleportation) return;

        if (Time.time >= nextTeleportTime && player != null)
        {
            Vector3 randomDir = Random.insideUnitSphere * teleportDistanceFromPlayer;
            randomDir += player.position;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                if (agent) agent.Warp(hit.position);
                Debug.Log("👻 Ghost teleported!");
            }

            nextTeleportTime = Time.time + teleportCooldown;
        }
    }

    void StartPatrolling()
    {
        currentState = GhostState.Patrolling;
        Roam();
    }

    void RandomRoam()
    {
        if (Time.time >= nextRoamTime && !agent.pathPending && agent.remainingDistance < 0.5f)
            Roam();
    }

    void Roam()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius + transform.position;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            nextRoamTime = Time.time + roamDelay;
        }
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBlinkTime, maxBlinkTime));
            if (ghostRenderer != null)
            {
                ghostRenderer.enabled = false;
                yield return new WaitForSeconds(0.5f);
                ghostRenderer.enabled = true;
            }
        }
    }

    void SetGhostAlpha(float alpha)
    {
        if (ghostRenderer != null && ghostRenderer.material.HasProperty("_Color"))
        {
            Color color = ghostRenderer.material.color;
            color.a = alpha;
            ghostRenderer.material.color = color;
        }
    }

    void DetectPlayerBySight()
    {
        if (player == null || eyePoint == null) return;

        Vector3 playerTargetPoint = player.position + Vector3.up * 1.2f;
        Vector3 directionToPlayer = playerTargetPoint - eyePoint.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > visionRange) return;

        float angleToPlayer = Vector3.Angle(eyePoint.forward, directionToPlayer.normalized);

        Debug.DrawRay(eyePoint.position, eyePoint.forward * visionRange, Color.green);
        Debug.DrawRay(eyePoint.position, directionToPlayer.normalized * visionRange, Color.magenta);

        if (angleToPlayer < fieldOfView / 2f)
        {
            if (Physics.Raycast(eyePoint.position, directionToPlayer.normalized, out RaycastHit hit, visionRange))
            {
                Debug.DrawRay(eyePoint.position, directionToPlayer.normalized * hit.distance, Color.red);

                if (hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
                {
                    currentState = GhostState.ChasingPlayer;
                    lostSightTimer = 0f;
                    Debug.Log("👁️ Ghost sees the player! → CHASING");
                }
            }
        }
    }

    void TeleportPlayerOnCatch()
    {
        if (catchTeleportLocation != null && player != null)
        {
            // Move player to catch location
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = catchTeleportLocation.position;
            if (cc != null) cc.enabled = true;

            Debug.Log($"📍 Player teleported to {catchTeleportLocation.position}");
        }

        currentState = GhostState.Patrolling;
        lostSightTimer = 0f;
        Roam();
    }

    void DetectMicInput()
    {
        // legacy patrol->hearing behavior
        if (screamDetector != null && screamDetector.IsPlayerTalking() && currentState == GhostState.Patrolling)
        {
            currentState = GhostState.HearingPlayer;
            Debug.Log("🎤 Ghost hears the player talking! → HEARING");
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);

        // Catch check
        if (distance <= catchDistance)
        {
            if (!isJumpScareGhost)
            {
                Debug.Log("🪝 Player caught by ghost!");
                TeleportPlayerOnCatch();
            }
            return;
        }

        if (eyePoint == null)
        {
            // No eyes → fallback grace timer
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightGrace)
            {
                currentState = GhostState.Patrolling;
                Roam();
                Debug.Log("👁️ Lost sight (no eyePoint) for grace — resuming patrol.");
            }
            return;
        }

        // LOS
        Vector3 playerTargetPoint = player.position + Vector3.up * 1.2f;
        Vector3 directionToPlayer = playerTargetPoint - eyePoint.position;
        float angleToPlayer = Vector3.Angle(eyePoint.forward, directionToPlayer.normalized);

        bool canSeePlayer = false;
        if (distance <= visionRange && angleToPlayer < fieldOfView / 2f)
        {
            if (Physics.Raycast(eyePoint.position, directionToPlayer.normalized, out RaycastHit hit, visionRange))
            {
                if (hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
                    canSeePlayer = true;
            }
        }

        if (canSeePlayer)
        {
            lostSightTimer = 0f;
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightGrace)
            {
                currentState = GhostState.Patrolling;
                Roam();
                Debug.Log("👁️ Lost sight of player for grace — resuming patrol.");
            }
        }
    }

    void MoveTowardPlayerSound()
    {
        if (player == null) return;

        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < 3f || (screamDetector != null && !screamDetector.IsPlayerTalking()))
        {
            currentState = GhostState.Patrolling;
            Roam();
        }
    }

    // ---------- Mic event handlers ----------

    private void HandlePlayerLoudTalk(Vector3 talkPos)
    {
        if (currentState != GhostState.ChasingPlayer) return;
        if (agent == null) return;

        // Move toward last heard location (no teleport)
        agent.SetDestination(talkPos);
        lostSightTimer = 0f; // keep chase alive
        Debug.Log("👂 Loud talk heard — moving to last heard position.");
    }

    private void HandlePlayerScream(Vector3 screamPos)
    {
        if (currentState != GhostState.ChasingPlayer) return;
        TeleportVeryCloseToPlayer();
        Debug.Log("😱 Scream detected — TELEPORTING near player.");
    }

    private void TeleportVeryCloseToPlayer(float minDist = 1.2f, float maxDist = 2.0f)
    {
        if (player == null || agent == null) return;

        Vector2 rnd = Random.insideUnitCircle.normalized;
        Vector3 offset = new Vector3(rnd.x, 0f, rnd.y) * Random.Range(minDist, maxDist);
        Vector3 candidate = player.position + offset;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            // fallback in front of player
            Vector3 fwd = player.forward;
            agent.Warp(player.position + fwd.normalized * minDist);
        }

        // Face the player
        Vector3 look = player.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);

        // keep chase active so we don't drop to patrol
        lostSightTimer = 0f;
    }
}
