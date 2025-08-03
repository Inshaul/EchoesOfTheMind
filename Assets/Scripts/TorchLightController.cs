using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class TorchLightController : MonoBehaviour
{
    public Light torchLight;
    public InputActionReference toggleAction;
    public AudioSource clickAudio;

    private XRGrabInteractable grabInteractable;
    private bool isHeld = false;
    private bool isOn = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);

        if (toggleAction != null)
        {
            toggleAction.action.performed += ctx => {
                if (isHeld) ToggleLight();
            };
        }

        torchLight.enabled = isOn;
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);

        if (toggleAction != null)
            toggleAction.action.performed -= ctx => { if (isHeld) ToggleLight(); };
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isHeld = true;
        torchLight.enabled = isOn;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
    }

    private void ToggleLight()
    {
        isOn = !isOn;
        torchLight.enabled = isOn;
        
        if (clickAudio != null)
            clickAudio.Play();
    }
}
