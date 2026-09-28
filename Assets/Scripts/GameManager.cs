using System.Collections;
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
    private int displayedScore = 0;
    private Coroutine scoreAnimRoutine;
    private bool gameActive = true;

    /// <summary>False una vez que termino la partida (Game Over ya ejecutado).</summary>
    public bool IsGameActive => gameActive;

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

        if (scoreAnimRoutine != null) StopCoroutine(scoreAnimRoutine);
        scoreAnimRoutine = StartCoroutine(AnimateScoreTo(score));
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
        displayedScore = score;
        scoreText.text = displayedScore.ToString();
    }

    /// <summary>
    /// Hace que el numero de puntaje "cuente" hasta el valor nuevo en vez de
    /// saltar de golpe. Usa tiempo real para que se siga viendo aunque el
    /// gol congele el juego un instante (ver GameFlowManager.PresentGoalFollowUp).
    /// </summary>
    private IEnumerator AnimateScoreTo(int target)
    {
        int start = displayedScore;
        float duration = Mathf.Clamp(Mathf.Abs(target - start) * 0.03f, 0.15f, 0.6f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            scoreText.text = displayedScore.ToString();
            yield return null;
        }

        displayedScore = target;
        scoreText.text = displayedScore.ToString();
        scoreAnimRoutine = null;
    }
}
