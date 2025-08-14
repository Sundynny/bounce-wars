using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 6f, -8f);
    public float followSmoothTime = 0.15f; // nhỏ -> bám chặt hơn
    public bool lookAtTarget = true;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, followSmoothTime);

        if (lookAtTarget)
        {
            Vector3 lookPoint = target.position + Vector3.up * 1.5f; // nhìn hơi chếch lên
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position), 10f * Time.deltaTime);
        }
    }
}
