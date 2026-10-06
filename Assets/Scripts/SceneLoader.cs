using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("Nombre de la escena de juego. La usa LoadTutorial() para saber que escena cargar.")]
    [SerializeField] private string gameSceneName = "Jueguitos";

    // Cargar una escena por nombre
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Conectar al boton "Como Jugar" del menu. Fuerza la ronda guiada (ver
    /// GuidedTutorial) aunque ya se haya jugado antes -- a diferencia de
    /// Play, que solo la activa la primera vez (TutorialProgress.Completed).
    /// </summary>
    public void LoadTutorial()
    {
        GuidedTutorialRequest.RequestNext();
        SceneManager.LoadScene(gameSceneName);
    }
    public void RestartLevel()
    {
        // Restaurar el tiempo por si venimos de un Game Over o slow-motion.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        Scene escenaActual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escenaActual.buildIndex);
    }
    // Cerrar el juego
    public void ExitGame()
    {
        Debug.Log("Saliendo del juego...");

        Application.Quit();

    }
}