using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QTE circulaire : l'anneau d'approche se referme sur la cible et la précision dépend de
/// l'écart à l'échelle de validation. Le clic UI et les touches Entrée/Espace sont pris en charge.
/// </summary>
public class CircleQTE : MonoBehaviour
{
    private const float DefaultMaximumAccuracyError = 1.5f;

    [Header("Références")]
    [SerializeField] private RectTransform approachCircle;
    [SerializeField] private RectTransform hitCircle;

    [Header("Configuration")]
    [SerializeField] private float startScale = 2.5f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float maximumAccuracyError = DefaultMaximumAccuracyError;

    private float timer;
    private bool isActive;
    private IQTECircleHit qteHitReceiver;

    /// <summary>Initialise le QTE avec sa durée et le gestionnaire qui recevra son résultat.</summary>
    public void Initialize(float qteDuration, IQTECircleHit hitReceiver)
    {
        duration = Mathf.Max(0.1f, qteDuration);
        qteHitReceiver = hitReceiver;
        StartQTE();
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);

        if (approachCircle != null)
        {
            float currentScale = Mathf.Lerp(startScale, endScale, progress);
            approachCircle.localScale = Vector3.one * currentScale;
        }

        if (WasConfirmPressed())
        {
            OnPlayerClick();
            return;
        }

        if (progress >= 1f)
        {
            FailQTE();
        }
    }

    /// <summary>Active ou réactive le QTE à son état initial.</summary>
    public void StartQTE()
    {
        timer = 0f;
        isActive = true;

        if (approachCircle != null)
        {
            approachCircle.localScale = Vector3.one * startScale;
        }
    }

    /// <summary>Résout le QTE avec la précision correspondant au moment de l'action du joueur.</summary>
    public void OnPlayerClick()
    {
        if (!isActive || approachCircle == null)
        {
            return;
        }

        float error = Mathf.Abs(approachCircle.localScale.x - endScale);
        float accuracy = CalculateAccuracy(error);
        isActive = false;
        qteHitReceiver?.Hit(accuracy);
        Destroy(gameObject);
    }

    private bool WasConfirmPressed()
    {
        Keyboard keyboard = Keyboard.current;
        bool keyboardPressed = keyboard != null
            && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        return keyboardPressed || gamepadPressed;
    }

    private float CalculateAccuracy(float error)
    {
        float safeMaximumError = Mathf.Max(0.01f, maximumAccuracyError);
        return Mathf.Clamp01(1f - error / safeMaximumError) * 100f;
    }

    private void FailQTE()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;
        qteHitReceiver?.Miss();
        Destroy(gameObject);
    }
}
