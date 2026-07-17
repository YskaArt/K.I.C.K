using UnityEngine;

public class KickZone : MonoBehaviour
{
    [SerializeField] private BallController ball;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private JueguitosPowerBar powerBar;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private GameObject kickIndicator; // UI "¡AHORA!" opcional

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

            playerAnimator.SetTrigger("Kick");
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
