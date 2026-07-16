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

    [Tooltip("Multiplica la intensidad de la curva")]
    [SerializeField] private float curveStrength = 1f;

    [Tooltip("Si esta activo, la curva se va apagando con el tiempo en vez de ser constante")]
    [SerializeField] private bool fadeOverTime = true;

    private Rigidbody rb;
    private Vector3 curveDirection; // direccion lateral de la curva (en espacio local del tiro)
    private float curveAmount;      // -1 a 1, viene del swipe.x
    private float timer;
    private bool isCurving;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Activa el efecto de curva. Llamar justo despues de aplicarle el
    /// impulso principal a la pelota en el momento del disparo.
    /// </summary>
    /// <param name="normalizedCurveAmount">
    /// Valor entre -1 (curva a la izquierda) y 1 (curva a la derecha).
    /// Normalmente viene de swipe.x normalizado.
    /// </param>
    /// <param name="shotForwardDirection">
    /// Direccion hacia adelante del tiro, para calcular la lateral perpendicular.
    /// </param>
    public void ApplyCurve(float normalizedCurveAmount, Vector3 shotForwardDirection)
    {
        curveAmount = Mathf.Clamp(normalizedCurveAmount, -1f, 1f);

        // Lateral perpendicular a la direccion del tiro (en el plano horizontal)
        curveDirection = Vector3.Cross(Vector3.up, shotForwardDirection.normalized);

        timer = 0f;
        isCurving = curveAmount != 0f;
    }

    private void FixedUpdate()
    {
        if (!isCurving) return;

        timer += Time.fixedDeltaTime;

        if (timer >= curveDuration)
        {
            isCurving = false;
            return;
        }

        float intensity = curveAmount * curveStrength;

        if (fadeOverTime)
        {
            // La curva pega mas fuerte al principio del vuelo y se atenua
            float fade = 1f - (timer / curveDuration);
            intensity *= fade;
        }

        rb.AddForce(curveDirection * intensity, ForceMode.Force);
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