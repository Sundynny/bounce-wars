using UnityEngine;

// Đặt [ExecuteInEditMode] để Gizmo cập nhật ngay cả khi không ở chế độ Play
[ExecuteInEditMode]
public class DrawSpawnArea : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [Tooltip("Màu sắc của hình hộp biểu diễn.")]
    public Color color = new Color(1f, 0.92f, 0.016f, 0.25f); // Màu vàng trong suốt

    [Header("Spawner Data (Copy from OrbSpawner)")]
    [Tooltip("Copy giá trị Spawn Area từ OrbSpawner vào đây.")]
    public Rect spawnArea = new Rect(-50f, -50f, 100f, 100f);

    // --- THÊM MỚI: Biến để thể hiện độ cao ---
    [Tooltip("Độ cao mà Raycast bắt đầu bắn xuống (thường là 100f trong OrbSpawner).")]
    public float raycastOriginHeight = 100f;
    [Tooltip("Độ dài của tia Raycast (thường là 200f trong OrbSpawner).")]
    public float raycastDistance = 200f;

    // OnDrawGizmosSelected được gọi khi GameObject này được chọn trong Hierarchy
    void OnDrawGizmosSelected()
    {
        // Tính toán tâm của hình hộp
        Vector3 center = new Vector3(
            spawnArea.x + spawnArea.width / 2,
            raycastOriginHeight - (raycastDistance / 2), // Tâm của hình hộp sẽ nằm ở giữa chiều cao của tia Raycast
            spawnArea.y + spawnArea.height / 2
        );

        // Tính toán kích thước của hình hộp
        Vector3 size = new Vector3(
            spawnArea.width,
            raycastDistance, // Chiều cao của hình hộp giờ đây bằng độ dài của tia Raycast
            spawnArea.height
        );

        // Vẽ hình hộp trong suốt
        Gizmos.color = color;
        Gizmos.DrawCube(center, size);

        // Vẽ thêm một đường viền để dễ nhìn hơn
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}