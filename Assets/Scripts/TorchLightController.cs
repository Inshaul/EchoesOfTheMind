using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class TorchLightController : MonoBehaviour
{
    public Light torchLight;
    public InputActionReference toggleLeftAction;
    public InputActionReference toggleRightAction;
    public AudioSource clickAudio;

    public bool pickedUpTorch = false;
    private bool hasGrabbedBefore = false;

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private bool isOn = false;
    private InputAction activeToggleAction = null;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        isOn = true;
        torchLight.enabled = isOn;
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        UnsubscribeToggle();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        pickedUpTorch = true;
        if (!hasGrabbedBefore)
        {
            hasGrabbedBefore = true;
            GameDirector.Instance.OnFirstTorchGrabbed();
        }
        isHeld = true;
        torchLight.enabled = isOn;
        string interactorName = args.interactorObject.transform.name.ToLower();

        UnsubscribeToggle();

        if (interactorName.Contains("left"))
            SubscribeToggle(toggleLeftAction);
        else if (interactorName.Contains("right"))
            SubscribeToggle(toggleRightAction);
        else
            Debug.LogWarning("Torch grabbed, but couldn't determine hand!");
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        UnsubscribeToggle();
    }

    private void SubscribeToggle(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
        {
            activeToggleAction = actionRef.action;
            activeToggleAction.performed += OnToggle;
        }
    }

    private void UnsubscribeToggle()
    {
        if (activeToggleAction != null)
        {
            activeToggleAction.performed -= OnToggle;
            activeToggleAction = null;
        }
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        if (isHeld)
            ToggleLight();
    }

    private void ToggleLight()
    {
        isOn = !isOn;
        torchLight.enabled = isOn;
        if (clickAudio != null)
            clickAudio.Play();
    }
}
