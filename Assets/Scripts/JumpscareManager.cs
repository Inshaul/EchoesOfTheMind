using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager Instance;

    [Header("References")]
    [Tooltip("XR camera (CenterEye) or player transform.")]
    public Transform player;
    public GhostAIController ghost;             // optional (for GhostBlink scare)
    public ScreenOverlayController overlay;     // optional (if you want text flashes, not required)

    [Header("Timing / Limits")]
    [Tooltip("Minimum seconds between any two jumpscares.")]
    public float globalCooldown = 10f;
    [Tooltip("At high fear, cooldown scales toward globalCooldown * fearCooldownScale.")]
    public float fearCooldownScale = 0.5f;      // 0.5 => half cooldown at high fear
    [Tooltip("Hard cap to avoid spam.")]
    public int maxScaresPerMinute = 4;
    [Tooltip("Allow scares during an active chase? Usually false for fairness.")]
    public bool allowDuringChase = false;

    [Header("Fear Gates")]
    [Range(0, 100)] public int minFearForRandom = 20;
    [Range(0, 100)] public int highFear = 65;

    [Header("Spawn / Targeting")]
    public float minSpawnDist = 2.5f;
    public float maxSpawnDist = 6f;
    [Tooltip("Optional fixed spawn points (e.g., corridor corners).")]
    public Transform[] presetSpawnPoints;

    [Header("Audio (3D One-Shot)")]
    [Tooltip("A movable 3D AudioSource in the scene (NOT on the player).")]
    public AudioSource oneShot3D;
    public AudioClip[] stings;                  // short jump stings
    public AudioClip[] whispers;                // quiet whisper clips
    public AudioClip breathClip;                // optional: heavy breathing close-by

    [Header("Door Slam (Audio Only)")]
    public AudioClip doorSlamClip;

    [Header("Stairs Running")]
    public AudioClip[] stairsRunClips;
    [Tooltip("Optional fixed locations to play stairs audio from.")]
    public Transform[] stairPoints;
    public float stairsRunMinDelay = 0.0f;
    public float stairsRunDuration = 2.0f;

    [Header("Ghost Blink-In (Optional Visual Scare)")]
    public bool allowGhostBlinkScare = true;
    public float blinkVisibleTime = 0.6f;

    [Header("Haptics (VR-safe)")]
    public XRBaseController leftController;
    public XRBaseController rightController;
    [Range(0, 1f)] public float hapticAmpLight = 0.2f;
    public float hapticDurLight = 0.06f;
    [Range(0, 1f)] public float hapticAmpHeavy = 0.5f;
    public float hapticDurHeavy = 0.12f;

    [Header("Durations")]
    [Tooltip("How long to run the fusebox flicker for the LightFlicker scare.")]
    public float flickerDuration = 1.2f;

    // Runtime state
    private float lastScareTime = -999f;
    private readonly Queue<float> scareTimestamps = new Queue<float>(8);
    private bool isBusy;

    public bool IsScareRunning { get; private set; }

    public enum ScareType
    {
        AudioSting,
        WhisperBehind,
        LightFlicker,
        DoorSlam,
        ObjectLaunch,    // requires throwable props (not used by you right now, can ignore)
        GhostBlink,
        StairsRun
    }

    // ------------------------- Public API -------------------------

    /// <summary>
    /// Attempts a random scare (respects fear, cooldowns, and chase rules).
    /// </summary>
    public void TryScheduleRandomScare(int currentFear, GhostAIController.GhostState ghostState)
    {
        Debug.LogError("canscarenow: " + CanScareNow(currentFear, ghostState));
        if (!CanScareNow(currentFear, ghostState)) return;

        var options = new List<ScareType>
        {
            ScareType.WhisperBehind,
            ScareType.LightFlicker,
            ScareType.StairsRun
        };

        if (currentFear >= highFear)
        {
            options.Add(ScareType.AudioSting);
            options.Add(ScareType.DoorSlam);
            if (allowGhostBlinkScare) options.Add(ScareType.GhostBlink);
        }

        var type = options[Random.Range(0, options.Count)];
        TriggerScare(type, currentFear);
    }

    /// <summary>
    /// Triggers a specific scare now. Respects limits unless force==true.
    /// </summary>
    public void TriggerScare(ScareType type, int currentFear, bool force = false)
    {
        var state = ghost != null ? ghost.currentState : GhostAIController.GhostState.Patrolling;
        if (!force && !CanScareNow(currentFear, state)) return;
        StartCoroutine(DoScare(type, currentFear));
    }

    // ---------------------- Core logic & helpers -------------------

    private bool CanScareNow(int fear, GhostAIController.GhostState ghostState)
    {
        if (isBusy) return false;
        if (player == null) return false;

        if (!allowDuringChase && ghostState == GhostAIController.GhostState.ChasingPlayer)
            return false;

        if (Time.time - lastScareTime < CurrentCooldown(fear))
            return false;

        // hard cap per minute
        float now = Time.time;
        while (scareTimestamps.Count > 0 && now - scareTimestamps.Peek() > 60f)
            scareTimestamps.Dequeue();
        if (scareTimestamps.Count >= maxScaresPerMinute)
            return false;

        if (fear < minFearForRandom) // too calm
            return false;

        return true;
    }

    private float CurrentCooldown(int fear)
    {
        // Interpolate cooldown as fear rises
        float t = Mathf.InverseLerp(minFearForRandom, 100f, fear);
        float scaled = Mathf.Lerp(globalCooldown, globalCooldown * fearCooldownScale, t);
        return Mathf.Max(2f, scaled);
    }

    private IEnumerator DoScare(ScareType type, int fear)
    {
        isBusy = true;
        lastScareTime = Time.time;
        IsScareRunning = true;
        scareTimestamps.Enqueue(Time.time);

        switch (type)
        {
            case ScareType.AudioSting: yield return StartCoroutine(AudioSting()); break;
            case ScareType.WhisperBehind: yield return StartCoroutine(WhisperBehind()); break;
            case ScareType.LightFlicker: yield return StartCoroutine(LightFlick()); break;
            case ScareType.DoorSlam: yield return StartCoroutine(DoorSlam()); break;
            case ScareType.ObjectLaunch: yield return StartCoroutine(ObjectLaunchTowardsPlayer()); break;
            case ScareType.GhostBlink: yield return StartCoroutine(GhostBlinkIn()); break;
            case ScareType.StairsRun: yield return StartCoroutine(StairsRun()); break;
        }

        isBusy = false;
        IsScareRunning = false;
    }

    // ----------------------- Individual scares ---------------------

    private IEnumerator AudioSting()
    {
        if (oneShot3D == null || stings == null || stings.Length == 0) yield break;

        var clip = stings[Random.Range(0, stings.Length)];
        oneShot3D.transform.position = PickNavMeshPointNearPlayer();
        oneShot3D.spatialBlend = 1f;
        oneShot3D.PlayOneShot(clip);

        PulseHeavy();
        yield return new WaitForSeconds(Mathf.Min(0.6f, clip.length));
    }

    private IEnumerator WhisperBehind()
    {
        if (oneShot3D == null || whispers == null || whispers.Length == 0) yield break;

        var clip = whispers[Random.Range(0, whispers.Length)];

        // place exactly behind the player's facing direction
        Vector3 behind = player.position - player.forward * 0.8f + Vector3.up * 0.1f;
        oneShot3D.transform.position = behind;
        oneShot3D.spatialBlend = 1f;
        oneShot3D.minDistance = 0.1f;
        oneShot3D.maxDistance = 3f;
        oneShot3D.PlayOneShot(clip);

        PulseLight();
        yield return new WaitForSeconds(Mathf.Min(1.0f, clip.length));
    }

    private IEnumerator LightFlick()
    {
        // ✅ Use your FuseBoxController
        PulseLight();
        if (GameDirector.Instance.fuseBox != null)
        {
            GameDirector.Instance.fuseBox.FlickerLights(true);
            yield return new WaitForSeconds(flickerDuration);
            GameDirector.Instance.fuseBox.FlickerLights(false);
        }
        else
        {
            yield return new WaitForSeconds(flickerDuration);
        }
    }

    private IEnumerator DoorSlam()
    {
        // ✅ Audio-only door slam
        if (oneShot3D == null || doorSlamClip == null) yield break;

        oneShot3D.transform.position = PickNavMeshPointNearPlayer();
        oneShot3D.spatialBlend = 1f;
        oneShot3D.PlayOneShot(doorSlamClip);

        PulseHeavy();
        yield return new WaitForSeconds(Mathf.Min(0.6f, doorSlamClip.length));
    }

    private IEnumerator StairsRun()
    {
        if (oneShot3D == null || stairsRunClips == null || stairsRunClips.Length == 0) yield break;

        var clip = stairsRunClips[Random.Range(0, stairsRunClips.Length)];

        // Choose a location: prefer stairPoints; otherwise fake “upstairs” behind/aside
        Vector3 pos;
        if (stairPoints != null && stairPoints.Length > 0)
        {
            pos = stairPoints[Random.Range(0, stairPoints.Length)].position;
        }
        else
        {
            Vector3 offset = (Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * player.forward) * 2.5f;
            pos = player.position + offset + Vector3.up * 2.2f;
        }

        oneShot3D.transform.position = pos;
        oneShot3D.spatialBlend = 1f;

        if (stairsRunMinDelay > 0f)
            yield return new WaitForSeconds(stairsRunMinDelay);

        oneShot3D.PlayOneShot(clip);
        PulseLight();

        yield return new WaitForSeconds(Mathf.Min(stairsRunDuration, clip.length));
    }

    private IEnumerator ObjectLaunchTowardsPlayer()
    {
        // Placeholder – only needed if you wire in throwable props later.
        yield return null;
    }

    private IEnumerator GhostBlinkIn()
    {
        if (ghost == null || !allowGhostBlinkScare) yield break;

        // pick a point front-left/right of the player
        Vector3 dir = Quaternion.Euler(0f, Random.value < 0.5f ? -35f : 35f, 0f) * player.forward;
        Vector3 target = player.position + dir.normalized * Random.Range(minSpawnDist, Mathf.Min(maxSpawnDist, 4.5f));

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            ghost.gameObject.SetActive(true);
            var agent = ghost.GetComponent<NavMeshAgent>();
            if (agent != null) agent.Warp(hit.position); else ghost.transform.position = hit.position;

            // face player
            Vector3 look = (player.position - ghost.transform.position);
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) ghost.transform.rotation = Quaternion.LookRotation(look);

            // optional audio sting
            if (oneShot3D != null && stings != null && stings.Length > 0)
            {
                oneShot3D.transform.position = ghost.transform.position;
                oneShot3D.spatialBlend = 1f;
                oneShot3D.PlayOneShot(stings[Random.Range(0, stings.Length)]);
            }

            PulseHeavy();

            // briefly toggle renderer visibility
            var renderer = ghost.ghostRenderer != null ? ghost.ghostRenderer : ghost.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                bool prev = renderer.enabled;
                renderer.enabled = true;
                yield return new WaitForSeconds(blinkVisibleTime);
                renderer.enabled = prev;
            }
        }

        yield return null;
    }

    // -------------------------- Utilities --------------------------

    private Vector3 PickNavMeshPointNearPlayer()
    {
        Vector3 origin = player.position;
        Vector3 randomDir = Random.onUnitSphere; randomDir.y = 0f;
        Vector3 candidate = origin + randomDir.normalized * Random.Range(minSpawnDist, maxSpawnDist);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, 2.0f, NavMesh.AllAreas))
            return hit.position;

        // fallback to preset points
        if (presetSpawnPoints != null && presetSpawnPoints.Length > 0)
            return presetSpawnPoints[Random.Range(0, presetSpawnPoints.Length)].position;

        return origin + randomDir.normalized * minSpawnDist;
    }

    private void PulseLight()
    {
        leftController?.SendHapticImpulse(hapticAmpLight, hapticDurLight);
        rightController?.SendHapticImpulse(hapticAmpLight, hapticDurLight);
    }

    private void PulseHeavy()
    {
        leftController?.SendHapticImpulse(hapticAmpHeavy, hapticDurHeavy);
        rightController?.SendHapticImpulse(hapticAmpHeavy, hapticDurHeavy);
    }
}
