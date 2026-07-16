using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta el swipe (touch o mouse, para poder probar en el editor),
/// muestra en pantalla el trazo dibujado, y dispara la pelota usando:
/// - La direccion recta entre inicio y fin del swipe -> apunta el tiro
///   (arriba/abajo = altura, izquierda/derecha = apertura del tiro).
/// - Que tan curvado esta el trazo respecto a esa linea recta -> efecto
///   de curva (banana) durante el vuelo, via BallCurveEffect.
/// A diferencia de la version inicial, ahora se registra el camino
/// completo del dedo (no solo el punto donde soltas) para que la
/// trayectoria coincida mejor con lo que dibujaste.
/// </summary>
public class SwipeShooter : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Rigidbody de la pelota que vamos a disparar")]
    [SerializeField] private Rigidbody ball;

    [Tooltip("Dibuja el trazo del swipe en pantalla. Si esta vacio se busca en este mismo GameObject")]
    [SerializeField] private SwipeTrailRenderer trailRenderer;

    [Header("Configuracion del swipe")]
    [Tooltip("Distancia minima en pixeles para que cuente como swipe valido")]
    [SerializeField] private float minSwipeDistance = 50f;

    [Header("Fuerza del disparo")]
    [Tooltip("Multiplica la velocidad del swipe para convertirla en fuerza")]
    [SerializeField] private float forceMultiplier = 0.02f;

    [Tooltip("Que tanto de la fuerza se convierte en altura (eje Y)")]
    [SerializeField] private float verticalFactor = 1.2f;

    [Tooltip("Que tanto de la fuerza se convierte en apertura lateral del tiro (eje X, apuntado inicial)")]
    [SerializeField] private float horizontalFactor = 0.8f;

    [Tooltip("Que tanto de la fuerza se convierte en profundidad (hacia el arco, eje Z)")]
    [SerializeField] private float forwardFactor = 1.5f;

    [Header("Curva del tiro")]
    [Tooltip("Cuantos pixeles de 'panza' en el trazo equivalen a curva maxima (1.0)")]
    [SerializeField] private float maxCurveScreenOffset = 120f;

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

        float curveAmount = CalculateCurveAmount(path, startPos, straightVector);

        Shoot(straightVector, curveAmount);
    }

    /// <summary>
    /// Mide cuanto se "arquea" el trazo respecto a la linea recta entre el
    /// primer y el ultimo punto. Un trazo perfectamente recto da 0. Un trazo
    /// que se curva hacia la derecha da positivo, hacia la izquierda negativo.
    /// </summary>
    private float CalculateCurveAmount(List<Vector2> path, Vector2 start, Vector2 straightVector)
    {
        if (path.Count < 3 || straightVector.sqrMagnitude < 0.001f)
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

        return Mathf.Clamp(maxOffset / maxCurveScreenOffset, -1f, 1f);
    }

    private void Shoot(Vector2 straightVector, float curveAmount)
    {
        if (ball == null)
        {
            Debug.LogWarning("SwipeShooter: falta asignar el Rigidbody de la pelota.");
            return;
        }

        // Convertimos la linea recta del swipe (pantalla, 2D) en la direccion
        // base del tiro en el mundo (3D):
        // - swipe.y (arriba/abajo) -> altura del tiro
        // - swipe.x (izquierda/derecha) -> apertura lateral del tiro (apuntado)
        // - magnitud total -> potencia hacia adelante (al arco)
        // La curva del trazo (curveAmount) se suma aparte, como efecto durante el vuelo.

        float power = straightVector.magnitude * forceMultiplier;

        Vector3 shootDirection = new Vector3(
            straightVector.x * forceMultiplier * horizontalFactor,
            Mathf.Max(straightVector.y, 0f) * forceMultiplier * verticalFactor,
            power * forwardFactor
        );

        ball.linearVelocity = Vector3.zero; // reset por si venia con velocidad previa
        ball.AddForce(shootDirection, ForceMode.Impulse);

        BallCurveEffect curveEffect = ball.GetComponent<BallCurveEffect>();
        if (curveEffect != null)
        {
            curveEffect.ApplyCurve(curveAmount, shootDirection);
        }

        shotFired = true;

        Debug.Log($"Tiro disparado. Direccion: {shootDirection}, Curva: {curveAmount}, Potencia swipe: {straightVector.magnitude}");
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