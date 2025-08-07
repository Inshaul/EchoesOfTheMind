using System.Collections.Generic;
using UnityEngine;


public class DollManager : MonoBehaviour
{
    public List<Transform> spawnPoints;        
    public GameObject dollPrefab;              


    private GameObject currentDoll;
    private int dollsRemaining = 5;           

    private List<int> usedIndices = new List<int>();
 
    public void SpawnNextDoll()
    {
        if (dollsRemaining <= 0)
        {
            Debug.Log("✅ All dolls completed.");
            return;
        }

        int index;
        do
        {
            index = Random.Range(0, spawnPoints.Count);
        } while (usedIndices.Contains(index) && usedIndices.Count < spawnPoints.Count);

        usedIndices.Add(index);
        Vector3 spawnPos = spawnPoints[index].position;

        currentDoll = Instantiate(dollPrefab, spawnPos, Quaternion.identity);
        dollsRemaining--;

        Debug.Log($"🪆 Doll spawned at {spawnPoints[index].name}. Dolls left: {dollsRemaining}");
    }

    public void OnDollDestroyedOrUsed()
    {
        Destroy(currentDoll);
        Invoke(nameof(SpawnNextDoll), 3f); // Delay to allow hunt or audio events
    }
}
