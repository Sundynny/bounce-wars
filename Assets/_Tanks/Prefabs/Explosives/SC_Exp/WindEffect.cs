using UnityEngine;

// Đặt tên namespace để nhất quán với dự án của bạn
namespace Tanks.Complete
{
    // Yêu cầu phải có Sphere Collider trên GameObject này
    [RequireComponent(typeof(SphereCollider))]
    public class WindEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [Tooltip("Thời gian (giây) lồng gió tồn tại.")]
        public float duration = 5f;
        [Tooltip("Lực đẩy tác động lên người chơi mỗi giây.")]
        public float pushForce = 20f;
        [Tooltip("Bán kính của lồng gió. Nên khớp với kích thước của hiệu ứng hạt.")]
        public float radius = 7.5f; // Bằng 5 (bán kính gốc) * 1.5 (cường hóa của Gió)

        // Biến để lưu trữ chủ nhân của hiệu ứng
        private GameObject m_Owner;

        // Tham chiếu đến Sphere Collider
        private SphereCollider m_Collider;

        // Hàm này sẽ được gọi bởi ShellExplosion nếu cần
        public void SetOwner(GameObject owner)
        {
            m_Owner = owner;
        }

        private void Start()
        {
            // Tự hủy sau khi hết thời gian tồn tại
            Destroy(gameObject, duration);

            // Lấy và cài đặt Sphere Collider
            m_Collider = GetComponent<SphereCollider>();
            // Đảm bảo nó là một trigger để không cản đường vật lý
            m_Collider.isTrigger = true;
            // Đặt bán kính cho collider
            m_Collider.radius = radius;
        }

        // OnTriggerStay được gọi mỗi frame cho mọi collider đang ở bên trong trigger
        private void OnTriggerStay(Collider other)
        {
            // Cố gắng lấy component Rigidbody từ đối tượng bên trong
            Rigidbody targetRigidbody = other.GetComponent<Rigidbody>();

            // KIỂM TRA:
            // 1. Phải có Rigidbody để có thể tác động lực
            // 2. Không tác động lực lên chính người đã tạo ra hiệu ứng
            if (targetRigidbody != null && other.gameObject != m_Owner)
            {
                // Tính toán vector hướng từ tâm lồng gió ra phía đối tượng
                Vector3 direction = (other.transform.position - transform.position).normalized;

                // Tác động một lực liên tục (ForceMode.Force) lên đối tượng theo hướng đó
                // Nhân với Time.deltaTime để lực không phụ thuộc vào framerate
                targetRigidbody.AddForce(direction * pushForce * Time.deltaTime, ForceMode.VelocityChange);
            }
        }
    }
}