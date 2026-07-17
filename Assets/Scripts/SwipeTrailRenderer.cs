using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dibuja en pantalla el trazo que va dejando el dedo durante el swipe,
/// para que el jugador vea la linea/curva que esta definiendo antes de soltar.
/// Tecnica: convierte cada punto de pantalla en un punto en el mundo, a una
/// distancia fija delante de la camara, y arma un LineRenderer con esos
/// puntos. Como la distancia a la camara es constante, visualmente queda
/// "pegado" a la pantalla, como si dibujaras sobre el vidrio.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class SwipeTrailRenderer : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Camara desde la que se ve el swipe. Si esta vacio usa Camera.main")]
    [SerializeField] private Camera targetCamera;

    [Header("Configuracion visual")]
    [Tooltip("A que distancia de la camara se dibuja el trazo")]
    [SerializeField] private float drawDistanceFromCamera = 1.5f;

    [Tooltip("Grosor de la linea")]
    [SerializeField] private float lineWidth = 0.05f;

    [Tooltip("Color del trazo")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Optimizacion")]
    [Tooltip("Distancia minima en pixeles de pantalla entre un punto y el siguiente, para no acumular puntos de mas")]
    [SerializeField] private float minPointDistance = 4f;

    private LineRenderer lineRenderer;
    private readonly List<Vector2> screenPoints = new List<Vector2>();
    private readonly List<Vector3> worldPoints = new List<Vector3>();

    /// <summary>
    /// Puntos de pantalla registrados durante el swipe actual (solo lectura).
    /// SwipeShooter los usa para calcular la curva real del trazo.
    /// </summary>
    public IReadOnlyList<Vector2> ScreenPoints => screenPoints;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        SetupLineRenderer();
        lineRenderer.enabled = false;
    }

    private void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;

        // Material simple sin depender de assets externos (para no romper el
        // proyecto si todavia no hay un material de trail asignado a mano)
        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            lineRenderer.material = new Material(shader);
        }

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f); // se desvanece hacia la punta
    }

    /// <summary>
    /// Llamar al empezar el swipe (touch/mouse down).
    /// </summary>
    public void BeginSwipe(Vector2 screenPos)
    {
        screenPoints.Clear();
        worldPoints.Clear();

        lineRenderer.enabled = true;
        AddPointInternal(screenPos);
    }

    /// <summary>
    /// Llamar en cada frame de movimiento mientras el dedo sigue en pantalla.
    /// </summary>
    public void AddPoint(Vector2 screenPos)
    {
        if (screenPoints.Count == 0)
        {
            AddPointInternal(screenPos);
            return;
        }

        // Evitamos acumular puntos redundantes si el dedo casi no se movio
        float distFromLast = Vector2.Distance(screenPoints[screenPoints.Count - 1], screenPos);
        if (distFromLast < minPointDistance) return;

        AddPointInternal(screenPos);
    }

    private void AddPointInternal(Vector2 screenPos)
    {
        screenPoints.Add(screenPos);
        worldPoints.Add(ScreenToWorld(screenPos));

        lineRenderer.positionCount = worldPoints.Count;
        lineRenderer.SetPositions(worldPoints.ToArray());
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, drawDistanceFromCamera);
        return targetCamera.ScreenToWorldPoint(screenPoint);
    }

    /// <summary>
    /// Llamar al soltar el dedo (o al cancelar el swipe) para ocultar y limpiar el trazo.
    /// </summary>
    public void EndSwipe()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
        screenPoints.Clear();
        worldPoints.Clear();
    }
}
