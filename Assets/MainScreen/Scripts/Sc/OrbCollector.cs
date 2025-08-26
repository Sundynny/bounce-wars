using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Complete
{
    // Yêu cầu phải có cả AbilityManager và PowerUpDetector trên cùng GameObject.
    [RequireComponent(typeof(AbilityManager))]
    [RequireComponent(typeof(PowerUpDetector))]
    public class OrbCollector : MonoBehaviour
    {
        // Tham chiếu đến các component quan trọng khác trên cùng nhân vật.
        private AbilityManager m_AbilityManager;
        private PowerUpDetector m_PowerUpDetector;

        // Dictionary để quản lý các quả cầu đã thu thập.
        private Dictionary<PowerOrbController.ElementType, PowerOrbController> m_CollectedOrbs = new Dictionary<PowerOrbController.ElementType, PowerOrbController>();

        // List để dễ dàng sắp xếp quỹ đạo bay.
        private List<PowerOrbController> m_OrbList = new List<PowerOrbController>();

        // Các thuộc tính chỉ đọc.
        public bool IsCarryingAnyOrb => m_OrbList.Count > 0;
        public int OrbCount => m_OrbList.Count;

        // Cài đặt cho quỹ đạo bay.
        [Header("Orbit Settings")]
        [SerializeField] private float m_OrbitRadius = 2.5f;
        [SerializeField] private Vector3 m_OrbitCenterOffset = new Vector3(0, 1.5f, -2f);
        [SerializeField] private float m_OrbitSpeed = 120f;
        private float m_CurrentAngle = 0f;

        // Awake được gọi khi script được tải.
        private void Awake()
        {
            // Lấy các tham chiếu cần thiết.
            m_AbilityManager = GetComponent<AbilityManager>();
            m_PowerUpDetector = GetComponent<PowerUpDetector>();
        }

        // Update được gọi mỗi frame.
        private void Update()
        {
            if (IsCarryingAnyOrb)
            {
                m_CurrentAngle += m_OrbitSpeed * Time.deltaTime;
                ArrangeOrbsInOrbit();
            }
        }

        // Hàm sắp xếp quỹ đạo bay (giữ nguyên).
        private void ArrangeOrbsInOrbit()
        {
            int count = m_OrbList.Count;
            if (count == 0) return;
            Vector3 orbitCenter = transform.position + transform.TransformDirection(m_OrbitCenterOffset);
            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = m_CurrentAngle + i * angleStep;
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * m_OrbitRadius;
                float y = Mathf.Sin(angle * Mathf.Deg2Rad) * m_OrbitRadius;
                Vector3 localPointPosition = new Vector3(x, y, 0);
                Vector3 worldPointPosition = orbitCenter + (transform.rotation * localPointPosition);
                m_OrbList[i].SetTargetPosition(worldPointPosition);
            }
        }

        // --- HÀM OnTriggerEnter ĐÃ ĐƯỢC ĐẠI TU ĐỂ THỰC HIỆN LOGIC TUẦN TỰ ---
        private void OnTriggerEnter(Collider other)
        {
            // Cố gắng lấy component PowerOrbController từ đối tượng va chạm.
            PowerOrbController orb = other.GetComponent<PowerOrbController>();

            // KIỂM TRA ĐIỀU KIỆN NHẶT:
            if (orb != null && !orb.IsCarried && !m_CollectedOrbs.ContainsKey(orb.elementType))
            {
                // --- THỰC HIỆN CÁC BƯỚC THEO ĐÚNG THỨ TỰ ---

                // BƯỚC 1: ÁP DỤNG BUFF TỨC THÌ
                // Ra lệnh cho quả cầu tự áp dụng buff lên chính người chơi này.
                orb.ApplyInstantBuff(m_PowerUpDetector);

                // BƯỚC 2: THU THẬP QUẢ CẦU
                // Thêm quả cầu vào hệ thống quản lý.
                m_CollectedOrbs.Add(orb.elementType, orb);
                UpdateOrbList();

                // Ra lệnh cho quả cầu bắt đầu hiệu ứng bay theo sau.
                orb.Capture(this.transform);

                // BƯỚC 3: CẬP NHẬT UI
                // Thông báo cho AbilityManager để làm sáng icon kỹ năng tương ứng.
                if (m_AbilityManager != null)
                {
                    m_AbilityManager.UpdateAvailableAbilitiesUI();
                }
            }
        }

        // Hàm đồng bộ hóa List từ Dictionary.
        private void UpdateOrbList()
        {
            m_OrbList.Clear();
            foreach (var orb in m_CollectedOrbs.Values)
            {
                m_OrbList.Add(orb);
            }
        }

        // Hàm công khai để các script khác có thể lấy thông tin.
        public List<PowerOrbController.ElementType> GetCollectedOrbTypes()
        {
            return new List<PowerOrbController.ElementType>(m_CollectedOrbs.Keys);
        }

        // Hàm công khai để AbilityManager ra lệnh tiêu thụ một quả cầu cụ thể.
        public void ConsumeOrb(PowerOrbController.ElementType typeToConsume)
        {
            if (m_CollectedOrbs.ContainsKey(typeToConsume))
            {
                PowerOrbController orbToConsume = m_CollectedOrbs[typeToConsume];
                orbToConsume.Consume();
                m_CollectedOrbs.Remove(typeToConsume);
                UpdateOrbList();
            }
        }
    }
}