using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Opcional: muestra el puesto conseguido en la tabla de puntajes (ej: \"Puesto #2\").")]
    [SerializeField] private TextMeshProUGUI rankText;

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

        // Registrar en la tabla de mejores puntajes (top 10).
        int rank = Scoreboard.Submit(score);
        if (rankText != null)
        {
            rankText.text = rank > 0 ? $"Puesto #{rank}" : string.Empty;
        }
    }

    public void RestartGame()
    {
        // GameOver y el slow-motion dejan el tiempo alterado; hay que
        // restaurarlo antes de recargar o la escena nueva arranca congelada.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void UpdateScoreUI()
    {
        scoreText.text = score.ToString();
    }
}
