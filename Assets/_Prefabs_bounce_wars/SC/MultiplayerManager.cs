//// 22/07/2025 AI-Tag
//// This was created with the help of Assistant, a Unity Artificial Intelligence product.

//using System;
//using UnityEditor;
//using UnityEngine;
//using Unity.Services.Multiplayer;
//using Unity.Services.Multiplayer.Relay;
//using Unity.Netcode;

//public class MultiplayerManager : MonoBehaviour
//{
//    public async void HostGame()
//    {
//        await UnityServices.InitializeAsync();
//        var allocation = await RelayService.Instance.CreateAllocationAsync(4); // Tối đa 4 người chơi
//        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

//        Debug.Log($"Join Code: {joinCode}");

//        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
//            allocation.RelayServer.IpV4,
//            allocation.RelayServer.Port,
//            allocation.AllocationIdBytes,
//            allocation.ConnectionData,
//            allocation.Key,
//            allocation.HostConnectionData
//        );

//        NetworkManager.Singleton.StartHost();
//    }

//    public async void JoinGame(string joinCode)
//    {
//        await UnityServices.InitializeAsync();
//        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

//        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
//            joinAllocation.RelayServer.IpV4,
//            joinAllocation.RelayServer.Port,
//            joinAllocation.AllocationIdBytes,
//            joinAllocation.ConnectionData,
//            joinAllocation.Key,
//            joinAllocation.HostConnectionData
//        );

//        NetworkManager.Singleton.StartClient();
//    }
//}
