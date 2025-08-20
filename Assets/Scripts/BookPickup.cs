using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class BookPickup : MonoBehaviour
{
    public bool pickedUpBook = false;
    private bool hasGrabbedBefore = false;
    public InputActionReference toggleLeftAction;
    public InputActionReference toggleRightAction;

    public GameObject bookUI;

    public AudioSource audioSource;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        bookUI.SetActive(false);
    }

    void Start()
    {
        //audioSource.Play();
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }
    private void OnGrab(SelectEnterEventArgs args)
    {
        pickedUpBook = true;
        if (!hasGrabbedBefore)
        {
            hasGrabbedBefore = true;
        }
        audioSource.Play();
        bookUI.SetActive(true);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        audioSource.Stop();
        bookUI.SetActive(false);
    }
}
