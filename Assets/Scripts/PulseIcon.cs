using UnityEngine;

/// <summary>
/// Hace "respirar" (escalar de a poco, ida y vuelta) cualquier RectTransform
/// o Transform -- pensado para iconos de ayuda del tutorial guiado (una
/// flecha, un dedo tocando la pantalla, etc), para que llamen la atencion
/// sin necesitar una animacion hecha a mano. Poné este componente en el
/// mismo objeto que se prende/apaga como resalto visual.
/// </summary>
public class PulseIcon : MonoBehaviour
{
    [Tooltip("Tamaño minimo y maximo del pulso (1 = tamaño normal).")]
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.15f;

    [Tooltip("Cuantas veces por segundo late.")]
    [SerializeField] private float speed = 2f;

    private Vector3 baseScale;

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = baseScale * scale;
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
    }
}
