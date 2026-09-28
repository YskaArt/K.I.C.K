using UnityEngine;

/// <summary>
/// Un efecto de particulas (explosion de gol, polvo de fallo, etc), pensado
/// para que lo edite el artista 2D sin tocar codigo: cada campo es un
/// numerito o un dibujo, no hay que entender el sistema de Particulas de
/// Unity para usarlo.
///
/// Para crear uno nuevo: Assets > Create > K.I.C.K > Impact Effect. Dibujale
/// un sprite (una estrellita, una chispa, una nube de polvo -- cualquier PNG
/// con transparencia sirve) y ajustá los numeros hasta que se vea bien
/// jugando. Despues arrastralo al campo correspondiente en GoalDetector,
/// Ground o GameFlowManager.
/// </summary>
[CreateAssetMenu(fileName = "ImpactEffect", menuName = "K.I.C.K/Impact Effect")]
public class ImpactEffect : ScriptableObject
{
    [Header("Dibujo (arte 2D)")]
    [Tooltip("El dibujo que usa cada particula. Cualquier PNG con transparencia sirve (estrella, chispa, polvo...).")]
    public Sprite particleSprite;

    [Tooltip("Color de cada particula a lo largo de su vida: izquierda = recien nace, derecha = justo antes de desaparecer. El alpha (transparencia) tambien se toma de aca.")]
    public Gradient colorOverLifetime = DefaultGradient();

    [Header("Cantidad y forma")]
    [Tooltip("Cuantas particulas salen de una sola vez.")]
    [Min(1)] public int burstCount = 20;

    [Tooltip("Angulo del cono de salida. 0 = todas van para el mismo lado (hacia arriba), 180 = salen para cualquier lado (como una explosion).")]
    [Range(0f, 180f)] public float spreadAngle = 45f;

    [Header("Movimiento")]
    [Tooltip("Velocidad minima y maxima con la que sale cada particula.")]
    public Vector2 speedRange = new Vector2(2f, 5f);

    [Tooltip("Tamaño minimo y maximo de cada particula.")]
    public Vector2 sizeRange = new Vector2(0.15f, 0.4f);

    [Tooltip("Cuanto tiempo (segundos) vive cada particula antes de desaparecer.")]
    public Vector2 lifetimeRange = new Vector2(0.35f, 0.7f);

    [Tooltip("Cuanto las afecta la gravedad. 0 = flotan sin caer, 1 = caen como un objeto real.")]
    [Range(0f, 2f)] public float gravityModifier = 0.4f;

    [Tooltip("Rotacion inicial al azar (en grados), para que no salgan todas mirando para el mismo lado.")]
    [Range(0f, 180f)] public float startRotationRandomDegrees = 180f;

    [Header("Sonido (opcional)")]
    [Tooltip("Si le ponés un audio, se reproduce junto con el efecto (por el bus de SFX central).")]
    public AudioClip sfx;

    private static Gradient DefaultGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }
}
