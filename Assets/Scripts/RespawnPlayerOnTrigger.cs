using UnityEngine;

public class RespawnPlayerOnTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerStats playerStats))
        {
            playerStats.Respawn();
        }
    }
}
