using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de poder que se llena con cada jueguito exitoso durante la Fase 1.
/// A partir del umbral (por defecto 50%) el jugador puede optar por patear.
/// Si sigue cargando hasta el 100% consigue mas puntaje y mas potencia en el tiro.
/// </summary>
public class JueguitosPowerBar : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("Progreso minimo (0-1) para habilitar el boton de patear")]
    [SerializeField] private float shootThreshold = 0.5f;

    [Tooltip("Multiplicador de potencia/puntaje al llegar justo al umbral minimo")]
    [SerializeField] private float minMultiplier = 1f;

    [Tooltip("Multiplicador de potencia/puntaje al llegar al 100%")]
    [SerializeField] private float maxMultiplier = 2f;

    [Header("UI opcional")]
    [Tooltip("Slider que representa visualmente el progreso (0-1). Opcional.")]
    [SerializeField] private Slider uiSlider;

    [Tooltip("Imagen con Fill Amount que representa el progreso. Alternativa opcional al Slider.")]
    [SerializeField] private Image uiFillImage;

    [Header("Color segun progreso")]
    [Tooltip("Si esta activo, el color de la barra cambia segun el progreso usando el gradiente de abajo")]
    [SerializeField] private bool useColorGradient = true;

    [Tooltip("Color en 0% -> color en 100%. Por defecto: verde -> amarillo -> rojo, como una barra de vida al reves")]
    [SerializeField]
    private Gradient colorGradient = CreateDefaultGradient();

    private static Gradient CreateDefaultGradient()
    {
        // Verde (vacia) -> Amarillo (a mitad) -> Rojo (llena). Es la barra de
        // vida clasica, pero acá se usa "invertida": llenarse de rojo es bueno
        // (mas potencia), no malo.
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.2f, 0.85f, 0.25f), 0f),   // verde
                new GradientColorKey(new Color(1f, 0.85f, 0.1f), 0.5f),    // amarillo
                new GradientColorKey(new Color(0.9f, 0.15f, 0.15f), 1f),   // rojo
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
        return gradient;
    }

    private float currentPower;

    /// <summary>Progreso actual de la barra, de 0 a 1.</summary>
    public float Power01 => currentPower;

    /// <summary>True si ya se llego al umbral minimo para poder patear.</summary>
    public bool CanShoot => currentPower >= shootThreshold;

    /// <summary>
    /// Multiplicador de potencia/puntaje actual, interpolado entre el umbral
    /// minimo de disparo y el 100% de carga. Antes de llegar al umbral no
    /// deberia usarse (CanShoot va a estar en false).
    /// </summary>
    public float ShotMultiplier
    {
        get
        {
            float t = Mathf.InverseLerp(shootThreshold, 1f, currentPower);
            return Mathf.Lerp(minMultiplier, maxMultiplier, t);
        }
    }

    /// <summary>
    /// Suma carga a la barra tras un jueguito exitoso. Llamar desde KickZone.
    /// </summary>
    public void AddCharge(float amount)
    {
        currentPower = Mathf.Clamp01(currentPower + amount);
        UpdateUI();
    }

    /// <summary>
    /// Reinicia la barra. Llamar al empezar una nueva ronda.
    /// </summary>
    public void ResetPower()
    {
        currentPower = 0f;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (uiSlider != null)
        {
            uiSlider.value = currentPower;
        }

        if (uiFillImage != null)
        {
            uiFillImage.fillAmount = currentPower;
        }

        if (useColorGradient)
        {
            Color color = colorGradient.Evaluate(currentPower);

            if (uiFillImage != null)
            {
                uiFillImage.color = color;
            }

            // El Slider de Unity tiene su propia imagen de relleno (Fill Rect),
            // separada del fillAmount de una Image comun. La coloreamos tambien
            // si esta asignado un Slider en vez de (o ademas de) la Image.
            if (uiSlider != null && uiSlider.fillRect != null)
            {
                Image sliderFillImage = uiSlider.fillRect.GetComponent<Image>();
                if (sliderFillImage != null)
                {
                    sliderFillImage.color = color;
                }
            }
        }
    }
}