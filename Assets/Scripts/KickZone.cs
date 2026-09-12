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

            // Reproducir sonido de jueguito (por el bus central si existe, para
            // que respete el volumen de SFX y el mute).
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx("SFX/jueguito");
            }
            else if (audioSource != null)
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

    /// <summary>
    /// Se ejecuta cuando GameFlowManager desactiva este script (kickZone.enabled
    /// = false) al pasar a la fase de disparo. Sin esto, el indicador se queda
    /// "pegado" con el ultimo estado que tenia (por ejemplo, encendido si justo
    /// se apreto el boton de patear estando parado en la zona de jueguitos),
    /// porque al dejar de correr Update() nadie lo vuelve a apagar.
    /// </summary>
    private void OnDisable()
    {
        if (kickIndicator != null)
            kickIndicator.SetActive(false);
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