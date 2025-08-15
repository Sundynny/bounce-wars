using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TankFlagCarrier : NetworkBehaviour
{
    [Header("Gameplay Settings")]
    [Tooltip("Số lượng vật thể tối đa có thể mang cùng lúc.")]
    [SerializeField] private int m_MaxCarriedPoints = 5;

    [Header("Effects")]
    [Tooltip("Âm thanh phát ra khi thu thập một vật thể.")]
    [SerializeField] private AudioClip m_PickupSound;
    [Tooltip("Hiệu ứng hạt phát ra khi thu thập.")]
    [SerializeField] private ParticleSystem m_PickupParticlesPrefab;

    [Header("Custom Orbit Settings")]
    [Tooltip("Bán kính của vòng tròn quay.")]
    [SerializeField] private float m_OrbitRadius = 2.5f;
    [Tooltip("Tốc độ quay của vòng tròn (độ/giây).")]
    [SerializeField] private float m_OrbitSpeed = 120f;
    [Tooltip("Tọa độ tùy chỉnh của tâm vòng tròn so với xe tăng.")]
    [SerializeField] private Vector3 m_OrbitCenterOffset = new Vector3(0, 1.5f, -2f);

    // --- NETCODE STATE ---
    // NetworkList sẽ đồng bộ danh sách ID của các vật thể đang mang từ server đến tất cả client.
    private NetworkList<ulong> m_CarriedPointObjectIds;
    // Cache cục bộ trên mỗi client để hiển thị hiệu ứng quay mà không cần truy vấn mạng mỗi khung hình.
    private readonly List<Point_Controller> m_LocalCarriedPointsCache = new List<Point_Controller>();

    // Public properties
    public bool IsCarryingAnyPointObject => m_LocalCarriedPointsCache.Count > 0;
    public int CarriedCount => m_LocalCarriedPointsCache.Count;

    // Private state
    private float m_CurrentAngle = 0f;
    private AudioSource m_AudioSource;

    private void Awake()
    {
        // Khởi tạo NetworkList.
        m_CarriedPointObjectIds = new NetworkList<ulong>();
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
        {
            m_AudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnNetworkSpawn()
    {
        m_CarriedPointObjectIds.OnListChanged += OnCarriedPointsChanged;
        // Cập nhật cache lần đầu khi spawn.
        UpdateLocalCache();
    }

    public override void OnNetworkDespawn()
    {
        m_CarriedPointObjectIds.OnListChanged -= OnCarriedPointsChanged;
    }

    // Được gọi trên tất cả client mỗi khi NetworkList thay đổi trên server.
    private void OnCarriedPointsChanged(NetworkListEvent<ulong> changeEvent)
    {
        UpdateLocalCache();
    }

    // Đồng bộ cache cục bộ với NetworkList.
    private void UpdateLocalCache()
    {
        m_LocalCarriedPointsCache.Clear();
        foreach (var pointId in m_CarriedPointObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pointId, out var pointNetworkObject))
            {
                if (pointNetworkObject != null && pointNetworkObject.TryGetComponent(out Point_Controller pointController))
                {
                    m_LocalCarriedPointsCache.Add(pointController);
                }
            }
        }
    }

    private void Update()
    {
        // Logic quay quanh dựa trên cache cục bộ đã được đồng bộ hóa.
        if (IsCarryingAnyPointObject)
        {
            m_CurrentAngle += m_OrbitSpeed * Time.deltaTime;
            ArrangePointsInCustomOrbit();
        }
    }

    private void ArrangePointsInCustomOrbit()
    {
        int count = m_LocalCarriedPointsCache.Count;
        if (count == 0) return;
        Vector3 orbitCenter = transform.position + transform.TransformDirection(m_OrbitCenterOffset);
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = m_CurrentAngle + i * angleStep;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * m_OrbitRadius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * m_OrbitRadius;
            Vector3 worldPointPosition = orbitCenter + transform.rotation * new Vector3(x, y, 0);
            m_LocalCarriedPointsCache[i].SetTargetPosition(worldPointPosition);
        }
    }

    /// <summary>
    /// Hàm này giờ xử lý hai loại va chạm: nhặt vật phẩm và ghi điểm tại bàn thờ.
    /// Logic được đặt ở đây thay vì trên các vật phẩm để tuân thủ mô hình "người chơi chủ động".
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Chỉ chủ sở hữu của xe tăng (người chơi cục bộ) mới có quyền kích hoạt logic va chạm.
        // Điều này là cực kỳ quan trọng để ngăn chặn tất cả client khác xử lý sự kiện này một cách không cần thiết.
        if (!IsOwner) return;

        // Ưu tiên kiểm tra xem có phải là một Point_Controller để nhặt không.
        if (other.TryGetComponent(out Point_Controller pointObject))
        {
            // Kiểm tra sơ bộ trên client để tránh gửi yêu cầu không cần thiết lên server nếu vật phẩm đã được nhặt.
            if (!pointObject.IsCarried)
            {
                // Yêu cầu server xử lý việc nhặt. Đây là bước quan trọng để đảm bảo tính nhất quán.
                TryPickupPointServerRpc(pointObject.NetworkObjectId);
            }
        }
        // Nếu không phải là vật phẩm, hãy kiểm tra xem có phải là Bàn thờ để ghi điểm không.
        else if (other.TryGetComponent(out AltarController altar))
        {
            // Chỉ ghi điểm nếu chúng ta thực sự đang mang ít nhất một vật phẩm.
            if (IsCarryingAnyPointObject)
            {
                // Gọi hàm OnScore(). Hàm này đã được chúng ta chuyển đổi để gửi một ServerRpc, 
                // do đó luồng xử lý sẽ tự động được chuyển lên server một cách an toàn.
                OnScore();
            }
        }
    }

    [ServerRpc]
    private void TryPickupPointServerRpc(ulong pointNetworkObjectId)
    {
        // 1. Giới hạn số lượng vật thể mang theo
        if (m_CarriedPointObjectIds.Count >= m_MaxCarriedPoints)
        {
            return;
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pointNetworkObjectId, out var pointNetworkObject))
        {
            if (pointNetworkObject.TryGetComponent(out Point_Controller pointController))
            {
                if (!pointController.IsCarried)
                {
                    // Thêm ID vào NetworkList, điều này sẽ tự động đồng bộ.
                    m_CarriedPointObjectIds.Add(pointNetworkObjectId);
                    // 2. Ra lệnh cho vật phẩm "bị bắt" và truyền ID của xe tăng này cho nó.
                    pointController.CaptureAndFollow(NetworkObjectId);
                    // 3. Ra lệnh cho tất cả client phát hiệu ứng.
                    PlayPickupEffectClientRpc();
                }
            }
        }
    }

    [ClientRpc]
    private void PlayPickupEffectClientRpc()
    {
        if (m_PickupSound != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(m_PickupSound);
        }
        if (m_PickupParticlesPrefab != null)
        {
            Instantiate(m_PickupParticlesPrefab, transform.position, Quaternion.identity);
        }
    }

    // 4. Hàm này sẽ được gọi từ script TankHealth khi xe tăng chết.
    public void DropAllPointsOnDeath()
    {
        // Chỉ server mới có quyền xử lý việc này.
        if (!IsServer) return;

        // Reset trạng thái của từng vật phẩm.
        foreach (var pointId in m_CarriedPointObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pointId, out var pointNetworkObject))
            {
                if (pointNetworkObject.TryGetComponent(out Point_Controller pointController))
                {
                    pointController.ResetState();
                }
            }
        }
        // Xóa tất cả các vật phẩm khỏi danh sách.
        m_CarriedPointObjectIds.Clear();
    }

    // Xử lý ghi điểm
    public void OnScore()
    {
        if (IsOwner)
        {
            ScorePointsServerRpc();
        }
    }




    [ServerRpc]
    private void ScorePointsServerRpc()
    {
        if (m_CarriedPointObjectIds.Count > 0)
        {
            // --- THAY ĐỔI Ở ĐÂY ---
            // Thay thế dòng // TODO cũ bằng lời gọi đến GameManager.
            // Giả sử xe tăng này thuộc đội 1. Bạn sẽ cần một hệ thống để xác định đội sau này.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(1, CarriedCount);
            }
            // --- KẾT THÚC THAY ĐỔI ---

            // Logic reset vật phẩm giống như khi chết.
            DropAllPointsOnDeath();
        }
    }
}