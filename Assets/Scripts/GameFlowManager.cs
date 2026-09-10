using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Orquesta el flujo completo de una ronda:
/// Fase 1 (Jueguitos) -> Fase 2/3 (Apuntado + Disparo) -> Resuelto.
/// Es el punto central que conecta el minijuego de jueguitos (KickZone,
/// JueguitosPowerBar) con el disparo (SwipeShooter) y el puntaje (GameManager).
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public enum GamePhase { Jueguitos, Aiming, Resolved }

    public static GameFlowManager Instance { get; private set; }

    [Header("Fase Jueguitos")]
    [SerializeField] private KickZone kickZone;
    [SerializeField] private JueguitosPowerBar powerBar;

    [Header("Fase Disparo")]
    [SerializeField] private SwipeShooter swipeShooter;

    [Header("Puntaje")]
    [SerializeField] private GameManager gameManager;

    [Header("Deteccion de tiro errado")]
    [Tooltip("Segundos maximos que se espera un gol despues del disparo. Si no entra, cuenta como fallo.")]
    [SerializeField] private float missTimeoutSeconds = 4f;

    [Tooltip("Ventana corta (segundos) para permitir un pique que entre despues de que la pelota toca el piso.")]
    [SerializeField] private float groundSettleSeconds = 1.2f;

    [Tooltip("Velocidad (u/s) por debajo de la cual se considera que la pelota se detuvo.")]
    [SerializeField] private float restSpeed = 0.4f;

    [Tooltip("Cuanto tiempo tiene que estar detenida la pelota para dar el tiro por resuelto.")]
    [SerializeField] private float restDuration = 0.4f;

    [Tooltip("Se dispara cuando la pelota NO entra al arco (tiro errado).")]
    public UnityEvent onShotMissed;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Jueguitos;

    // Multiplicador "congelado" en el momento de apretar el boton de patear,
    // para que el calculo de puntaje y de fuerza usen siempre el mismo valor.
    private float frozenMultiplier = 1f;

    // Vigilancia del resultado del tiro (gol vs. fallo).
    private Coroutine shotWatch;
    private bool shotResolved;
    // Recien despues de que el jugador patea de verdad se puede declarar fallo.
    // Mientras apunta/hace el swipe la pelota puede tocar el piso sin que eso
    // cuente como tiro errado.
    private bool shotTaken;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnterJueguitosPhase();
    }

    private void EnterJueguitosPhase()
    {
        CurrentPhase = GamePhase.Jueguitos;

        StopShotWatch();
        shotResolved = false;
        shotTaken = false;

        if (kickZone != null) kickZone.enabled = true;
        if (swipeShooter != null) swipeShooter.enabled = false;
        if (powerBar != null) powerBar.ResetPower();

        if (CameraController.Instance != null)
        {
            CameraController.Instance.SwitchToJueguitosView();
        }
    }

    /// <summary>
    /// Conectar al OnClick del boton de "Patear" en el Inspector.
    /// Solo funciona si ya se llego al umbral minimo de la barra.
    /// </summary>
    public void OnShootButtonPressed()
    {
        if (CurrentPhase != GamePhase.Jueguitos) return;
        if (powerBar != null && !powerBar.CanShoot) return;

        frozenMultiplier = powerBar != null ? powerBar.ShotMultiplier : 1f;

        CurrentPhase = GamePhase.Aiming;

        if (kickZone != null) kickZone.enabled = false;

        if (swipeShooter != null)
        {
            swipeShooter.SetPowerMultiplier(frozenMultiplier);
            swipeShooter.enabled = true;
        }

        if (CameraController.Instance != null)
        {
            CameraController.Instance.SwitchToShootView();
        }

        Debug.Log($"Pasando a fase de disparo. Multiplicador congelado: {frozenMultiplier}");
    }

    /// <summary>
    /// Conectar al evento "On Goal Scored" de cada GoalDetector del arco.
    /// Aplica el multiplicador de la barra de jueguitos al puntaje base de la zona.
    /// </summary>
    public void OnGoalScored(int baseScore)
    {
        if (CurrentPhase != GamePhase.Aiming) return;

        CurrentPhase = GamePhase.Resolved;
        NotifyGoalResolved();

        int finalScore = Mathf.RoundToInt(baseScore * frozenMultiplier);

        if (gameManager != null)
        {
            gameManager.AddPoints(finalScore);
        }

        Debug.Log($"Gol resuelto. Base: {baseScore} x Multiplicador: {frozenMultiplier} = {finalScore}");
    }

    // ---------------------------------------------------------------
    // Resultado del tiro: gol o fallo
    // ---------------------------------------------------------------

    /// <summary>
    /// Lo llama SwipeShooter justo despues de patear. Arranca la vigilancia
    /// que va a detectar si el tiro entra al arco o se erra.
    /// </summary>
    public void NotifyShotTaken(Rigidbody ballBody)
    {
        if (CurrentPhase != GamePhase.Aiming) return;

        shotTaken = true;
        shotResolved = false;
        StopShotWatch();
        shotWatch = StartCoroutine(WatchShotOutcome(ballBody));
    }

    /// <summary>
    /// Lo llama GoalDetector cuando la pelota entra a una zona de gol.
    /// Cancela la vigilancia para que no se cuente como fallo.
    /// </summary>
    public void NotifyGoalResolved()
    {
        shotResolved = true;
        StopShotWatch();
    }

    /// <summary>
    /// Lo llama Ground cuando la pelota toca el piso ya en fase de disparo.
    /// Le da una ventana corta a un posible pique que entre; si no, es fallo.
    /// </summary>
    public void NotifyBallHitGround()
    {
        // Solo cuenta si el jugador YA pateo. Antes del disparo (mientras
        // apunta y hace el swipe) la pelota puede caer al piso sin penalidad.
        if (!shotTaken || shotResolved || CurrentPhase != GamePhase.Aiming) return;

        StopShotWatch();
        shotWatch = StartCoroutine(MissAfter(groundSettleSeconds));
    }

    private IEnumerator WatchShotOutcome(Rigidbody ballBody)
    {
        float elapsed = 0f;
        float restTimer = 0f;

        while (elapsed < missTimeoutSeconds)
        {
            if (shotResolved) yield break;

            elapsed += Time.unscaledDeltaTime;

            if (ballBody != null &&
                ballBody.linearVelocity.sqrMagnitude < restSpeed * restSpeed)
            {
                restTimer += Time.unscaledDeltaTime;
                if (restTimer >= restDuration) break; // la pelota se detuvo sin entrar
            }
            else
            {
                restTimer = 0f;
            }

            yield return null;
        }

        ResolveMiss();
    }

    private IEnumerator MissAfter(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (shotResolved) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ResolveMiss();
    }

    private void ResolveMiss()
    {
        if (!shotTaken || shotResolved || CurrentPhase != GamePhase.Aiming) return;

        shotResolved = true;
        shotWatch = null;
        CurrentPhase = GamePhase.Resolved;

        Debug.Log("Tiro errado: la pelota no entro al arco.");

        onShotMissed?.Invoke();

        if (gameManager != null)
        {
            gameManager.GameOver();
        }
    }

    private void StopShotWatch()
    {
        if (shotWatch != null)
        {
            StopCoroutine(shotWatch);
            shotWatch = null;
        }
    }

    /// <summary>
    /// Consulta de Ground.cs: si seguimos en Jueguitos, tocar el piso es un
    /// fallo (Game Over). Si ya se paso a Aiming/Resolved, la pelota cayendo
    /// es parte normal del tiro y no debe cortar la partida.
    /// </summary>
    public bool ShouldEndOnGroundHit()
    {
        return CurrentPhase == GamePhase.Jueguitos;
    }
}