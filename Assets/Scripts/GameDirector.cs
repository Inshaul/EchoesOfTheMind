using UnityEngine;
using System.Collections;

public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance;

    public GhostAIController ghost; 
    public DollManager dollManager; 
    public FearManager fearManager; 

    public FuseBoxController fuseBox; // Drag in Inspector
    private bool powerOn = false;

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
        HideGhost();
        powerOn = false;
        if (fuseBox != null) fuseBox.TurnOffAllRooms();
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
        ShowGhost();
        ghostReason = GhostSpawnReason.Doll;
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
    }

    public void OnDollDestroyed()
    {
        if (ghostReason == GhostSpawnReason.Doll)
            DespawnGhost();
    }

    public void OnFearThreshold()
    {
        SpawnGhostByFear();
    }
}
