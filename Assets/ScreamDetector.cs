using UnityEngine;

public class ScreamDetector : MonoBehaviour
{
    // 🔊 Events
    public System.Action<Vector3> OnLoudTalk;   // fires when volume >= talkThreshold
    public System.Action<Vector3> OnScream;     // fires when volume >= screamThreshold

    [Header("Microphone")]
    public string microphoneDevice;
    public float checkInterval = 0.1f;

    private AudioClip micClip;
    private float nextCheckTime;
    private float micLoudness = 0f; // current RMS

    [Header("Thresholds")]
    [Tooltip("Volume at/above this is considered 'talking loud'.")]
    [Range(0f, 1f)] public float talkThreshold = 0.25f;
    [Tooltip("Volume at/above this is considered a 'scream'. Must be > talkThreshold.")]
    [Range(0f, 1f)] public float screamThreshold = 0.55f;

    [Header("Cooldowns")]
    [Tooltip("Min seconds between two talk events.")]
    public float talkCooldown = 0.5f;
    [Tooltip("Min seconds between two scream events.")]
    public float screamCooldown = 2.0f;

    private float _lastTalkTime = -999f;
    private float _lastScreamTime = -999f;

    [Header("Optional SFX")]
    public AudioSource audioSource;     // optional: to play a local sfx on scream
    public AudioClip screamClip;        // optional
    public bool playClipOnScream = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    [Tooltip("How often to print mic loudness logs (seconds).")]
    public float debugPrintInterval = 0.5f;
    public bool showDebugMeter = false; // simple on-screen bar
    private float _nextDebugLogTime = 0f;

    void Start()
    {
        // Use default mic if none specified
        microphoneDevice = (Microphone.devices.Length > 0) ? Microphone.devices[0] : null;

        if (microphoneDevice == null)
        {
            Debug.LogError("No microphone found! ScreamDetector disabled.");
            enabled = false;
            return;
        }

        // start a looping mic clip
        micClip = Microphone.Start(microphoneDevice, true, 10, 44100);
        if (enableDebugLogs) Debug.Log($"🎙️ Using mic: {microphoneDevice}");
    }

    void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        micLoudness = GetMicVolume();
        Vector3 sourcePos = transform.position; // put this on the XR camera/head

        // Throttled loudness logging
        if (enableDebugLogs && Time.time >= _nextDebugLogTime)
        {
            // Debug.Log($"[ScreamDetector] loudness={micLoudness:F3}  talk≥{talkThreshold:F2}  scream≥{screamThreshold:F2}");
            _nextDebugLogTime = Time.time + debugPrintInterval;
        }

        // Prioritize scream over talk
        if (micLoudness >= screamThreshold)
        {
            if (Time.time - _lastScreamTime >= screamCooldown)
            {
                _lastScreamTime = Time.time;

                if (playClipOnScream && screamClip && audioSource)
                    audioSource.PlayOneShot(screamClip);

                if (enableDebugLogs) Debug.Log($"😱 Scream event fired at {micLoudness:F3}");
                OnScream?.Invoke(sourcePos);
            }
        }
        else if (micLoudness >= talkThreshold)
        {
            if (Time.time - _lastTalkTime >= talkCooldown)
            {
                _lastTalkTime = Time.time;
                if (enableDebugLogs) Debug.Log($"🗣️ Loud talk event fired at {micLoudness:F3}");
                OnLoudTalk?.Invoke(sourcePos);
            }
        }
    }

    float GetMicVolume()
    {
        if (micClip == null || microphoneDevice == null || !Microphone.IsRecording(microphoneDevice)) return 0f;

        int micPosition = Microphone.GetPosition(microphoneDevice) - 128;
        if (micPosition < 0) return 0f;

        const int len = 256;
        float[] samples = new float[len];
        micClip.GetData(samples, micPosition);

        // RMS
        float sum = 0f;
        for (int i = 0; i < len; i++) sum += samples[i] * samples[i];
        return Mathf.Sqrt(sum / len);
    }

    // Backward-compat helper if other scripts use it
    public bool IsPlayerTalking() => micLoudness > talkThreshold;

    // Optional on-screen meter for quick tuning
    void OnGUI()
    {
        if (!showDebugMeter) return;

        const float w = 220f, h = 20f;
        float pct = Mathf.Clamp01(micLoudness); // 0..1
        Rect bg = new Rect(10, 10, w, h);
        Rect fill = new Rect(10, 10, w * pct, h);

        // Background
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Box(bg, GUIContent.none);

        // Fill
        GUI.color = Color.green;
        GUI.Box(fill, GUIContent.none);

        // Threshold tick marks
        GUI.color = Color.yellow;
        GUI.Box(new Rect(10 + w * talkThreshold - 1, 10, 2, h), GUIContent.none);
        GUI.color = Color.red;
        GUI.Box(new Rect(10 + w * screamThreshold - 1, 10, 2, h), GUIContent.none);

        // Label
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 34, w, 20), $"Mic: {micLoudness:F3}  (talk {talkThreshold:F2} / scream {screamThreshold:F2})");
    }
}
