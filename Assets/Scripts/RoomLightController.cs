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

    public void TurnOn()
    {
        foreach (var light in lights)
            if (light != null) light.enabled = true;

        foreach (var ps in fireParticles)
            if (ps != null && !ps.isPlaying) ps.Play();
    }

    public void TurnOff()
    {
        foreach (var light in lights)
            if (light != null) light.enabled = false;

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
