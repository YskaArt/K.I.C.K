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

    /// <summary>
    /// Suma 1 punto (uso original: cada jueguito exitoso durante la Fase 1).
    /// </summary>
    public void AddPoint()
    {
        AddPoints(1);
    }

    /// <summary>
    /// Suma una cantidad especifica de puntos (uso: puntaje del gol, ya
    /// calculado como puntos base de la zona x multiplicador de jueguitos).
    /// </summary>
    public void AddPoints(int amount)
    {
        if (!gameActive) return;
        score += amount;
        UpdateScoreUI();
    }

    public void GameOver()
    {
        if (!gameActive) return;
        Time.timeScale = 0f;
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
