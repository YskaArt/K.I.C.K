using UnityEngine;

public class Ground : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Ball")) return;
        if (gameManager == null) return;

        // Si todavia estamos en la fase de Jueguitos, tocar el piso es un
        // fallo (se te cayo la pelota) y termina la ronda. Si ya se paso a
        // Apuntado/Disparo, la pelota cayendo es parte normal del tiro y
        // no deberia cortar la partida.
        bool stillJuggling = GameFlowManager.Instance == null
            || GameFlowManager.Instance.ShouldEndOnGroundHit();

        if (stillJuggling)
        {
            gameManager.GameOver();
        }
    }
}
