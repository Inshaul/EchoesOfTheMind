using UnityEngine;
using System;

public class FearManager : MonoBehaviour
{
    [Header("Fear Value (0-100)")]
    [Range(0, 100)] public float fearLevel = 0f;
    public float CurrentFear => fearLevel;

    [Header("Environment Influence")]
    public bool isPlayerLit = false;
    [Tooltip("Fear per second added in darkness.")]
    public float fearDarkModidifier = 1f;
    [Tooltip("Fear per second removed in light.")]
    public float restoreModifier = 2f;

    [Header("Tiers & Thresholds")]
    [Tooltip("Fear tiers split points (ascending). Example: 33, 66 for 3 tiers.")]
    public int tier1 = 33;
    public int tier2 = 66;

    [Tooltip("When fear crosses this upward, we notify the director (e.g., to spawn ghost).")]
    public int spawnThreshold = 75;

    [Tooltip("Seconds to wait before allowing another threshold crossing event.")]
    public float thresholdCooldown = 6f;

    [Header("Integration")]
    [Tooltip("If true, fear won't change while a jumpscare is playing.")]
    public bool pauseDuringJumpscare = true;

    [Header("Tier Gating")]
    [Tooltip("If enabled, stop fear right when a higher tier is reached until ReleaseTierGate() is called.")]
    public bool enableTierGating = true;
    public event Action<int> OnTierGateReached; // fired when entering a new (higher) tier

    private bool tierGateActive = false;
    private int currentGateTier = -1;

    public void ReleaseTierGate()
    {
        tierGateActive = false;
        currentGateTier = -1;
    }

    // ------------------ Events ------------------
    public event Action<float, float> OnFearChanged;   // (newFear, delta)
    public event Action<int, int> OnFearTierChanged;   // (newTier, oldTier)
    public event Action<float> OnFearThresholdCrossed; // (threshold)

    // ------------------ Internals ------------------
    private float _lastFear;
    private int _lastTier;
    private float _lastThresholdEventTime = -999f;

    [Header("Max Fear Handling")]
    public float maxFearHoldSeconds = 30f;   // how long before reset
    private float _maxFearTimer = 0f;

    void Start()
    {
        _lastFear = fearLevel;
        _lastTier = GetTier(fearLevel);
    }

    void Update()
    {
        // Freeze during jumpscare if requested
        if (pauseDuringJumpscare && JumpscareRunning())
            return;
        if (fearLevel >= 100f)
        {
            _maxFearTimer += Time.deltaTime;

            if (_maxFearTimer >= maxFearHoldSeconds)
            {
                // Reset fear back to 0
                float old = fearLevel;
                fearLevel = 0f;
                NotifyChanges(old);

                _maxFearTimer = 0f; // reset timer so it won’t fire repeatedly
            }
        }
        else
        {
            _maxFearTimer = 0f; // player dropped below max fear, reset timer
        }
    }

    public void SetLit(bool value) => isPlayerLit = value;

    public void AddFear()
    {
        if (pauseDuringJumpscare && JumpscareRunning()) return;
        float old = fearLevel;
        fearLevel = Mathf.Min(100, fearLevel + fearDarkModidifier * Time.deltaTime);
        ApplyTierGating(ref fearLevel);
        if (!Mathf.Approximately(old, fearLevel)) NotifyChanges(old);
    }

    /// <summary> Time-scaled reduce (deltaTime * restoreModifier). </summary>
    public void ReduceFear()
    {
        if (pauseDuringJumpscare && JumpscareRunning()) return;
        float old = fearLevel;
        fearLevel = Mathf.Max(0, fearLevel - restoreModifier * Time.deltaTime);
        ApplyTierGating(ref fearLevel);
        if (!Mathf.Approximately(old, fearLevel)) NotifyChanges(old);
    }

    /// <summary> Immediate add by amount (0..100 scale). </summary>
    public void AddFearAmount(float amount)
    {
        if (pauseDuringJumpscare && JumpscareRunning()) return;
        float old = fearLevel;
        fearLevel = Mathf.Clamp(fearLevel + Mathf.Max(0f, amount), 0f, 100f);
        ApplyTierGating(ref fearLevel);
        if (!Mathf.Approximately(old, fearLevel)) NotifyChanges(old);
    }

    /// <summary> Immediate reduce by amount (0..100 scale). </summary>
    public void ReduceFearAmount(float amount)
    {
        if (pauseDuringJumpscare && JumpscareRunning()) return;
        float old = fearLevel;
        fearLevel = Mathf.Clamp(fearLevel - Mathf.Max(0f, amount), 0f, 100f);
        ApplyTierGating(ref fearLevel);
        if (!Mathf.Approximately(old, fearLevel)) NotifyChanges(old);
    }
    private void NotifyChanges(float old)
    {
        float delta = fearLevel - old;
        OnFearChanged?.Invoke(fearLevel, delta);

        int newTier = GetTier(fearLevel);
        if (newTier != _lastTier)
        {
            OnFearTierChanged?.Invoke(newTier, _lastTier);
            _lastTier = newTier;
        }

        // Upward crossing of spawn threshold (with cooldown)
        if (old < spawnThreshold && fearLevel >= spawnThreshold &&
            Time.time - _lastThresholdEventTime >= thresholdCooldown)
        {
            _lastThresholdEventTime = Time.time;
            OnFearThresholdCrossed?.Invoke(spawnThreshold);
        }

        _lastFear = fearLevel;
    }

    private int GetTier(float fear)
    {
        if (fear < tier1) return 0;
        if (fear < tier2) return 1;
        return 2;
    }

    private float GetTierUpperBound(int tier)
    {
        if (tier <= 0) return tier1;
        if (tier == 1) return tier2;
        return 100f;
    }

    private void ApplyTierGating(ref float value)
    {
        if (!enableTierGating) return;

        int prospectiveTier = GetTier(value);

        // If we just crossed into a higher tier and no gate is active, latch gate and clamp to boundary.
        if (prospectiveTier > _lastTier && !tierGateActive)
        {
            tierGateActive = true;
            currentGateTier = prospectiveTier;

            float cap = GetTierUpperBound(prospectiveTier);
            value = Mathf.Min(value, cap);

            OnTierGateReached?.Invoke(prospectiveTier);
        }

        // While gate is active, do not allow fear to exceed the gate tier's upper bound.
        if (tierGateActive && currentGateTier >= 0)
        {
            float cap = GetTierUpperBound(currentGateTier);
            value = Mathf.Min(value, cap);
        }
    }

    private bool JumpscareRunning()
    {
        return JumpscareManager.Instance != null && JumpscareManager.Instance.IsScareRunning;
    }
}
