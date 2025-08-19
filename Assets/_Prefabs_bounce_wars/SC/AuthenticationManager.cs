using System;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using TMPro; // Thêm nếu bạn muốn cập nhật UI

public class AuthenticationManager : MonoBehaviour
{
    // Tùy chọn: Thêm một Text để hiển thị trạng thái cho người dùng
    [SerializeField] private TMP_Text statusText;

    public async void Start()
    {
        try
        {
            if (statusText != null) statusText.text = "Initializing Services...";
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                if (statusText != null) statusText.text = "Signing In...";
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                string successMessage = "Signed in as: " + AuthenticationService.Instance.PlayerId;
                Debug.Log(successMessage);
                if (statusText != null) statusText.text = "Signed In!";
            }
            else
            {
                string alreadySignedInMessage = "Player is already signed in as: " + AuthenticationService.Instance.PlayerId;
                Debug.Log(alreadySignedInMessage);
                if (statusText != null) statusText.text = "Welcome Back!";
            }
        }
        catch (AuthenticationException ex)
        {
            // Xử lý các lỗi cụ thể từ dịch vụ Authentication
            string errorMessage = "Sign-in failed: " + ex.Message;
            Debug.LogError(errorMessage);
            if (statusText != null) statusText.text = "Sign-in Failed!";
        }
        catch (RequestFailedException ex)
        {
            // Xử lý các lỗi chung về kết nối mạng
            string errorMessage = "Sign-in failed: " + ex.Message;
            Debug.LogError(errorMessage);
            if (statusText != null) statusText.text = "Sign-in Failed!";
        }
    }
}