using UnityEngine;

public class HellDoorTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Doll"))
        {
            Debug.Log("🔥 Doll entered the hell gate!");


            Destroy(other.gameObject); // Destroy the doll

            GameDirector.Instance.OnDollDestroyed();

            // Optional: play effects or sounds here
            // e.g., GetComponent<AudioSource>().Play();
        }
    }
}
