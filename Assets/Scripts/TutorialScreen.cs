using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla "Como Jugar": un carrusel de capturas que se deslizan
/// horizontalmente, una por paso del juego. Se arma solo a partir del array
/// 'Slides' -- para agregar o sacar un paso del tutorial alcanza con tocar
/// esa lista, no hay que tocar nada mas.
///
/// Se puede pasar de slide arrastrando con el dedo/mouse (como un carrusel de
/// app) o con los botones Prev/Next opcionales.
///
/// Setup en el editor:
///   1. Un panel "TutorialPanel" (desactivado) para toda la pantalla.
///   2. Adentro, un "Viewport": un RectTransform con tamaño fijo (el marco
///      visible del carrusel) + un componente Mask o Rect Mask 2D + una
///      Image (puede ser transparente, sirve para recibir el drag) con
///      Raycast Target activado. Poné ESTE componente (TutorialScreen) ahi.
///   3. Adentro del Viewport, un RectTransform vacio llamado "SlidesContainer"
///      (ancla 0,0 a 0,1; pivot 0, 0.5; tamaño lo calcula el script solo)
///      -> asignalo en 'Slides Container'.
///   4. Asignar 'Viewport' (el propio RectTransform del paso 2).
///   5. Cargar las capturas de Assets/TutoCapturas como Sprite (Texture Type
///      = Sprite (2D and UI)) y arrastrarlas, en orden, a 'Slides'.
///   6. Opcional: botones "Prev"/"Next" -> Prev()/Next(); texto "1 / 7" ->
///      'Page Label'; fila de puntitos -> 'Dots Container' + 'Dot Template'.
///   7. Boton "Como Jugar" del menu -> TutorialScreen.Open(). Boton "Volver"
///      del panel -> TutorialScreen.Close().
/// </summary>
public class TutorialScreen : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Datos")]
    [Tooltip("Capturas del tutorial, en el orden en que se deben mostrar.")]
    [SerializeField] private Sprite[] slides;

    [Header("UI")]
    [Tooltip("Ventana visible del carrusel (con Mask/Rect Mask 2D). Su ancho define el ancho de cada slide.")]
    [SerializeField] private RectTransform viewport;

    [Tooltip("Contenedor que se desliza. Se le crea un hijo Image por slide.")]
    [SerializeField] private RectTransform slidesContainer;

    [Tooltip("Texto opcional tipo '1 / 7'.")]
    [SerializeField] private TMP_Text pageLabel;

    [Header("Puntos de pagina (opcional)")]
    [SerializeField] private RectTransform dotsContainer;
    [SerializeField] private GameObject dotTemplate;
    [SerializeField] private Color dotActiveColor = Color.white;
    [SerializeField] private Color dotInactiveColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("Comportamiento")]
    [SerializeField] private float slideDuration = 0.3f;
    [Tooltip("Fraccion del ancho de la ventana que hay que arrastrar para pasar de slide (0-1).")]
    [SerializeField] private float dragThreshold = 0.18f;
    [SerializeField] private bool loopSlides = false;
    [SerializeField] private bool allowArrowKeys = true;

    [Header("Flechas anterior / siguiente")]
    [Tooltip("Boton propio de 'anterior'. Si queda vacio y 'Auto Create Arrows' esta activo, se crea uno solo.")]
    [SerializeField] private Button prevButton;

    [Tooltip("Boton propio de 'siguiente'. Si queda vacio y 'Auto Create Arrows' esta activo, se crea uno solo.")]
    [SerializeField] private Button nextButton;

    [Tooltip("Crea flechas < > sobre los bordes del viewport cuando no asignas botones propios.")]
    [SerializeField] private bool autoCreateArrows = true;
    [SerializeField] private Vector2 arrowSize = new Vector2(90f, 90f);
    [SerializeField] private Color arrowColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Modo obligatorio (primera vez)")]
    [Tooltip("Boton 'Jugar' que aparece en la ultima captura cuando el tutorial es obligatorio. Si queda vacio se crea uno solo.")]
    [SerializeField] private Button startButton;
    [SerializeField] private string startButtonText = "¡JUGAR!";

    private bool mandatory;
    private System.Action onMandatoryFinished;

    private readonly List<GameObject> spawnedDots = new List<GameObject>();
    private int currentIndex;
    private float dragStartX;
    private float containerStartX;
    private bool dragging;
    private Coroutine slideRoutine;
    private bool built;

    private void OnEnable()
    {
        BuildSlides();

        if (prevButton != null) prevButton.onClick.AddListener(Prev);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);

        GoTo(0, animated: false);
    }

    private void OnDisable()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(Prev);
        if (nextButton != null) nextButton.onClick.RemoveListener(Next);
        if (startButton != null) startButton.onClick.RemoveListener(OnStartPressed);
    }

    private void Update()
    {
        if (!allowArrowKeys) return;

        if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Prev();
    }

    /// <summary>Muestra la pantalla (conectar al OnClick del boton "Como Jugar").</summary>
    public void Open()
    {
        for (Transform t = transform; t != null; t = t.parent)
        {
            if (t.GetComponent<Canvas>() != null) break;
            t.gameObject.SetActive(true);
        }
    }

    /// <summary>Oculta la pantalla (conectar al OnClick del boton "Volver").</summary>
    public void Close()
    {
        // En modo obligatorio no se puede cerrar hasta haber visto todo.
        if (mandatory && !TutorialProgress.Completed) return;

        mandatory = false;
        onMandatoryFinished = null;

        // Apagar el panel raiz (hijo directo del Canvas), no solo el Viewport.
        Transform root = transform;
        while (root.parent != null && root.parent.GetComponent<Canvas>() == null)
        {
            root = root.parent;
        }
        root.gameObject.SetActive(false);
    }

    /// <summary>
    /// Abre el tutorial como paso obligatorio: no se puede cerrar hasta llegar
    /// a la ultima captura, y ahi aparece el boton "Jugar" que ejecuta
    /// 'onFinished' (por ej. cargar la escena del juego).
    /// </summary>
    public void OpenMandatory(System.Action onFinished)
    {
        mandatory = true;
        onMandatoryFinished = onFinished;

        // Este componente vive en el Viewport, hijo de "TutorialPanel" (que
        // arranca desactivado): hay que prender tambien los padres.
        for (Transform t = transform; t != null; t = t.parent)
        {
            if (t.GetComponent<Canvas>() != null) break;
            t.gameObject.SetActive(true);
        }

        GoTo(0, animated: false);
    }

    private void OnStartPressed()
    {
        System.Action callback = onMandatoryFinished;
        mandatory = false;
        onMandatoryFinished = null;
        callback?.Invoke();
    }

    /// <summary>Conectar al boton "Siguiente".</summary>
    public void Next() => GoTo(currentIndex + 1, animated: true);

    /// <summary>Conectar al boton "Anterior".</summary>
    public void Prev() => GoTo(currentIndex - 1, animated: true);

    private void BuildSlides()
    {
        if (built) return;

        if (slidesContainer == null || viewport == null || slides == null || slides.Length == 0)
        {
            Debug.LogWarning("TutorialScreen: falta asignar Viewport, Slides Container o Slides.");
            return;
        }

        built = true;
        float width = viewport.rect.width;

        for (int i = 0; i < slides.Length; i++)
        {
            var go = new GameObject("Slide_" + i, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(slidesContainer, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);
            rt.anchoredPosition = new Vector2(width * i, 0f);

            Image img = go.GetComponent<Image>();
            img.sprite = slides[i];
            img.preserveAspect = true;
        }

        slidesContainer.sizeDelta = new Vector2(width * slides.Length, slidesContainer.sizeDelta.y);

        BuildDots();
        EnsureArrows();
    }

    private void EnsureArrows()
    {
        if (!autoCreateArrows) return;

        // Se crean como hijos del viewport (despues del SlidesContainer) para
        // que se dibujen encima de las capturas y no las recorte la Mask.
        if (prevButton == null) prevButton = CreateArrow("PrevArrow", "<", left: true);
        if (nextButton == null) nextButton = CreateArrow("NextArrow", ">", left: false);

        if (startButton == null)
        {
            startButton = CreateStartButton();
            startButton.onClick.AddListener(OnStartPressed);
        }
    }

    private Button CreateStartButton()
    {
        var go = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        // Va FUERA del viewport (hermano, justo debajo) para no tapar la ultima
        // captura ni quedar recortado por la Mask.
        rt.SetParent(viewport.parent, false);
        rt.anchorMin = viewport.anchorMin;
        rt.anchorMax = viewport.anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(420f, 110f);

        float bottomY = viewport.anchoredPosition.y - viewport.rect.height * viewport.pivot.y;
        float centerX = viewport.anchoredPosition.x + viewport.rect.width * (0.5f - viewport.pivot.x);
        rt.anchoredPosition = new Vector2(centerX, bottomY - 20f);

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.15f, 0.7f, 0.25f, 0.95f);
        go.GetComponent<Button>().targetGraphic = bg;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = startButtonText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 56f;
        label.color = Color.white;
        label.raycastTarget = false;

        return go.GetComponent<Button>();
    }

    private Button CreateArrow(string objectName, string glyph, bool left)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(viewport, false);

        float x = left ? 0f : 1f;
        rt.anchorMin = new Vector2(x, 0.5f);
        rt.anchorMax = new Vector2(x, 0.5f);
        rt.pivot = new Vector2(x, 0.5f);
        rt.sizeDelta = arrowSize;
        rt.anchoredPosition = Vector2.zero;

        var bg = go.GetComponent<Image>();
        bg.color = arrowColor;

        var button = go.GetComponent<Button>();
        button.targetGraphic = bg;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = glyph;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 64f;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }

    private void UpdateArrows()
    {
        if (slides == null) return;

        bool canPrev = loopSlides || currentIndex > 0;
        bool canNext = loopSlides || currentIndex < slides.Length - 1;

        if (prevButton != null) prevButton.interactable = canPrev;
        if (nextButton != null) nextButton.interactable = canNext;

        bool onLast = currentIndex == slides.Length - 1;
        if (onLast) TutorialProgress.Completed = true;

        if (startButton != null) startButton.gameObject.SetActive(mandatory && onLast);
    }

    private void BuildDots()
    {
        if (dotsContainer == null || dotTemplate == null || slides == null) return;

        foreach (GameObject d in spawnedDots)
        {
            if (d != null) Destroy(d);
        }
        spawnedDots.Clear();

        dotTemplate.SetActive(false);

        for (int i = 0; i < slides.Length; i++)
        {
            GameObject dot = Instantiate(dotTemplate, dotsContainer);
            dot.SetActive(true);
            dot.name = "Dot_" + i;
            spawnedDots.Add(dot);
        }
    }

    private void GoTo(int index, bool animated)
    {
        if (slides == null || slides.Length == 0 || viewport == null || slidesContainer == null) return;

        if (loopSlides)
        {
            index = ((index % slides.Length) + slides.Length) % slides.Length;
        }
        else
        {
            index = Mathf.Clamp(index, 0, slides.Length - 1);
        }

        currentIndex = index;
        float targetX = -viewport.rect.width * currentIndex;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        if (animated && isActiveAndEnabled)
        {
            slideRoutine = StartCoroutine(SlideTo(targetX));
        }
        else
        {
            slidesContainer.anchoredPosition = new Vector2(targetX, slidesContainer.anchoredPosition.y);
        }

        UpdateDots();
        UpdatePageLabel();
        UpdateArrows();
    }

    private IEnumerator SlideTo(float targetX)
    {
        float startX = slidesContainer.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            float x = Mathf.Lerp(startX, targetX, t);
            slidesContainer.anchoredPosition = new Vector2(x, slidesContainer.anchoredPosition.y);
            yield return null;
        }

        slidesContainer.anchoredPosition = new Vector2(targetX, slidesContainer.anchoredPosition.y);
        slideRoutine = null;
    }

    private void UpdateDots()
    {
        for (int i = 0; i < spawnedDots.Count; i++)
        {
            Image img = spawnedDots[i].GetComponent<Image>();
            if (img != null) img.color = (i == currentIndex) ? dotActiveColor : dotInactiveColor;
        }
    }

    private void UpdatePageLabel()
    {
        if (pageLabel != null && slides != null && slides.Length > 0)
        {
            pageLabel.text = $"{currentIndex + 1} / {slides.Length}";
        }
    }

    // ---------------------------------------------------------------
    // Arrastre (swipe) para pasar de slide
    // ---------------------------------------------------------------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slides == null || slides.Length <= 1 || slidesContainer == null) return;

        dragging = true;
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        dragStartX = eventData.position.x;
        containerStartX = slidesContainer.anchoredPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        float delta = eventData.position.x - dragStartX;
        slidesContainer.anchoredPosition = new Vector2(containerStartX + delta, slidesContainer.anchoredPosition.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        float delta = eventData.position.x - dragStartX;
        float width = viewport != null ? viewport.rect.width : 0f;

        if (width > 0f && Mathf.Abs(delta) > width * dragThreshold)
        {
            GoTo(currentIndex + (delta < 0 ? 1 : -1), animated: true);
        }
        else
        {
            GoTo(currentIndex, animated: true); // vuelve a acomodar el slide actual
        }
    }
}
