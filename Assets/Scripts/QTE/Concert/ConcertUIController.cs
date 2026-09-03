using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Relie les éléments UGUI éditables du concert aux données de <see cref="ConcertManager"/>.
/// Cette couche gère le HUD, le panneau de séquence et l'écran de résultats sans IMGUI.
/// </summary>
[RequireComponent(typeof(ConcertManager))]
public class ConcertUIController : MonoBehaviour
{
    private const string QteTypeCircle = "QTE RYTHME";
    private const string QteTypeSequence = "SÉQUENCE DE TOUCHES";
    private const string QteTypeWaiting = "EN ATTENTE";

    [Header("HUD pendant le concert")]
    [SerializeField] private GameObject gameplayHud;
    [SerializeField] private Slider satisfactionSlider;
    [SerializeField] private TextMeshProUGUI satisfactionValueLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI comboLabel;
    [SerializeField] private TextMeshProUGUI timeRemainingLabel;
    [SerializeField] private TextMeshProUGUI currentQteLabel;

    [Header("QTE de séquence")]
    [SerializeField] private GameObject sequencePanel;
    [SerializeField] private TextMeshProUGUI sequenceTitleLabel;
    [SerializeField] private TextMeshProUGUI sequenceKeysLabel;
    [SerializeField] private TextMeshProUGUI sequenceProgressLabel;
    [SerializeField] private Image sequenceTimerFill;

    [Header("Bilan de fin")]
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private TextMeshProUGUI gradeLabel;
    [SerializeField] private TextMeshProUGUI finalScoreLabel;
    [SerializeField] private TextMeshProUGUI finalSatisfactionLabel;
    [SerializeField] private TextMeshProUGUI finalAccuracyLabel;
    [SerializeField] private TextMeshProUGUI finalComboLabel;
    [SerializeField] private TextMeshProUGUI improvementFeedbackLabel;
    [SerializeField] private Button nextButton;

    private ConcertManager concertManager;
    private ConcertKeySequenceQTE activeSequenceQte;

    private void Awake()
    {
        concertManager = GetComponent<ConcertManager>();
        SetActive(gameplayHud, true);
        SetActive(sequencePanel, false);
        SetActive(resultOverlay, false);
    }

    private void OnEnable()
    {
        concertManager.Completed += ShowResults;
    }

    private void OnDisable()
    {
        concertManager.Completed -= ShowResults;
    }

    private void Update()
    {
        RefreshGameplayHud();
        RefreshSequencePanel();
    }

    /// <summary>Transmet le clic du bouton Suivant au gestionnaire de concert.</summary>
    public void OnNextButtonClicked()
    {
        concertManager.SubmitResult();
    }

    private void RefreshGameplayHud()
    {
        if (gameplayHud == null || !gameplayHud.activeSelf)
        {
            return;
        }

        float satisfaction = concertManager.Satisfaction;
        if (satisfactionSlider != null)
        {
            satisfactionSlider.normalizedValue = satisfaction / 100f;
        }

        SetLabel(satisfactionValueLabel, Mathf.RoundToInt(satisfaction) + "%");
        SetLabel(scoreLabel, "SCORE  " + concertManager.Score);
        SetLabel(comboLabel, "COMBO  x" + concertManager.CurrentCombo);
        SetLabel(timeRemainingLabel, "TEMPS  " + concertManager.TimeRemaining.ToString("0") + " s");

        if (!concertManager.IsQteActive)
        {
            SetLabel(currentQteLabel, QteTypeWaiting);
        }
        else if (activeSequenceQte != null)
        {
            SetLabel(currentQteLabel, QteTypeSequence);
        }
        else
        {
            SetLabel(currentQteLabel, QteTypeCircle);
        }
    }

    private void RefreshSequencePanel()
    {
        ConcertKeySequenceQTE sequenceQte = concertManager.ActiveSequenceQte;
        if (sequenceQte == activeSequenceQte)
        {
            if (sequenceQte != null)
            {
                RefreshSequenceValues(sequenceQte);
            }

            return;
        }

        activeSequenceQte = sequenceQte;
        SetActive(sequencePanel, activeSequenceQte != null);

        if (activeSequenceQte != null)
        {
            SetLabel(sequenceTitleLabel, "REPRODUIRE LA SÉQUENCE");
            RefreshSequenceValues(activeSequenceQte);
        }
    }

    private void RefreshSequenceValues(ConcertKeySequenceQTE sequenceQte)
    {
        SetLabel(sequenceKeysLabel, sequenceQte.SequenceDisplay);
        SetLabel(sequenceProgressLabel, "TOUCHE " + sequenceQte.CurrentStep + "/" + sequenceQte.SequenceLength);

        if (sequenceTimerFill != null)
        {
            sequenceTimerFill.fillAmount = sequenceQte.RemainingTimeNormalized;
        }
    }

    private void ShowResults(ConcertPerformanceResult result)
    {
        SetActive(gameplayHud, false);
        SetActive(sequencePanel, false);
        SetActive(resultOverlay, true);
        SetLabel(gradeLabel, result.Grade);
        SetLabel(finalScoreLabel, "SCORE FINAL  " + result.Score);
        SetLabel(finalSatisfactionLabel, "SATISFACTION DU PUBLIC  " + result.FinalSatisfaction.ToString("0") + "%");
        SetLabel(finalAccuracyLabel, "PRÉCISION MOYENNE  " + result.AverageAccuracy.ToString("0") + "%");
        SetLabel(finalComboLabel, "MEILLEUR COMBO  x" + result.MaximumCombo);
        SetLabel(improvementFeedbackLabel, result.ImprovementFeedback);

        if (nextButton != null)
        {
            nextButton.interactable = true;
        }
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private static void SetLabel(TextMeshProUGUI label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }
}
