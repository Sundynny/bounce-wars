using UnityEngine;

public class quaynhanvat : MonoBehaviour
{
    public float rotationSpeed = 20f; // Tốc độ quay (độ/giây)

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}
