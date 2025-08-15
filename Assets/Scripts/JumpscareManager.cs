using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager Instance;

    [Header("References")]
    [Tooltip("XR camera (CenterEye) or player root transform.")]
    public Transform player;
    public GhostAIController ghost;             // optional (for GhostBlink scare)
    public FuseBoxController fuseBox;           // uses your flicker system
    public ScreenOverlayController overlay;     // optional (not used here, but kept)

    [Header("Audio (3D One-Shot Emitter)")]
    [Tooltip("Movable 3D AudioSource in the scene (NOT on player). spatialBlend=1.")]
    public AudioSource oneShot3D;
    public AudioClip[] stings;                  // short jump stings
    public AudioClip[] whispers;                // quiet whisper clips
    public AudioClip doorSlamClip;
    public AudioClip[] stairsRunClips;

    [Header("Placement")]
    public float minSpawnDist = 2.0f;
    public float maxSpawnDist = 6.0f;
    [Tooltip("Optional fixed locations to play stairs audio from.")]
    public Transform[] stairPoints;

    [Header("Durations")]
    [Tooltip("Fusebox flicker duration for the LightFlicker scare.")]
    public float flickerDuration = 1.2f;
    public float blinkVisibleTime = 0.6f;
    public float stairsRunMinDelay = 0.0f;
    public float stairsRunHold = 2.0f;

    [Header("Haptics (VR-safe)")]
    public XRBaseController leftController;
    public XRBaseController rightController;
    [Range(0, 1f)] public float hapticAmpLight = 0.2f;
    public float hapticDurLight = 0.06f;
    [Range(0, 1f)] public float hapticAmpHeavy = 0.5f;
    public float hapticDurHeavy = 0.12f;

    [Header("Ghost Blink (Optional Visual)")]
    public bool allowGhostBlinkScare = true;

    [Header("Tier Scare Pools")]
    [Tooltip("Scares used when entering Tier 0 (subtle).")]
    public ScareType[] tier0Scares = { ScareType.LightFlicker, ScareType.WhisperBehind };

    [Tooltip("Scares used when entering Tier 1 (medium).")]
    public ScareType[] tier1Scares = { ScareType.WhisperBehind, ScareType.StairsRun, ScareType.LightFlicker };

    [Tooltip("Scares used when entering Tier 2 (intense).")]
    public ScareType[] tier2Scares = { ScareType.DoorSlam, ScareType.AudioSting, ScareType.StairsRun, ScareType.GhostBlink };

    // Runtime state
    public bool IsScareRunning { get; private set; }
    public int TotalScaresTriggered { get; private set; }

    private float lastScareTime = -999f;
    private readonly Queue<float> scareTimestamps = new Queue<float>(8);

    public enum ScareType
    {
        AudioSting,
        WhisperBehind,
        LightFlicker,
        DoorSlam,
        ObjectLaunch,   // placeholder (not used here)
        GhostBlink,
        StairsRun
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    // -------------------- Public API --------------------

    /// <summary>
    /// Random, tier-appropriate scare. Force=true ignores internal cooldowns (good for tier beats).
    /// </summary>
    public void TriggerRandomTierScare(int tier, int currentFear, bool force = true)
    {
        ScareType[] pool = tier switch
        {
            0 => tier0Scares,
            1 => tier1Scares,
            _ => tier2Scares
        };
        if (pool == null || pool.Length == 0) return;

        var type = pool[Random.Range(0, pool.Length)];
        TriggerScare(type, currentFear, force);
    }

    /// <summary>
    /// Triggers a specific scare now. If force=false, a simple cooldown is applied.
    /// </summary>
    public void TriggerScare(ScareType type, int currentFear, bool force = false)
    {
        if (!force && !CanRun()) return;
        StartCoroutine(DoScare(type));
    }

    // -------------------- Core --------------------

    private bool CanRun()
    {
        // Simple guard: avoid overlapping scares & basic per-minute cap
        if (IsScareRunning) return false;

        float now = Time.time;
        while (scareTimestamps.Count > 0 && now - scareTimestamps.Peek() > 60f)
            scareTimestamps.Dequeue();
        if (scareTimestamps.Count >= 6) return false; // hard cap per minute

        if (now - lastScareTime < 2.0f) return false; // local cooldown (2s)
        return true;
    }

    private IEnumerator DoScare(ScareType type)
    {
        IsScareRunning = true;
        TotalScaresTriggered++;
        lastScareTime = Time.time;
        scareTimestamps.Enqueue(Time.time);

        switch (type)
        {
            case ScareType.AudioSting:     yield return AudioSting(); break;
            case ScareType.WhisperBehind:  yield return WhisperBehind(); break;
            case ScareType.LightFlicker:   yield return LightFlick(); break;
            case ScareType.DoorSlam:       yield return DoorSlam(); break;
            case ScareType.GhostBlink:     yield return GhostBlinkIn(); break;
            case ScareType.StairsRun:      yield return StairsRun(); break;
            case ScareType.ObjectLaunch:   yield return ObjectLaunchTowardsPlayer(); break; // placeholder
        }

        IsScareRunning = false;
    }

    // -------------------- Scares --------------------

    private IEnumerator AudioSting()
    {
        if (!EnsureEmitter() || stings == null || stings.Length == 0) yield break;
        oneShot3D.transform.position = PickPointNearPlayer();
        oneShot3D.PlayOneShot(stings[Random.Range(0, stings.Length)]);
        PulseHeavy();
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator WhisperBehind()
    {
        if (!EnsureEmitter() || whispers == null || whispers.Length == 0) yield break;
        Vector3 behind = player.position - player.forward * 0.8f + Vector3.up * 0.1f;
        oneShot3D.transform.position = behind;
        oneShot3D.minDistance = 0.1f;
        oneShot3D.maxDistance = Mathf.Max(oneShot3D.maxDistance, 3f);
        oneShot3D.PlayOneShot(whispers[Random.Range(0, whispers.Length)]);
        PulseLight();
        yield return new WaitForSeconds(0.8f);
    }

    private IEnumerator LightFlick()
    {
        PulseLight();
        if (fuseBox != null)
        {
            fuseBox.FlickerChandalierRoom(true);
            yield return new WaitForSeconds(flickerDuration);
            fuseBox.FlickerChandalierRoom(false);
        }
        else
        {
            yield return new WaitForSeconds(flickerDuration);
        }
    }

    private IEnumerator DoorSlam()
    {
        if (!EnsureEmitter() || doorSlamClip == null) yield break;
        oneShot3D.transform.position = PickPointNearPlayer();
        oneShot3D.PlayOneShot(doorSlamClip);
        PulseHeavy();
        yield return new WaitForSeconds(Mathf.Min(0.6f, doorSlamClip.length));
    }

    private IEnumerator StairsRun()
    {
        if (!EnsureEmitter() || stairsRunClips == null || stairsRunClips.Length == 0) yield break;

        Vector3 pos;
        if (stairPoints != null && stairPoints.Length > 0)
            pos = stairPoints[Random.Range(0, stairPoints.Length)].position;
        else
            pos = player.position + (Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * player.forward) * 2.5f + Vector3.up * 2.0f;

        oneShot3D.transform.position = pos;

        if (stairsRunMinDelay > 0f) yield return new WaitForSeconds(stairsRunMinDelay);
        var clip = stairsRunClips[Random.Range(0, stairsRunClips.Length)];
        oneShot3D.PlayOneShot(clip);
        PulseLight();
        yield return new WaitForSeconds(Mathf.Min(stairsRunHold, clip.length));
    }

    private IEnumerator GhostBlinkIn()
    {
        if (ghost == null || !allowGhostBlinkScare) yield break;

        Vector3 dir = Quaternion.Euler(0f, Random.value < 0.5f ? -35f : 35f, 0f) * player.forward;
        Vector3 target = player.position + dir.normalized * Random.Range(minSpawnDist, Mathf.Min(maxSpawnDist, 4.5f));

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            ghost.gameObject.SetActive(true);

            var agent = ghost.GetComponent<NavMeshAgent>();
            if (agent != null) agent.Warp(hit.position);
            else ghost.transform.position = hit.position;

            Vector3 look = player.position - ghost.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) ghost.transform.rotation = Quaternion.LookRotation(look);

            if (EnsureEmitter() && stings != null && stings.Length > 0)
            {
                oneShot3D.transform.position = ghost.transform.position;
                oneShot3D.PlayOneShot(stings[Random.Range(0, stings.Length)]);
            }

            PulseHeavy();

            // --- New logic: wait until ghost reaches player or 5s ---
            float timer = 0f;
            float stopDistance = 1.5f; // how close ghost must be to count as "reached"
            while (timer < 20f)
            {
                if (Vector3.Distance(ghost.transform.position, player.position) <= stopDistance)
                {
                    break; // player reached
                }
                timer += Time.deltaTime;
                yield return null;
            }

            ghost.gameObject.SetActive(false); // disable after 5s or if reached
        }
    }

    private IEnumerator ObjectLaunchTowardsPlayer()
    {
        AudioSting();
        // Placeholder for future prop-throw scare. Currently no-op.
        yield return null;
    }

    // -------------------- Utils --------------------

    private bool EnsureEmitter()
    {
        if (oneShot3D == null || !oneShot3D.gameObject.activeInHierarchy)
            return false;

        // Ensure 3D
        oneShot3D.spatialBlend = 1f;
        if (oneShot3D.minDistance < 0.3f) oneShot3D.minDistance = 0.6f;
        if (oneShot3D.maxDistance < 6f) oneShot3D.maxDistance = 8f;
        return true;
    }

    private Vector3 PickPointNearPlayer()
    {
        Vector3 origin = player != null ? player.position : Vector3.zero;
        Vector3 randomDir = Random.insideUnitCircle.normalized;
        Vector3 flat = new Vector3(randomDir.x, 0f, randomDir.y);
        Vector3 candidate = origin + flat * Random.Range(minSpawnDist, maxSpawnDist);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, 2.0f, NavMesh.AllAreas))
            return hit.position;

        // Fallback 2m ahead of player to guarantee audibility
        Vector3 forward = player != null ? player.forward : Vector3.forward;
        return origin + forward.normalized * 2f;
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
