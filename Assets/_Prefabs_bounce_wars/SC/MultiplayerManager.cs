
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class MultiplayerManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_Text joinCodeText;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Player signed in with ID: {AuthenticationService.Instance.PlayerId}");
        }

        // --- THÊM MỚI ---
        // Đăng ký các callback cho sự kiện kết nối và ngắt kết nối.
        // Làm ở Start() thay vì OnEnable() để đảm bảo NetworkManager.Singleton đã tồn tại.
        SubscribeToEvents();
    }

    // --- THÊM MỚI ---
    /// <summary>
    /// Đăng ký các sự kiện của NetworkManager.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("NetworkManager Singleton is not yet available. Subscribing will be delayed.");
            // Thử lại sau một chút nếu NetworkManager chưa sẵn sàng
            Invoke(nameof(SubscribeToEvents), 0.1f);
            return;
        }

        // Đăng ký lắng nghe sự kiện khi một client ngắt kết nối.
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    // --- THÊM MỚI ---
    // Hủy đăng ký sự kiện khi đối tượng này bị hủy để tránh lỗi rò rỉ bộ nhớ.
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    // --- THÊM MỚI ---
    /// <summary>
    /// Hàm này sẽ được gọi trên Server mỗi khi một Client ngắt kết nối.
    /// </summary>
    /// <param name="clientId">ID của client đã ngắt kết nối.</param>
    private void HandleClientDisconnect(ulong clientId)
    {
        // Chỉ Server mới cần xử lý việc này.
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"Client with ID = {clientId} has disconnected.");

        // Thông thường, Netcode sẽ tự động despawn đối tượng người chơi của client đã ngắt kết nối.
        // Bạn có thể thêm logic tùy chỉnh ở đây nếu cần, ví dụ:
        // - Thông báo cho những người chơi còn lại trong phòng.
        // - Nếu đây là Host, có thể cần phải di chuyển vai trò Host (nếu có cơ chế đó).
        // - Cập nhật lại danh sách người chơi trong sảnh chờ (Lobby).
    }

    public async void HostGame()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Host created with Join Code: {joinCode}");
            if (joinCodeText != null) joinCodeText.text = $"Join Code: {joinCode}";

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to host game: {e.Message}");
        }
    }

    public async void JoinGame()
    {
        string joinCode = joinCodeInputField.text;

        try
        {
            Debug.Log($"Attempting to join game with code: {joinCode}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join game: {e.Message}");
        }
    }
}