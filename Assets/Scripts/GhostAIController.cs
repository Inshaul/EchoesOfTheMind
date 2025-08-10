using System.Collections;
using System.Collections.Generic;
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
    public float visionRange = 12f;
    public float fieldOfView = 60f;
    public float verticalFieldOfView = 45f;

    [Header("Catch Settings")]
    public Transform catchTeleportLocation; // Target location when player is caught
    public float catchDistance = 2f;        // Distance to trigger catch

    [Header("Mic Detection")]
    public ScreamDetector screamDetector; // Drag reference in Inspector

    [Header("Blinking Settings")]
    public float minBlinkTime = 1f;
    public float maxBlinkTime = 2f;
    public SkinnedMeshRenderer ghostRenderer;

    [Header("Teleport Settings")]
    public bool allowTeleportation = false;
    public float teleportCooldown = 20f;
    private float nextTeleportTime = 0f;
    public float teleportDistanceFromPlayer = 10f;

    private Vector3 lastPosition;

    public AudioSource audioSource;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!player)
        {
            Debug.LogError("❌ Player not found in scene!");
            enabled = false;
            return;
        }

        // Delay hunt start slightly
        Invoke(nameof(StartPatrolling), 2f);
        StartCoroutine(BlinkRoutine());
    }



    void Update()
    {
        switch (currentState)
        {
            case GhostState.Patrolling:
                RandomRoam();
                DetectPlayerBySight();
                DetectMicInput();
                TryTeleportNearPlayer();
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
            // Pick a random position around the player
            Vector3 randomDir = Random.insideUnitSphere * teleportDistanceFromPlayer;
            randomDir += player.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position); // Ensures NavMesh sync
                Debug.Log("👻 Ghost teleported!");
            }

            nextTeleportTime = Time.time + teleportCooldown;
        }
    }

    void StartPatrolling()
    {
        currentState = GhostState.Patrolling;
        Roam(); // Start with a random roam
    }

    void RandomRoam()
    {
        if (Time.time >= nextRoamTime && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Roam();
        }
    }

    void Roam()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
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
                yield return new WaitForSeconds(0.5f); // Blink duration
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

        Vector3 playerTargetPoint = player.position + Vector3.up * 1.2f; // chest/head level
        Vector3 directionToPlayer = playerTargetPoint - eyePoint.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > visionRange) return;

        // 🔵 3D angle between ghost forward and direction to player (no projection)
        float angleToPlayer = Vector3.Angle(eyePoint.forward, directionToPlayer.normalized);

        // 💡 Visual debug lines
        Debug.DrawRay(eyePoint.position, eyePoint.forward * visionRange, Color.green); // forward
        Debug.DrawRay(eyePoint.position, directionToPlayer.normalized * visionRange, Color.magenta); // toward player

        if (angleToPlayer < fieldOfView / 2f)
        {
            if (Physics.Raycast(eyePoint.position, directionToPlayer.normalized, out RaycastHit hit, visionRange))
            {
                Debug.DrawRay(eyePoint.position, directionToPlayer.normalized * hit.distance, Color.red); // hit line

                Debug.Log($"Ray hit: {hit.transform.name} | Tag: {hit.transform.tag}");

                // Check if we hit player or any object under player root
                if (hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
                {
                    Debug.Log("👁️ Ghost sees the player!");
                    currentState = GhostState.ChasingPlayer;
                }
            }
        }
    }

    void TeleportPlayerOnCatch()
    {
        if (catchTeleportLocation != null && player != null)
        {
            player.position = catchTeleportLocation.position;

            // If player has a CharacterController, reset it properly
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.position = catchTeleportLocation.position;
                cc.enabled = true;
            }

            Debug.Log($"📍 Player teleported to {catchTeleportLocation.position}");
        }

        currentState = GhostState.Patrolling;
        Roam();
    }    

    void DetectMicInput()
    {
        if (screamDetector != null && screamDetector.IsPlayerTalking())
        {
            currentState = GhostState.HearingPlayer;
            Debug.Log("🎤 Ghost hears the player talking!");
        }
    }

    // void ChasePlayer()
    // {
    //     Debug.Log("🚨 Ghost is chasing the player!");
    //     agent.SetDestination(player.position);

    //     float distance = Vector3.Distance(transform.position, player.position);
    //     if (distance > visionRange * 1.5f)
    //     {
    //         currentState = GhostState.Patrolling;
    //         Roam();
    //     }
    // }
    void ChasePlayer()
    {
        Debug.Log("🚨 Ghost is chasing the player!");
        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);

        // ✅ Player caught check
        if (distance <= catchDistance)
        {
            Debug.Log("🪝 Player caught by ghost!");
            TeleportPlayerOnCatch();
            return;
        }

        Vector3 playerTargetPoint = player.position + Vector3.up * 1.2f;
        Vector3 directionToPlayer = playerTargetPoint - eyePoint.position;
        float angleToPlayer = Vector3.Angle(eyePoint.forward, directionToPlayer.normalized);

        bool canSeePlayer = false;
        if (distance <= visionRange && angleToPlayer < fieldOfView / 2f)
        {
            if (Physics.Raycast(eyePoint.position, directionToPlayer.normalized, out RaycastHit hit, visionRange))
            {
                if (hit.transform.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
                {
                    canSeePlayer = true;
                }
            }
        }

        if (!canSeePlayer)
        {
            currentState = GhostState.Patrolling;
            Roam();
            Debug.Log("👁️ Lost sight of player — resuming patrol.");
        }
    }
    void MoveTowardPlayerSound()
    {
        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < 3f || (screamDetector != null && !screamDetector.IsPlayerTalking()))
        {
            currentState = GhostState.Patrolling;
            Roam();
        }
    }
}
