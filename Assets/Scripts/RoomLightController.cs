using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomLightController : MonoBehaviour
{
    [Header("Room Identity")]
    public string roomName;

    [Header("Lighting References")]
    public List<Light> lights;

    [Header("Effect References (Optional)")]
    public List<ParticleSystem> fireParticles;

    //private bool isFlickering = false;

    public bool isLit = true;

    public void TurnOn()
    {
        foreach (var light in lights)
            if (light != null) light.enabled = true;

        isLit = true;

        // foreach (var ps in fireParticles)
        //     if (ps != null && !ps.isPlaying) ps.Play();
    }

    public void TurnOff()
    {
        foreach (var light in lights)
            if (light != null) light.enabled = false;

        isLit = false;

        foreach (var ps in fireParticles)
            if (ps != null && ps.isPlaying) ps.Stop();
    }

    public void SetLightColor(Color color)
    {
        foreach (var light in lights)
            if (light != null) light.color = color;
    }

    public void ActivateFire()
    {
        foreach (var ps in fireParticles)
            if (ps != null && !ps.isPlaying) ps.Play();
    }

    public void DeactivateFire()
    {
        foreach (var ps in fireParticles)
            if (ps != null && ps.isPlaying) ps.Stop();
    }

    private Coroutine flickerCoroutine;

    private Coroutine smoothFlickerCoroutine;

    public void StartSmoothFlicker(float duration, float minIntensity = 0.1f, float maxIntensity = 1.5f, float speed = 10f, bool loop = false)
    {
        if (smoothFlickerCoroutine != null)
            StopCoroutine(smoothFlickerCoroutine);
        smoothFlickerCoroutine = StartCoroutine(SmoothFlickerRoutine(duration, minIntensity, maxIntensity, speed, loop));
    }

    private IEnumerator SmoothFlickerRoutine(float duration, float minIntensity, float maxIntensity, float speed, bool loop)
    {
        var originalIntensities = new Dictionary<Light, float>();
        foreach (var light in lights)
            if (light != null)
                originalIntensities[light] = light.intensity;

        float timer = 0f;
        while (loop || timer < duration)
        {
            foreach (var light in lights)
            {
                if (light != null)
                {
                    float target = Random.Range(minIntensity, maxIntensity);
                    light.intensity = Mathf.Lerp(light.intensity, target, Time.deltaTime * speed);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
        // Restore intensities after non-loop flicker
        foreach (var pair in originalIntensities)
            if (pair.Key != null) pair.Key.intensity = pair.Value;
        smoothFlickerCoroutine = null;
    }



    public void StartFlicker(float duration, float interval, bool loop = false)
    {
        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);
        flickerCoroutine = StartCoroutine(Flicker(duration, interval, loop));
    }

    public void StopFlicker()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        foreach (var light in lights)
            if (light != null) light.enabled = true;
    }

    private IEnumerator Flicker(float duration, float interval, bool loop)
    {
        float timer = 0f;
        bool on = false;

        if (loop)
        {
            while (true)
            {
                on = !on;
                foreach (var light in lights)
                    if (light != null) light.enabled = on;
                yield return new WaitForSeconds(interval);
            }
        }
        else
        {
            while (timer < duration)
            {
                on = !on;
                foreach (var light in lights)
                    if (light != null) light.enabled = on;
                timer += interval;
                yield return new WaitForSeconds(interval);
            }
            foreach (var light in lights)
                if (light != null) light.enabled = true;
        }
        flickerCoroutine = null;
    }
}
