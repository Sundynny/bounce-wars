using UnityEngine;

// Đặt tên namespace để nhất quán với dự án của bạn
namespace Tanks.Complete
{
    // Yêu cầu phải có Rigidbody và Sphere Collider trên GameObject này
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class FireParticle : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Lượng sát thương mà một hạt lửa gây ra khi va chạm.")]
        public float damage = 5f;
        [Tooltip("Thời gian (giây) hạt lửa tồn tại trước khi tự hủy nếu không va chạm.")]
        public float lifetime = 2.0f;
        [Tooltip("Hiệu ứng hạt sẽ được tạo ra khi hạt lửa va chạm.")]
        public ParticleSystem impactEffect;

        // Biến để lưu trữ ai là người bắn ra hạt lửa này
        private GameObject m_Owner;

        // Hàm này sẽ được gọi bởi FireZone.cs để gán chủ nhân
        public void SetOwner(GameObject owner)
        {
            m_Owner = owner;
        }

        // Start được gọi khi hạt lửa được tạo ra
        private void Start()
        {
            // Đặt lịch để tự hủy sau một khoảng thời gian
            Destroy(gameObject, lifetime);
        }

        // OnTriggerEnter được gọi khi collider (được đặt là Trigger) của hạt lửa va chạm với một collider khác
        private void OnTriggerEnter(Collider other)
        {
            // Cố gắng lấy component TankHealth từ đối tượng va chạm
            TankHealth targetHealth = other.GetComponent<TankHealth>();

            // KIỂM TRA:
            // 1. Đối tượng va chạm phải có component TankHealth (tức là một người chơi)
            // 2. Đối tượng đó không phải là người đã bắn ra hạt lửa này (tránh tự gây sát thương)
            if (targetHealth != null && other.gameObject != m_Owner)
            {
                // Gây sát thương cho mục tiêu.
                // Chúng ta dùng "isDirectHit = false" vì đây là sát thương hiệu ứng, không phải va chạm đạn trực tiếp.
                targetHealth.TakeDamage(damage, false);

                // Kích hoạt hiệu ứng va chạm nếu có
                if (impactEffect != null)
                {
                    // Tạo hiệu ứng tại điểm va chạm
                    ParticleSystem effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
                    // Tự hủy hiệu ứng sau khi nó đã phát xong
                    Destroy(effect.gameObject, effect.main.duration);
                }

                // Hủy hạt lửa ngay lập tức sau khi gây sát thương
                Destroy(gameObject);
            }
            // Nếu va chạm với một thứ không phải người chơi (ví dụ: đất, tường), hạt lửa cũng nên biến mất
            else if (other.gameObject != m_Owner)
            {
                // Kích hoạt hiệu ứng va chạm nếu có
                if (impactEffect != null)
                {
                    ParticleSystem effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
                    Destroy(effect.gameObject, effect.main.duration);
                }

                // Hủy hạt lửa
                Destroy(gameObject);
            }
        }
    }
}