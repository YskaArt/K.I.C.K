using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ronda guiada para aprender a jugar: muestra indicaciones contextuales
/// durante la primera partida real (jueguitos -> apuntar -> patear ->
/// resultado) en vez de un carrusel de capturas separado.
///
/// Para que leer el texto no distraiga y haga perder (pelota que cae,
/// tiempo que se acaba), cada indicacion -- salvo la primera -- CONGELA el
/// juego y espera a que el jugador toque "Entendido" para seguir. Asi se
/// puede leer con calma sin arriesgar la ronda.
///
/// La primera indicacion (jueguitos) no pausa por su cuenta: aprovecha que
/// el juego ya arranca pausado por GameStartManager hasta el primer tap,
/// que funciona como su propio "Entendido". Pausar ahi tambien generaria un
/// doble freeze dificil de deshacer.
///
/// Se activa sola en dos casos:
///   - Es la primera vez que se juega (TutorialProgress.Completed == false).
///   - Se fuerza desde el menu con el boton "Como Jugar"
///     (SceneLoader.LoadTutorial -> GuidedTutorialRequest).
///
/// Apenas se resuelve el primer tiro (gol o fallo), se marca el tutorial
/// como completado y las indicaciones se apagan solas.
/// </summary>
public class GuidedTutorial : MonoBehaviour
{
    [Header("Referencias (vacio = usa los singleton de la escena)")]
    [SerializeField] private GameFlowManager flowManager;
    [SerializeField] private JueguitosPowerBar powerBar;
    [SerializeField] private SwipeShooter swipeShooter;
    [SerializeField] private KickZone kickZone;

    [Tooltip("El boton 'Patear' del HUD. Hay que bloquearlo aparte durante la pausa del hint 'ya podes patear': ShootButtonUI recalcula su propio interactable todos los frames sin importar el freeze, asi que si no se desactiva el componente, el boton queda clickeable igual.")]
    [SerializeField] private ShootButtonUI shootButtonUI;

    [Header("UI de indicaciones")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;

    [Tooltip("Boton 'Entendido / Seguir'. Se muestra en todos los pasos salvo el primero (ver resumen de la clase).")]
    [SerializeField] private Button continueButton;

    [Header("Paso 1: Jueguitos (sin pausa propia)")]
    [SerializeField]
    private string jueguitosHint =
        "Tocá la pantalla (o Espacio) cuando la pelota esté cerca para hacer jueguitos.\nLlená la barra para poder patear.";
    [Tooltip("Resalto visual opcional (una flecha, un dedo tocando la pantalla...). Podés agregarle el componente PulseIcon para que llame la atencion solo.")]
    [SerializeField] private GameObject jueguitosHighlight;

    [Header("Paso 2: Barra llena")]
    [SerializeField]
    private string readyToShootHint =
        "¡Ya podés patear! Tocá el botón Patear (o Enter) cuando quieras.";
    [SerializeField] private GameObject readyToShootHighlight;

    [Header("Paso 3: Apuntar")]
    [SerializeField]
    private string aimHint =
        "Tocá una zona del arco para elegir dónde apuntar.";
    [SerializeField] private GameObject aimHighlight;

    [Header("Paso 4: Swipe")]
    [SerializeField]
    private string swipeHint =
        "Ahora deslizá el dedo (o el mouse) para definir la fuerza y la curva. Soltá para patear.";
    [SerializeField] private GameObject swipeHighlight;

    private bool active;
    private bool awaitingContinue;
    private float timeScaleBeforePause = 1f;
    private GameObject currentHighlight;

    private bool readyHintShown;
    private bool aimHintShown;
    private bool swipeHintShown;

    private GameFlowManager Flow => flowManager != null ? flowManager : GameFlowManager.Instance;

    private void Start()
    {
        bool forced = GuidedTutorialRequest.ConsumeRequest();
        active = forced || !TutorialProgress.Completed;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        if (!active)
        {
            enabled = false;
            return;
        }

        GameFlowManager fm = Flow;
        if (fm != null)
        {
            fm.onGoalFollowUp.AddListener(OnShotResolved);
            fm.onShotMissed.AddListener(OnShotResolved);
        }

        if (continueButton != null) continueButton.onClick.AddListener(OnContinuePressed);

        // Sin pausa propia: el juego ya esta pausado por GameStartManager,
        // y su propio tap para arrancar cumple la funcion de "Entendido".
        ShowHintUnpaused(jueguitosHint, jueguitosHighlight);
    }

    private void OnDisable()
    {
        GameFlowManager fm = Flow;
        if (fm != null)
        {
            fm.onGoalFollowUp.RemoveListener(OnShotResolved);
            fm.onShotMissed.RemoveListener(OnShotResolved);
        }

        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinuePressed);
    }

    private void Update()
    {
        if (!active || awaitingContinue) return;

        GameFlowManager fm = Flow;
        if (fm == null) return;

        switch (fm.CurrentPhase)
        {
            case GameFlowManager.GamePhase.Jueguitos:
                if (!readyHintShown && powerBar != null && powerBar.CanShoot)
                {
                    readyHintShown = true;
                    ShowHintPaused(readyToShootHint, readyToShootHighlight);
                }
                break;

            case GameFlowManager.GamePhase.Aiming:
                if (!aimHintShown)
                {
                    aimHintShown = true;
                    ShowHintPaused(aimHint, aimHighlight);
                }
                else if (!swipeHintShown && swipeShooter != null && swipeShooter.AimPointSelected)
                {
                    swipeHintShown = true;
                    ShowHintPaused(swipeHint, swipeHighlight);
                }
                break;
        }
    }

    /// <summary>Paso 1: solo muestra texto/resalto, sin tocar el tiempo ni pedir boton.</summary>
    private void ShowHintUnpaused(string message, GameObject highlight)
    {
        if (hintText != null) hintText.text = message;
        if (hintPanel != null) hintPanel.SetActive(true);

        SwapHighlight(highlight);
    }

    /// <summary>Pasos 2 a 4: congela el juego y espera el toque en "Entendido".</summary>
    private void ShowHintPaused(string message, GameObject highlight)
    {
        awaitingContinue = true;
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        // Evita que un toque sobre el boton "Entendido" cuente de paso como
        // un tap de jueguito o de apuntado. Con swipeShooter usamos
        // InputLocked (no 'enabled'): desactivar el componente reseteria el
        // punto del arco ya elegido via su propio OnEnable().
        if (kickZone != null) kickZone.enabled = false;
        if (swipeShooter != null) swipeShooter.InputLocked = true;

        // El boton "Patear" NO depende de kickZone/swipeShooter: ShootButtonUI
        // decide su propio interactable todos los frames (sin importar el
        // freeze), asi que hay que apagarlo aparte para que no se pueda
        // presionar mientras se esta leyendo el hint.
        if (shootButtonUI != null)
        {
            shootButtonUI.enabled = false;
            var btn = shootButtonUI.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }

        if (hintText != null) hintText.text = message;
        if (hintPanel != null) hintPanel.SetActive(true);
        if (continueButton != null) continueButton.gameObject.SetActive(true);

        SwapHighlight(highlight);
    }

    private void OnContinuePressed()
    {
        if (!awaitingContinue) return;
        awaitingContinue = false;

        Time.timeScale = timeScaleBeforePause;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        SwapHighlight(null);

        // Reacomoda kickZone segun la fase actual, en vez de asumir que
        // siempre hay que reactivar lo mismo que se desactivo.
        GameFlowManager fm = Flow;
        GameFlowManager.GamePhase phase = fm != null ? fm.CurrentPhase : GameFlowManager.GamePhase.Jueguitos;

        if (kickZone != null) kickZone.enabled = phase == GameFlowManager.GamePhase.Jueguitos;
        if (swipeShooter != null) swipeShooter.InputLocked = false;

        // Vuelve a dejar que ShootButtonUI decida su propio interactable.
        if (shootButtonUI != null) shootButtonUI.enabled = true;
    }

    /// <summary>
    /// Se llama cuando se resuelve el primer tiro (gol o fallo): la ronda
    /// guiada termino. Conectado a GameFlowManager.onGoalFollowUp y
    /// onShotMissed, asi que no hace falta llamarlo a mano.
    /// </summary>
    private void OnShotResolved()
    {
        if (awaitingContinue)
        {
            Time.timeScale = timeScaleBeforePause;
        }

        TutorialProgress.Completed = true;
        active = false;
        awaitingContinue = false;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        SwapHighlight(null);

        // Por si el tiro se resolvio justo en medio de una pausa: no dejar
        // nada bloqueado de forma permanente (GameFlowManager los vuelve a
        // acomodar el solo en el proximo loop, pero mejor no arriesgar).
        if (shootButtonUI != null) shootButtonUI.enabled = true;

        enabled = false;
    }

    /// <summary>Conectar a un boton opcional "Saltar tutorial".</summary>
    public void SkipTutorial()
    {
        OnShotResolved();
    }

    private void SwapHighlight(GameObject highlight)
    {
        if (currentHighlight != null) currentHighlight.SetActive(false);
        currentHighlight = highlight;
        if (currentHighlight != null) currentHighlight.SetActive(true);
    }
}
