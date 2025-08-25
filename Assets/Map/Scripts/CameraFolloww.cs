using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;               // Nhân vật
    public Vector3 offset = new Vector3(0, 6, -6); // Độ cao và khoảng cách phía sau
    public float smoothSpeed = 10f;        // Độ mượt khi di chuyển camera
    public float rotationSmooth = 5f;      // Độ mượt khi xoay camera

    void LateUpdate()
    {
        if (!target) return;

        // Tính vị trí camera phía sau nhân vật, xoay theo hướng nhìn của nhân vật
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // Di chuyển mượt tới vị trí mong muốn
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Camera nhìn theo hướng target (ngang + một chút lên trên vai)
        Vector3 lookPoint = target.position + Vector3.up * 1.5f; // 1.5f = ngang vai
        Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmooth * Time.deltaTime);
    }
}
