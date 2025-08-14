using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // <- New Input System
using UnityEngine.XR.Interaction.Toolkit; // (optional, for haptics)

public class ScreenOverlayController : MonoBehaviour
{
    public static ScreenOverlayController Instance;

    [Header("UI")]
    public CanvasGroup blackOverlay;
    public TMP_Text overlayText;
    public AudioSource voiceSource;

    [Header("Behavior")]
    public float fadeDuration = 1f;
    public float minSkipTime = 0.75f;

    [Header("Skip (XRI / Input System)")]
    [Tooltip("Bind this to XR Controller primary button or trigger in your Input Actions.")]
    public InputActionProperty skipAction; // e.g., RightHand/primaryButton or trigger

    [Header("Optional Haptics")]
    public XRBaseController rightController;
    public XRBaseController leftController;
    [Range(0, 1f)] public float skipHapticAmplitude = 0.25f;
    public float skipHapticDuration = 0.06f;

    private bool running;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (blackOverlay != null) blackOverlay.alpha = 0f;
        if (overlayText != null) overlayText.text = "";
    }

    void OnEnable()
    {
        if (skipAction.action != null) skipAction.action.Enable();
    }

    void OnDisable()
    {
        if (skipAction.action != null) skipAction.action.Disable();
    }

    void Update()
    {
        if (!running || skipAction.action == null) return;

        // Allow skip after a short delay to avoid accidental presses
        if (Time.timeSinceLevelLoad > minSkipTime && skipAction.action.triggered)
        {
            // Optional: quick haptic tick on either hand
            TryHaptic(rightController);
            TryHaptic(leftController);

            StopAllCoroutines(); // ends current sequence immediately
        }
    }

    public IEnumerator PlayBlackScreen(string message, AudioClip clip, bool keepBlackDuringAudio = true, bool fadeOutAfter = true)
    {
        running = true;

        overlayText.text = message ?? "";
        yield return Fade(blackOverlay.alpha, 1f, fadeDuration); // fade in to black

        if (clip != null && voiceSource != null)
        {
            voiceSource.clip = clip;
            voiceSource.Play();

            if (!keepBlackDuringAudio)
                yield return Fade(1f, 0f, fadeDuration); // reveal during VO

            while (voiceSource.isPlaying) yield return null; // wait, but Update() can skip
        }

        if (fadeOutAfter)
        {
            overlayText.text = "";
            yield return Fade(blackOverlay.alpha, 0f, fadeDuration);
        }

        running = false;
    }

    public IEnumerator ShowHoldBlack(string message, float fadeIn = 0.6f)
    {
        overlayText.text = message ?? "";
        yield return Fade(blackOverlay.alpha, 1f, fadeIn);
    }

    public IEnumerator Hide(float duration = 0.6f)
    {
        overlayText.text = "";
        yield return Fade(blackOverlay.alpha, 0f, duration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (blackOverlay == null || Mathf.Approximately(from, to)) yield break;
        float t = 0f;
        blackOverlay.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            blackOverlay.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        blackOverlay.alpha = to;
    }

    private void TryHaptic(XRBaseController controller)
    {
        if (controller == null) return;
        controller.SendHapticImpulse(skipHapticAmplitude, skipHapticDuration);
    }
}
