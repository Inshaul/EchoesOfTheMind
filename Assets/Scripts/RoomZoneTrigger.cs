using UnityEngine;

public class RoomZoneTrigger : MonoBehaviour
{
    public RoomLightController roomController;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PlayerReference"))
        {
            if (roomController != null && roomController.isLit)
            {
                GameDirector.Instance.fearManager.SetLit(true);
            }
            else if (roomController != null && !roomController.isLit)
            {
                GameDirector.Instance.fearManager.SetLit(false);
            }
        }
    }
}
