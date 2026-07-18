using UnityEngine;

public class KickZone : MonoBehaviour
{
    [SerializeField] private BallController ball;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private JueguitosPowerBar powerBar;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private GameObject kickIndicator; // UI "¡AHORA!" opcional
    [SerializeField] private AudioSource audioSource; // Para reproducir efectos de sonido

    [Tooltip("Cuanto carga la barra cada jueguito exitoso (0-1). Ej: 0.08 = necesitas ~13 jueguitos para llenarla")]
    [SerializeField] private float chargePerKick = 0.08f;

    private void Update()
    {
        if (kickIndicator != null)
            kickIndicator.SetActive(ball.IsInKickZone);

        if (DetectTap() && ball.IsInKickZone)
        {
            ball.Kick();
            gameManager.AddPoint();

            if (powerBar != null)
                powerBar.AddCharge(chargePerKick);

            // Activar trigger de animación y reproducir sonido
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("Kick");
            }

            // Reproducir sonido de jueguito
            if (audioSource != null)
            {
                AudioClip jueguitoClip = Resources.Load<AudioClip>("SFX/jueguito");
                if (jueguitoClip != null)
                {
                    audioSource.PlayOneShot(jueguitoClip);
                }
                else
                {
                    Debug.LogWarning("KickZone: No se pudo cargar el audio 'SFX/jueguito'");
                }
            }
        }
    }

    private bool DetectTap()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            return true;
#endif

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;

        return false;
    }
}
