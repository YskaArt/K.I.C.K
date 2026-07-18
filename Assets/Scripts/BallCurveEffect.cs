using UnityEngine;

/// <summary>
/// Simula el efecto de "comba" (curva) en la trayectoria de la pelota.
/// A diferencia de un simple impulso diagonal, esto aplica una fuerza lateral
/// continua mientras la pelota esta en el aire, haciendo que se desvie de
/// forma curva en vez de en linea recta. Va sobre la misma pelota.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallCurveEffect : MonoBehaviour
{
    [Header("Configuracion de la curva")]
    [Tooltip("Cuanto dura el efecto de curva despues del disparo (segundos)")]
    [SerializeField] private float curveDuration = 0.8f;

    [Tooltip("Multiplica la intensidad de la curva (aceleracion lateral en m/s^2, independiente de la masa de la pelota)")]
    [SerializeField] private float curveStrength = 8f;

    [Tooltip("Si esta activo, la curva se va apagando con el tiempo en vez de ser constante")]
    [SerializeField] private bool fadeOverTime = true;

    private Rigidbody rb;
    private Vector3 curveDirection; // direccion lateral de la curva (en espacio local del tiro)
    private float curveAmount;      // -1 a 1, viene del swipe.x
    private float activeDuration;   // duracion real usada para este tiro (puede venir sincronizada con el flightTime)
    private float timer;
    private bool isCurving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Activa el efecto de curva. Llamar justo despues de fijar la
    /// velocidad inicial de la pelota en el momento del disparo.
    /// </summary>
    /// <param name="normalizedCurveAmount">
    /// Valor entre -1 (curva a la izquierda) y 1 (curva a la derecha).
    /// </param>
    /// <param name="shotForwardDirection">
    /// Direccion hacia adelante del tiro, para calcular la lateral perpendicular.
    /// </param>
    /// <param name="flightDuration">
    /// Duracion esperada del vuelo (en segundos), para sincronizar exactamente
    /// cuando termina la curva con cuando la pelota deberia llegar al arco. Si
    /// no se pasa, se usa la duracion configurada en el Inspector (curveDuration).
    /// </param>
    public void ApplyCurve(float normalizedCurveAmount, Vector3 shotForwardDirection, float? flightDuration = null)
    {
        curveAmount = Mathf.Clamp(normalizedCurveAmount, -1f, 1f);

        // Lateral perpendicular a la direccion del tiro (en el plano horizontal)
        curveDirection = Vector3.Cross(Vector3.up, shotForwardDirection.normalized);

        activeDuration = flightDuration.HasValue ? flightDuration.Value : curveDuration;

        timer = 0f;
        isCurving = curveAmount != 0f;

        Debug.Log($"[Diagnostico curva] BallCurveEffect activado. Cantidad: {curveAmount:F2}, Direccion: {curveDirection}, Duracion: {activeDuration:F2}s, isCurving: {isCurving}");
    }

    private void FixedUpdate()
    {
        if (!isCurving) return;

        timer += Time.fixedDeltaTime;

        if (timer >= activeDuration)
        {
            isCurving = false;
            return;
        }

        float intensity = curveAmount * curveStrength;

        if (fadeOverTime)
        {
            // La curva pega mas fuerte al principio del vuelo y se atenua
            float fade = 1f - (timer / activeDuration);
            intensity *= fade;
        }

        rb.AddForce(curveDirection * intensity, ForceMode.Acceleration);
    }

    /// <summary>
    /// Corta el efecto de curva (por ejemplo, al reiniciar el tiro).
    /// </summary>
    public void StopCurve()
    {
        isCurving = false;
        timer = 0f;
    }
}