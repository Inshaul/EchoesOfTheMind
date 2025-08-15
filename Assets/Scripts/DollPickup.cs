using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class DollPickup : MonoBehaviour
{
    public bool pickedUpDoll = false;
    private bool hasGrabbedBefore = false;
    public InputActionReference toggleLeftAction;
    public InputActionReference toggleRightAction;

    public AudioSource audioSource;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void Start()
    {
        audioSource.Play();
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        hasGrabbedBefore = false;
        pickedUpDoll = false;
    }
    private void OnGrab(SelectEnterEventArgs args)
    {
        pickedUpDoll = true;
        if (!hasGrabbedBefore)
        {
            hasGrabbedBefore = true;
            GameDirector.Instance.OnDollGrabbed();
        }
        audioSource.Stop();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        audioSource.Play();
    }
}
