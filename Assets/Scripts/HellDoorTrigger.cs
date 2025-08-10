using UnityEngine;

public class HellDoorTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Doll"))
        {
            Debug.Log("🔥 Doll entered the hell gate!");

            Destroy(other.gameObject); // Destroy the doll

            // Tell the DollManager to spawn the next one
            DollManager manager = FindFirstObjectByType<DollManager>();

            if (manager != null)
                manager.OnDollDestroyedOrUsed();

            GameDirector.Instance.OnDollDestroyed();

            // Optional: play effects or sounds here
            // e.g., GetComponent<AudioSource>().Play();
        }
    }
}
