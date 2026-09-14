using UnityEngine;

/// <summary>
/// Mueve al dron dentro de un area rectangular delante del arco, rebotando
/// en los bordes como el clasico logo de DVD que rebota por la pantalla.
/// Funciona como un "arquero artificial": un obstaculo movil que el
/// jugador tiene que esquivar calculando bien la trayectoria y la curva
/// del tiro — encaja con la tematica robotica/tecnologica del juego.
/// </summary>
public class DroneKeeper : MonoBehaviour
{
    [Header("Area de movimiento")]
    [Tooltip("Collider (marcalo como Is Trigger) que define los limites del area donde se mueve el dron. Crea un GameObject vacio dentro del arco, ponele un Box Collider del tamaño que quieras que cubra, y arrastralo aca.")]
    [SerializeField] private BoxCollider moveArea;

    [Header("Movimiento")]
    [Tooltip("Velocidad de desplazamiento, en unidades por segundo")]
    [SerializeField] private float speed = 2.5f;

    [Tooltip("Si esta activo, el dron solo se mueve en el plano X/Y (izquierda-derecha y arriba-abajo), quedando siempre a la misma profundidad frente al arco. Destildalo si tambien queres que vaya y venga en Z.")]
    [SerializeField] private bool moveOnlyOnXY = true;

    [Header("Estetica")]
    [Tooltip("Si esta activo, el dron rota suavemente para 'mirar' hacia donde se esta moviendo")]
    [SerializeField] private bool faceMoveDirection = true;

    [Tooltip("Velocidad de rotacion (grados por segundo) al girar hacia la nueva direccion despues de un rebote")]
    [SerializeField] private float turnSpeed = 220f;

    private Vector3 direction;
    private Bounds bounds;

    private void Start()
    {
        if (moveArea == null)
        {
            Debug.LogWarning("DroneKeeper: falta asignar el Move Area (Box Collider) en el Inspector.");
            enabled = false;
            return;
        }

        bounds = moveArea.bounds;

        // Arranca en una direccion aleatoria, como el DVD, que nunca sale
        // derechito hacia un lado predecible.
        direction = RandomDirection();
    }

    private void Update()
    {
        Vector3 pos = transform.position + direction * speed * Time.deltaTime;
        Vector3 newDirection = direction;

        // Rebote por eje separado: si pega justo en una esquina, rebota en
        // ambos ejes a la vez (el mismo comportamiento que hace que el DVD
        // a veces "case" perfecto en el rincon de la pantalla).
        if (pos.x <= bounds.min.x || pos.x >= bounds.max.x)
        {
            newDirection.x = -newDirection.x;
            pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        }

        if (pos.y <= bounds.min.y || pos.y >= bounds.max.y)
        {
            newDirection.y = -newDirection.y;
            pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);
        }

        if (!moveOnlyOnXY && (pos.z <= bounds.min.z || pos.z >= bounds.max.z))
        {
            newDirection.z = -newDirection.z;
            pos.z = Mathf.Clamp(pos.z, bounds.min.z, bounds.max.z);
        }

        transform.position = pos;
        direction = newDirection;

        if (faceMoveDirection && direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    private Vector3 RandomDirection()
    {
        Vector2 dir2D = Random.insideUnitCircle.normalized;
        Vector3 dir = new Vector3(dir2D.x, dir2D.y, moveOnlyOnXY ? 0f : Random.Range(-1f, 1f));
        return dir.normalized;
    }

    // Dibuja el area de movimiento en el editor para poder ajustarla a ojo
    // sin tener que jugar la escena.
    private void OnDrawGizmosSelected()
    {
        if (moveArea == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(moveArea.bounds.center, moveArea.bounds.size);
    }
}