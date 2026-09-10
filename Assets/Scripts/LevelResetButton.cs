using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Boton de "reiniciar nivel". Poné este componente sobre un Button de la UI
/// y funciona solo: recarga la escena activa y restaura el <c>Time.timeScale</c>
/// (que queda en 0 tras un Game Over o el slow-motion).
///
/// Tambien expone <see cref="ResetLevel"/> como metodo publico por si preferis
/// conectarlo a mano desde el OnClick del Inspector o llamarlo desde otro script.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelResetButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(ResetLevel);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(ResetLevel);
    }

    /// <summary>Reinicia la escena actual desde cero.</summary>
    public void ResetLevel()
    {
        // El Game Over y el slow-motion dejan el tiempo congelado/alterado.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
