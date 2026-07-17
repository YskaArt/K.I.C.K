using UnityEngine;

public class Ground : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ball") && gameManager != null)
            gameManager.GameOver();
    }
}
