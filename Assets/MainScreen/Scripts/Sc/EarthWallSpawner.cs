using UnityEngine;
using System.Collections.Generic; // Cần dùng để có List

namespace Tanks.Complete
{
    public class EarthWallSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Prefab của hòn đá sẽ được triệu hồi.")]
        public GameObject fallingRockPrefab;
        [Tooltip("Tổng thời gian (giây) tường đá tồn tại trước khi tất cả các hòn đá biến mất.")]
        public float wallDuration = 30f;

        [Header("Spawning Settings")]
        [Tooltip("Số lượng hòn đá sẽ được tạo ra.")]
        public int numberOfRocks = 8;
        [Tooltip("Bán kính của khu vực mà các hòn đá sẽ rơi xuống.")]
        public float spawnRadius = 4f;
        [Tooltip("Độ cao ban đầu mà các hòn đá sẽ xuất hiện.")]
        public float spawnHeight = 10f;

        // --- THÊM MỚI: Một danh sách để lưu trữ tất cả các hòn đá đã tạo ---
        private List<GameObject> spawnedRocks = new List<GameObject>();

        void Start()
        {
            if (fallingRockPrefab == null)
            {
                Debug.LogError("Falling Rock Prefab chưa được gán trên EarthWallSpawner!");
                Destroy(gameObject);
                return;
            }

            // Tạo ra các hòn đá
            SpawnRocks();

            // --- THAY ĐỔI: Đặt lịch để hủy toàn bộ bức tường ---
            // Gọi hàm DestroyWall sau 'wallDuration' giây
            Invoke(nameof(DestroyWall), wallDuration);
        }

        void SpawnRocks()
        {
            for (int i = 0; i < numberOfRocks; i++)
            {
                Vector2 randomPointOnCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomPointOnCircle.x, spawnHeight, randomPointOnCircle.y);
                spawnPosition += Random.insideUnitSphere * 0.5f;

                GameObject newRock = Instantiate(fallingRockPrefab, spawnPosition, Random.rotation);

                // --- THÊM MỚI: Thêm hòn đá vừa tạo vào danh sách quản lý ---
                spawnedRocks.Add(newRock);
            }
        }

        // --- HÀM MỚI: Hủy tất cả các hòn đá đã tạo ---
        void DestroyWall()
        {
            // Duyệt qua danh sách và hủy từng hòn đá
            foreach (var rock in spawnedRocks)
            {
                // Thêm một kiểm tra để đảm bảo hòn đá chưa bị phá hủy bởi một lý do nào khác
                if (rock != null)
                {
                    // (Tùy chọn) Thêm hiệu ứng tan biến ở đây trước khi hủy
                    // ví dụ: rock.GetComponent<Animator>().SetTrigger("Vanish");
                    Destroy(rock);
                }
            }

            // Cuối cùng, tự hủy chính cái Spawner này
            Destroy(gameObject);
        }
    }
}