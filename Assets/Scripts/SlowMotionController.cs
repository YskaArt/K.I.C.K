using System.Collections;
using UnityEngine;

/// <summary>
/// Maneja la transicion de tiempo normal a "camara lenta" durante la Fase 2
/// (Apuntado) y la vuelta a velocidad normal al disparar o al terminar el tiro.
/// Es un singleton simple para poder llamarlo desde cualquier script sin
/// tener que arrastrar referencias en el Inspector.
/// </summary>
public class SlowMotionController : MonoBehaviour
{
    public static SlowMotionController Instance { get; private set; }

    [Header("Configuracion de camara lenta")]
    [Tooltip("A que fraccion del tiempo normal se ralentiza (0.2 = 20% velocidad)")]
    [SerializeField] private float slowMoScale = 0.2f;

    [Tooltip("Cuanto tarda en entrar/salir del slow-motion (en segundos reales)")]
    [SerializeField] private float transitionDuration = 0.25f;

    private Coroutine currentTransition;

    private void Awake()
    {
        // Singleton basico: si ya existe uno, este se destruye
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Entra en camara lenta (llamar al empezar el swipe / Fase 2).
    /// </summary>
    public void EnterSlowMo()
    {
        StartTransition(slowMoScale);
    }

    /// <summary>
    /// Vuelve a velocidad normal (llamar al soltar el swipe / disparar).
    /// </summary>
    public void ExitSlowMo()
    {
        StartTransition(1f);
    }

    private void StartTransition(float targetScale)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(TransitionRoutine(targetScale));
    }

    private IEnumerator TransitionRoutine(float targetScale)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            // Usamos unscaledDeltaTime para que la transicion no dependa
            // del propio timeScale que estamos cambiando
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / transitionDuration;

            Time.timeScale = Mathf.Lerp(startScale, targetScale, t);
            UpdateFixedDeltaTime();

            yield return null;
        }

        Time.timeScale = targetScale;
        UpdateFixedDeltaTime();
    }

    private void UpdateFixedDeltaTime()
    {
        // Ajustamos el fixedDeltaTime en proporcion al timeScale para que la
        // fisica no se vea "entrecortada" durante el slow-motion
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OnDestroy()
    {
        // Por las dudas, si se destruye el controlador dejamos el tiempo normal
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
