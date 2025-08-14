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

    public List<String> hintsText = new List<string> { "Pick up the Flashlight", "Find the fuse box to restore the full power", "Find the vodoo doll-Whispers helps", "Find the door to hell and throw the doll into it", "Leave the house" };

    public int destroyedDollCounter = 0;
    
    public ScreenOverlayController overlay;       // drag ScreenOverlayController.Instance here
    [Header("Intro")]
    public AudioClip introClip;                   // assign your ElevenLabs mp3/wav
    [TextArea(2,6)]
    public string introText =
        "My name is Sha… a police investigator chasing the trail of a missing friend.\n\n" +
        "They say a demon roams these halls — feeding on fear, binding lost souls to cursed voodoo dolls.\n" +
        "Destroy the dolls, and you might set the spirits free. Fail… and you will join them forever.\n\n" +
        "Even my voice can draw it closer — and one scream will be my last.\n\n" +
        "Somewhere in the darkness lies a magical book… find it, and it may guide you.";

    [Header("Endings")]
    public AudioClip gameOverClip;                // optional TTS for death
    public AudioClip gameWinClip;                 // optional TTS for victory

    [TextArea] public string gameOverText = "You have fallen to the entity… Your mind is not your own.";
    [TextArea] public string gameWinText  = "The dolls are ash. The whispers fade. You step out… but the asylum remembers your name.";
    
    //private bool powerOn = false;

    private Coroutine ghostTimeoutCoroutine;
    private enum GhostSpawnReason { None, Fear, Doll }
    private GhostSpawnReason ghostReason = GhostSpawnReason.None;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // HideGhost();
        // //powerOn = false;
        // hintManager.SetHint(hintsText[0]);
        // if (fuseBox != null) fuseBox.TurnOffAllRooms();
        StartCoroutine(GameStartFlow());

    }

    private IEnumerator GameStartFlow()
    {
        // Prepare scene: hide ghost, cut power, clear hints until intro finishes.
        HideGhost();
        if (fuseBox != null) fuseBox.TurnOffAllRooms();
        if (hintManager != null) hintManager.SetHint(""); // hide during intro

        // Play intro: keep screen black during VO, then fade out to gameplay.
        if (overlay != null)
            yield return overlay.PlayBlackScreen(introText, introClip, keepBlackDuringAudio: true, fadeOutAfter: true);

        // Now begin your normal loop
        if (hintManager != null) hintManager.SetHint(hintsText[0]); // "Pick up the Flashlight"
    }

    public void OnFirstTorchGrabbed()
    {
        hintManager.SetHint(hintsText[1]);
    }

    public void OnFirstPowerRestored()
    {
        hintManager.SetHint(hintsText[2]);
        dollManager.SpawnNextDoll();
    }

    public void SpawnGhostByFear()
    {
        if (ghostReason != GhostSpawnReason.None) return;
        ShowGhost();
        ghostReason = GhostSpawnReason.Fear;
        if (ghostTimeoutCoroutine != null) StopCoroutine(ghostTimeoutCoroutine);
        ghostTimeoutCoroutine = StartCoroutine(GhostTimeout(60f));
    }

    public void SpawnGhostByDoll()
    {
        if (ghostReason != GhostSpawnReason.None) return;
        ghostReason = GhostSpawnReason.Doll;
        StartGhostHunt();
        //ShowGhost();
    }

    void SpawnGhostAtRandomLocation()
    {
        NavMeshHit hit;
        Vector3 randomPoint = Vector3.zero;

        if (NavMesh.SamplePosition(UnityEngine.Random.insideUnitSphere * 50f, out hit, 10f, NavMesh.AllAreas))
        {
            randomPoint = hit.position;
        }

        ghost.transform.position = randomPoint;
        ghost.GetComponent<NavMeshAgent>().Warp(randomPoint);
    }

    public void DespawnGhost()
    {
        HideGhost();
        ghostReason = GhostSpawnReason.None;
        if (ghostTimeoutCoroutine != null) StopCoroutine(ghostTimeoutCoroutine);
    }

    private void ShowGhost()
    {
        if (ghost != null) ghost.gameObject.SetActive(true);
    }
    private void HideGhost()
    {
        if (ghost != null) ghost.gameObject.SetActive(false);
    }

    private IEnumerator GhostTimeout(float t)
    {
        yield return new WaitForSeconds(t);
        if (ghostReason == GhostSpawnReason.Fear)
            DespawnGhost();
    }

    public void OnDollGrabbed()
    {
        SpawnGhostByDoll();
        fuseBox.FlickerLights();
        StartCoroutine(hellManager.DelayedHellRoomActivation());
        hintManager.SetHint(hintsText[3]);
    }

    public void OnDollDestroyed()
    {
        destroyedDollCounter++;

        // Increase ghost speed
        var agent = ghost.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent) agent.speed *= 1.1f;
        int dollsRemaining = dollManager.TotalDolls - destroyedDollCounter;
        // Enable teleport if <= 2 dolls left
        if (dollsRemaining <= 2)
        {
            var ghostAI = ghost.GetComponent<GhostAIController>();
            if (ghostAI != null)
            {
                ghostAI.allowTeleportation = true;
            }
        }

        // End ghost hunt
        DespawnGhost();
        Debug.LogWarning("Despawning Ghost!");
        //if (ghostReason == GhostSpawnReason.Doll) DespawnGhost();
        fuseBox.FlickerLights(false); // stop global flicker after ritual
        fuseBox.fuseBoxLever.TogglePower();
        StartCoroutine(DelayedDollSpawnActions());
    }

    private IEnumerator DelayedDollSpawnActions()
    {
        yield return new WaitForSeconds(5f);
        OnFirstPowerRestored();
    }


    void StartGhostHunt()
    {
        SpawnGhostAtRandomLocation();  // Random spawn point
        ghost.gameObject.SetActive(true);

        var ghostAI = ghost.GetComponent<GhostAIController>();
        if (ghostAI != null)
            ghostAI.currentState = GhostAIController.GhostState.Patrolling;

        // Optional: start looping hunt audio
        var audio = ghost.GetComponent<AudioSource>();
        if (audio != null)
        {
            audio.loop = true;
            audio.Play();
        }
    }

    public void OnFearThreshold()
    {
        SpawnGhostByFear();
    }
}
