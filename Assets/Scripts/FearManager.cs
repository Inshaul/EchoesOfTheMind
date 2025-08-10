using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FearManager : MonoBehaviour
{
    public static FearManager Instance;
    public float fearLevel = 0f; // 0-100
    private bool isPlayerLit = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void SetLit(bool value)
    {
        isPlayerLit = value;
    }

    void Update()
    {
        if (isPlayerLit)
        {
            // Reduce fear gradually when in light
            fearLevel = Mathf.Max(0, fearLevel - Time.deltaTime * 2f);
        }
        else
        {
            // Increase fear gradually in darkness
            fearLevel = Mathf.Min(100, fearLevel + Time.deltaTime * 1f);
        }
        // Optionally, update UI or trigger effects here
    }
    public void AddFear(float amount)
    {
        fearLevel = Mathf.Clamp(fearLevel + amount, 0, 100);
        // Optional: trigger SFX, screen effect, etc.
    }
    public void ReduceFear(float amount)
    {
        fearLevel = Mathf.Clamp(fearLevel - amount, 0, 100);
    }
}
