using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.AI;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    public GhostAIController ghost;
    public DollManager dollManager;
    public FearManager fearManager;

    public FuseBoxController fuseBox;
    public HintManager hintManager;
    public HellManager hellManager;

    public FinalEscapeManager finalEscapeManager;

    [SerializeField]
    private List<string> hintsText = new List<string>
    {
        "This place is too dark… I need a light. A flashlight should be around here somewhere.",
        "The air hums with silence… maybe the fuse box can restore the asylum’s power.",
        "Whispers… faint and restless. I wonder where they’re calling me from. Could it be the doll?",
        "That cursed door radiates heat… only fire will break the doll’s bond. Throw it into hell itself.",
        "Ummm...No more whispers...Be prepared!",
        "The walls tremble… the spirits cry out. The final path has opened — run, before the demon takes me too!"
    };

    public int destroyedDollCounter = 0;

    public ScreenOverlayController overlay; // optional

    [Header("Intro")]
    public AudioClip introClip;
    [TextArea(2, 6)]
    private string introText =
        "My name is Sha… a police investigator chasing the trail of a missing friend.\n\n" +
        "The search has led me to Ravenswood Asylum… abandoned for decades, yet the air still carries whispers of the damned.\n\n" +
        "They say a demon roams these halls — feeding on fear, binding lost souls to cursed voodoo dolls. Destroy the dolls, and you might set the spirits free. Fail… and you will join them forever.\n\n" +
        "But every step I take, it’s watching me… listening… waiting for the moment I slip. Even my voice can draw it closer, and one scream will be my last.\n\n" +
        "The longer I linger, the more restless it becomes. Its anger twists reality — shadows move where no light falls, and the walls themselves breathe.\n\n" +
        "Stay in the dark for too long, and it will find me… sometimes appearing in places it never should, as if space itself bends to its will.\n\n" +
        "Somewhere in the darkness lies a magical book… find it, and it may guide me before the demon’s hunger consumes all hope.";

    [Header("Endings")]
    public AudioClip gameOverClip;
    public AudioClip gameWinClip;
    [TextArea] public string gameOverText = "You have fallen to the entity… Your mind is not your own.";
    [TextArea] public string gameWinText = "<size=15>The dolls are ash. The whispers fade. You step out… but the asylum remembers your name.</size>";

    [Header("Jumpscares")]
    public JumpscareManager jumpscares;   // assign in Inspector

    [Tooltip("Auto-spawn ghost when fear crosses the FearManager.spawnThreshold?")]
    public bool spawnGhostOnFearThreshold = false; // ⬅ keep off for tier-first flow

    public AudioSource finalExitAppearedAudio;

    public AudioSource finalEscapeAudio;

    private Coroutine ghostTimeoutCoroutine;
    private enum GhostSpawnReason { None, Fear, Doll }
    private GhostSpawnReason ghostReason = GhostSpawnReason.None;

    public bool PostIntro = false;

    public Transform playerTeleportLocation;

    private Transform player;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(GameStartFlow());
    }

    void OnDestroy()
    {
        if (fearManager != null)
        {
            fearManager.OnFearChanged -= HandleFearChanged;
            fearManager.OnFearTierChanged -= HandleFearTierChanged;
            fearManager.OnFearThresholdCrossed -= HandleFearThresholdCrossed;
            fearManager.OnTierGateReached -= HandleTierGateReached;
        }
    }

    private IEnumerator GameStartFlow()
    {
        // Prepare scene
        HideGhost();
        if (fuseBox != null) fuseBox.TurnOffAllRooms();
        if (hintManager != null) hintManager.SetHint("");

        // intro
        if (overlay != null)
            yield return overlay.PlayBlackScreen(introText, introClip, keepBlackDuringAudio: true, fadeOutAfter: true);

        PostIntro = true;
        // Start gameplay
        if (hintManager != null) hintManager.SetHint(hintsText[0]); // "Pick up the Flashlight"

        // Subscribe to fear events (tier-driven)
        if (fearManager != null)
        {
            fearManager.OnFearChanged += HandleFearChanged; // optional; keep for logs/telemetry
            fearManager.OnFearTierChanged += HandleFearTierChanged; // optional
            fearManager.OnFearThresholdCrossed += HandleFearThresholdCrossed; // guarded by flag
            fearManager.OnTierGateReached += HandleTierGateReached; // main driver
        }
        //TestClimax(); //Test Climax
        yield return null;
    }

    // -------- Fear Event Handlers --------

    private void HandleFearChanged(float newFear, float delta)
    {
        // Optional debugging hook; not used to trigger scares anymore
        // Debug.Log($"Fear: {newFear:F1} (Δ{delta:+0.00;-0.00})");
    }

    private void HandleFearTierChanged(int newTier, int oldTier)
    {
        // Optional: you can log this. The real work happens in HandleTierGateReached.
        // Debug.Log($"Tier change: {oldTier} -> {newTier}");
    }

    private void HandleTierGateReached(int tier)
    {
        // Tier just reached and fear is clamped at boundary.
        // Run a quick tier beat (flicker + random tier scare), then release the gate.
        StartCoroutine(TierBeatThenReleaseGate(tier));
    }

    private IEnumerator TierBeatThenReleaseGate(int tier)
    {
        // 1) Punctuate with a short global flicker
        if (fuseBox != null)
        {
            fuseBox.FlickerLights(true);
            yield return new WaitForSeconds(1.0f);
            fuseBox.FlickerLights(false);
        }

        // 2) Random, tier-appropriate scare (forced so it always lands)
        if (jumpscares != null && fearManager != null)
        {
            int fearNow = Mathf.RoundToInt(fearManager.CurrentFear);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.5f)); // tiny organic offset
            jumpscares.TriggerRandomTierScare(tier, fearNow, force: true);

            // Optional: sometimes add a second quick beat for higher tiers
            if (tier >= 1 && UnityEngine.Random.value < 0.4f)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 0.8f));
                jumpscares.TriggerRandomTierScare(tier, fearNow, force: true);
            }
        }

        // 3) Let fear progress beyond this tier
        yield return new WaitForSeconds(0.4f);
        fearManager.ReleaseTierGate();
    }

    private void HandleFearThresholdCrossed(float threshold)
    {
        if (!spawnGhostOnFearThreshold) return;
        int dollsRemaining = dollManager.TotalDolls - destroyedDollCounter;
        // Only spawn if not already hunting for another reason
        if (ghost != null && !ghost.gameObject.activeSelf && dollsRemaining > 0)
            OnFearThreshold();
    }

    // -------- High-level game hooks --------

    public void OnFirstTorchGrabbed()
    {
        if (hintManager != null) hintManager.SetHint(hintsText[1]);
    }

    public void OnFirstPowerRestored()
    {
        if (hintManager != null) hintManager.SetHint(hintsText[2]);
        if (dollManager != null) dollManager.SpawnNextDoll();
    }

    public void OnDollGrabbed()
    {
        SpawnGhostByDoll();
        fuseBox?.FlickerLights();
        if (hellManager != null) StartCoroutine(hellManager.DelayedHellRoomActivation());
        hintManager?.SetHint(hintsText[3]);
    }

    public void OnDollDestroyed()
    {
        destroyedDollCounter++;

        // Increase ghost speed + animation speed a bit each doll
        var agent = ghost != null ? ghost.GetComponent<NavMeshAgent>() : null;
        if (agent) agent.speed *= 1.1f;
        if (ghost != null && ghost.animator != null) ghost.animator.speed *= 1.1f;

        int dollsRemaining = dollManager != null ? (dollManager.TotalDolls - destroyedDollCounter) : 0;

        // Enable teleport if <= 2 dolls left
        if (dollsRemaining <= 2 && ghost != null)
        {
            ghost.allowTeleportation = true;
        }

        // End ghost hunt
        DespawnGhost();

        hellManager?.ResetHellRooms();
        if (fuseBox != null && fuseBox.fuseBoxLever != null && fuseBox.fuseBoxLever.IspowerOn()) fuseBox.fuseBoxLever.TogglePower();

        // StartCoroutine(Climax()); // test

        if (dollsRemaining == 0)
        {
            StartCoroutine(Climax());
        }
        else
        {
            StartCoroutine(DelayedDollSpawnActions());
        }
    }

    private void TestClimax()
    {
        var agent = ghost != null ? ghost.GetComponent<NavMeshAgent>() : null;
        if (agent) agent.speed = 1.9f;
        if (ghost != null && ghost.animator != null) ghost.animator.speed *= 1.7f;
        StartCoroutine(Climax());
    }

    private IEnumerator Climax()
    {
        hintManager.SetHint(hintsText[4]);
        yield return new WaitForSeconds(40f);
        finalEscapeAudio.Play();
        fuseBox.SetFinalRunEnvironment();
        StartGhostHunt(true);
        StartCoroutine(AppearFinalEscape());
    }

    public void OnEscape(Action action)
    {
        DespawnGhost();
        action?.Invoke();
        ShowGameEnd();
    }

    private IEnumerator AppearFinalEscape()
    {
        yield return new WaitForSeconds(60f);
        hintManager.SetHint(hintsText[5]);
        finalExitAppearedAudio.Play();
        finalEscapeManager.finalEscapeDoor.SetActive(true);
    }

    private IEnumerator DelayedDollSpawnActions()
    {
        yield return new WaitForSeconds(5f);
        OnFirstPowerRestored();
    }

    // -------- Ghost control --------

    public void OnFearThreshold()
    {
        SpawnGhostByFear();
    }

    public void SpawnGhostByFear()
    {
        if (ghostReason != GhostSpawnReason.None) return;
        StartGhostHunt();
        ghostReason = GhostSpawnReason.Fear;
        if (ghostTimeoutCoroutine != null) StopCoroutine(ghostTimeoutCoroutine);
        ghostTimeoutCoroutine = StartCoroutine(GhostTimeout(60f));
    }

    public void SpawnGhostByDoll()
    {
        if (ghostReason != GhostSpawnReason.None) return;
        ghostReason = GhostSpawnReason.Doll;
        StartGhostHunt();
    }

    private void StartGhostHunt(bool allowTeleoprtation = false)
    {
        SpawnGhostAtRandomLocation();
        if (ghost != null) ghost.gameObject.SetActive(true);

        var ghostAI = ghost != null ? ghost.GetComponent<GhostAIController>() : null;
        if (ghostAI != null) ghostAI.currentState = GhostAIController.GhostState.Patrolling;

        var audio = ghost != null ? ghost.GetComponent<AudioSource>() : null;
        if (audio != null) { audio.loop = true; audio.Play(); }

        if (allowTeleoprtation)
        {
            ghost.allowTeleportation = true;
        }
    }

    private void SpawnGhostAtRandomLocation()
    {
        if (ghost == null) return;

        NavMeshHit hit;
        Vector3 randomPoint = ghost.transform.position;

        if (NavMesh.SamplePosition(UnityEngine.Random.insideUnitSphere * 50f, out hit, 20f, NavMesh.AllAreas))
            randomPoint = hit.position;

        ghost.transform.position = randomPoint;
        var agent = ghost.GetComponent<NavMeshAgent>();
        if (agent) agent.Warp(randomPoint);
    }

    public void DespawnGhost()
    {
        HideGhost();
        ghostReason = GhostSpawnReason.None;
        fuseBox?.FlickerLights(false);
        if (ghostTimeoutCoroutine != null) StopCoroutine(ghostTimeoutCoroutine);
    }

    private IEnumerator GhostTimeout(float t)
    {
        yield return new WaitForSeconds(t);
        if (ghostReason == GhostSpawnReason.Fear) DespawnGhost();
    }

    private void ShowGhost()
    {
        if (ghost != null) ghost.gameObject.SetActive(true);
    }

    private void HideGhost()
    {
        if (ghost != null) ghost.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {
        TeleportPlayerOnScreenDisplay();
        finalEscapeAudio.Stop();
        DespawnGhost();
        if (overlay != null)
        {
            // Play overlay with game over text + audio
            StartCoroutine(overlay.PlayBlackScreen(gameOverText, gameOverClip, keepBlackDuringAudio: true, fadeOutAfter: false));
        }
        else
        {
            Debug.LogWarning("No ScreenOverlayController assigned for Game Over!");
        }
    }

    public void ShowGameEnd()
    {
        TeleportPlayerOnScreenDisplay();
        finalEscapeAudio.Stop();
        DespawnGhost();
        if (overlay != null)
        {
            string endText = gameWinText + "\n<size=10>Thanks for playing — Echoes of the Mind</size>";
            StartCoroutine(overlay.PlayBlackScreen(endText, gameWinClip, keepBlackDuringAudio: true, fadeOutAfter: false));
        }
        else
        {
            Debug.LogWarning("No ScreenOverlayController assigned for Game End!");
        }
    }
    
    void TeleportPlayerOnScreenDisplay()
    {
        if (playerTeleportLocation != null && player != null)
        {
            // Move player to catch location
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = playerTeleportLocation.position;
            if (cc != null) cc.enabled = true;
        }
    }
}
