using System.Collections.Generic;
using UnityEngine;

// Đặt tên namespace để nhất quán với dự án của bạn
namespace Tanks.Complete
{
    public class OrbCollector : MonoBehaviour
    {
        // --- CÁC BIẾN VÀ THUỘC TÍNH ĐÃ ĐƯỢC CẬP NHẬT ---
        // Sử dụng Dictionary để lưu trữ các quả cầu đã nhặt, giới hạn 1 quả mỗi loại
        private Dictionary<PowerOrbController.ElementType, PowerOrbController> m_CollectedOrbs = new Dictionary<PowerOrbController.ElementType, PowerOrbController>();

        // Chuyển danh sách các quả cầu thành một List để dễ dàng sắp xếp quỹ đạo
        private List<PowerOrbController> m_OrbList = new List<PowerOrbController>();

        public bool IsCarryingAnyOrb => m_OrbList.Count > 0;
        public int OrbCount => m_OrbList.Count;

        // --- CÁC CÀI ĐẶT QUỸ ĐẠO (GIỮ NGUYÊN) ---
        [Header("Orbit Settings")]
        [Tooltip("Bán kính của vòng tròn quay.")]
        [SerializeField] private float m_OrbitRadius = 2.5f;
        [Tooltip("Tọa độ tùy chỉnh của tâm vòng tròn so với nhân vật.")]
        [SerializeField] private Vector3 m_OrbitCenterOffset = new Vector3(0, 1.5f, -2f);
        [Tooltip("Tốc độ quay của vòng tròn (độ/giây).")]
        [SerializeField] private float m_OrbitSpeed = 120f;

        private float m_CurrentAngle = 0f;

        // Update được gọi mỗi frame
        private void Update()
        {
            // Nếu đang mang ít nhất một quả cầu, thì thực hiện logic xoay
            if (IsCarryingAnyOrb)
            {
                m_CurrentAngle += m_OrbitSpeed * Time.deltaTime;
                ArrangeOrbsInOrbit();
            }
        }


        // --- HÀM MỚI: Logic quay tròn "Vầng hào quang" (ngang sau vai) ---
        private void ArrangeOrbsInOrbit()
        {
            int count = m_OrbList.Count;
            if (count == 0) return;

            // 1. Xác định tâm của vòng tròn quay (dựa trên offset bạn đã cài đặt)
            Vector3 orbitCenter = transform.position + transform.TransformDirection(m_OrbitCenterOffset);

            // 2. Tính toán góc giữa các quả cầu
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                // Góc hiện tại của quả cầu trên vòng tròn
                float angle = m_CurrentAngle + i * angleStep;

                // 3. Tính toán vị trí trên vòng tròn quay NGANG (trên mặt phẳng XY)
                // X sẽ là chiều ngang (trái/phải)
                // Y sẽ là chiều cao (lên/xuống)
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * m_OrbitRadius;
                float y = Mathf.Sin(angle * Mathf.Deg2Rad) * m_OrbitRadius;

                // Vector vị trí cục bộ của điểm trên vòng tròn
                Vector3 localPointPosition = new Vector3(x, y, 0);

                // 4. CHUYỂN VỊ TRÍ VÀO KHÔNG GIAN CỦA NHÂN VẬT
                // Nhân với transform.rotation để vòng tròn luôn nằm sau lưng nhân vật
                Vector3 worldPointPosition = orbitCenter + (transform.rotation * localPointPosition);

                // 5. Cập nhật vị trí mục tiêu cho quả cầu
                m_OrbList[i].SetTargetPosition(worldPointPosition);
            }
        }

        // --- HÀM THU THẬP QUẢ CẦU ĐÃ ĐƯỢC VIẾT LẠI HOÀN TOÀN ---
        private void OnTriggerEnter(Collider other)
        {
            // Cố gắng lấy component PowerOrbController từ đối tượng va chạm
            PowerOrbController orb = other.GetComponent<PowerOrbController>();

            // KIỂM TRA:
            // 1. Phải là một quả cầu (orb != null)
            // 2. Quả cầu đó chưa được mang bởi ai khác (!orb.IsCarried)
            // 3. Người chơi CHƯA mang một quả cầu cùng loại nguyên tố (!m_CollectedOrbs.ContainsKey(orb.elementType))
            if (orb != null && !orb.IsCarried && !m_CollectedOrbs.ContainsKey(orb.elementType))
            {
                // Thêm quả cầu vào Dictionary để theo dõi loại
                m_CollectedOrbs.Add(orb.elementType, orb);

                // Cập nhật lại List để sắp xếp quỹ đạo
                UpdateOrbList();

                // Ra lệnh cho quả cầu bắt đầu hiệu ứng "bị bắt" và bay theo
                orb.Capture(this.transform);
            }
        }

        // Hàm cập nhật lại List từ Dictionary
        private void UpdateOrbList()
        {
            m_OrbList.Clear();
            foreach (var orb in m_CollectedOrbs.Values)
            {
                m_OrbList.Add(orb);
            }
        }

        // --- CÁC HÀM CÔNG KHAI MỚI ĐỂ TANKSHOOTING SỬ DỤNG ---

        /// <summary>
        /// Trả về danh sách các loại nguyên tố mà người chơi đang mang.
        /// </summary>
        public List<PowerOrbController.ElementType> GetCollectedOrbTypes()
        {
            return new List<PowerOrbController.ElementType>(m_CollectedOrbs.Keys);
        }

        /// <summary>
        /// Tiêu thụ tất cả các quả cầu đang mang. Được gọi bởi TankShooting sau khi bắn.
        /// </summary>
        public void ConsumeOrbs()
        {
            if (IsCarryingAnyOrb)
            {
                // Duyệt qua tất cả các quả cầu trong list
                for (int i = m_OrbList.Count - 1; i >= 0; i--)
                {
                    // Ra lệnh cho mỗi quả cầu tự tiêu thụ (biến mất và tự hủy)
                    m_OrbList[i].Consume();
                }
                // Xóa sạch cả Dictionary và List
                m_CollectedOrbs.Clear();
                m_OrbList.Clear();
            }
        }
    }
}