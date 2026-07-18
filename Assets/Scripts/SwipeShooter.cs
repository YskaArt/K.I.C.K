using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flujo de disparo en dos pasos:
///
/// 1. TAP: el jugador toca la pantalla. Se hace un raycast desde la camara
///    hacia ese punto de pantalla, y el punto 3D exacto donde pega contra el
///    arco queda guardado como objetivo del tiro. Se muestra un marcador ahi.
///
/// 2. SWIPE: una vez elegido el punto, el jugador arrastra el dedo para
///    definir la potencia (largo del swipe) y la curva (forma del trazo).
///    El punto de destino YA esta fijo desde el paso 1, asi que el tiro
///    coincide siempre con lo que se toco -- no hay formula aproximada de
///    por medio.
///
/// La curva sigue funcionando igual que antes: si el trazo se curva, el
/// punto de impacto se desplaza levemente hacia el lado contrario al armar
/// el tiro, y despues BallCurveEffect lo trae de vuelta durante el vuelo
/// (efecto "banana"), pero ahora esa compensacion es alrededor del punto
/// real tocado, no de un centro aproximado del arco.
/// </summary>
public class SwipeShooter : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Rigidbody de la pelota que vamos a disparar")]
    [SerializeField] private Rigidbody ball;

    [Tooltip("Dibuja el trazo del swipe en pantalla. Si esta vacio se busca en este mismo GameObject")]
    [SerializeField] private SwipeTrailRenderer trailRenderer;

    [Header("Paso 1: Tap + Raycast (elegir punto del arco)")]
    [Tooltip("Camara desde la que se hace el raycast al tocar la pantalla. Si esta vacio usa Camera.main")]
    [SerializeField] private Camera aimCamera;

    [Tooltip("Capas validas para elegir el punto de disparo. Poné aca SOLO la capa del arco (crea una capa 'Goal' y asignasela al collider del arco)")]
    [SerializeField] private LayerMask goalLayerMask = ~0;

    [Tooltip("Distancia maxima del raycast, en unidades del mundo")]
    [SerializeField] private float aimRaycastDistance = 100f;

    [Tooltip("Marcador visual que se mueve al punto elegido (opcional). Cualquier GameObject, por ejemplo una esferita o un decal")]
    [SerializeField] private GameObject aimMarker;

    [Header("Paso 2: Swipe (potencia y curva)")]
    [Tooltip("Distancia minima en pixeles para que el swipe cuente como valido")]
    [SerializeField] private float minSwipeDistance = 50f;

    [Header("Tiempo de vuelo (parabola)")]
    [Tooltip("Duracion del vuelo con un swipe muy corto/lento (tiro mas arqueado)")]
    [SerializeField] private float maxFlightTime = 1.1f;

    [Tooltip("Duracion del vuelo con un swipe muy largo/rapido (tiro mas directo y plano)")]
    [SerializeField] private float minFlightTime = 0.55f;

    [Tooltip("Largo de swipe (en pixeles) que ya se considera 'swipe maximo' para el calculo de potencia")]
    [SerializeField] private float maxSwipeReferenceDistance = 500f;

    [Header("Curva del tiro")]
    [Tooltip("Que porcentaje del largo total del swipe equivale a curva maxima (0.3 = 30%). Relativo al gesto, no a pixeles fijos.")]
    [SerializeField] private float maxCurveRatio = 0.3f;

    [Tooltip("Cuanto se corre el punto de impacto (en unidades del mundo) cuando hay curva maxima. La curva despues trae la pelota de vuelta durante el vuelo.")]
    [SerializeField] private float curveCompensationDistance = 1.2f;

    [Header("Multiplicador de potencia (Jueguitos)")]
    [Tooltip("Achica el tiempo de vuelo (tiro mas directo/potente) sin perder precision. Lo fija GameFlowManager segun la barra de jueguitos")]
    [SerializeField] private float powerMultiplier = 1f;

    // Estado interno
    private bool aimPointSelected;
    private Vector3 selectedAimPoint;

    private Vector2 startPos;
    private bool isDragging;
    private bool shotFired;

    private void Awake()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<SwipeTrailRenderer>();
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    /// <summary>
    /// Se ejecuta cada vez que el script pasa de desactivado a activado
    /// (GameFlowManager lo activa al entrar a la fase de disparo). Arranca
    /// la ronda siempre pidiendo un tap nuevo para elegir el punto.
    /// </summary>
    private void OnEnable()
    {
        aimPointSelected = false;
        isDragging = false;
        shotFired = false;

        if (aimMarker != null)
        {
            aimMarker.SetActive(false);
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
        if (!aimPointSelected)
        {
            HandleAimTapInput();
        }
        else
        {
            HandleSwipeInput();
        }
    }

    // ---------------------------------------------------------------
    // PASO 1: Tap + Raycast
    // ---------------------------------------------------------------

    private void HandleAimTapInput()
    {
        if (shotFired) return;

        bool tapped = false;
        Vector2 tapPos = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapped = true;
            tapPos = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
            tapPos = Input.mousePosition;
        }

        if (!tapped) return;

        TrySelectAimPoint(tapPos);
    }

    private void TrySelectAimPoint(Vector2 screenPos)
    {
        if (aimCamera == null)
        {
            Debug.LogWarning("SwipeShooter: no hay camara asignada para el raycast del tap (Aim Camera / Camera.main).");
            return;
        }

        Ray ray = aimCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, aimRaycastDistance, goalLayerMask))
        {
            selectedAimPoint = hit.point;
            aimPointSelected = true;

            if (aimMarker != null)
            {
                aimMarker.SetActive(true);
                aimMarker.transform.position = selectedAimPoint;
            }

            Debug.Log($"Punto de disparo elegido: {selectedAimPoint}");
        }
        else
        {
            Debug.Log("SwipeShooter: el tap no pego contra el arco (revisa el Goal Layer Mask). Proba de nuevo.");
        }
    }

    // ---------------------------------------------------------------
    // PASO 2: Swipe (potencia y curva)
    // ---------------------------------------------------------------

    private void HandleSwipeInput()
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
        if (shotFired) return;

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
            // swipe muy corto, no dispara (el punto elegido se mantiene, puede reintentar el swipe)
            return;
        }

        float curveAmount = CalculateCurveAmount(path, startPos, straightVector);

        Debug.Log($"[Diagnostico curva] Puntos del trazo: {path.Count}, Curva detectada: {curveAmount:F2} (0 = recto, +1 = maxima a la derecha, -1 = maxima a la izquierda)");

        Shoot(straightVector, curveAmount);
    }

    /// <summary>
    /// Mide cuanto y hacia que lado se "arquea" el trazo respecto a la
    /// linea recta entre el primer y el ultimo punto (integrando el area
    /// entre el trazo y esa linea recta, para que una "C" completa de un
    /// resultado estable y no solo dependa de un pico de ruido puntual).
    /// </summary>
    private float CalculateCurveAmount(List<Vector2> path, Vector2 start, Vector2 straightVector)
    {
        if (path.Count < 3 || straightVector.magnitude < 1f)
        {
            return 0f;
        }

        Vector2 straightDir = straightVector.normalized;
        Vector2 perp = new Vector2(-straightDir.y, straightDir.x);

        float accumulatedArea = 0f;
        float totalLength = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 pointA = path[i];
            Vector2 pointB = path[i + 1];

            float offsetA = Vector2.Dot(pointA - start, perp);
            float offsetB = Vector2.Dot(pointB - start, perp);

            float segmentLength = Vector2.Distance(pointA, pointB);

            accumulatedArea += (offsetA + offsetB) * 0.5f * segmentLength;
            totalLength += segmentLength;
        }

        if (totalLength < 1f)
        {
            return 0f;
        }

        float averageOffset = accumulatedArea / totalLength;
        float curveRatio = averageOffset / straightVector.magnitude;

        return Mathf.Clamp(curveRatio / maxCurveRatio, -1f, 1f);
    }

    private void Shoot(Vector2 straightVector, float curveAmount)
    {
        if (ball == null)
        {
            Debug.LogWarning("SwipeShooter: falta asignar el Rigidbody de la pelota.");
            return;
        }

        // Direccion horizontal real desde la pelota al punto elegido con el
        // tap. No depende de ejes fijos del mundo ni de como este rotada la
        // camara: el punto ya es exacto, aca solo armamos una base
        // izquierda/derecha para poder desplazarlo con la curva.
        Vector3 flatToTarget = selectedAimPoint - ball.position;
        flatToTarget.y = 0f;

        if (flatToTarget.sqrMagnitude < 0.0001f)
        {
            flatToTarget = Vector3.forward;
        }

        Vector3 shotForward = flatToTarget.normalized;
        Vector3 shotRight = Vector3.Cross(Vector3.up, shotForward).normalized;

        // Compensacion de curva: si el tiro va a curvar hacia un lado durante
        // el vuelo, apuntamos primero un poco hacia el lado contrario del
        // punto real, para que la curva lo termine trayendo justo ahi.
        // Dibujar una "C" hace que la pelota arranque desviada y se doble
        // hacia el punto que tocaste. Clampeado para que no se vaya de mambo
        // con una curva exagerada.
        float curveOffset = Mathf.Clamp(
            curveAmount * curveCompensationDistance,
            -curveCompensationDistance,
            curveCompensationDistance);

        Vector3 compensatedTarget = selectedAimPoint - shotRight * curveOffset;

        // Potencia del swipe (0 a 1) define que tan rapido/directo es el tiro
        float swipeMagnitude01 = Mathf.Clamp01(straightVector.magnitude / maxSwipeReferenceDistance);
        float baseFlightTime = Mathf.Lerp(maxFlightTime, minFlightTime, swipeMagnitude01);

        // El multiplicador de la barra de jueguitos achica el tiempo de
        // vuelo (tiro mas potente) sin perder precision, porque seguimos
        // apuntando al mismo punto exacto.
        float flightTime = Mathf.Max(0.2f, baseFlightTime / powerMultiplier);

        Vector3 launchVelocity = CalculateLaunchVelocity(ball.position, compensatedTarget, flightTime);

        ball.linearVelocity = launchVelocity;

        BallCurveEffect curveEffect = ball.GetComponent<BallCurveEffect>();
        if (curveEffect != null)
        {
            curveEffect.ApplyCurve(curveAmount, shotForward, flightTime);
        }

        shotFired = true;

        if (aimMarker != null)
        {
            aimMarker.SetActive(false);
        }

        Debug.Log($"Tiro disparado hacia {selectedAimPoint}. Velocidad: {launchVelocity}, Tiempo de vuelo: {flightTime:F2}s, Curva: {curveAmount:F2}");
    }

    /// <summary>
    /// Resuelve analiticamente la velocidad inicial necesaria para que un
    /// proyectil bajo gravedad, partiendo de "origin", llegue exactamente
    /// a "target" en "flightTime" segundos.
    /// </summary>
    private Vector3 CalculateLaunchVelocity(Vector3 origin, Vector3 target, float flightTime)
    {
        Vector3 displacement = target - origin;
        Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);

        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 velocityXZ = displacementXZ / flightTime;
        float velocityY = (displacement.y + 0.5f * gravity * flightTime * flightTime) / flightTime;

        return velocityXZ + Vector3.up * velocityY;
    }

    /// <summary>
    /// Llamar esto desde un boton de "Reintentar" para poder tirar de nuevo.
    /// Vuelve a pedir un tap para elegir punto (no reusa el anterior).
    /// </summary>
    public void ResetShot()
    {
        shotFired = false;
        isDragging = false;
        aimPointSelected = false;

        if (aimMarker != null)
        {
            aimMarker.SetActive(false);
        }

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