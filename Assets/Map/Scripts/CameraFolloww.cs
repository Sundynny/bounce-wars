using UnityEngine;

public class CameraFolloww : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -6);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (!target) return;

        // Tính vị trí mới dựa trên hướng của nhân vật
        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Luôn nhìn vào nhân vật
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
