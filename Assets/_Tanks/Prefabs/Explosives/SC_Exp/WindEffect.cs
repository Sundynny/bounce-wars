using UnityEngine;
using System.Collections.Generic; // Cần dùng để có List

namespace Tanks.Complete
{
    [RequireComponent(typeof(SphereCollider))]
    public class WindEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [Tooltip("Thời gian (giây) lồng gió tồn tại.")]
        public float duration = 5f;
        [Tooltip("Bán kính của lồng gió.")]
        public float radius = 7.5f;

        [Header("Initial Blast (Hất văng ban đầu)")]
        [Tooltip("Lực hất văng tức thời ngay khi lồng gió xuất hiện.")]
        public float initialBlastForce = 1500f;

        [Header("Sustained Push (Đẩy duy trì)")]
        [Tooltip("Lực đẩy duy trì tác động lên những ai ở trong lồng gió.")]
        public float sustainedPushForce = 20f;

        [Tooltip("Layer của các đối tượng sẽ bị ảnh hưởng bởi lồng gió.")]
        public LayerMask m_TankMask;

        // Biến để lưu trữ chủ nhân của hiệu ứng
        private GameObject m_Owner;
        private SphereCollider m_Collider;

        // Hàm được gọi bởi ShellExplosion
        public void SetOwner(GameObject owner)
        {
            m_Owner = owner;
        }

        private void Start()
        {
            // Tự hủy sau khi hết thời gian tồn tại
            Destroy(gameObject, duration);

            // Cài đặt Sphere Collider
            m_Collider = GetComponent<SphereCollider>();
            m_Collider.isTrigger = true;
            m_Collider.radius = radius;

            // --- LOGIC MỚI: Hất văng ban đầu ---
            ApplyInitialBlast();
        }

        // --- HÀM MỚI: Xử lý vụ nổ hất văng tức thời ---
        private void ApplyInitialBlast()
        {
            // Tìm tất cả các collider của người chơi trong bán kính
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius, m_TankMask);

            // Tạo một danh sách để tránh tác động lực hai lần lên cùng một đối tượng (nếu có nhiều collider)
            List<Rigidbody> affectedRigidbodies = new List<Rigidbody>();

            foreach (var col in colliders)
            {
                // Bỏ qua nếu là chính người bắn
                if (col.gameObject == m_Owner) continue;

                Rigidbody rb = col.GetComponentInParent<Rigidbody>();

                // Nếu tìm thấy Rigidbody và chưa xử lý nó...
                if (rb != null && !affectedRigidbodies.Contains(rb))
                {
                    // Tác động một lực nổ mạnh
                    rb.AddExplosionForce(initialBlastForce, transform.position, radius);
                    affectedRigidbodies.Add(rb);
                }
            }
        }

        // OnTriggerStay vẫn giữ vai trò đẩy duy trì
        private void OnTriggerStay(Collider other)
        {
            // Bỏ qua nếu là chính người bắn
            if (other.gameObject == m_Owner) return;

            Rigidbody targetRigidbody = other.GetComponent<Rigidbody>();

            if (targetRigidbody != null)
            {
                // Tính toán hướng đẩy từ tâm ra
                Vector3 direction = (other.transform.position - transform.position).normalized;

                // Tác động lực đẩy duy trì
                targetRigidbody.AddForce(direction * sustainedPushForce, ForceMode.Force);
            }
        }
    }
}