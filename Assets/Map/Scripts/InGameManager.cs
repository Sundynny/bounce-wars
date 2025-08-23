using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class InGameManager : MonoBehaviour
{
    [Header("UI InGame")]
    public Text player1ScoreInGameText;
    public Text player2ScoreInGameText;

    [Header("UI")]
    public Text timerText;        
    public GameObject inGameUIPanel;
    public GameObject endGamePanel;
    public GameObject pausePanel;
    public Button nextRoundButton;

    [Header("UI EndGame")]
    public Text timeResultText;
    public Text player1ScoreText;
    public Text player2ScoreText;
    public Image player1ResultIcon;
    public Image player2ResultIcon;
    public Sprite skullIcon;
    public Sprite sunIcon;

    [Header("Settings")]
    public float matchTime = 180f; 
    public int maxScore = 200;     

    private float currentTime;
    private bool isPaused = false;
    private bool isGameOver = false;

    // Điểm số 2 người chơi
    public int player1Score = 0;
    public int player2Score = 50;

    //private int player1Wins = 0;
    //private int player2Wins = 0;

    public Sprite player1WinSprite;
    public Sprite player2WinSprite;
    public Image[] leftIconSlots;
    public Image[] rightIconSlots;
    public Text objective_txt;

    [Header("Start Round UI")]
    public GameObject startRoundPanel;
    public Text roundCountText;
    public float startRoundDuration = 3f;

    void Start()
    {
        currentTime = matchTime;
        endGamePanel.SetActive(false);
        pausePanel.SetActive(false);

        SetupMatchIcons();
        StartCoroutine(ShowStartRoundPanel());
    }
    void SetupMatchIcons()
    {
        int iconCount = (GameSettings.MatchCount + 1) / 2;
        if (objective_txt != null)
            objective_txt.text = "" + iconCount;

        // Ẩn tất cả icon trước
        foreach (var icon in leftIconSlots) icon.gameObject.SetActive(false);
        foreach (var icon in rightIconSlots) icon.gameObject.SetActive(false);

        // Bật số icon tương ứng cho mỗi bên
        for (int i = 0; i < iconCount && i < leftIconSlots.Length; i++)
        {
            leftIconSlots[i].gameObject.SetActive(true);
            rightIconSlots[i].gameObject.SetActive(true);
        }
        if (GameSettings.player1Wins != 0 || GameSettings.player2Wins != 0)
        {
            for (int i = 0; i < GameSettings.player1Wins; i++)
            {
                leftIconSlots[i].sprite = player1WinSprite;
            }
            for (int i = 0; i < GameSettings.player2Wins; i++)
            {
                rightIconSlots[i].sprite = player2WinSprite;
            }
        }
    }

    IEnumerator ShowStartRoundPanel()
    {
        if (startRoundPanel != null)
        {
            startRoundPanel.SetActive(true);

            // Cập nhật số Round
            if (roundCountText != null)
                roundCountText.text = "" + GameSettings.CurrentRound;

            yield return new WaitForSecondsRealtime(startRoundDuration); // dùng Realtime để không bị ảnh hưởng Time.timeScale

            startRoundPanel.SetActive(false);
        }
    }


    void Update()
    {
        if (isGameOver || isPaused) return;

        currentTime -= Time.deltaTime;
        if (currentTime < 0) currentTime = 0;

        UpdateTimerUI();

        if (currentTime <= 0 || player1Score >= maxScore || player2Score >= maxScore)
        {
            EndGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    void UpdateTimerUI()
    {
        player1ScoreInGameText.text = "" + player1Score;
        player2ScoreInGameText.text = "" + player2Score;
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void EndGame()
    {
        isGameOver = true;
        endGamePanel.SetActive(true);
        inGameUIPanel.SetActive(false);
        Time.timeScale = 0f; 

        // 🟢 Hiển thị kết quả
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        if (timeResultText != null)
            timeResultText.text = "Time Left: " + string.Format("{0:00}:{1:00}", minutes, seconds);

        if (player1ScoreText != null)
            player1ScoreText.text = "" + player1Score;

        if (player2ScoreText != null)
            player2ScoreText.text = "" + player2Score;

        if (player1Score > player2Score)
        {
            GameSettings.player1Wins++;
            player1ResultIcon.sprite = sunIcon;
            player2ResultIcon.sprite = skullIcon;
        }
        else if (player2Score > player1Score)
        {
            GameSettings.player2Wins++;
            player1ResultIcon.sprite = skullIcon;
            player2ResultIcon.sprite = sunIcon;
        }
        else
        {
            player1ResultIcon.sprite = skullIcon;
            player2ResultIcon.sprite = skullIcon;
        }

        if (GameSettings.CurrentRound >= GameSettings.MatchCount)
        {
            nextRoundButton.gameObject.SetActive(false);
        }
        else
        {
            nextRoundButton.gameObject.SetActive(true);

        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        inGameUIPanel.SetActive(!isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void OnHomeButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScreen");
    }

    public void OnNextRoundButton()
    {
        Time.timeScale = 1f;
        GameSettings.CurrentRound++;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void OnResumeButton()
    {
        TogglePause();
    }

    public void OnPauseHomeButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScreen");
    }

    public void AddScore(int playerId, int score)
    {
        if (playerId == 1) player1Score += score;
        else if (playerId == 2) player2Score += score;
    }
}
