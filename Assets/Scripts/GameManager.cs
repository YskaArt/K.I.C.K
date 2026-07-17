using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private GameObject gameOverPanel;

    private int score = 0;
    private bool gameActive = true;

    private const string HighScoreKey = "HighScore";

    private void Start()
    {
        gameOverPanel.SetActive(false);
        UpdateScoreUI();
    }

    public void AddPoint()
    {
        if (!gameActive) return;
        score++;
        UpdateScoreUI();
    }

    public void GameOver()
    {
        if (!gameActive) return;

        gameActive = false;
        finalScoreText.text = "Score: " + score;
        gameOverPanel.SetActive(true);

        if (score > PlayerPrefs.GetInt(HighScoreKey, 0))
            PlayerPrefs.SetInt(HighScoreKey, score);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void UpdateScoreUI()
    {
        scoreText.text = score.ToString();
    }
}