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
    [SerializeField] private float curveStrength = 50f;

    [Tooltip("Si esta activo, la curva se va apagando con el tiempo en vez de ser constante")]
    [SerializeField] private bool fadeOverTime = true;

    [Header("Sistema Avanzado")]
    [Tooltip("Usa el sistema avanzado que respeta la progresión de curva dibujada")]
    [SerializeField] private bool useAdvancedSystem = true;

    private Rigidbody rb;
    private Vector3 curveDirection; // direccion lateral de la curva (en espacio local del tiro)
    private float curveAmount;      // -1 a 1, viene del swipe.x
    private float timer;
    private bool isCurving;

    // Variables para el sistema avanzado
    private bool usingAdvancedCurve;
    private AnimationCurve curveProgression;
    private float curveComplexity;
    private float originalCurveStrength;

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
        usingAdvancedCurve = false; // Método básico
        originalCurveStrength = curveStrength;
    }

    /// <summary>
    /// Versión avanzada que usa los datos completos de la curva dibujada por el jugador.
    /// Aplica la progresión temporal exacta de la curva según el trazo.
    /// </summary>
    /// <param name="curveData">Datos completos de la curva analizada del trazo</param>
    /// <param name="curveDirection3D">Dirección 3D precalculada de la curva (incluye componentes lateral y vertical)</param>
    public void ApplyCurveAdvanced(CurveData curveData, Vector3 curveDirection3D)
    {
        if (!useAdvancedSystem)
        {
            // Fallback al método básico si el sistema avanzado está desactivado
            Vector3 fallbackDirection = Vector3.Cross(Vector3.up, curveDirection3D).normalized;
            ApplyCurve(curveData.intensity, fallbackDirection);
            return;
        }

        curveAmount = curveData.intensity;
        curveProgression = curveData.progression;
        curveComplexity = curveData.complexity;

        // Usar la dirección 3D precalculada que incluye componentes lateral y vertical
        curveDirection = curveDirection3D.normalized;
        
        // Test temporal: forzar dirección lateral simple para diagnóstico
        if (curveAmount >= 0.8f)
        {
            curveDirection = Vector3.right; // Forzar curva hacia la derecha para test
            Debug.Log("DIAGNÓSTICO: Forzando dirección hacia la derecha para test");
        }

        timer = 0f;
        isCurving = curveAmount != 0f || curveComplexity > 0f;
        usingAdvancedCurve = true;
        originalCurveStrength = curveStrength;

        // Ajustar la fuerza base según la complejidad de la curva
        // Para curvas detectadas correctamente, asegurar fuerza suficiente
        if (curveComplexity > 0.1f)
        {
            curveStrength *= (1f + curveComplexity * 0.5f);
        }
        
        // Para curvas con intensidad alta, asegurar que la fuerza sea suficiente
        if (curveAmount >= 0.8f)
        {
            curveStrength = Mathf.Max(curveStrength, 150f); // Aumentado para diagnóstico
        }
        
        // Asegurar que nunca sea menor a un mínimo razonable
        curveStrength = Mathf.Max(curveStrength, 50f);

        Debug.Log($"BallCurveEffect: Aplicando curva. Intensidad={curveAmount:F2}, Dirección={curveDirection}, Fuerza={curveStrength:F1}, isCurving={isCurving}");
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

        float intensity;

        if (usingAdvancedCurve && curveProgression != null)
        {
            // Sistema avanzado: usar la progresión de curva dibujada por el jugador
            float normalizedTime = timer / curveDuration;
            float curveProgressionValue = curveProgression.Evaluate(normalizedTime);
            
            // La intensidad viene directamente de la progresión de la curva
            intensity = curveProgressionValue * curveStrength;

            // Aplicar fade solo si está activado y es una curva simple
            if (fadeOverTime && curveComplexity < 0.2f)
            {
                float fade = 1f - (normalizedTime * 0.3f); // Fade más sutil para curvas avanzadas
                intensity *= fade;
            }
        }
        else
        {
            // Sistema básico (legacy): curva constante o con fade lineal
            intensity = curveAmount * curveStrength;

            if (fadeOverTime)
            {
                // La curva pega mas fuerte al principio del vuelo y se atenua
                float fade = 1f - (timer / curveDuration);
                intensity *= fade;
            }
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
        
        // Resetear variables del sistema avanzado
        usingAdvancedCurve = false;
        curveProgression = null;
        curveComplexity = 0f;
        
        // Restaurar la fuerza original si fue modificada
        if (originalCurveStrength > 0f)
        {
            curveStrength = originalCurveStrength;
        }
    }
}
