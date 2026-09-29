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

    [Header("Loop: seguir jugando o terminar")]
    [Tooltip("Rigidbody/controlador de la pelota, para devolverla a su posicion inicial si el jugador sigue jugando.")]
    [SerializeField] private BallController ball;

    [Tooltip("Se dispara justo despues de convertir un gol, para mostrar el panel de '¿Seguir jugando?'. Conectalo a SetActive(true) del panel.")]
    public UnityEvent onGoalFollowUp;

    [Tooltip("Escena a cargar cuando el jugador elige terminar y volver al menu principal.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Loop: tiempo de gracia al continuar")]
    [Tooltip("Segundos que el tiempo queda congelado al volver a la fase de Jueguitos despues de elegir 'Seguir jugando', para poder ubicarse antes de que la pelota se mueva.")]
    [SerializeField] private float juggleGraceSeconds = 2f;

    [Tooltip("Cuanto se reduce ese tiempo de gracia en cada loop siguiente (dificulta la partida a medida que se sigue jugando).")]
    [SerializeField] private float graceDecreasePerLoop = 0.3f;

    [Tooltip("Piso minimo del tiempo de gracia: nunca baja de este valor.")]
    [SerializeField] private float minGraceSeconds = 0.4f;

    [Header("Loop: dificultad progresiva")]
    [Tooltip("Dron/obstaculo cuya velocidad sube en cada loop. Opcional: si queda vacio, no hace nada.")]
    [SerializeField] private DroneKeeper droneKeeper;

    [Tooltip("Cuanto aumenta la velocidad del dron en cada loop siguiente.")]
    [SerializeField] private float droneSpeedIncreasePerLoop = 0.6f;

    [Tooltip("Velocidad maxima a la que puede llegar el dron, para que la partida siga siendo jugable.")]
    [SerializeField] private float droneMaxSpeed = 8f;

    [Header("Deteccion de tiro errado")]
    [Tooltip("Segundos maximos que se espera un gol despues del disparo. Si no entra, cuenta como fallo.")]
    [SerializeField] private float missTimeoutSeconds = 4f;

    [Tooltip("Ventana corta (segundos) para permitir un pique que entre despues de que la pelota toca el piso.")]
    [SerializeField] private float groundSettleSeconds = 1.2f;

    [Tooltip("Ventana corta (segundos) para permitir que, despues de chocar un ShotBlocker (dron, arquero), el rebote siga y entre igual al arco.")]
    [SerializeField] private float blockSettleSeconds = 0.5f;

    [Tooltip("Velocidad (u/s) por debajo de la cual se considera que la pelota se detuvo.")]
    [SerializeField] private float restSpeed = 0.4f;

    [Tooltip("Cuanto tiempo tiene que estar detenida la pelota para dar el tiro por resuelto.")]
    [SerializeField] private float restDuration = 0.4f;

    [Tooltip("Particulas cuando el tiro se erra (no entro al arco). Crealo con Assets > Create > K.I.C.K > Impact Effect.")]
    [SerializeField] private ImpactEffect missEffect;

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

    // Tiempo de gracia restante para el proximo loop (va bajando de a poco).
    private float currentGraceSeconds;
    private Coroutine graceRoutine;
    // Mientras esta activo, tocar el piso en fase Jueguitos NO cuenta como
    // fallo. Es la red de seguridad real: aunque el freeze de Time.timeScale
    // falle o quede un colision en cola, un toque de piso durante la gracia
    // nunca termina la partida.
    private bool graceActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentGraceSeconds = juggleGraceSeconds;
    }

    private void Start()
    {
        EnterJueguitosPhase();
    }

    private void Update()
    {
        if (CurrentPhase != GamePhase.Jueguitos) return;

        // PC: Enter = patear (igual que el boton "Patear", solo si la barra llego al umbral).
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnShootButtonPressed();
            return;
        }

        // Barra llena (todos los jueguitos hechos) -> pasa solo a la fase de disparo.
        if (powerBar != null && powerBar.Power01 >= 1f)
        {
            OnShootButtonPressed();
        }
    }

    private void EnterJueguitosPhase()
    {
        CurrentPhase = GamePhase.Jueguitos;

        StopShotWatch();
        shotResolved = false;
        shotTaken = false;

        if (graceRoutine != null)
        {
            StopCoroutine(graceRoutine);
            graceRoutine = null;
        }
        graceActive = false;

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
    // Loop: seguir jugando o terminar tras un gol
    // ---------------------------------------------------------------

    /// <summary>
    /// Lo llama GoalDetector justo despues de sumar los puntos del gol.
    /// En vez de cortar la partida de una, pausa el juego y deja la decision
    /// en manos del jugador: seguir jugando (loop, el puntaje sigue sumando)
    /// o terminar aca con el puntaje actual.
    /// </summary>
    public void PresentGoalFollowUp()
    {
        if (CurrentPhase != GamePhase.Aiming) return;

        CurrentPhase = GamePhase.Resolved;

        if (swipeShooter != null) swipeShooter.enabled = false;

        StartCoroutine(FreezeAndShowGoalFollowUp());
    }

    /// <summary>
    /// Le da un instante (tiempo real) antes de congelar y mostrar el panel,
    /// para que el screen shake del gol se alcance a ver contra la escena en
    /// movimiento en vez de quedar tapado por el freeze/panel del mismo frame.
    /// </summary>
    private IEnumerator FreezeAndShowGoalFollowUp()
    {
        yield return new WaitForSecondsRealtime(0.12f);

        // Pausamos como en un Game Over: la pelota y la camara quedan
        // congeladas mientras el jugador decide.
        Time.timeScale = 0f;

        onGoalFollowUp?.Invoke();
    }

    /// <summary>
    /// Conectar al boton "Seguir jugando" del panel post-gol. Reinicia la
    /// pelota y las zonas de gol, y vuelve a la fase de Jueguitos SIN tocar
    /// el puntaje acumulado (sigue sumando en el proximo loop).
    /// </summary>
    public void ContinuePlaying()
    {
        if (CurrentPhase != GamePhase.Resolved) return;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (swipeShooter != null) swipeShooter.ResetShot();
        if (ball != null) ball.ResetToStart();

        foreach (GoalDetector zone in FindObjectsByType<GoalDetector>(FindObjectsInactive.Include))
        {
            zone.ResetZone();
        }

        EnterJueguitosPhase();
        StartGraceWindow();

        // El proximo loop tiene un poco menos de tiempo de gracia (nunca por
        // debajo del piso configurado).
        currentGraceSeconds = Mathf.Max(minGraceSeconds, currentGraceSeconds - graceDecreasePerLoop);

        // ...y el dron (si hay uno asignado) se mueve un poco mas rapido,
        // hasta el tope configurado.
        if (droneKeeper != null)
        {
            droneKeeper.Speed = Mathf.Min(droneKeeper.Speed + droneSpeedIncreasePerLoop, droneMaxSpeed);
        }
    }

    /// <summary>
    /// Congela el tiempo por 'currentGraceSeconds' (segundos reales) para que
    /// el jugador pueda ubicarse antes de que la pelota empiece a caer/moverse.
    /// La KickZone queda activa todo el tiempo (se puede tocar apenas se
    /// pueda) y, ademas, mientras dure la gracia un toque de piso NO cuenta
    /// como fallo: es la red de seguridad para que nunca se pierda la
    /// partida por esto.
    /// </summary>
    private void StartGraceWindow()
    {
        if (graceRoutine != null)
        {
            StopCoroutine(graceRoutine);
            graceRoutine = null;
        }

        graceActive = currentGraceSeconds > 0f;
        if (!graceActive) return;

        Time.timeScale = 0f;
        graceRoutine = StartCoroutine(EndGraceAfter(currentGraceSeconds));
    }

    private IEnumerator EndGraceAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        graceRoutine = null;
        graceActive = false;

        if (CurrentPhase == GamePhase.Jueguitos)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Conectar al boton "Menu principal" del panel post-gol. Guarda el
    /// puntaje acumulado hasta este momento (high score + tabla de puntajes,
    /// via GameManager.GameOver) y vuelve directo al menu principal, igual
    /// que el boton "Menu Principal" del panel de Game Over normal.
    /// </summary>
    public void EndRun()
    {
        if (CurrentPhase != GamePhase.Resolved) return;

        if (gameManager != null)
        {
            gameManager.GameOver();
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
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

    /// <summary>
    /// Lo llama un ShotBlocker (dron, y a futuro un arquero) cuando la
    /// pelota lo choca en pleno vuelo. No corta el tiro de una: le da una
    /// ventana corta por si el rebote sigue y entra igual al arco (ahi
    /// GoalDetector cancela esto con NotifyGoalResolved). Si no entra en
    /// ese tiempo, recien ahi se resuelve como fallo.
    /// </summary>
    public void NotifyShotBlocked()
    {
        if (!shotTaken || shotResolved || CurrentPhase != GamePhase.Aiming) return;

        // Golpe de camara chico, inmediato, solo como feedback del impacto
        // -- el resultado (gol o fallo) todavia no esta definido.
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.08f, 0.15f);
        }

        StopShotWatch();
        shotWatch = StartCoroutine(MissAfter(blockSettleSeconds));
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

        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.2f, 0.4f);
        }
        Handheld.Vibrate();

        if (missEffect != null && ball != null)
        {
            ImpactEffectPlayer.Ensure().Play(missEffect, ball.transform.position);
        }

        onShotMissed?.Invoke();

        StartCoroutine(DelayedGameOver());
    }

    /// <summary>
    /// Mismo motivo que FreezeAndShowGoalFollowUp: darle al screen shake un
    /// instante antes de que el freeze del Game Over lo tape.
    /// </summary>
    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSecondsRealtime(0.12f);

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
    /// es parte normal del tiro y no debe cortar la partida. Durante la
    /// ventana de gracia al reanudar un loop tampoco cuenta como fallo.
    /// </summary>
    public bool ShouldEndOnGroundHit()
    {
        return CurrentPhase == GamePhase.Jueguitos && !graceActive;
    }
}