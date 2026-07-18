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

    private bool alreadyScored;
    public GameManager manager;
    private void OnTriggerEnter(Collider other)
    {
        if (onlyOnce && alreadyScored) return;

        if (!other.CompareTag(ballTag)) return;

        alreadyScored = true;

        // El multiplicador de los jueguitos se suma aca cuando este listo
        // ese sistema; por ahora el puntaje final es el valor base de la zona.
        int finalScore = pointValue;
        manager.AddPoints(finalScore);
        manager.GameOver();
       
    }

    /// <summary>
    /// Llamar al reiniciar la ronda (junto con ResetShot del SwipeShooter).
    /// </summary>
    public void ResetZone()
    {
        alreadyScored = false;
    }
}
