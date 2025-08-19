using System.Collections.Generic;
using UnityEngine;

public class TankFlagCarrier : MonoBehaviour
{
    private List<Point_Controller> m_CarriedPoints = new List<Point_Controller>();
    public bool IsCarryingAnyPointObject => m_CarriedPoints.Count > 0;
    public int CarriedCount => m_CarriedPoints.Count;

    [Header("Custom Orbit Settings")]
    [Tooltip("Bán kính của vòng tròn quay.")]
    [SerializeField] private float m_OrbitRadius = 2.5f; // Giảm bán kính một chút
    [Tooltip("Vòng tròn sẽ ở sau xe tăng bao xa.")]
    [SerializeField] private float m_OrbitDistanceBehind = 2f; // Giảm khoảng cách lại gần
    [Tooltip("Tốc độ quay của vòng tròn (độ/giây).")]
    [SerializeField] private float m_OrbitSpeed = 120f; // Tăng tốc độ quay cho đẹp hơn

    // --- BIẾN MỚI ĐỂ CHỈNH TÂM ---
    [Tooltip("Tọa độ tùy chỉnh của tâm vòng tròn so với xe tăng. X: Trái/Phải, Y: Lên/Xuống, Z: Trước/Sau.")]
    [SerializeField] private Vector3 m_OrbitCenterOffset = new Vector3(0, 1.5f, -2f);

    private float m_CurrentAngle = 0f;

    private void Update()
    {
        if (IsCarryingAnyPointObject)
        {
            m_CurrentAngle += m_OrbitSpeed * Time.deltaTime;
            ArrangePointsInCustomOrbit();
        }
    }

    // --- HÀM ĐÃ ĐƯỢC THAY ĐỔI LOGIC QUAY ---
    private void ArrangePointsInCustomOrbit()
    {
        int count = m_CarriedPoints.Count;
        if (count == 0) return;

        // --- CÔNG THỨC TÍNH TÂM ĐÃ ĐƯỢC CẬP NHẬT ---
        // 1. Chuyển đổi Offset cục bộ thành tọa độ thế giới
        // Điều này đảm bảo Offset (ví dụ Z=-2) sẽ luôn ở phía sau xe tăng dù xe tăng quay hướng nào.
        Vector3 worldOffset = transform.TransformDirection(m_OrbitCenterOffset);

        // 2. Xác định tâm của vòng tròn quay
        Vector3 orbitCenter = transform.position + worldOffset;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = m_CurrentAngle + i * angleStep;

            // 2. TÍNH TOÁN TRÊN MẶT PHẲNG X-Y (xoay ngang 90 độ so với dọc)
            // Trục X sẽ là chiều ngang (trái/phải)
            // Trục Y sẽ là chiều cao (lên/xuống)
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * m_OrbitRadius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * m_OrbitRadius;

            // Vector vị trí cục bộ của điểm trên vòng tròn
            Vector3 localPointPosition = new Vector3(x, y, 0);

            // 3. Chuyển vị trí cục bộ đó vào không gian 3D của thế giới, căn theo xe tăng
            Vector3 worldPointPosition = orbitCenter + transform.rotation * localPointPosition;

            m_CarriedPoints[i].SetTargetPosition(worldPointPosition);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Point_Controller pointObject = other.GetComponent<Point_Controller>();

        if (pointObject != null && !pointObject.IsCarried)
        {
            m_CarriedPoints.Add(pointObject);
            pointObject.transform.SetParent(this.transform);
            pointObject.Capture();
        }
    }

    public void OnScore()
    {
        if (IsCarryingAnyPointObject)
        {
            for (int i = m_CarriedPoints.Count - 1; i >= 0; i--)
            {
                var point = m_CarriedPoints[i];
                point.transform.SetParent(null);
                point.ResetState();
            }
            m_CarriedPoints.Clear();
        }
    }

    public int GetCarriedCount()
    {
        return m_CarriedPoints.Count;
    }
}