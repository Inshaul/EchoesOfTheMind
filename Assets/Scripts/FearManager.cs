using UnityEngine;
using System;

public class FearManager : MonoBehaviour
{
    [Header("Fear Value (0-100)")]
    [Range(0,100)] public float fearLevel = 0f;

    [Header("Environment Influence")]
    public bool isPlayerLit = false;
    public float fearDarkModidifier = 1f;
    public float restoreModifier = 2f;

    [Header("Tiers & Thresholds")]
    [Tooltip("Fear tiers split points (ascending). Example: 33, 66 for 3 tiers.")]
    public int tier1 = 33;
    public int tier2 = 66;

    [Tooltip("When fear crosses this upward, we notify the GameDirector (for ghost spawn by fear).")]
    public int spawnThreshold = 75;

    [Tooltip("Seconds to wait before allowing another threshold crossing event.")]
    public float thresholdCooldown = 6f;

    public bool pauseDuringJumpscare = true;

    // Events
    public event Action<float, float> OnFearChanged;         // (newFear, delta)
    public event Action<int, int> OnFearTierChanged;         // (newTier, oldTier)
    public event Action<float> OnFearThresholdCrossed;       // (threshold)

    private float _lastFear;
    private int _lastTier;
    private float _lastThresholdEventTime = -999f;

    public float CurrentFear => fearLevel;

    public void SetLit(bool value) => isPlayerLit = value;

    [Header("Tier Gating")]
    public bool enableTierGating = true;

    public event Action<int> OnTierGateReached; // fired when entering a new tier and gating is enabled

    private bool tierGateActive = false;
    private int currentGateTier = -1;

    public void ReleaseTierGate()
    {
        tierGateActive = false;
        currentGateTier = -1;
    }

    // (helper) upper bound for a tier (0..2)
    private float GetTierUpperBound(int tier)
    {
        if (tier <= 0) return tier1;
        if (tier == 1) return tier2;
        return 100f;
}

    void Start()
    {
        _lastFear = fearLevel;
        _lastTier = GetTier(fearLevel);
    }

    void Update()
    {
        // float old = fearLevel;
        // if (pauseDuringJumpscare && GameDirector.Instance.jumpscares.IsScareRunning)
        // {
        //     return; // no changes this frame
        // }
        // if (isPlayerLit)
        //     fearLevel = Mathf.Max(0, fearLevel - Time.deltaTime * restoreModifier);
        // else
        //     fearLevel = Mathf.Min(100, fearLevel + Time.deltaTime * fearDarkModidifier);

        // if (!Mathf.Approximately(old, fearLevel))
        // {
        //     float delta = fearLevel - old;
        //     OnFearChanged?.Invoke(fearLevel, delta);

        //     int newTier = GetTier(fearLevel);
        //     if (newTier != _lastTier)
        //     {
        //         OnFearTierChanged?.Invoke(newTier, _lastTier);
        //         _lastTier = newTier;
        //     }

        //     // Upward crossing of spawn threshold (with cooldown)
        //     if (old < spawnThreshold && fearLevel >= spawnThreshold &&
        //         Time.time - _lastThresholdEventTime >= thresholdCooldown)
        //     {
        //         _lastThresholdEventTime = Time.time;
        //         OnFearThresholdCrossed?.Invoke(spawnThreshold);
        //     }
        // }
    }

    public void AddFear()
    {
        if (pauseDuringJumpscare && GameDirector.Instance.jumpscares.IsScareRunning)
        {
            return; 
        }
        float old = fearLevel;
        fearLevel = Mathf.Min(100, fearLevel + Time.deltaTime * fearDarkModidifier);
        ManualNotify(old);
    }

    public void ReduceFear()
    {
        if (pauseDuringJumpscare && GameDirector.Instance.jumpscares.IsScareRunning)
        {
            return; 
        }
        float old = fearLevel;
        fearLevel = Mathf.Max(0, fearLevel - Time.deltaTime * restoreModifier);
        ManualNotify(old);
    }

    private void ManualNotify(float old)
    {
        if (!Mathf.Approximately(old, fearLevel))
        {
            float delta = fearLevel - old;
            OnFearChanged?.Invoke(fearLevel, delta);

            int newTier = GetTier(fearLevel);
            if (newTier != _lastTier)
            {
                OnFearTierChanged?.Invoke(newTier, _lastTier);
                _lastTier = newTier;
            }
            if (old < spawnThreshold && fearLevel >= spawnThreshold &&
                Time.time - _lastThresholdEventTime >= thresholdCooldown)
            {
                _lastThresholdEventTime = Time.time;
                OnFearThresholdCrossed?.Invoke(spawnThreshold);
            }
        }
        _lastFear = fearLevel;
    }

    private int GetTier(float fear)
    {
        if (fear < tier1) return 0;
        if (fear < tier2) return 1;
        return 2; // highest tier
    }
}
