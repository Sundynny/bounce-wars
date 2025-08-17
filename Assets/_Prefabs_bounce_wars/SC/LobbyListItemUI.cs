// 15/08/2025 AI-Tag
// This version is updated to work with the stable LobbyService API.

using UnityEngine;
using TMPro;                // Cần thiết để sử dụng các thành phần TextMeshPro
using UnityEngine.UI;       // Cần thiết để sử dụng thành phần Button
using Unity.Services.Lobbies.Models; // << Rất quan trọng: Namespace chứa lớp Lobby

/// <summary>
/// Script này quản lý giao diện và hành động cho MỘT mục trong danh sách các phòng chờ.
/// Nó sẽ được gắn vào prefab `lobbyListItemPrefab`.
/// </summary>
public class LobbyListItemUI : MonoBehaviour
{
    [Header("UI Component References")]
    [Tooltip("Thành phần Text để hiển thị tên phòng.")]
    [SerializeField] private TMP_Text lobbyNameText;
    [Tooltip("Thành phần Text để hiển thị số lượng người chơi.")]
    [SerializeField] private TMP_Text playerCountText;
    [Tooltip("Nút để người chơi nhấn vào để tham gia phòng này.")]
    [SerializeField] private Button joinButton;

    // Lưu trữ dữ liệu của phòng chờ mà mục UI này đang hiển thị.
    private Lobby m_Lobby;

    /// <summary>
    /// Hàm này được gọi từ MultiplayerManager để thiết lập thông tin hiển thị và hành động cho mục UI này.
    /// </summary>
    /// <param name="lobby">Đối tượng Lobby chứa tất cả dữ liệu về phòng chờ (tên, số người chơi, v.v.).</param>
    /// <param name="joinAction">Hành động (một hàm) sẽ được thực thi khi người chơi nhấn nút "Join".</param>
    public void Setup(Lobby lobby, System.Action joinAction)
    {
        // Lưu lại dữ liệu lobby để có thể sử dụng sau này nếu cần.
        m_Lobby = lobby;

        // Cập nhật các thành phần UI với dữ liệu từ lobby.
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        // Gán sự kiện cho nút bấm "Join".
        // 1. Xóa tất cả các sự kiện cũ để tránh việc một nút gọi nhiều hàm cùng lúc.
        joinButton.onClick.RemoveAllListeners();
        // 2. Thêm một sự kiện mới: khi nút được nhấn, nó sẽ gọi hàm `joinAction` đã được truyền vào.
        joinButton.onClick.AddListener(() =>
        {
            // Vô hiệu hóa nút ngay sau khi nhấn để tránh người dùng nhấn nhiều lần.
            joinButton.interactable = false;
            // Thực thi hành động tham gia phòng (chính là hàm JoinLobby trong MultiplayerManager).
            joinAction?.Invoke();
        });
    }

    /// <summary>
    /// Hàm này được gọi tự động bởi Unity khi đối tượng bị vô hiệu hóa hoặc bị hủy.
    /// Dùng để dọn dẹp, tránh rò rỉ bộ nhớ.
    /// </summary>
    private void OnDisable()
    {
        // Đảm bảo rằng sự kiện của nút bấm đã được gỡ bỏ khi không còn cần thiết.
        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
        }
    }
}