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
                GameDirector.Instance.fearManager.ReduceFear(Time.deltaTime * 2f); // Lit: reduce fear
            }
            else if (roomController != null && !roomController.isLit)
            {
                GameDirector.Instance.fearManager.AddFear(Time.deltaTime * 2f);    // Dark: increase fear
            }
        }
    }
}
