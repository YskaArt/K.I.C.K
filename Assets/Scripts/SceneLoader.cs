using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Cargar una escena por nombre
    public void LoadScene(string sceneName)
    {
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