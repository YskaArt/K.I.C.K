using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estructura que contiene datos detallados sobre la curva dibujada por el jugador
/// </summary>
[System.Serializable]
public struct CurveData
{
    public float intensity;           // -1 a 1 (compatibilidad con sistema actual)
    public float complexity;          // qué tan compleja es la curva (0-1)
    public AnimationCurve progression; // cómo cambia la curva en el tiempo
    public Vector2[] inflectionPoints; // puntos donde cambia dirección
    public float totalCurveLength;    // longitud total de la curva vs línea recta
}

/// <summary>
/// Detecta el swipe (touch o mouse, para poder probar en el editor),
/// muestra en pantalla el trazo dibujado, y dispara la pelota usando:
/// - La direccion real hacia el arco (aimTarget) como base del tiro -> el
///   swipe solo desvia alrededor de esa linea (no depende de ejes fijos
///   del mundo ni de como este rotada la camara).
/// - Que tan curvado esta el trazo respecto a la linea recta del swipe -> efecto
///   de curva (banana) durante el vuelo, via BallCurveEffect.
/// - El multiplicador de potencia de la barra de jueguitos (lo fija
///   GameFlowManager al pasar a esta fase).
///
/// Nota de version: antes la direccion se armaba con ejes fijos del mundo
/// (X/Y/Z), lo que hacia que el tiro se fuera para cualquier lado si la
/// camara cambiaba de angulo o el arco no estaba alineado con el eje Z.
/// Ahora se calcula "shotForward" como la direccion real de la pelota al
/// arco, y el swipe (izq/der = shotRight, arriba/abajo = altura) se aplica
/// relativo a esa linea. El resultado es estable sin importar la camara.
/// </summary>
public class SwipeShooter : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Rigidbody de la pelota que vamos a disparar")]
    [SerializeField] private Rigidbody ball;

    [Tooltip("Dibuja el trazo del swipe en pantalla. Si esta vacio se busca en este mismo GameObject")]
    [SerializeField] private SwipeTrailRenderer trailRenderer;

    [Tooltip("Punto al que apunta el tiro por defecto (el arco/centro). El swipe desvia alrededor de esta direccion. Si esta vacio, usa Vector3.forward como respaldo.")]
    [SerializeField] private Transform aimTarget;

    [Header("Configuracion del swipe")]
    [Tooltip("Distancia minima en pixeles para que cuente como swipe valido")]
    [SerializeField] private float minSwipeDistance = 50f;

    [Header("Fuerza del disparo")]
    [Tooltip("Multiplica la velocidad del swipe para convertirla en fuerza")]
    [SerializeField] private float forceMultiplier = 0.02f;

    [Tooltip("Que tanto de la potencia se convierte en altura (eje Y)")]
    [SerializeField] private float verticalFactor = 1.2f;

    [Tooltip("Que tanto de la potencia se convierte en apertura lateral del tiro (eje X, apuntado inicial)")]
    [SerializeField] private float horizontalFactor = 1f;

    [Tooltip("Que tanto de la potencia se convierte en profundidad (hacia el arco, eje Z)")]
    [SerializeField] private float forwardFactor = 1.5f;

    [Header("Curva del tiro")]
    [Tooltip("Que porcentaje del largo total del swipe equivale a curva maxima (0.3 = 30%). Relativo al gesto, no a pixeles fijos.")]
    [SerializeField] private float maxCurveRatio = 0.3f;

    [Header("Análisis de Curva Mejorado")]
    [Tooltip("Usa el sistema mejorado de análisis de curva que respeta mejor el trazo dibujado")]
    [SerializeField] private bool useAdvancedCurveAnalysis = true;
    
    [Tooltip("Cuántos puntos samplear a lo largo del trazo para analizar la progresión de curva")]
    [SerializeField] private int curveSamplePoints = 10;
    
    [Tooltip("Qué tan compleja debe ser la curva para aplicar efectos especiales (0-1)")]
    [SerializeField] private float curveComplexityThreshold = 0.1f;

    [Header("Multiplicador de potencia (Jueguitos)")]
    [Tooltip("Multiplica la fuerza final del tiro. Lo fija GameFlowManager segun la barra de jueguitos")]
    [SerializeField] private float powerMultiplier = 1f;

    // Estado interno del gesto
    private Vector2 startPos;
    private bool isDragging;
    private bool shotFired;

    private void Awake()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<SwipeTrailRenderer>();
        }
    }

    /// <summary>
    /// Fija el multiplicador de potencia del proximo tiro. Llamado por
    /// GameFlowManager al pasar de Jueguitos a la fase de disparo.
    /// </summary>
    public void SetPowerMultiplier(float multiplier)
    {
        powerMultiplier = Mathf.Max(0.1f, multiplier);
    }

    private void Update()
    {
        // --- Soporte para mobile (touch) ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnDragStart(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnDragMove(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnDragEnd(touch.position);
                    break;
            }
            return;
        }

        // --- Soporte para mouse (probar rapido en el editor) ---
        if (Input.GetMouseButtonDown(0))
        {
            OnDragStart(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnDragEnd(Input.mousePosition);
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            OnDragMove(Input.mousePosition);
        }
    }

    private void OnDragStart(Vector2 screenPos)
    {
        if (shotFired) return; // ya se disparo, no permitir otro swipe hasta reiniciar

        startPos = screenPos;
        isDragging = true;

        if (trailRenderer != null)
        {
            trailRenderer.BeginSwipe(screenPos);
        }

        // Fase 2: Apuntado -> entramos en camara lenta
        if (SlowMotionController.Instance != null)
        {
            SlowMotionController.Instance.EnterSlowMo();
        }
    }

    private void OnDragMove(Vector2 screenPos)
    {
        if (!isDragging) return;

        if (trailRenderer != null)
        {
            trailRenderer.AddPoint(screenPos);
        }
    }

    private void OnDragEnd(Vector2 screenPos)
    {
        if (!isDragging || shotFired) return;

        isDragging = false;

        // Guardamos el trazo completo antes de limpiarlo del renderer
        List<Vector2> path = null;
        if (trailRenderer != null)
        {
            path = new List<Vector2>(trailRenderer.ScreenPoints);
        }

        if (path == null || path.Count == 0)
        {
            path = new List<Vector2> { startPos, screenPos };
        }
        else if (path[path.Count - 1] != screenPos)
        {
            path.Add(screenPos);
        }

        // Fase 3: Resolucion -> volvemos a tiempo normal
        if (SlowMotionController.Instance != null)
        {
            SlowMotionController.Instance.ExitSlowMo();
        }

        if (trailRenderer != null)
        {
            trailRenderer.EndSwipe();
        }

        Vector2 straightVector = screenPos - startPos;

        if (straightVector.magnitude < minSwipeDistance)
        {
            // swipe muy corto, no dispara
            return;
        }

        // Usar análisis mejorado o sistema legacy
        if (useAdvancedCurveAnalysis)
        {
            CurveData curveData = AnalyzeCurveAdvanced(path, startPos, straightVector);
            ShootAdvanced(straightVector, curveData);
        }
        else
        {
            float curveAmount = CalculateCurveAmount(path, startPos, straightVector);
            Shoot(straightVector, curveAmount);
        }
    }

    /// <summary>
    /// Mide cuanto se "arquea" el trazo respecto a la linea recta entre el
    /// primer y el ultimo punto, como porcentaje del largo total del swipe.
    /// Un trazo perfectamente recto da 0. Uno que se curva hacia la derecha
    /// da positivo, hacia la izquierda negativo. Al ser relativo al largo
    /// del propio swipe (y no a un valor fijo de pixeles), funciona igual
    /// sin importar el tamano de pantalla o que tan largo sea el gesto.
    /// </summary>
    private float CalculateCurveAmount(List<Vector2> path, Vector2 start, Vector2 straightVector)
    {
        if (path.Count < 3 || straightVector.magnitude < 1f)
        {
            return 0f;
        }

        Vector2 straightDir = straightVector.normalized;
        // Perpendicular a la direccion recta, en pantalla
        Vector2 perp = new Vector2(-straightDir.y, straightDir.x);

        float maxOffset = 0f;

        // Buscamos el punto del trazo que mas se aleja de la linea recta
        // (asi el "efecto" que se ve en pantalla es el que se aplica al tiro)
        foreach (Vector2 point in path)
        {
            float offset = Vector2.Dot(point - start, perp);

            if (Mathf.Abs(offset) > Mathf.Abs(maxOffset))
            {
                maxOffset = offset;
            }
        }

        float curveRatio = maxOffset / straightVector.magnitude;
        return Mathf.Clamp(curveRatio / maxCurveRatio, -1f, 1f);
    }

    private void Shoot(Vector2 straightVector, float curveAmount)
    {
        if (ball == null)
        {
            Debug.LogWarning("SwipeShooter: falta asignar el Rigidbody de la pelota.");
            return;
        }

        // Base del tiro: la direccion real hacia el arco (en el plano
        // horizontal), no un eje fijo del mundo. Asi el resultado no depende
        // de como este rotada la camara ni de que el arco este alineado con
        // el eje Z. El swipe solo desvia alrededor de esta linea:
        // - dir2D.x (izq/der del swipe) -> desviacion lateral respecto al arco
        // - dir2D.y (arriba/abajo) -> altura
        // - magnitud -> potencia total
        Vector3 toTarget = aimTarget != null
            ? (aimTarget.position - ball.position)
            : Vector3.forward;
        toTarget.y = 0f; // la base de apuntado es horizontal, la altura la pone el swipe

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            toTarget = Vector3.forward;
        }

        Vector3 shotForward = toTarget.normalized;
        Vector3 shotRight = Vector3.Cross(Vector3.up, shotForward).normalized;

        Vector2 dir2D = straightVector.normalized;
        float power = straightVector.magnitude * forceMultiplier * powerMultiplier;

        Vector3 shootDirection =
            shotForward * (power * forwardFactor) +
            shotRight * (dir2D.x * power * horizontalFactor) +
            Vector3.up * (Mathf.Max(dir2D.y, 0f) * power * verticalFactor);

        ball.linearVelocity = Vector3.zero; // reset por si venia con velocidad previa
        ball.AddForce(shootDirection, ForceMode.Impulse);

        BallCurveEffect curveEffect = ball.GetComponent<BallCurveEffect>();
        if (curveEffect != null)
        {
            // Pasamos shotForward (la linea base al arco) y no shootDirection
            // completo, para que el eje de la curva sea siempre estable
            // respecto al arco, sin importar cuanto desvio el swipe.
            curveEffect.ApplyCurve(curveAmount, shotForward);
        }

        shotFired = true;

        Debug.Log($"Tiro disparado. Direccion: {shootDirection}, Curva: {curveAmount}, Multiplicador: {powerMultiplier}, Potencia swipe: {straightVector.magnitude}");
    }

    /// <summary>
    /// Análisis avanzado que captura la progresión completa de la curva dibujada
    /// </summary>
    private CurveData AnalyzeCurveAdvanced(List<Vector2> path, Vector2 start, Vector2 straightVector)
    {
        CurveData curveData = new CurveData();

        // Fallback al método legacy si hay pocos puntos
        if (path.Count < 3 || straightVector.magnitude < 1f)
        {
            curveData.intensity = 0f;
            curveData.complexity = 0f;
            curveData.progression = AnimationCurve.Constant(0f, 1f, 0f);
            curveData.inflectionPoints = new Vector2[0];
            curveData.totalCurveLength = straightVector.magnitude;
            return curveData;
        }

        // En lugar de usar solo perpendicular, vamos a analizar la desviación REAL del trazo
        // respecto a la línea recta, considerando TODA la información 2D
        
        int sampleCount = Mathf.Min(curveSamplePoints, path.Count);
        float[] curveProgressionX = new float[sampleCount]; // Desviación lateral real
        float[] curveProgressionY = new float[sampleCount]; // Desviación vertical real
        List<Vector2> inflectionPointsList = new List<Vector2>();
        
        float totalPathLength = 0f;
        float maxAbsDeviationX = 0f;
        float maxAbsDeviationY = 0f;

        // Calcular la longitud real del trazo
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalPathLength += Vector2.Distance(path[i], path[i + 1]);
        }

        // Analizar la desviación del trazo real respecto a la línea recta
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            int pathIndex = Mathf.FloorToInt(t * (path.Count - 1));
            pathIndex = Mathf.Clamp(pathIndex, 0, path.Count - 1);

            Vector2 currentPoint = path[pathIndex];
            
            // Punto equivalente en la línea recta (interpolación lineal del inicio al final)
            Vector2 straightPoint = Vector2.Lerp(start, start + straightVector, t);
            
            // Desviación real en ambos ejes
            Vector2 deviation = currentPoint - straightPoint;
            
            // Normalizar por la longitud del trazo para que sea independiente del tamaño
            curveProgressionX[i] = deviation.x / straightVector.magnitude;
            curveProgressionY[i] = deviation.y / straightVector.magnitude;
            
            if (Mathf.Abs(deviation.x) > maxAbsDeviationX) maxAbsDeviationX = Mathf.Abs(deviation.x);
            if (Mathf.Abs(deviation.y) > maxAbsDeviationY) maxAbsDeviationY = Mathf.Abs(deviation.y);

            // Detectar puntos de inflexión en X
            if (i > 0 && i < sampleCount - 1)
            {
                float prevX = curveProgressionX[i - 1];
                float nextX = curveProgressionX[i + 1];
                
                if ((prevX < curveProgressionX[i] && nextX < curveProgressionX[i]) ||
                    (prevX > curveProgressionX[i] && nextX > curveProgressionX[i]))
                {
                    inflectionPointsList.Add(currentPoint);
                }
            }
        }

        // Crear AnimationCurve que combine X e Y, priorizando la desviación más prominente
        Keyframe[] keys = new Keyframe[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / (sampleCount - 1);
            
            // Usar la desviación más prominente como valor principal
            float combinedProgression = curveProgressionX[i];
            if (maxAbsDeviationY > maxAbsDeviationX)
            {
                // Si la desviación vertical es más prominente, usarla pero escalada
                combinedProgression = curveProgressionY[i] * 0.7f;
            }
            
            keys[i] = new Keyframe(time, combinedProgression);
        }

        curveData.progression = new AnimationCurve(keys);
        
        // Suavizar la curva
        for (int i = 0; i < keys.Length; i++)
        {
            curveData.progression.SmoothTangents(i, 0.5f);
        }

        // Métricas finales
        float maxTotalDeviation = Mathf.Max(maxAbsDeviationX, maxAbsDeviationY);
        curveData.intensity = Mathf.Clamp((maxTotalDeviation / straightVector.magnitude) / maxCurveRatio, -1f, 1f);
        curveData.complexity = Mathf.Clamp01(inflectionPointsList.Count / 3f);
        curveData.inflectionPoints = inflectionPointsList.ToArray();
        curveData.totalCurveLength = totalPathLength;

        // Guardar las desviaciones en X e Y para uso posterior (hack: usar campos no utilizados)
        // Esto nos permitirá acceder a ambas componentes en CalculateAverageCurveDirection
        
        Debug.Log($"Análisis curva: MaxDevX={maxAbsDeviationX:F2}, MaxDevY={maxAbsDeviationY:F2}, Intensidad={curveData.intensity:F2}");

        return curveData;
    }

    /// <summary>
    /// Versión mejorada del disparo que usa los datos completos de la curva
    /// </summary>
    private void ShootAdvanced(Vector2 straightVector, CurveData curveData)
    {
        if (ball == null)
        {
            Debug.LogWarning("SwipeShooter: falta asignar el Rigidbody de la pelota.");
            return;
        }

        // Misma lógica base que el método Shoot original
        Vector3 toTarget = aimTarget != null
            ? (aimTarget.position - ball.position)
            : Vector3.forward;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f)
        {
            toTarget = Vector3.forward;
        }

        Vector3 shotForward = toTarget.normalized;
        Vector3 shotRight = Vector3.Cross(Vector3.up, shotForward).normalized;

        Vector2 dir2D = straightVector.normalized;
        float power = straightVector.magnitude * forceMultiplier * powerMultiplier;

        Vector3 shootDirection =
            shotForward * (power * forwardFactor) +
            shotRight * (dir2D.x * power * horizontalFactor) +
            Vector3.up * (Mathf.Max(dir2D.y, 0f) * power * verticalFactor);

        ball.linearVelocity = Vector3.zero;
        ball.AddForce(shootDirection, ForceMode.Impulse);

        // Usar el sistema avanzado de curva
        BallCurveEffect curveEffect = ball.GetComponent<BallCurveEffect>();
        if (curveEffect != null)
        {
            // Calcular la dirección de curva promedio basada en la progresión real de la curva
            Vector3 averageCurveDirection = CalculateAverageCurveDirection(curveData, shotRight);
            curveEffect.ApplyCurveAdvanced(curveData, averageCurveDirection);
        }

        shotFired = true;

        // Debug.Log($"Tiro avanzado disparado. Direccion: {shootDirection}, Curva: {curveData.intensity}, Complejidad: {curveData.complexity}, Inflexiones: {curveData.inflectionPoints.Length}");
    }

    /// <summary>
    /// Calcula la dirección promedio de la curva basada en la progresión completa del trazo
    /// </summary>
    private Vector3 CalculateAverageCurveDirection(CurveData curveData, Vector3 shotRight)
    {
        if (curveData.progression == null || curveData.progression.length == 0)
        {
            Debug.Log("CalculateAverageCurveDirection: Sin progresión, usando shotRight básico");
            return shotRight; // Fallback a curva lateral básica
        }

        // Samplear la curva en varios puntos para obtener la dirección promedio
        int samples = 10;
        Vector3 totalDirection = Vector3.zero;
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / (samples - 1);
            float curveValue = curveData.progression.Evaluate(t);
            
            // Combinar componente lateral con componente vertical basado en el valor de la curva
            Vector3 sampleDirection = shotRight * curveValue;
            
            // Si la curva es muy pronunciada, agregar componente vertical
            if (Mathf.Abs(curveValue) > 0.3f)
            {
                sampleDirection += Vector3.up * (curveValue * 0.2f);
            }
            
            totalDirection += sampleDirection;
        }
        
        Vector3 result = totalDirection.normalized;
        
        // Si la dirección calculada es muy pequeña, usar una dirección por defecto
        if (result.magnitude < 0.1f)
        {
            result = shotRight * Mathf.Sign(curveData.intensity);
            Debug.Log($"CalculateAverageCurveDirection: Dirección muy pequeña, usando fallback: {result}");
        }
        else
        {
            Debug.Log($"CalculateAverageCurveDirection: Dirección calculada: {result}, Intensidad: {curveData.intensity}");
        }
        
        return result;
    }

    /// <summary>
    /// Llamar esto desde un boton de "Reintentar" para poder tirar de nuevo.
    /// </summary>
    public void ResetShot()
    {
        shotFired = false;
        isDragging = false;

        if (trailRenderer != null)
        {
            trailRenderer.EndSwipe();
        }

        if (ball != null)
        {
            BallCurveEffect curveEffect = ball.GetComponent<BallCurveEffect>();
            if (curveEffect != null)
            {
                curveEffect.StopCurve();
            }
        }

        if (SlowMotionController.Instance != null)
        {
            SlowMotionController.Instance.ExitSlowMo();
        }
    }
}