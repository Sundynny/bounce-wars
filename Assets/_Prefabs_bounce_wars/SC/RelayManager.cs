// 15/08/2025 AI-Tag
// This version is updated to use the correct RelayService API.

using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay; // << Rất quan trọng: Cần cho RelayService
using Unity.Services.Relay.Models; // << Rất quan trọng: Cần cho Allocation
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Script này quản lý việc tạo và tham gia một kết nối Relay đơn giản.
/// Nó không sử dụng Lobby.
/// </summary>
public class RelayManager : MonoBehaviour
{
    private async void Start()
    {
        // Khởi tạo các dịch vụ và đăng nhập
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    /// <summary>
    /// Tạo một kết nối Relay và khởi động Host.
    /// </summary>
    public async Task<string> CreateRelay()
    {
        try
        {
            // Bước 1: Tạo một "Allocation" trên server của Unity.
            // Tham số (3) có nghĩa là ngoài Host, có thể có tối đa 3 người khác tham gia.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            // Bước 2: Lấy "Join Code". Đây là mã bạn sẽ chia sẻ cho người khác.
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Relay created. Join Code: " + joinCode);

            // Bước 3: Cấu hình Unity Transport với thông tin từ Allocation.
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Bước 4: Bắt đầu làm Host.
            NetworkManager.Singleton.StartHost();

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to create Relay: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Tham gia một kết nối Relay bằng mã tham gia và khởi động Client.
    /// </summary>
    /// <param name="joinCode">Mã tham gia do Host cung cấp.</param>
    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with Join Code: " + joinCode);
            // Bước 1: Tham gia vào "Allocation" của Host bằng mã tham gia.
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Bước 2: Cấu hình Unity Transport với thông tin nhận được.
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Bước 3: Bắt đầu làm Client.
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to join Relay: " + e.Message);
        }
    }
}