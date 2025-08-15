using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FearManager : MonoBehaviour
{
    public float fearLevel = 0f; // 0-100
    private bool isPlayerLit = false;

    public float fearDarkModidifier = 1f;

    public float restoreModifier = 2f;

    public void SetLit(bool value)
    {
        isPlayerLit = value;
    }

    void Update()
    {
        if (isPlayerLit)
        {
            fearLevel = Mathf.Max(0, fearLevel - Time.deltaTime * restoreModifier);
        }
        else
        {
            fearLevel = Mathf.Min(100, fearLevel + Time.deltaTime * fearDarkModidifier);
        }
    }
    public void AddFear(float amount)
    {
        fearLevel = Mathf.Clamp(fearLevel + amount, 0, 100);
    }
    public void ReduceFear(float amount)
    {
        fearLevel = Mathf.Clamp(fearLevel - amount, 0, 100);
    }
}
