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
    public float roamRadius = 20f;
    public float roamDelay = 5f;
    private float nextRoamTime = 0f;

    [Header("Vision Settings")]
    public float visionRange = 12f;
    public float fieldOfView = 60f;
    public float verticalFieldOfView = 45f;

    [Header("Mic Detection")]
    public ScreamDetector screamDetector; // Drag reference in Inspector

    [Header("Blinking Settings")]
    public float minBlinkTime = 1f;
    public float maxBlinkTime = 2f;
    public SkinnedMeshRenderer ghostRenderer;

    [Header("Teleport Settings")]
    public bool allowTeleportation = true;
    public float teleportCooldown = 20f;
    private float nextTeleportTime = 0f;
    public float teleportDistanceFromPlayer = 10f;

    private Vector3 lastPosition;
    private float stuckTimer = 0f; 


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
                break;

            case GhostState.ChasingPlayer:
                ChasePlayer();
                break;

            case GhostState.HearingPlayer:
                MoveTowardPlayerSound();
                break;
        }

        //CheckIfStuck();
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

    // IEnumerator BlinkRoutine()
    // {
    //     while (true)
    //     {
    //         yield return new WaitForSeconds(Random.Range(minBlinkTime, maxBlinkTime));

    //         // Fade out
    //         SetGhostAlpha(0f);
    //         yield return new WaitForSeconds(2f); // Invisible duration

    //         // Fade in
    //         SetGhostAlpha(1f);
    //     }
    // }

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


    void TryTeleport()
    {
        if (!allowTeleportation || Time.time < nextTeleportTime || player == null) return;

        Vector3 randomOffset = Random.onUnitSphere;
        randomOffset.y = 0; // Keep on ground
        randomOffset = randomOffset.normalized * teleportDistanceFromPlayer;

        Vector3 newPos = player.position + randomOffset;

        if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            nextTeleportTime = Time.time + teleportCooldown;
            Debug.Log("👻 Ghost teleported!");
        }
    }

    void CheckIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance < 0.05f) // not moving
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 3f) // stuck for 3 seconds
            {
                Debug.LogWarning("🚧 Ghost seems stuck, teleporting to new location.");
                Roam(); // Force new destination
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f; // Reset if moving
        }

        lastPosition = transform.position;
    }

    // void DetectPlayerBySight()
    // {
    //     if (player == null || eyePoint == null) return;

    //     // Target the player's chest/head
    //     Vector3 playerTargetPoint = player.position + Vector3.up * 1.2f;

    //     // Direction from ghost's eyes to player
    //     Vector3 directionToPlayer = playerTargetPoint - eyePoint.position;
    //     float angle = Vector3.Angle(transform.forward, directionToPlayer);

    //     if (directionToPlayer.magnitude < visionRange && angle < fieldOfView / 2f)
    //     {
    //         Ray ray = new Ray(eyePoint.position, directionToPlayer.normalized);
    //         if (Physics.Raycast(ray, out RaycastHit hit, visionRange))
    //         {
    //             if (hit.transform.CompareTag("Player"))
    //             {
    //                 currentState = GhostState.ChasingPlayer;
    //                 Debug.Log("👁️ Ghost sees the player!");
    //             }
    //         }
    //     }
    // }

    // void DetectPlayerBySight()
    // {
    //     if (player == null || eyePoint == null) return;

    //     // Target chest/head area of player
    //     Vector3 playerTargetPoint = player.position + Vector3.up * 1.2f;

    //     // Calculate direction and distance
    //     Vector3 directionToPlayer = playerTargetPoint - eyePoint.position;
    //     float distanceToPlayer = directionToPlayer.magnitude;
    //     float angle = Vector3.Angle(eyePoint.forward, directionToPlayer);

    //     // Draw field of view cone (debug)
    //     Debug.DrawRay(eyePoint.position, eyePoint.forward * visionRange, Color.green); // center line
    //     Debug.DrawRay(eyePoint.position, Quaternion.Euler(0, fieldOfView / 2, 0) * eyePoint.forward * visionRange, Color.yellow);
    //     Debug.DrawRay(eyePoint.position, Quaternion.Euler(0, -fieldOfView / 2, 0) * eyePoint.forward * visionRange, Color.yellow);

    //     // Check if player is within range and angle
    //     if (distanceToPlayer < visionRange && angle < fieldOfView / 2f)
    //     {
    //         // Perform raycast to see if ghost has line-of-sight
    //         Ray ray = new Ray(eyePoint.position, directionToPlayer.normalized);
    //         if (Physics.Raycast(ray, out RaycastHit hit, visionRange))
    //         {
    //             //Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red); // show hit line
    //             //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.magenta);
    //             if (hit.transform.root.name != "Asylum")
    //             {
    //                 Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red); // show hit line
    //                 Debug.Log("hit name: " + hit.transform.name + " | tag: " + hit.transform.tag + " | root: " + hit.transform.root.name);
    //             }
    //             if (hit.transform.root.CompareTag("Player"))
    //             {
    //                 Debug.Log("👁️ Ghost sees the player!");
    //                 currentState = GhostState.ChasingPlayer;
    //             }
    //         }
    //     }
    // }


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

    void DetectMicInput()
    {
        if (screamDetector != null && screamDetector.IsPlayerTalking())
        {
            currentState = GhostState.HearingPlayer;
            Debug.Log("🎤 Ghost hears the player talking!");
        }
    }

    void ChasePlayer()
    {
        Debug.Log("🚨 Ghost is chasing the player!");
        agent.SetDestination(player.position);

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > visionRange * 1.5f)
        {
            currentState = GhostState.Patrolling;
            Roam();
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
