// ScoringSystem.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ScoringSystem : MonoBehaviour
{
    [Header("References")]
    public List<CapturePoint> capturePoints;

    [Header("UI")]
    public Text player1ScoreText;
    public Text player2ScoreText;

    [Header("Scoring")]
    public int player1Score;
    public int player2Score;

    // nội suy điểm dạng float để cộng theo deltaTime mượt
    private float p1ScoreFloat;
    private float p2ScoreFloat;

    private readonly Dictionary<int, int> flagCountToRate = new Dictionary<int, int>
    {
        {0, 0},
        {1, 1},
        {2, 3},
        {3, 5}
    };

    private void Start()
    {
        foreach (var cp in capturePoints)
        {
            if (cp == null) continue;
            cp.OnCaptured += HandleCaptured;
        }

        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        foreach (var cp in capturePoints)
        {
            if (cp == null) continue;
            cp.OnCaptured -= HandleCaptured;
        }
    }

    private void Update()
    {
        int p1Flags = CountFlags(Team.Team1);
        int p2Flags = CountFlags(Team.Team2);

        int p1Rate = flagCountToRate[Mathf.Clamp(p1Flags, 0, 3)];
        int p2Rate = flagCountToRate[Mathf.Clamp(p2Flags, 0, 3)];

        p1ScoreFloat += p1Rate * Time.deltaTime;
        p2ScoreFloat += p2Rate * Time.deltaTime;

        // Làm tròn xuống để hiển thị/cập nhật integer
        int newP1 = Mathf.FloorToInt(p1ScoreFloat);
        int newP2 = Mathf.FloorToInt(p2ScoreFloat);

        if (newP1 != player1Score || newP2 != player2Score)
        {
            player1Score = newP1;
            player2Score = newP2;
            UpdateScoreUI();
        }
    }

    private void HandleCaptured(string pointId, Team newOwner)
    {
        // Có thể phát âm thanh, hiệu ứng, log, vv.
        // Debug.Log($"Point {pointId} captured by {newOwner}");
    }

    private int CountFlags(Team team)
    {
        int c = 0;
        foreach (var cp in capturePoints)
        {
            if (cp != null && cp.Owner == team) c++;
        }
        return c;
    }

    private void UpdateScoreUI()
    {
        if (player1ScoreText) player1ScoreText.text = player1Score.ToString();
        if (player2ScoreText) player2ScoreText.text = player2Score.ToString();
    }

    // Gọi hàm này khi reset round
    public void ResetScoresAndOwnership()
    {
        p1ScoreFloat = player1Score = 0;
        p2ScoreFloat = player2Score = 0;
        UpdateScoreUI();

        foreach (var cp in capturePoints)
        {
            if (cp == null) continue;
            // reset cờ về trung lập
            // (có thể thêm hàm public Reset() trong CapturePoint nếu muốn reset cả UI/progress)
        }
    }
}
