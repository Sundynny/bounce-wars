using UnityEngine;
using Tanks.Complete;


public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        TankHealth health = other.GetComponent<TankHealth>();
        if (health != null)
        {
            health.OnDeath();
        }
    }
}
