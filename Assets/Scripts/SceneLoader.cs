using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Cargar una escena por nombre
    [Tooltip("Escena de juego: si es la primera vez y no se vio el tutorial, se muestra antes de cargarla.")]
    [SerializeField] private string gameSceneName = "Jueguitos";

    public void LoadScene(string sceneName)
    {
        if (sceneName == gameSceneName && !TutorialProgress.Completed)
        {
            var tutorial = FindAnyObjectByType<TutorialScreen>(FindObjectsInactive.Include);
            if (tutorial != null)
            {
                tutorial.OpenMandatory(() => SceneManager.LoadScene(sceneName));
                return;
            }
        }

        SceneManager.LoadScene(sceneName);
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