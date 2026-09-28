using System.Collections;
using UnityEngine;

public class Ground : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Tooltip("Particulas de polvo/impacto cuando se te cae la pelota. Crealo con Assets > Create > K.I.C.K > Impact Effect.")]
    [SerializeField] private ImpactEffect dropEffect;

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
            if (CameraController.Instance != null)
            {
                CameraController.Instance.Shake(0.2f, 0.4f);
            }
            Handheld.Vibrate();

            if (dropEffect != null)
            {
                ImpactEffectPlayer.Ensure().Play(dropEffect, other.transform.position);
            }

            // Retraso corto (tiempo real) para que el shake se alcance a ver
            // antes de que el freeze del Game Over tape la pantalla.
            StartCoroutine(DelayedGameOver());
        }
        else if (GameFlowManager.Instance != null)
        {
            // Ya estamos en fase de disparo: la pelota tocando el piso sin
            // haber entrado al arco es un tiro errado (con una ventana corta
            // por si pica y entra).
            GameFlowManager.Instance.NotifyBallHitGround();
        }
    }

    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSecondsRealtime(0.12f);
        gameManager.GameOver();
    }
}
