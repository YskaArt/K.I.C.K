using UnityEngine;

public class BallController : MonoBehaviour
{
    [SerializeField] private float kickForce = 10f;

    private Rigidbody rb;
    public bool IsInKickZone { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Kick()
    {
        if (!IsInKickZone) return;

        // Cancela velocidad y aplica impulso hacia arriba
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.3f, 0f, rb.linearVelocity.z * 0.3f);
        rb.AddForce(Vector3.up * kickForce, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("KickZone"))
            IsInKickZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("KickZone"))
            IsInKickZone = false;
    }
}