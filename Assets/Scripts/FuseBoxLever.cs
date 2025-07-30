using UnityEngine;

public class FuseBoxLever : MonoBehaviour
{
    public FuseBoxController fuseBoxController;
    public Transform leverHandle;
    public float onAngle = -30f;
    public float offAngle = 30f;
    private bool powerOn = false;

    void Start()
    {
        TogglePower();
        SetLeverAngle(offAngle);
        if (fuseBoxController != null)
            fuseBoxController.RestoreAllRooms(); 
    }

    void Update()
    {
        float angle = leverHandle.localEulerAngles.x;
        angle = (angle > 180) ? angle - 360 : angle; 

        if (!powerOn && angle < -25f)
        {
            TogglePower();
        }
        else if (powerOn && angle > 25f)
        {
            TogglePower();
        }
    }

    void TogglePower()
    {
        powerOn = !powerOn;
        if (fuseBoxController == null) return;
        if (powerOn) {
            fuseBoxController.RestoreAllRooms();
            fuseBoxController.FlickerRoom("GateHouse", true);//temp
        }
        else
            foreach (var room in fuseBoxController.roomControllers)
                room.TurnOff();
        SetLeverAngle(powerOn ? onAngle : offAngle);
    }

    private void SetLeverAngle(float angle)
    {
        if (leverHandle != null)
            leverHandle.localRotation = Quaternion.Euler(angle, 0, 0); 
    }
}
