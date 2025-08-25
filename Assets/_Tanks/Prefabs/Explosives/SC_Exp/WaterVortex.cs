using UnityEngine;

// Đặt tên namespace để nhất quán với dự án của bạn
namespace Tanks.Complete
{
    [RequireComponent(typeof(SphereCollider))]
    public class WaterVortex : MonoBehaviour
    {
        [Header("Vortex Settings")]
        [Tooltip("Thời gian (giây) vòi rồng tồn tại.")]
        public float duration = 6f;
        [Tooltip("Bán kính của vùng ảnh hưởng.")]
        public float radius = 6f;
        [Tooltip("Lực hút về tâm. Giá trị càng lớn, hút càng mạnh.")]
        public float pullForce = 50f;

        [Header("Damage Settings")]
        [Tooltip("Sát thương gây ra mỗi giây cho những ai bị kẹt trong vòi rồng.")]
        public float damagePerSecond = 5f;

        [Tooltip("Layer của các đối tượng sẽ bị ảnh hưởng.")]
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
            // Tự hủy sau khi hết thời gian
            Destroy(gameObject, duration);

            // Cài đặt Sphere Collider
            m_Collider = GetComponent<SphereCollider>();
            m_Collider.isTrigger = true;
            m_Collider.radius = radius;
        }

        // OnTriggerStay được gọi liên tục cho mọi đối tượng bên trong vùng ảnh hưởng
        private void OnTriggerStay(Collider other)
        {
            // Bỏ qua nếu là chính người bắn
            if (other.gameObject == m_Owner) return;

            // Kiểm tra xem đối tượng có thuộc layer bị ảnh hưởng không
            if ((m_TankMask.value & (1 << other.gameObject.layer)) == 0) return;

            Rigidbody targetRigidbody = other.GetComponent<Rigidbody>();

            // Nếu đối tượng có Rigidbody (có thể di chuyển)
            if (targetRigidbody != null)
            {
                // 1. TÍNH TOÁN LỰC HÚT
                //-------------------------
                // Tính toán vector hướng từ đối tượng VỀ PHÍA TÂM vòi rồng
                Vector3 direction = (transform.position - other.transform.position).normalized;

                // Tác động một lực liên tục để kéo đối tượng về tâm
                targetRigidbody.AddForce(direction * pullForce, ForceMode.Force);

                // 2. GÂY SÁT THƯƠNG THEO THỜI GIAN
                //-------------------------
                TankHealth targetHealth = other.GetComponent<TankHealth>();
                if (targetHealth != null)
                {
                    // Gây sát thương, nhân với Time.deltaTime để thành sát thương/giây
                    // isDirectHit = false vì đây là sát thương hiệu ứng
                    targetHealth.TakeDamage(damagePerSecond * Time.deltaTime, false);
                }
            }
        }
    }
}