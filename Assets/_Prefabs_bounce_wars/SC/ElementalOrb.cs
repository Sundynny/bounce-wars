using Tanks.Complete;
using UnityEngine;
using Unity.Netcode; // --- THAY ĐỔI NETCODE ---

// --- THAY ĐỔI NETCODE ---
public class ElementalOrb : NetworkBehaviour
{
    // --- CÁC BIẾN VÀ COMMENT GỐC CỦA BẠN ĐƯỢC GIỮ NGUYÊN ---
    public enum ElementType
    {
        Fire,   // Lửa: Tăng sát thương
        Water,  // Nước: Hồi máu
        Wind,   // Gió: Tăng tốc độ
        Earth   // Đất: Tăng phòng thủ (khiên)
    }

    public ElementType elementType;
    public float effectDuration = 5f;
    public float effectStrength = 10f;
    public float damageMultiplier = 1.5f;

    // --- NETCODE STATE ---
    // Biến này để theo dõi trạng thái đã được nhặt hay chưa.
    // Chỉ Server có quyền ghi, giúp chống lại các race condition.
    private NetworkVariable<bool> m_IsPickedUp = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- THAY ĐỔI NETCODE ---
    // Logic va chạm giờ đây chỉ dùng để KÍCH HOẠT một yêu cầu lên Server.
    private void OnTriggerEnter(Collider other)
    {
        // Cố gắng lấy NetworkObject của đối tượng va chạm.
        if (other.TryGetComponent<NetworkObject>(out var networkObject))
        {
            // CHỈ người chơi sở hữu chiếc xe tăng đó (người chơi cục bộ) mới gửi yêu cầu lên server.
            // Điều này ngăn chặn việc 10 client cùng gửi yêu cầu cho 1 lần va chạm.
            if (networkObject.IsOwner)
            {
                // Gọi một ServerRpc để thông báo cho server biết rằng chúng ta muốn nhặt vật phẩm này.
                TryPickupServerRpc();
            }
        }
    }

    // --- THAY ĐỔI NETCODE ---
    // [ServerRpc] - Hàm này được client gọi, nhưng chỉ thực thi trên Server.
    // RequireOwnership = false vì client không "sở hữu" quả cầu, nhưng vẫn cần có quyền gọi RPC trên nó.
    [ServerRpc(RequireOwnership = false)]
    private void TryPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        // --- Xử lý Race Condition ---
        // Server kiểm tra trạng thái cuối cùng. Nếu đã bị nhặt, không làm gì cả.
        if (m_IsPickedUp.Value)
        {
            return;
        }

        // Đánh dấu là đã bị nhặt NGAY LẬP TỨC để yêu cầu tiếp theo sẽ thất bại.
        m_IsPickedUp.Value = true;

        // Lấy thông tin của người chơi đã gửi yêu cầu này.
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        NetworkObject playerNetworkObject = NetworkManager.Singleton.ConnectedClients[senderClientId].PlayerObject;

        // Server tìm PowerUpDetector trên xe tăng của người chơi đó.
        if (playerNetworkObject != null && playerNetworkObject.TryGetComponent<PowerUpDetector_NETCODE>(out var detector))
        {
            // Server gọi hàm áp dụng hiệu ứng.
            // Vì chúng ta đang ở trên server, hàm này sẽ gọi các ServerRpc trong PowerUpDetector một cách chính xác.
            ApplyEffect(detector);
        }

        // Sau khi áp dụng hiệu ứng, Server hủy vật phẩm này trên TOÀN MẠNG.
        // Điều này sẽ làm nó biến mất khỏi game của tất cả người chơi.
        NetworkObject.Despawn();
    }


    // Hàm này không thay đổi, giờ nó được gọi trên Server.
    private void ApplyEffect(PowerUpDetector_NETCODE detector)
    {
        switch (elementType)
        {
            case ElementType.Fire:
                detector.PowerUpSpecialShell(damageMultiplier);
                break;

            case ElementType.Water:
                detector.PowerUpHealing(effectStrength);
                break;

            case ElementType.Wind:
                detector.PowerUpSpeed(effectStrength, effectStrength, effectDuration);
                break;

            case ElementType.Earth:
                float shieldAmount = Mathf.Clamp(effectStrength, 0.1f, 0.9f);
                detector.PickUpShield(shieldAmount, effectDuration);
                break;
        }
    }
}