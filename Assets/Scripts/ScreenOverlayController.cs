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

    [Header("Text Animation")]
    [Tooltip("Play a scale-in animation on the overlay text when shown.")]
    public bool enableTextScale = true;
    [Tooltip("Starting local scale for the text (0 = invisible).")]
    public float textScaleFrom = 0.0f;
    [Tooltip("Ending local scale for the text. Use 0.2 for a subtle small text, or 1.0 for normal size.")]
    public float textScaleTo = 0.2f;
    [Tooltip("Duration of the scale-in animation.")]
    public float textScaleDuration = 0.6f;
    [Tooltip("Easing curve for the scale-in (default: ease-out back-ish).")]
    public AnimationCurve textScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool running;
    private Coroutine textScaleCo;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (blackOverlay != null) blackOverlay.alpha = 0f;
        if (overlayText != null) overlayText.text = "";

        // nicer default easing (overshoot feel)
        if (textScaleCurve.keys.Length <= 2)
            textScaleCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2.5f),
                new Keyframe(0.85f, 1.1f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
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
            running = false;
            // hide overlay quickly
            if (overlayText) overlayText.text = "";
            if (blackOverlay) blackOverlay.alpha = 0f;
            // reset text scale
            ResetTextScale();
        }
    }

    public IEnumerator PlayBlackScreen(string message, AudioClip clip, bool keepBlackDuringAudio = true, bool fadeOutAfter = true)
    {
        running = true;

        if (overlayText != null)
        {
            overlayText.text = message ?? "";
            PrepareTextScale();
        }

        // Fade in to black
        yield return Fade(blackOverlay != null ? blackOverlay.alpha : 0f, 1f, fadeDuration);

        // Scale-in text once we are black
        if (enableTextScale && overlayText != null)
        {
            StartTextScaleIn();
        }

        // Voiceover block
        if (clip != null && voiceSource != null)
        {
            voiceSource.clip = clip;
            voiceSource.Play();

            if (!keepBlackDuringAudio)
                yield return Fade(1f, 0f, fadeDuration); // reveal during VO if desired

            while (voiceSource.isPlaying) yield return null; // wait, but Update() can skip
        }

        if (fadeOutAfter)
        {
            if (overlayText != null) overlayText.text = "";
            ResetTextScale(); // ready for the next time
            yield return Fade(blackOverlay != null ? blackOverlay.alpha : 1f, 0f, fadeDuration);
        }

        running = false;
    }

    public IEnumerator ShowHoldBlack(string message, float fadeIn = 0.6f)
    {
        if (overlayText != null)
        {
            overlayText.text = message ?? "";
            PrepareTextScale();
        }

        yield return Fade(blackOverlay != null ? blackOverlay.alpha : 0f, 1f, fadeIn);

        if (enableTextScale && overlayText != null)
        {
            StartTextScaleIn();
        }
    }

    public IEnumerator Hide(float duration = 0.6f)
    {
        if (overlayText != null) overlayText.text = "";
        ResetTextScale();
        yield return Fade(blackOverlay != null ? blackOverlay.alpha : 1f, 0f, duration);
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

    // ---------- Text scale helpers ----------

    private void PrepareTextScale()
    {
        if (!enableTextScale || overlayText == null) return;

        // Ensure pivot/anchor are centered for clean scale-in
        var rt = overlayText.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; // center

        overlayText.rectTransform.localScale = Vector3.one * Mathf.Max(0f, textScaleFrom);
    }

    private void StartTextScaleIn()
    {
        if (!enableTextScale || overlayText == null) return;

        if (textScaleCo != null) StopCoroutine(textScaleCo);
        textScaleCo = StartCoroutine(ScaleTextInCoroutine());
    }

    private IEnumerator ScaleTextInCoroutine()
    {
        float from = Mathf.Max(0f, textScaleFrom);
        float to = Mathf.Max(0f, textScaleTo);
        float dur = Mathf.Max(0.01f, textScaleDuration);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float eased = textScaleCurve.Evaluate(u);
            float s = Mathf.LerpUnclamped(from, to, eased);
            overlayText.rectTransform.localScale = Vector3.one * s;
            yield return null;
        }
        overlayText.rectTransform.localScale = Vector3.one * to;
        textScaleCo = null;
    }

    private void ResetTextScale()
    {
        if (overlayText == null) return;
        overlayText.rectTransform.localScale = Vector3.one * Mathf.Max(0f, textScaleFrom);
        if (textScaleCo != null)
        {
            StopCoroutine(textScaleCo);
            textScaleCo = null;
        }
    }
}
