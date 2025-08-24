using UnityEngine;
using System.Collections.Generic;

public class CaptureManager : MonoBehaviour
{
    public List<CapturePoint> capturePoints; // tất cả cờ trên map
    public InGameManager gameManager;        // tham chiếu InGameManager

    // Map số cờ sở hữu -> điểm/giây
    private readonly Dictionary<int, int> flagToScore = new Dictionary<int, int>
    {
        {0, 0},
        {1, 1},
        {2, 3},
        {3, 5}
    };

    private float p1ScoreFloat = 0f;
    private float p2ScoreFloat = 0f;

    void Start()
    {
        foreach (var cp in capturePoints)
        {
            if (cp == null) continue;
            cp.OnCaptured += HandleCaptured;
        }
    }

    void Update()
    {
        if (gameManager == null) return;

        int p1Flags = CountFlags(Team.Team1);
        int p2Flags = CountFlags(Team.Team2);

        int p1Rate = flagToScore[Mathf.Clamp(p1Flags, 0, 3)];
        int p2Rate = flagToScore[Mathf.Clamp(p2Flags, 0, 3)];

        p1ScoreFloat += p1Rate * Time.deltaTime;
        p2ScoreFloat += p2Rate * Time.deltaTime;

        // Chỉ lấy phần nguyên, cập nhật InGameManager
        int newP1 = Mathf.FloorToInt(p1ScoreFloat);
        int newP2 = Mathf.FloorToInt(p2ScoreFloat);

        if (newP1 != gameManager.player1Score)
            gameManager.player1Score = newP1;

        if (newP2 != gameManager.player2Score)
            gameManager.player2Score = newP2;
    }

    private int CountFlags(Team team)
    {
        int count = 0;
        foreach (var cp in capturePoints)
        {
            if (cp.Owner == team) count++;
        }
        return count;
    }

    private void HandleCaptured(string pointId, Team newOwner)
    {
        // Khi một cờ vừa được chiếm xong, có thể phát âm thanh hoặc hiệu ứng
        Debug.Log($"Point {pointId} captured by {newOwner}");
    }
}
