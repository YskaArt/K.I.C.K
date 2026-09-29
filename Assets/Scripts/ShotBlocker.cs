using UnityEngine;

/// <summary>
/// Poné este componente sobre CUALQUIER collider que deba bloquear un tiro
/// al arco -- hoy el dron, mas adelante un arquero. Si la pelota lo toca
/// mientras el tiro esta en vuelo, arranca una ventana corta (Block Settle
/// Seconds en GameFlowManager) para el fallo: si el rebote sigue y entra
/// igual al arco, cuenta como gol; si no, recien ahi se resuelve como fallo.
///
/// La pelota rebota fisicamente sola si el collider de este objeto es
/// solido (no trigger) y tiene un Rigidbody de por medio en la pelota; este
/// script solo se encarga de avisarle al flujo del juego que el tiro se
/// bloqueo. Funciona tanto si el collider es solido (OnCollisionEnter) como
/// si esta marcado Is Trigger (OnTriggerEnter).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShotBlocker : MonoBehaviour
{
    [Tooltip("Tag que debe tener la pelota para contar el bloqueo.")]
    [SerializeField] private string ballTag = "Ball";

    private void OnCollisionEnter(Collision collision)
    {
        TryBlock(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBlock(other.gameObject);
    }

    private void TryBlock(GameObject other)
    {
        if (!other.CompareTag(ballTag)) return;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.NotifyShotBlocked();
        }
    }
}
