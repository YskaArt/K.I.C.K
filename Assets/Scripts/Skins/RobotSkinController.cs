using UnityEngine;

/// <summary>
/// Aplica la skin elegida al robot. Poné este componente en el robot (o en un
/// padre suyo) tanto en la escena del menu como en la del juego.
///
/// Por defecto agarra automaticamente todos los Renderer hijos. Si solo querés
/// pintar algunas partes, cargá esos Renderer a mano en 'Target Renderers'.
///
/// El cambio de textura se hace con un MaterialPropertyBlock, asi que NO crea
/// instancias de material ni ensucia los assets del proyecto.
/// </summary>
[DisallowMultipleComponent]
public class RobotSkinController : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // Fallback para shaders viejos (Standard / Built-in).
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Datos")]
    [SerializeField] private RobotSkinLibrary library;

    [Tooltip("Renderers a pintar. Si queda vacio se usan todos los Renderer hijos.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Comportamiento")]
    [Tooltip("Aplicar la skin guardada en disco al arrancar la escena.")]
    [SerializeField] private bool applySavedOnStart = true;

    [Tooltip("Indice usado para previsualizar en el editor (boton derecho > Aplicar Skin De Preview).")]
    [SerializeField] private int previewIndex;

    private MaterialPropertyBlock block;
    private Material[][] originalMaterials;
    private int currentIndex = -1;

    public RobotSkinLibrary Library => library;
    public int CurrentIndex => currentIndex;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        CacheRenderers();
    }

    private void OnEnable()
    {
        PlayerSkinPrefs.Changed += ApplySkin;
    }

    private void OnDisable()
    {
        PlayerSkinPrefs.Changed -= ApplySkin;
    }

    private void Start()
    {
        if (applySavedOnStart)
        {
            ApplySkin(PlayerSkinPrefs.SelectedIndex);
        }
    }

    private void CacheRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        originalMaterials = new Material[targetRenderers.Length][];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                originalMaterials[i] = targetRenderers[i].sharedMaterials;
            }
        }
    }

    /// <summary>Aplica la skin de ese indice de la libreria.</summary>
    public void ApplySkin(int index)
    {
        if (library == null || library.Count == 0) return;

        index = library.ClampIndex(index);
        ApplySkin(library.Get(index));
        currentIndex = index;
    }

    /// <summary>Aplica una skin concreta a todos los renderers configurados.</summary>
    public void ApplySkin(RobotSkin skin)
    {
        if (skin == null || targetRenderers == null) return;

        if (block == null) block = new MaterialPropertyBlock();

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            if (r == null) continue;

            if (skin.overrideMaterial && skin.customMaterial != null)
            {
                r.SetPropertyBlock(null);

                int subMeshCount = originalMaterials != null && originalMaterials[i] != null
                    ? originalMaterials[i].Length
                    : r.sharedMaterials.Length;

                var mats = new Material[Mathf.Max(1, subMeshCount)];
                for (int m = 0; m < mats.Length; m++) mats[m] = skin.customMaterial;
                r.sharedMaterials = mats;
                continue;
            }

            // Skin por textura/color: restauramos el material original y solo
            // pisamos _BaseMap / _BaseColor con un property block.
            if (originalMaterials != null && originalMaterials[i] != null)
            {
                r.sharedMaterials = originalMaterials[i];
            }

            r.GetPropertyBlock(block);

            if (skin.bodyTexture != null)
            {
                block.SetTexture(BaseMapId, skin.bodyTexture);
                block.SetTexture(MainTexId, skin.bodyTexture);
            }

            block.SetColor(BaseColorId, skin.tint);
            block.SetColor(ColorId, skin.tint);

            r.SetPropertyBlock(block);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Aplicar Skin De Preview")]
    private void ApplyPreviewSkin()
    {
        CacheRenderers();
        if (block == null) block = new MaterialPropertyBlock();
        ApplySkin(previewIndex);
    }
#endif
}
