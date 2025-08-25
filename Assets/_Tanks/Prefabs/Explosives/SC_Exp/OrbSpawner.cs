using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tanks.Complete
{
    public class OrbSpawner : MonoBehaviour
    {
        // ... (Tất cả các biến và các hàm khác giữ nguyên) ...
        [Header("Orb Prefabs")]
        public List<GameObject> orbPrefabs;
        [Header("Spawning Area")]
        public Rect spawnArea = new Rect(-50f, -50f, 100f, 100f);
        public LayerMask groundLayer;
        public float minDistanceBetweenOrbs = 5f;
        [Header("Spawning Settings")]
        public float respawnTime = 30f;
        public int maxConcurrentOrbs = 5;

        private List<GameObject> activeOrbs = new List<GameObject>();
        private float respawnTimer = 0f;

        void Start()
        {
            if (orbPrefabs == null || orbPrefabs.Count == 0)
            {
                Debug.LogError("Chưa gán Orb Prefabs vào OrbSpawner!");
                return;
            }
        }

        void Update()
        {
            activeOrbs.RemoveAll(item => item == null);

            if (activeOrbs.Count < maxConcurrentOrbs)
            {
                respawnTimer += Time.deltaTime;
                float currentRespawnThreshold = activeOrbs.Count == 0 ? 0.1f : respawnTime;

                if (respawnTimer >= currentRespawnThreshold)
                {
                    respawnTimer = 0f;
                    TrySpawnRandomOrb();
                }
            }
        }

        // --- HÀM TrySpawnRandomOrb ĐÃ ĐƯỢC THAY ĐỔI ---
        void TrySpawnRandomOrb()
        {
            int maxAttempts = 20;
            for (int i = 0; i < maxAttempts; i++)
            {
                float randomX = Random.Range(spawnArea.x, spawnArea.x + spawnArea.width);
                float randomZ = Random.Range(spawnArea.y, spawnArea.y + spawnArea.height);
                Vector3 randomPoint = new Vector3(randomX, 100f, randomZ);

                RaycastHit hit;
                if (Physics.Raycast(randomPoint, Vector3.down, out hit, 200f, groundLayer))
                {
                    // --- THAY ĐỔI DUY NHẤT LÀ Ở ĐÂY ---
                    // Lấy vị trí va chạm và cộng thêm 0.5f vào chiều cao (trục Y)
                    Vector3 spawnPosition = hit.point + Vector3.up * 0.5f;

                    if (IsPositionValid(spawnPosition))
                    {
                        GameObject randomOrbPrefab = orbPrefabs[Random.Range(0, orbPrefabs.Count)];

                        // Tạo quả cầu tại vị trí đã được nâng lên
                        GameObject newOrb = Instantiate(randomOrbPrefab, spawnPosition, Quaternion.identity);
                        activeOrbs.Add(newOrb);
                        return;
                    }
                }
            }
            Debug.LogWarning("Không thể tìm thấy vị trí hợp lệ để tạo quả cầu sau " + maxAttempts + " lần thử.");
        }

        bool IsPositionValid(Vector3 position)
        {
            foreach (var orb in activeOrbs)
            {
                if (orb != null && Vector3.Distance(orb.transform.position, position) < minDistanceBetweenOrbs)
                {
                    return false;
                }
            }
            return true;
        }
    }
}