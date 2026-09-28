using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Va sobre un Collider (isTrigger = true) que representa una zona de gol
/// dentro del arco. Ponele uno en cada "angulo" del arco con distinto
/// pointValue para premiar la precision (ej: esquinas valen mas que el centro).
/// La pelota debe tener Collider + Rigidbody y el tag "Ball".
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalDetector : MonoBehaviour
{
    [Header("Configuracion de la zona")]
    [Tooltip("Puntos base que otorga esta zona del arco (angulos = mas puntos)")]
    [SerializeField] private int pointValue = 100;

    [Tooltip("Tag que debe tener la pelota para contar como gol")]
    [SerializeField] private string ballTag = "Ball";

    [Tooltip("Evita contar el mismo gol mas de una vez hasta que se reinicie la ronda")]
    [SerializeField] private bool onlyOnce = true;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando esta zona detecta el gol. Pasa el puntaje base de la zona.")]
    public UnityEvent<int> onGoalScored;

    [Header("Efectos")]
    [Tooltip("Particulas que se reproducen en la pelota al convertir. Crealo con Assets > Create > K.I.C.K > Impact Effect.")]
    [SerializeField] private ImpactEffect goalEffect;

    private bool alreadyScored;
    public GameManager manager;
    private void OnTriggerEnter(Collider other)
    {
        if (onlyOnce && alreadyScored) return;

        if (!other.CompareTag(ballTag)) return;

        alreadyScored = true;

        // Feedback inmediato de impacto: sacudida de camara + vibracion (en
        // celular; en editor/PC Handheld.Vibrate() no hace nada).
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.25f, 0.6f);
        }
        Handheld.Vibrate();

        if (goalEffect != null)
        {
            ImpactEffectPlayer.Ensure().Play(goalEffect, other.transform.position);
        }

        // Cancela la vigilancia de "tiro errado" del GameFlowManager: entro.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.NotifyGoalResolved();
        }

        // El multiplicador de los jueguitos se suma aca cuando este listo
        // ese sistema; por ahora el puntaje final es el valor base de la zona.
        int finalScore = pointValue;
        manager.AddPoints(finalScore);

        // En vez de cortar la partida de una, el jugador elige si sigue
        // jugando (el puntaje sigue sumando en cada loop) o termina aca.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.PresentGoalFollowUp();
        }
        else
        {
            manager.GameOver();
        }
    }

    /// <summary>
    /// Llamar al reiniciar la ronda (junto con ResetShot del SwipeShooter).
    /// </summary>
    public void ResetZone()
    {
        alreadyScored = false;
    }
}
