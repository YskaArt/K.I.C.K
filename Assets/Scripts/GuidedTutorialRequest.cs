/// <summary>
/// Bandera en memoria (no se guarda en disco) para forzar la ronda guiada
/// la proxima vez que arranque la escena de juego, aunque ya se haya
/// completado antes. La usa el boton "Como Jugar" del menu
/// (SceneLoader.LoadTutorial) para poder repetirla cuando se quiera.
/// </summary>
public static class GuidedTutorialRequest
{
    private static bool forceNext;

    /// <summary>Pide que la proxima vez que arranque Jueguitos se active el modo guiado.</summary>
    public static void RequestNext()
    {
        forceNext = true;
    }

    /// <summary>Consume el pedido (una sola vez) y devuelve si estaba activo.</summary>
    public static bool ConsumeRequest()
    {
        bool value = forceNext;
        forceNext = false;
        return value;
    }
}
