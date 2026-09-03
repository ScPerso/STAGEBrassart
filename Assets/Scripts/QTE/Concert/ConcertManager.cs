using System;
using System.Collections.Generic;
using Magma.Data;
using Magma.Gameplay;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestre le concert : alternance de plans caméra, file exclusive de QTE, satisfaction,
/// score et bilan de prestation. Un seul QTE est actif à la fois pour une lecture claire.
/// </summary>
public class ConcertManager : MonoBehaviour, IQTECircleHit
{
    private const float MinimumSessionDuration = 1f;
    private const float MinimumInterval = 0.1f;
    private const float DefaultCameraTransitionDuration = 1.25f;
    private const float PerfectAccuracyThreshold = 90f;
    private const float GoodAccuracyThreshold = 65f;
    private const int PerfectScore = 100;
    private const int GoodScore = 60;
    private const int SequenceBonusScore = 40;
    private const float SatisfactionAtGradeS = 90f;
    private const float SatisfactionAtGradeA = 75f;
    private const float SatisfactionAtGradeB = 60f;
    private const float SatisfactionAtGradeC = 40f;
    private const float ResultWidth = 700f;
    private const float ResultHeight = 420f;

    [Header("Références QTE")]
    [SerializeField] private RectTransform spawnZone;
    [SerializeField] private Slider satisfactionBar;
    [SerializeField] private QTEConfig[] qteConfigs;

    [Header("Déroulé du concert")]
    [SerializeField] private float sessionDuration = 45f;
    [Tooltip("Temps de respiration entre la résolution d'un QTE et le suivant.")]
    [SerializeField] private float delayBetweenQtes = 0.5f;
    [Tooltip("Intervalle entre deux QTE de séquence. Les autres créneaux génèrent un QTE circulaire.")]
    [SerializeField] private float sequenceInterval = 8f;
    [SerializeField] private float sequenceDuration = 2.5f;

    [Header("Satisfaction du public")]
    [SerializeField, Range(0f, 100f)] private float initialSatisfaction = 50f;
    [SerializeField] private float perfectSatisfactionGain = 10f;
    [SerializeField] private float goodSatisfactionGain = 5f;
    [SerializeField] private float failedSatisfactionLoss = 10f;

    [Header("Montage caméra")]
    [SerializeField] private Camera cinematicCamera;
    [Tooltip("Plans caméra parcourus dans l'ordre. Le premier est utilisé au démarrage.")]
    [SerializeField] private Transform[] cinematicCameraShots;
    [Tooltip("Durée d'un plan avant de passer au suivant.")]
    [SerializeField] private float cameraShotDuration = 6f;
    [Tooltip("Temps de transition fluide entre deux plans.")]
    [SerializeField] private float cameraTransitionDuration = DefaultCameraTransitionDuration;
    [Tooltip("Courbe de lissage des déplacements et rotations de caméra.")]
    [SerializeField] private AnimationCurve cameraTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Behaviour[] playerControlsToDisable;
    [SerializeField] private Animator performerAnimator;
    [SerializeField] private string performanceTrigger = "Perform";
    [SerializeField] private AudioSource concertMusic;

    [Header("Intégration au flux de jeu")]
    [SerializeField] private bool automaticallyReportResult;

    private readonly List<Behaviour> disabledControls = new List<Behaviour>();
    private float satisfaction;
    private float sessionTimer;
    private float qteCooldownTimer;
    private float sequenceTimer;
    private float cameraShotTimer;
    private float cameraTransitionTimer;
    private float totalAccuracy;
    private int currentCameraShotIndex;
    private int resolvedQteCount;
    private int successfulQteCount;
    private int failedQteCount;
    private int perfectCount;
    private int score;
    private int combo;
    private int maximumCombo;
    private bool isRunning;
    private bool isFinished;
    private bool hasReportedResult;
    private bool isCameraTransitioning;
    private CircleQTE activeCircleQte;
    private ConcertKeySequenceQTE activeSequenceQte;
    private Vector3 cameraTransitionStartPosition;
    private Quaternion cameraTransitionStartRotation;
    private ConcertPerformanceResult finalResult;

    /// <summary>Déclenché à la fin du concert avec le bilan de la prestation.</summary>
    public event Action<ConcertPerformanceResult> Completed;
    /// <summary>Satisfaction actuelle du public, de 0 à 100.</summary>
    public float Satisfaction => satisfaction;
    /// <summary>Score total de la prestation.</summary>
    public int Score => score;
    /// <summary>Temps restant avant le bilan.</summary>
    public float TimeRemaining => Mathf.Max(0f, sessionDuration - sessionTimer);
    /// <summary>Indique qu'un QTE circulaire ou une séquence est actuellement en cours.</summary>
    public bool IsQteActive => activeCircleQte != null || activeSequenceQte != null;
    /// <summary>Combo actuellement en cours.</summary>
    public int CurrentCombo => combo;
    /// <summary>QTE de séquence actif, ou nul lorsqu'aucune séquence ne se déroule.</summary>
    public ConcertKeySequenceQTE ActiveSequenceQte => activeSequenceQte;
    /// <summary>Dernier bilan calculé à la fin de la prestation.</summary>
    public ConcertPerformanceResult FinalResult => finalResult;

    private void Start()
    {
        StartPerformance();
    }

    private void Update()
    {
        if (!isRunning)
        {
            if (isFinished && !hasReportedResult && automaticallyReportResult)
            {
                SubmitResult();
            }

            return;
        }

        sessionTimer += Time.deltaTime;
        UpdateCinematicCamera();
        UpdateQteScheduler();

        if (sessionTimer >= sessionDuration)
        {
            FinishPerformance();
        }
    }

    /// <summary>Démarre une nouvelle prestation et réinitialise ses QTE, score, public et caméra.</summary>
    public void StartPerformance()
    {
        StopActiveQte();
        sessionDuration = Mathf.Max(MinimumSessionDuration, sessionDuration);
        delayBetweenQtes = Mathf.Max(0f, delayBetweenQtes);
        sequenceInterval = Mathf.Max(MinimumInterval, sequenceInterval);
        cameraShotDuration = Mathf.Max(MinimumInterval, cameraShotDuration);
        cameraTransitionDuration = Mathf.Max(MinimumInterval, cameraTransitionDuration);
        satisfaction = initialSatisfaction;
        sessionTimer = 0f;
        qteCooldownTimer = 0f;
        sequenceTimer = 0f;
        cameraShotTimer = 0f;
        cameraTransitionTimer = 0f;
        currentCameraShotIndex = 0;
        totalAccuracy = 0f;
        resolvedQteCount = 0;
        successfulQteCount = 0;
        failedQteCount = 0;
        perfectCount = 0;
        score = 0;
        combo = 0;
        maximumCombo = 0;
        isFinished = false;
        hasReportedResult = false;
        isRunning = true;
        SnapCameraToFirstShot();
        UpdateSatisfactionBar();
        LockPlayerControls();

        if (performerAnimator != null && !string.IsNullOrWhiteSpace(performanceTrigger))
        {
            performerAnimator.SetTrigger(performanceTrigger);
        }

        if (concertMusic != null && !concertMusic.isPlaying)
        {
            concertMusic.Play();
        }
    }

    /// <summary>Relance immédiatement le concert depuis le début.</summary>
    public void RestartPerformance()
    {
        StartPerformance();
    }

    /// <summary>Transmet le résultat au gestionnaire global de flux si celui-ci est présent.</summary>
    public void SubmitResult()
    {
        if (!isFinished || hasReportedResult)
        {
            return;
        }

        hasReportedResult = true;
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ReportMiniGameResult(BuildMiniGameResult());
        }
    }

    /// <summary>Reçoit la réussite d'un QTE circulaire et libère le créneau pour le prochain QTE.</summary>
    public void Hit(float accuracy)
    {
        if (activeCircleQte == null)
        {
            return;
        }

        activeCircleQte = null;
        RegisterTimingResult(Mathf.Clamp(accuracy, 0f, 100f), false);
        StartQteCooldown();
    }

    /// <summary>Reçoit l'expiration d'un QTE circulaire et libère le créneau pour le prochain QTE.</summary>
    public void Miss()
    {
        if (activeCircleQte == null)
        {
            return;
        }

        activeCircleQte = null;
        RegisterFailure();
        StartQteCooldown();
    }

    private void UpdateQteScheduler()
    {
        if (IsQteActive)
        {
            return;
        }

        if (qteCooldownTimer > 0f)
        {
            qteCooldownTimer -= Time.deltaTime;
            return;
        }

        sequenceTimer += Time.deltaTime;
        if (sequenceTimer >= sequenceInterval)
        {
            sequenceTimer = 0f;
            SpawnSequenceQte();
        }
        else
        {
            SpawnCircleQte();
        }
    }

    private void SpawnCircleQte()
    {
        if (spawnZone == null)
        {
            StartQteCooldown();
            return;
        }

        QTEConfig config = GetRandomQteConfig();
        if (config == null || config.prefab == null)
        {
            StartQteCooldown();
            return;
        }

        GameObject qteObject = Instantiate(config.prefab, spawnZone);
        RectTransform qteRectTransform = qteObject.GetComponent<RectTransform>();
        if (qteRectTransform != null)
        {
            qteRectTransform.anchoredPosition = GetSpawnPosition();
        }

        activeCircleQte = qteObject.GetComponent<CircleQTE>();
        if (activeCircleQte == null)
        {
            Destroy(qteObject);
            StartQteCooldown();
            return;
        }

        activeCircleQte.Initialize(config.duration, this);
    }

    private void SpawnSequenceQte()
    {
        GameObject sequenceObject = new GameObject("Concert_Key_Sequence_QTE");
        activeSequenceQte = sequenceObject.AddComponent<ConcertKeySequenceQTE>();
        activeSequenceQte.Initialize(sequenceDuration, HandleSequenceSuccess, HandleSequenceFailure);
    }

    private QTEConfig GetRandomQteConfig()
    {
        float totalWeight = 0f;
        foreach (QTEConfig config in qteConfigs)
        {
            if (IsValidCircleQteConfig(config))
            {
                totalWeight += config.spawnChance;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float accumulatedWeight = 0f;
        foreach (QTEConfig config in qteConfigs)
        {
            if (!IsValidCircleQteConfig(config))
            {
                continue;
            }

            accumulatedWeight += config.spawnChance;
            if (roll <= accumulatedWeight)
            {
                return config;
            }
        }

        return null;
    }

    private static bool IsValidCircleQteConfig(QTEConfig config)
    {
        return config != null
            && config.prefab != null
            && config.prefab.GetComponent<CircleQTE>() != null
            && config.spawnChance > 0f;
    }

    private Vector2 GetSpawnPosition()
    {
        return new Vector2(
            UnityEngine.Random.Range(-spawnZone.rect.width * 0.5f, spawnZone.rect.width * 0.5f),
            UnityEngine.Random.Range(-spawnZone.rect.height * 0.5f, spawnZone.rect.height * 0.5f));
    }

    private void HandleSequenceSuccess(float accuracy)
    {
        activeSequenceQte = null;
        RegisterTimingResult(accuracy, true);
        StartQteCooldown();
    }

    private void HandleSequenceFailure()
    {
        activeSequenceQte = null;
        RegisterFailure();
        StartQteCooldown();
    }

    private void StartQteCooldown()
    {
        qteCooldownTimer = delayBetweenQtes;
    }

    private void RegisterTimingResult(float accuracy, bool isSequence)
    {
        if (!isRunning)
        {
            return;
        }

        if (accuracy < GoodAccuracyThreshold)
        {
            RegisterFailure();
            return;
        }

        resolvedQteCount++;
        successfulQteCount++;
        totalAccuracy += accuracy;
        combo++;
        maximumCombo = Mathf.Max(maximumCombo, combo);

        if (accuracy >= PerfectAccuracyThreshold)
        {
            perfectCount++;
            score += PerfectScore + (isSequence ? SequenceBonusScore : 0);
            ChangeSatisfaction(perfectSatisfactionGain);
        }
        else
        {
            score += GoodScore + (isSequence ? SequenceBonusScore : 0);
            ChangeSatisfaction(goodSatisfactionGain);
        }
    }

    private void RegisterFailure()
    {
        if (!isRunning)
        {
            return;
        }

        resolvedQteCount++;
        failedQteCount++;
        combo = 0;
        ChangeSatisfaction(-failedSatisfactionLoss);
    }

    private void UpdateCinematicCamera()
    {
        if (cinematicCamera == null || cinematicCameraShots == null || cinematicCameraShots.Length == 0)
        {
            return;
        }

        if (isCameraTransitioning)
        {
            cameraTransitionTimer += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(cameraTransitionTimer / cameraTransitionDuration);
            float easedTime = cameraTransitionCurve != null ? cameraTransitionCurve.Evaluate(normalizedTime) : normalizedTime;
            Transform targetShot = cinematicCameraShots[currentCameraShotIndex];

            if (targetShot != null)
            {
                cinematicCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(cameraTransitionStartPosition, targetShot.position, easedTime),
                    Quaternion.Slerp(cameraTransitionStartRotation, targetShot.rotation, easedTime));
            }

            if (normalizedTime >= 1f)
            {
                isCameraTransitioning = false;
                cameraShotTimer = 0f;
            }

            return;
        }

        cameraShotTimer += Time.deltaTime;
        if (cameraShotTimer >= cameraShotDuration)
        {
            StartNextCameraTransition();
        }
    }

    private void SnapCameraToFirstShot()
    {
        isCameraTransitioning = false;
        if (cinematicCamera == null || cinematicCameraShots == null || cinematicCameraShots.Length == 0)
        {
            return;
        }

        Transform firstShot = cinematicCameraShots[0];
        if (firstShot != null)
        {
            cinematicCamera.transform.SetPositionAndRotation(firstShot.position, firstShot.rotation);
        }
    }

    private void StartNextCameraTransition()
    {
        if (cinematicCameraShots.Length < 2)
        {
            cameraShotTimer = 0f;
            return;
        }

        int nextShotIndex = FindNextCameraShotIndex();
        if (nextShotIndex == currentCameraShotIndex)
        {
            cameraShotTimer = 0f;
            return;
        }

        currentCameraShotIndex = nextShotIndex;
        cameraTransitionStartPosition = cinematicCamera.transform.position;
        cameraTransitionStartRotation = cinematicCamera.transform.rotation;
        cameraTransitionTimer = 0f;
        isCameraTransitioning = true;
    }

    private int FindNextCameraShotIndex()
    {
        for (int offset = 1; offset < cinematicCameraShots.Length; offset++)
        {
            int candidateIndex = (currentCameraShotIndex + offset) % cinematicCameraShots.Length;
            if (cinematicCameraShots[candidateIndex] != null)
            {
                return candidateIndex;
            }
        }

        return currentCameraShotIndex;
    }

    private void ChangeSatisfaction(float delta)
    {
        satisfaction = Mathf.Clamp(satisfaction + delta, 0f, 100f);
        UpdateSatisfactionBar();
    }

    private void UpdateSatisfactionBar()
    {
        if (satisfactionBar != null)
        {
            satisfactionBar.normalizedValue = satisfaction / 100f;
        }
    }

    private void FinishPerformance()
    {
        isRunning = false;
        isFinished = true;
        StopActiveQte();
        UnlockPlayerControls();

        if (concertMusic != null && concertMusic.isPlaying)
        {
            concertMusic.Stop();
        }

        finalResult = BuildPerformanceResult();
        Completed?.Invoke(finalResult);
    }

    private void StopActiveQte()
    {
        if (activeCircleQte != null)
        {
            Destroy(activeCircleQte.gameObject);
            activeCircleQte = null;
        }

        if (activeSequenceQte != null)
        {
            Destroy(activeSequenceQte.gameObject);
            activeSequenceQte = null;
        }
    }

    private void LockPlayerControls()
    {
        disabledControls.Clear();
        foreach (Behaviour control in playerControlsToDisable)
        {
            if (control != null && control.enabled)
            {
                control.enabled = false;
                disabledControls.Add(control);
            }
        }
    }

    private void UnlockPlayerControls()
    {
        foreach (Behaviour control in disabledControls)
        {
            if (control != null)
            {
                control.enabled = true;
            }
        }

        disabledControls.Clear();
    }

    private ConcertPerformanceResult BuildPerformanceResult()
    {
        float averageAccuracy = resolvedQteCount > 0 ? totalAccuracy / resolvedQteCount : 0f;
        float performanceValue = satisfaction * 0.65f + averageAccuracy * 0.35f;

        return new ConcertPerformanceResult(
            GetGrade(performanceValue), score, satisfaction, averageAccuracy, successfulQteCount,
            failedQteCount, perfectCount, maximumCombo, GetImprovementFeedback(averageAccuracy));
    }

    private MiniGameResult BuildMiniGameResult()
    {
        return new MiniGameResult
        {
            Id = MiniGameId.Stage,
            RawScore = score,
            Accuracy = finalResult.AverageAccuracy / 100f,
            TargetStat = StatType.Stage,
            StatGain = Mathf.RoundToInt(Mathf.Lerp(-5f, 15f, finalResult.AverageAccuracy / 100f)),
            Completed = true
        };
    }

    private static string GetGrade(float performanceValue)
    {
        if (performanceValue >= SatisfactionAtGradeS) return "S";
        if (performanceValue >= SatisfactionAtGradeA) return "A";
        if (performanceValue >= SatisfactionAtGradeB) return "B";
        if (performanceValue >= SatisfactionAtGradeC) return "C";
        return "D";
    }

    private string GetImprovementFeedback(float averageAccuracy)
    {
        if (satisfaction < SatisfactionAtGradeC) return "Le public décroche vite : sécurise les QTE avant de chercher la perfection.";
        if (averageAccuracy < GoodAccuracyThreshold) return "Travaille le rythme : attends que l'anneau arrive sur la cible avant de valider.";
        if (perfectCount == 0) return "Bonne régularité. Vise davantage de validations PARFAIT pour faire monter l'ambiance.";
        if (failedQteCount > successfulQteCount / 3) return "Les séquences rapides restent fragiles : lis les touches dans l'ordre, sans précipitation.";
        return "Excellente présence scénique : conserve ce timing et fais grandir tes combos.";
    }

    private void OnGUI()
    {
        // L'interface est construite dans la scène et alimentée par ConcertUIController.
    }
}

/// <summary>Résumé de la performance de concert, prêt à être affiché dans une interface de bilan.</summary>
public struct ConcertPerformanceResult
{
    /// <summary>Note finale de D à S.</summary>
    public string Grade { get; }
    /// <summary>Score total obtenu.</summary>
    public int Score { get; }
    /// <summary>Satisfaction finale du public.</summary>
    public float FinalSatisfaction { get; }
    /// <summary>Précision moyenne des QTE résolus.</summary>
    public float AverageAccuracy { get; }
    /// <summary>Nombre de QTE réussis.</summary>
    public int SuccessfulQteCount { get; }
    /// <summary>Nombre de QTE ratés.</summary>
    public int FailedQteCount { get; }
    /// <summary>Nombre de validations parfaites.</summary>
    public int PerfectCount { get; }
    /// <summary>Meilleur combo atteint.</summary>
    public int MaximumCombo { get; }
    /// <summary>Conseil généré depuis la performance.</summary>
    public string ImprovementFeedback { get; }

    /// <summary>Construit un résumé complet de la prestation.</summary>
    public ConcertPerformanceResult(string grade, int score, float finalSatisfaction, float averageAccuracy, int successfulQteCount, int failedQteCount, int perfectCount, int maximumCombo, string improvementFeedback)
    {
        Grade = grade;
        Score = score;
        FinalSatisfaction = finalSatisfaction;
        AverageAccuracy = averageAccuracy;
        SuccessfulQteCount = successfulQteCount;
        FailedQteCount = failedQteCount;
        PerfectCount = perfectCount;
        MaximumCombo = maximumCombo;
        ImprovementFeedback = improvementFeedback;
    }
}
