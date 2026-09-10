using UnityEngine;

/// <summary>
/// Una "skin" del robot. Cada skin es un asset independiente que se crea
/// desde el menu: Assets > Create > K.I.C.K > Robot Skin.
///
/// Para agregar una skin nueva:
///   1. Create > K.I.C.K > Robot Skin.
///   2. Arrastrale la textura (Body Texture) y, si querés, un color y un icono.
///   3. Sumala a la lista de <see cref="RobotSkinLibrary"/>.
/// La pantalla de seleccion se arma sola con lo que haya en la libreria.
/// </summary>
[CreateAssetMenu(fileName = "RobotSkin", menuName = "K.I.C.K/Robot Skin")]
public class RobotSkin : ScriptableObject
{
    [Tooltip("Nombre visible en la pantalla de seleccion")]
    public string displayName = "Skin";

    [Tooltip("Icono que se muestra en el boton de la pantalla de seleccion (opcional)")]
    public Sprite previewIcon;

    [Header("Aspecto del robot")]
    [Tooltip("Textura que se aplica al cuerpo del robot (_BaseMap). Dejala vacia si solo querés cambiar el color o usar un material completo.")]
    public Texture2D bodyTexture;

    [Tooltip("Color multiplicador sobre la textura (_BaseColor). Blanco = sin cambios.")]
    public Color tint = Color.white;

    [Header("Avanzado (opcional)")]
    [Tooltip("Si esta activo, en vez de solo cambiar la textura se reemplaza el material entero de los renderers por 'Custom Material'.")]
    public bool overrideMaterial;

    [Tooltip("Material completo a usar cuando 'Override Material' esta activo.")]
    public Material customMaterial;
}
