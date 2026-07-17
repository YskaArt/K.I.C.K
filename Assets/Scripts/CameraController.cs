using System.Collections;
using UnityEngine;

/// <summary>
/// Mueve la camara principal entre dos puntos de vista:
/// - Jueguitos: de frente al personaje (Fase 1).
/// - Disparo: desde atras del personaje, mirando hacia el arco (Fase 2/3).
/// Usa dos Transform vacios como referencia de posicion/rotacion, para que
/// el equipo pueda acomodar los puntos de vista a ojo en el editor sin
/// tocar codigo. Es un singleton, como SlowMotionController.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Referencias")]
    [Tooltip("Camara que se va a mover. Si esta vacio usa Camera.main")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Punto de vista durante los jueguitos (de frente al personaje)")]
    [SerializeField] private Transform jueguitosViewPoint;

    [Tooltip("Punto de vista durante el disparo (desde atras del personaje)")]
    [SerializeField] private Transform shootViewPoint;

    [Header("Configuracion")]
    [Tooltip("Cuanto tarda la transicion entre vistas (segundos reales)")]
    [SerializeField] private float transitionDuration = 0.5f;

    private Coroutine currentTransition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        // Arrancamos ya posicionados en la vista de jueguitos, sin transicion
        if (jueguitosViewPoint != null && targetCamera != null)
        {
            targetCamera.transform.SetPositionAndRotation(
                jueguitosViewPoint.position,
                jueguitosViewPoint.rotation);
        }
    }

    /// <summary>Llamar al entrar o volver a la fase de Jueguitos.</summary>
    public void SwitchToJueguitosView()
    {
        MoveTo(jueguitosViewPoint);
    }

    /// <summary>Llamar al pasar a la fase de Apuntado/Disparo.</summary>
    public void SwitchToShootView()
    {
        MoveTo(shootViewPoint);
    }

    private void MoveTo(Transform target)
    {
        if (target == null || targetCamera == null) return;

        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(TransitionRoutine(target));
    }

    private IEnumerator TransitionRoutine(Transform target)
    {
        Transform camTransform = targetCamera.transform;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            // unscaledDeltaTime para que la transicion no dependa del
            // slow-motion que puede estar activo en simultaneo
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            // SmoothStep para que arranque y termine suave, no lineal
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            camTransform.position = Vector3.Lerp(startPos, target.position, smoothT);
            camTransform.rotation = Quaternion.Slerp(startRot, target.rotation, smoothT);

            yield return null;
        }

        camTransform.SetPositionAndRotation(target.position, target.rotation);
    }
}
