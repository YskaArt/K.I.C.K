using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa de la partida. Congela el tiempo, muestra el panel con los
/// sliders de audio (componentes <see cref="AudioVolumeSlider"/> ya
/// existentes, uno por canal) y deja elegir entre seguir jugando o cortar la
/// partida guardando el puntaje y volviendo al menu principal.
///
/// Setup en el editor:
///   1. Un panel (desactivado) con este componente encima.
///   2. Adentro, 3 Slider con AudioVolumeSlider (Master/Music/Sfx) y,
///      opcional, un boton con MuteButton.
///   3. Un boton "Seguir jugando" -> PauseMenu.Resume().
///   4. Un boton "Menu Principal" -> PauseMenu.QuitToMainMenu().
///   5. El boton de pausa del HUD -> PauseMenu.Pause() (o TogglePause()).
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameManager gameManager;

    [Header("Salir al menu")]
    [Tooltip("Escena a cargar al elegir Menu Principal. Guarda el puntaje actual antes de cargarla.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Comportamiento")]
    [Tooltip("Permite abrir/cerrar la pausa con la tecla Esc, ademas del boton de UI.")]
    [SerializeField] private bool allowEscapeKey = true;

    private float timeScaleBeforePause = 1f;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (allowEscapeKey && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>Conectar al boton de pausa del HUD.</summary>
    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    /// <summary>Abre el menu de pausa y congela el juego.</summary>
    public void Pause()
    {
        if (IsPaused) return;

        // No pausar si la partida ya termino, o si justo se esta mostrando
        // el panel de "gol: seguir jugando o terminar" (otra pantalla modal).
        if (gameManager != null && !gameManager.IsGameActive) return;
        if (GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CurrentPhase == GameFlowManager.GamePhase.Resolved)
        {
            return;
        }

        IsPaused = true;
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        if (panel != null) panel.SetActive(true);
    }

    /// <summary>Conectar al boton "Seguir jugando". Cierra el panel y descongela el juego.</summary>
    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = timeScaleBeforePause;

        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// Conectar al boton "Menu Principal". Guarda el puntaje actual
    /// (high score + tabla de puntajes, via GameManager.GameOver) y vuelve
    /// al menu principal.
    /// </summary>
    public void QuitToMainMenu()
    {
        if (gameManager != null)
        {
            gameManager.GameOver();
        }

        IsPaused = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
