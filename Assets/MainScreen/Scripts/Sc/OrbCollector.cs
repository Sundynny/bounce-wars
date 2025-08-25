using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Complete
{
    // Yêu cầu phải có AbilityManager trên cùng GameObject
    [RequireComponent(typeof(AbilityManager))]
    public class OrbCollector : MonoBehaviour
    {
        // --- THÊM MỚI: Tham chiếu đến AbilityManager ---
        private AbilityManager m_AbilityManager;

        // --- CÁC BIẾN CŨ GIỮ NGUYÊN ---
        private Dictionary<PowerOrbController.ElementType, PowerOrbController> m_CollectedOrbs = new Dictionary<PowerOrbController.ElementType, PowerOrbController>();
        private List<PowerOrbController> m_OrbList = new List<PowerOrbController>();
        public bool IsCarryingAnyOrb => m_OrbList.Count > 0;
        public int OrbCount => m_OrbList.Count;

        [Header("Orbit Settings")]
        [SerializeField] private float m_OrbitRadius = 2.5f;
        [SerializeField] private Vector3 m_OrbitCenterOffset = new Vector3(0, 1.5f, -2f);
        [SerializeField] private float m_OrbitSpeed = 120f;
        private float m_CurrentAngle = 0f;

        // --- THÊM MỚI: Lấy tham chiếu trong Awake ---
        private void Awake()
        {
            m_AbilityManager = GetComponent<AbilityManager>();
        }

        private void Update()
        {
            if (IsCarryingAnyOrb)
            {
                m_CurrentAngle += m_OrbitSpeed * Time.deltaTime;
                ArrangeOrbsInOrbit();
            }
        }

        private void ArrangeOrbsInOrbit()
        {
            // ... (Hàm này giữ nguyên hoàn toàn) ...
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

        private void OnTriggerEnter(Collider other)
        {
            PowerOrbController orb = other.GetComponent<PowerOrbController>();
            if (orb != null && !orb.IsCarried && !m_CollectedOrbs.ContainsKey(orb.elementType))
            {
                // --- DÒNG DEBUG MỚI ---
                // In ra Console tên của người chơi và loại quả cầu vừa nhặt
                Debug.Log(gameObject.name + " đã nhặt quả cầu: " + orb.elementType.ToString());
                m_CollectedOrbs.Add(orb.elementType, orb);
                UpdateOrbList();
                orb.Capture(this.transform);

                // --- THÊM MỚI: Thông báo cho AbilityManager ---
                // Sau khi nhặt thành công, báo cho AbilityManager cập nhật UI
                if (m_AbilityManager != null)
                {
                    m_AbilityManager.UpdateAvailableAbilitiesUI();
                }
            }
        }

        private void UpdateOrbList()
        {
            m_OrbList.Clear();
            foreach (var orb in m_CollectedOrbs.Values)
            {
                m_OrbList.Add(orb);
            }
        }

        public List<PowerOrbController.ElementType> GetCollectedOrbTypes()
        {
            return new List<PowerOrbController.ElementType>(m_CollectedOrbs.Keys);
        }

        // --- THAY ĐỔI: Hàm ConsumeOrbs() đã được thay thế bằng hàm mới ---
        /// <summary>
        /// Tiêu thụ MỘT quả cầu cụ thể theo loại nguyên tố.
        /// </summary>
        public void ConsumeOrb(PowerOrbController.ElementType typeToConsume)
        {
            // Kiểm tra xem có đang giữ quả cầu loại đó không
            if (m_CollectedOrbs.ContainsKey(typeToConsume))
            {
                // Lấy quả cầu cần tiêu thụ từ Dictionary
                PowerOrbController orbToConsume = m_CollectedOrbs[typeToConsume];

                // Ra lệnh cho nó tự hủy
                orbToConsume.Consume();

                // Xóa nó khỏi hệ thống quản lý
                m_CollectedOrbs.Remove(typeToConsume);
                UpdateOrbList();
            }
        }
    }
}