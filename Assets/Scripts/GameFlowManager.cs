using UnityEngine;

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

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Jueguitos;

    // Multiplicador "congelado" en el momento de apretar el boton de patear,
    // para que el calculo de puntaje y de fuerza usen siempre el mismo valor.
    private float frozenMultiplier = 1f;

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

        int finalScore = Mathf.RoundToInt(baseScore * frozenMultiplier);

        if (gameManager != null)
        {
            gameManager.AddPoints(finalScore);
        }

        Debug.Log($"Gol resuelto. Base: {baseScore} x Multiplicador: {frozenMultiplier} = {finalScore}");
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