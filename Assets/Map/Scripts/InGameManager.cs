using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Settings")]
    public float matchTime = 180f; 
    public int maxScore = 200;     

    private float currentTime;
    private bool isPaused = false;
    private bool isGameOver = false;

    // Điểm số 2 người chơi
    public int player1Score = 0;
    public int player2Score = 50;

    

    void Start()
    {
        currentTime = matchTime;
        endGamePanel.SetActive(false);
        pausePanel.SetActive(false);
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
