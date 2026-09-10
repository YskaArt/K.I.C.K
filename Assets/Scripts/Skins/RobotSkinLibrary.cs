using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lista central de skins del robot. Es el UNICO lugar donde se agregan o
/// sacan skins: crea el asset con Assets > Create > K.I.C.K > Robot Skin Library
/// y arrastrá ahi cada <see cref="RobotSkin"/>.
///
/// El orden de la lista es el orden en que aparecen los botones en la pantalla
/// de seleccion y el indice que se guarda en disco.
/// </summary>
[CreateAssetMenu(fileName = "RobotSkinLibrary", menuName = "K.I.C.K/Robot Skin Library")]
public class RobotSkinLibrary : ScriptableObject
{
    [Tooltip("Todas las skins disponibles, en orden.")]
    [SerializeField] private List<RobotSkin> skins = new List<RobotSkin>();

    public int Count => skins.Count;

    public IReadOnlyList<RobotSkin> Skins => skins;

    /// <summary>Devuelve la skin en ese indice, o null si esta fuera de rango.</summary>
    public RobotSkin Get(int index)
    {
        if (index < 0 || index >= skins.Count) return null;
        return skins[index];
    }

    /// <summary>Acota un indice al rango valido de la lista.</summary>
    public int ClampIndex(int index)
    {
        if (skins.Count == 0) return 0;
        return Mathf.Clamp(index, 0, skins.Count - 1);
    }
}
