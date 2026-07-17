using UnityEngine;

public class KickZone : MonoBehaviour
{
    [SerializeField] private BallController ball;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private GameObject kickIndicator; // UI "¡AHORA!" opcional

    private void Update()
    {
        if (kickIndicator != null)
            kickIndicator.SetActive(ball.IsInKickZone);

        if (DetectTap() && ball.IsInKickZone)
        {
            ball.Kick();
            gameManager.AddPoint();
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