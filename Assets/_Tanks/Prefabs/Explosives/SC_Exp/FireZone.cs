using UnityEngine;

// Đặt tên namespace để nhất quán với dự án của bạn
namespace Tanks.Complete
{
    public class FireZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [Tooltip("Prefab của hạt lửa sẽ được bắn ra.")]
        public GameObject fireParticlePrefab;
        [Tooltip("Thời gian (giây) vùng lửa tồn tại.")]
        public float duration = 10f;

        [Header("Spawning Settings")]
        [Tooltip("Số lượng hạt lửa được bắn ra mỗi giây.")]
        public float particlesPerSecond = 5f;
        [Tooltip("Bán kính của khu vực mà các hạt lửa sẽ được bắn ra.")]
        public float spawnRadius = 3f;
        [Tooltip("Lực bắn ban đầu của các hạt lửa.")]
        public float launchForce = 5f;

        // Biến để theo dõi thời gian giữa các lần bắn
        private float timeBetweenSpawns;
        private float spawnTimer;

        // Biến để lưu trữ chủ nhân của vùng lửa (để truyền cho các hạt lửa)
        private GameObject m_Owner;

        // Hàm này sẽ được gọi bởi ShellExplosion nếu cần
        public void SetOwner(GameObject owner)
        {
            m_Owner = owner;
        }

        private void Start()
        {
            // Tự hủy vùng lửa sau khi hết thời gian tồn tại
            Destroy(gameObject, duration);

            // Kiểm tra xem đã gán prefab hạt lửa chưa
            if (fireParticlePrefab == null)
            {
                Debug.LogError("Fire Particle Prefab chưa được gán trên FireZone!");
                // Vô hiệu hóa script này nếu thiếu prefab
                this.enabled = false;
                return;
            }

            // Tính toán thời gian cần chờ giữa mỗi lần tạo hạt lửa
            timeBetweenSpawns = 1f / particlesPerSecond;
            // Khởi tạo timer để bắn hạt đầu tiên ngay lập tức
            spawnTimer = timeBetweenSpawns;
        }

        private void Update()
        {
            // Tăng bộ đếm thời gian
            spawnTimer += Time.deltaTime;

            // Nếu đã đến lúc bắn một hạt lửa mới
            if (spawnTimer >= timeBetweenSpawns)
            {
                // Reset bộ đếm
                spawnTimer = 0f;

                // Gọi hàm để tạo và bắn một hạt lửa
                SpawnAndLaunchParticle();
            }
        }

        private void SpawnAndLaunchParticle()
        {
            // Tạo một vị trí ngẫu nhiên trên một vòng tròn nằm ngang
            Vector2 randomPointOnCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomPointOnCircle.x, 0, randomPointOnCircle.y);

            // Tạo một thực thể của prefab hạt lửa tại vị trí ngẫu nhiên đó
            GameObject particleGO = Instantiate(fireParticlePrefab, spawnPosition, Quaternion.identity);

            // Lấy component Rigidbody và script FireParticle từ hạt lửa vừa tạo
            Rigidbody rb = particleGO.GetComponent<Rigidbody>();
            FireParticle fireParticle = particleGO.GetComponent<FireParticle>();

            // Gán chủ nhân cho hạt lửa để nó không tự gây sát thương
            if (fireParticle != null)
            {
                fireParticle.SetOwner(m_Owner);
            }

            // Tác động một lực ngẫu nhiên để bắn hạt lửa bay ra
            if (rb != null)
            {
                // Hướng bắn có thể là ngẫu nhiên hướng lên trên
                Vector3 randomDirection = (Vector3.up + Random.insideUnitSphere * 0.5f).normalized;
                rb.AddForce(randomDirection * launchForce, ForceMode.Impulse);
            }
        }
    }
}