using UnityEngine;

public class ThrownRock : MonoBehaviour
{
    private bool hasDamagedPlayer = false;

    void Start()
    {
        Destroy(gameObject, 6f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasDamagedPlayer) return; // prevent double hits in the same frame

        Transform hitObject = collision.collider.transform;
        //Debug.Log($"[RockHit] collided with {hitObject.GetComponent<Collider>().name}");

        if (hitObject.CompareTag("Player"))
        {
            PlayerController pc = hitObject.GetComponentInParent<PlayerController>();

            if (pc != null)
            {
                hasDamagedPlayer = true;
                pc.TakeDamage();
                Debug.Log("[Rock] Player hit by the rock.");
            }

            Destroy(gameObject);
            return;
        }

    }
}
