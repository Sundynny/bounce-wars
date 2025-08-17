// --- KHỐI USING ĐÃ ĐƯỢỢC DỌN DẸP VÀ CHUẨN HÓA ---
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;        // Quan trọng: Cần cho LobbyService và LobbyServiceException
using Unity.Services.Lobbies.Models;  // Quan trọng: Cần cho lớp Lobby, QueryResponse, DataObject, v.v.
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

/// <summary>
/// Quản lý toàn bộ luồng kết nối multiplayer trong Scene Lobby.
/// Script này giả định người chơi đã được xác thực từ Scene trước (LoginScene).
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Ô để người dùng nhập tên phòng muốn tạo.")]
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [Tooltip("Panel (khung chứa) để hiển thị danh sách các phòng tìm được.")]
    [SerializeField] private GameObject lobbyListPanel;
    [Tooltip("Prefab cho mỗi mục trong danh sách phòng. Cần có script LobbyListItemUI.")]
    [SerializeField] private GameObject lobbyListItemPrefab;
    [Tooltip("Text để hiển thị các thông báo trạng thái cho người dùng.")]
    [SerializeField] private TMP_Text statusText;

    // Lưu trữ thông tin về phòng chờ hiện tại mà người chơi đang ở trong.
    private Lobby m_CurrentLobby;
    // Bộ đếm thời gian để gửi tín hiệu sống (heartbeat) cho lobby.
    private float m_LobbyHeartbeatTimer;
    // Một "chìa khóa" không đổi để lưu và truy xuất mã tham gia Relay từ dữ liệu của Lobby.
    private const string KEY_RELAY_CODE = "RelayJoinCode";

    #region --- Setup & Lifecycle ---

    /// <summary>
    /// Awake được gọi khi Scene được tải. Đây là nơi tốt nhất để đăng ký các sự kiện.
    /// </summary>
    private void Awake()
    {
        // Bắt đầu lắng nghe các sự kiện mạng quan trọng ngay lập tức.
        SubscribeToEvents();
    }

    // --- HÀM START() ĐÃ ĐƯỢC XÓA BỎ ---
    // Logic khởi tạo và đăng nhập giờ đã được xử lý bởi LoginManager ở Scene trước.

    /// <summary>
    /// Hàm Update chỉ dùng để xử lý việc gửi tín hiệu sống (heartbeat) đều đặn.
    /// </summary>
    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    /// <summary>
    /// Đăng ký các sự kiện của NetworkManager.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (NetworkManager.Singleton == null)
        {
            Invoke(nameof(SubscribeToEvents), 0.1f);
            return;
        }
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    /// <summary>
    /// Dọn dẹp khi đối tượng bị hủy.
    /// </summary>
    private void OnDestroy()
    {
        LeaveLobby();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    #endregion

    #region --- Core Logic ---

    /// <summary>
    /// Hàm này được gọi trên Server mỗi khi một Client ngắt kết nối.
    /// </summary>
    private void HandleClientDisconnect(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log($"Client with ID = {clientId} has disconnected.");
        // TODO: Cập nhật UI để xóa người chơi đã thoát khỏi danh sách trong lobby.
    }

    /// <summary>
    /// Gửi tín hiệu sống (heartbeat) đến dịch vụ Lobby của Unity để giữ cho phòng không bị đóng.
    /// </summary>
    private async void HandleLobbyHeartbeat()
    {
        if (m_CurrentLobby != null && NetworkManager.Singleton.IsHost)
        {
            m_LobbyHeartbeatTimer -= Time.deltaTime;
            if (m_LobbyHeartbeatTimer <= 0f)
            {
                m_LobbyHeartbeatTimer = 15f;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogWarning($"Failed to send heartbeat ping: {e}");
                }
            }
        }
    }

    /// <summary>
    /// Hàm tiện ích để cập nhật Text trạng thái một cách an toàn.
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    #endregion

    #region --- Public Functions (Called by UI Buttons) ---

    /// <summary>
    /// Tạo một phòng chờ mới, sử dụng tên người chơi đã được xác thực.
    /// </summary>
    public async void CreateLobby()
    {
        UpdateStatusText("Creating Lobby...");
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string lobbyName = string.IsNullOrEmpty(lobbyNameInputField.text) ? $"{AuthenticationService.Instance.PlayerName}'s Game" : lobbyNameInputField.text;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerName) }
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            };

            m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, options);
            Debug.Log($"Lobby created by {AuthenticationService.Instance.PlayerName}! Name: {m_CurrentLobby.Name}");
            UpdateStatusText($"Lobby Created: {m_CurrentLobby.Name}");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData
            );
            NetworkManager.Singleton.StartHost();
        }
        catch (LobbyServiceException e) { Debug.LogError($"Failed to create lobby: {e.Message}"); UpdateStatusText("Failed to create lobby."); }
        catch (RelayServiceException e) { Debug.LogError($"Failed to create relay: {e.Message}"); UpdateStatusText("Failed to create lobby."); }
    }

    /// <summary>
    /// Tìm kiếm và hiển thị danh sách các phòng chờ đang mở.
    /// </summary>
    public async void ListLobbies()
    {
        UpdateStatusText("Finding Lobbies...");
        try
        {
            if (lobbyListPanel != null)
            {
                foreach (Transform child in lobbyListPanel.transform) { Destroy(child.gameObject); }
            }

            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter> { new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Lobby lobby in queryResponse.Results)
            {
                GameObject lobbyItem = Instantiate(lobbyListItemPrefab, lobbyListPanel.transform);
                var lobbyUI = lobbyItem.GetComponent<LobbyListItemUI>();
                if (lobbyUI != null)
                {
                    lobbyUI.Setup(lobby, () => JoinLobby(lobby));
                }
            }
            UpdateStatusText($"Found {queryResponse.Results.Count} lobbies.");
        }
        catch (LobbyServiceException e) { Debug.LogError($"Failed to list lobbies: {e.Message}"); UpdateStatusText("Failed to find lobbies."); }
    }

    /// <summary>
    /// Tham gia một phòng chờ đã chọn, cập nhật tên của người chơi tham gia.
    /// </summary>
    private async void JoinLobby(Lobby lobby)
    {
        UpdateStatusText($"Joining {lobby.Name}...");
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                         { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerName) }
                    }
                }
            };

            m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
            string relayJoinCode = m_CurrentLobby.Data[KEY_RELAY_CODE].Value;

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes, joinAllocation.Key,
                joinAllocation.ConnectionData, joinAllocation.HostConnectionData
            );
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e) { Debug.LogError($"Failed to join lobby: {e.Message}"); UpdateStatusText("Failed to join lobby."); }
        catch (RelayServiceException e) { Debug.LogError($"Failed to join relay: {e.Message}"); UpdateStatusText("Failed to join lobby."); }
    }

    /// <summary>
    /// Rời khỏi phòng chờ hiện tại.
    /// </summary>
    public async void LeaveLobby()
    {
        if (m_CurrentLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
                m_CurrentLobby = null;

                NetworkManager.Singleton.Shutdown();
                UpdateStatusText("You left the lobby.");
            }
            catch (LobbyServiceException e) { Debug.LogWarning($"Failed to leave lobby: {e}"); }
        }
    }
    #endregion
}