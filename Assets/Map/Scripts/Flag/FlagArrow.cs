using UnityEngine;
using UnityEngine.UI;

public class ArrowDirectionUI : MonoBehaviour
{
    public Transform player;          // Nhân vật
    public Transform targetPoint;     // Điểm cần chỉ
    public float hideDistance = 5f;   // Khoảng cách ẩn mũi tên
    public float rotationOffset = 0f; // Góc bù nếu mũi tên lệch

    private RectTransform rect;
    private Image img;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        img  = GetComponent<Image>();
    }

    void Update()
    {
        if (player == null || targetPoint == null) return;

        // Tính vector hướng trong 2D (X, Z)
        Vector3 dir = targetPoint.position - player.position;
        float distance = new Vector2(dir.x, dir.z).magnitude;

        // Ẩn nếu quá gần
        img.enabled = distance > hideDistance;

        if (!img.enabled) return;

        // Góc xoay 2D (chỉ dùng X, Z)
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // Xoay trên trục Z
        rect.localRotation = Quaternion.Euler(0f, 0f, -angle + rotationOffset);
    }
}
