// 15/08/2025 AI-Tag
// This version is updated to handle Scene transitions instead of Panels.

using UnityEngine;
using TMPro;                // Cần thiết để sử dụng các thành phần TextMeshPro
using UnityEngine.UI;       // Cần thiết để sử dụng thành phần Button
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using UnityEngine.SceneManagement; // << THÊM MỚI: Rất quan trọng để quản lý Scene

/// <summary>
/// Quản lý luồng đăng nhập ban đầu của người chơi và chuyển sang Scene tiếp theo.
/// </summary>
public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Ô để người chơi nhập tên.")]
    [SerializeField] private TMP_InputField nameInputField;
    [Tooltip("Nút để xác nhận đăng nhập.")]
    [SerializeField] private Button loginButton;
    [Tooltip("Text để hiển thị các thông báo trạng thái cho người dùng.")]
    [SerializeField] private TMP_Text statusText; // << THÊM MỚI: Để cung cấp phản hồi tốt hơn

    // --- THAY ĐỔI: Chuyển từ quản lý Panel sang Scene ---
    [Header("Scene Navigation")]
    [Tooltip("Tên của Scene (dưới dạng chuỗi) sẽ được tải sau khi đăng nhập thành công.")]
    [SerializeField] private string sceneToLoadAfterLogin = "LobbyScene";

    private void Start()
    {
        // Gán sự kiện cho nút bấm khi game bắt đầu.
        loginButton.onClick.AddListener(Login);
        // Đặt một thông báo ban đầu cho người dùng.
        if (statusText != null) statusText.text = "Please enter your name to continue.";
    }

    /// <summary>
    /// Hàm được gọi khi người chơi nhấn nút Login.
    /// </summary>
    public async void Login()
    {
        // Lấy tên người chơi từ ô nhập liệu và loại bỏ khoảng trắng thừa.
        string playerName = nameInputField.text.Trim();

        // Kiểm tra xem người chơi đã nhập tên hay chưa.
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Player name cannot be empty.");
            if (statusText != null) statusText.text = "Your name cannot be empty!";
            return;
        }

        // Vô hiệu hóa UI để người dùng không thể tương tác trong khi đang xử lý.
        loginButton.interactable = false;
        nameInputField.interactable = false;
        if (statusText != null) statusText.text = "Logging in...";

        try
        {
            // Khởi tạo và đăng nhập ẩn danh.
            await InitializeAndSignIn();

            // Cập nhật tên người chơi lên dịch vụ của Unity.
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            Debug.Log($"Player name updated to: {AuthenticationService.Instance.PlayerName}");
            if (statusText != null) statusText.text = "Login successful! Loading lobby...";

            // --- THAY ĐỔI: Chuyển từ quản lý Panel sang Scene ---
            // Đăng nhập thành công, tải Scene tiếp theo.
            // Đảm bảo Scene này đã được thêm vào Build Settings.
            SceneManager.LoadScene(sceneToLoadAfterLogin);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
            if (statusText != null) statusText.text = "Login failed. Please check your connection and try again.";
            // Bật lại UI nếu có lỗi để người chơi có thể thử lại.
            loginButton.interactable = true;
            nameInputField.interactable = true;
        }
    }

    /// <summary>
    /// Gói gọn logic khởi tạo và đăng nhập.
    /// </summary>
    private async Task InitializeAndSignIn()
    {
        // Khởi tạo nếu cần.
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        // Đăng nhập nếu cần.
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Player signed in with ID: {AuthenticationService.Instance.PlayerId}");
        }
    }

    private void OnDestroy()
    {
        // Dọn dẹp sự kiện khi đối tượng bị hủy.
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(Login);
        }
    }
}