using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Habilita/deshabilita el boton de "Patear" segun si la barra de jueguitos
/// ya llego al umbral minimo y si todavia estamos en esa fase.
/// El OnClick del Button se conecta desde el Inspector directamente a
/// GameFlowManager.OnShootButtonPressed().
/// </summary>
[RequireComponent(typeof(Button))]
public class ShootButtonUI : MonoBehaviour
{
    [SerializeField] private JueguitosPowerBar powerBar;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Update()
    {
        if (powerBar == null || button == null) return;

        bool canShootNow = powerBar.CanShoot
            && (GameFlowManager.Instance == null
                || GameFlowManager.Instance.CurrentPhase == GameFlowManager.GamePhase.Jueguitos);

        button.interactable = canShootNow;
    }
}
