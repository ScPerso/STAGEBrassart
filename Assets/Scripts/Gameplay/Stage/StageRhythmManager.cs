using System;
using System.Collections.Generic;
using Magma.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Orchestrateur du mini-jeu de prestation scénique : fait défiler des cibles depuis un point
    /// choisi au hasard sur un cercle autour du joueur (angle imprévisible, pas seulement 4 côtés
    /// fixes), chacune filant en ligne droite vers le joueur. Juge le timing des clics/taps du
    /// joueur par rapport à la cible la plus proche, à l'aide de zones circulaires concentriques
    /// (comme un oignon : Parfait au centre, puis Bien, puis Ok, puis raté au-delà), et renvoie le
    /// résultat final (score, précision) en fin de session.
    /// </summary>
    public class StageRhythmManager : MonoBehaviour
    {
        /// <summary>Gain de statistique accordé pour une session à 0% de précision (pénalité).</summary>
        private const float StatGainAtZeroAccuracy = -5f;

        /// <summary>Gain de statistique accordé pour une session à 100% de précision.</summary>
        private const float StatGainAtFullAccuracy = 15f;

        /// <summary>Poids de qualité d'un jugement Parfait dans le calcul de la précision globale.</summary>
        private const float PerfectQualityWeight = 1f;

        /// <summary>Poids de qualité d'un jugement Bien dans le calcul de la précision globale.</summary>
        private const float GoodQualityWeight = 0.7f;

        /// <summary>Poids de qualité d'un jugement Ok dans le calcul de la précision globale.</summary>
        private const float OkQualityWeight = 0.4f;

        /// <summary>Poids de qualité d'un raté dans le calcul de la précision globale.</summary>
        private const float MissQualityWeight = 0f;

        [Header("Cibles")]
        [Tooltip("Sprites piochés aléatoirement à chaque apparition de cible.")]
        [SerializeField] private Sprite[] targetSprites;

        [Tooltip("Prefab de cible instancié à chaque spawn (Image + StageTarget).")]
        [SerializeField] private StageTarget targetPrefab;

        [Tooltip("Parent des cibles instanciées (RectTransform du Canvas ou d'un conteneur dédié).")]
        [SerializeField] private RectTransform targetsParent;

        [Tooltip("Position du joueur, au centre de l'écran, contre laquelle le timing est jugé.")]
        [SerializeField] private RectTransform playerPosition;

        [Header("Spawn circulaire")]
        [Tooltip("Distance (unités de Canvas) entre le joueur et le point de spawn d'une cible. Un angle aléatoire (0-360°) est tiré à chaque apparition, donc les cibles arrivent de partout et pas seulement de 4 points fixes. Doit rester assez grand pour spawn hors écran même sur un téléphone en portrait, où il y a beaucoup plus d'espace vertical que latéral.")]
        [SerializeField] private float spawnRadius = 700f;

        [Header("Réglages de rythme")]
        [Tooltip("Vitesse de défilement des cibles, en unités de Canvas par seconde.")]
        [SerializeField] private float scrollSpeed = 400f;

        [Tooltip("Intervalle initial entre deux apparitions de cible, en secondes, en début de session.")]
        [SerializeField] private float spawnInterval = 1f;

        [Tooltip("Intervalle minimal entre deux apparitions de cible, jamais franchi même en fin de session.")]
        [SerializeField] private float minimumSpawnInterval = 0.6f;

        [Tooltip("Réduction de l'intervalle de spawn par seconde de session écoulée (accélération progressive du rythme).")]
        [SerializeField] private float spawnIntervalAcceleration = 0.015f;

        [Header("Zones de jugement (cercles concentriques autour du joueur, comme un oignon)")]
        [Tooltip("Rayon de la zone Parfait, la plus centrale.")]
        [SerializeField] private float perfectRadius = 20f;

        [Tooltip("Rayon de la zone Bien (englobe la zone Parfait).")]
        [SerializeField] private float goodRadius = 40f;

        [Tooltip("Rayon de la zone Ok, la plus large (englobe les zones précédentes) ; au-delà, c'est un raté.")]
        [SerializeField] private float okRadius = 65f;

        [Tooltip("Durée totale d'une session, en secondes.")]
        [SerializeField] private float sessionDuration = 30f;

        [Header("Retour visuel")]
        [Tooltip("Placeholder temporaire représentant le personnage, tant qu'aucun modèle n'est intégré.")]
        [SerializeField] private StagePlayerPlaceholder playerPlaceholder;

        [Tooltip("Prefab de texte flottant affiché au point de jugement (RATE/OK/BIEN/PARFAIT).")]
        [SerializeField] private StageJudgementFeedback feedbackPrefab;

        [Tooltip("Couleur du texte de feedback pour un raté.")]
        [SerializeField] private Color missFeedbackColor = new Color(0.95f, 0.25f, 0.25f);

        [Tooltip("Couleur du texte de feedback pour un Ok.")]
        [SerializeField] private Color okFeedbackColor = new Color(0.85f, 0.85f, 0.35f);

        [Tooltip("Couleur du texte de feedback pour un Bien.")]
        [SerializeField] private Color goodFeedbackColor = new Color(0.35f, 0.85f, 0.45f);

        [Tooltip("Couleur du texte de feedback pour un Parfait (doré).")]
        [SerializeField] private Color perfectFeedbackColor = new Color(1f, 0.84f, 0f);

        private readonly List<StageTarget> activeTargets = new List<StageTarget>();

        private float spawnTimer;
        private float sessionTimer;
        private bool isRunning;
        private int successCount;
        private int failureCount;
        private float qualitySum;
        private int judgedCount;

        /// <summary>Déclenché à la fin de la session, avec le résultat final du mini-jeu.</summary>
        public event Action<MiniGameResult> Completed;

        /// <summary>Nombre de cibles réussies (Ok, Bien ou Parfait) depuis le début de la session courante.</summary>
        public int SuccessCount => successCount;

        /// <summary>Nombre de cibles ratées depuis le début de la session courante.</summary>
        public int FailureCount => failureCount;

        /// <summary>Temps restant avant la fin de la session, en secondes.</summary>
        public float TimeRemaining => Mathf.Max(0f, sessionDuration - sessionTimer);

        private void Start()
        {
            StartSession();
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            UpdateSpawning();
            UpdateActiveTargets();
            HandleInput();
            UpdateSessionTimer();
        }

        /// <summary>Démarre (ou relance) une session de mini-jeu depuis le début.</summary>
        public void StartSession()
        {
            ClearActiveTargets();

            spawnTimer = 0f;
            sessionTimer = 0f;
            successCount = 0;
            failureCount = 0;
            qualitySum = 0f;
            judgedCount = 0;
            isRunning = true;
        }

        private void UpdateSpawning()
        {
            spawnTimer += Time.deltaTime;

            float currentSpawnInterval = GetCurrentSpawnInterval();

            if (spawnTimer < currentSpawnInterval)
            {
                return;
            }

            spawnTimer -= currentSpawnInterval;
            SpawnTarget();
        }

        /// <summary>
        /// Intervalle de spawn courant : diminue linéairement au fil de la session pour
        /// accélérer progressivement le rythme, sans jamais descendre sous le minimum réglé.
        /// </summary>
        private float GetCurrentSpawnInterval()
        {
            float elapsedReduction = sessionTimer * spawnIntervalAcceleration;

            return Mathf.Max(minimumSpawnInterval, spawnInterval - elapsedReduction);
        }

        /// <summary>
        /// Fait apparaître une cible à un angle aléatoire (0-360°) autour du joueur, à
        /// <see cref="spawnRadius"/> de distance, pour qu'elle puisse arriver de n'importe quelle
        /// direction plutôt que d'un nombre fixe et prévisible de points.
        /// </summary>
        private void SpawnTarget()
        {
            if (targetPrefab == null || targetSprites == null || targetSprites.Length == 0)
            {
                return;
            }

            Vector2 playerAnchoredPosition = playerPosition != null ? playerPosition.anchoredPosition : Vector2.zero;

            float angleRadians = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector2 offsetFromPlayer = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * spawnRadius;
            Vector2 spawnPosition = playerAnchoredPosition + offsetFromPlayer;

            Transform parent = targetsParent != null ? (Transform)targetsParent : transform;
            StageTarget target = Instantiate(targetPrefab, parent);

            RectTransform targetRectTransform = target.GetComponent<RectTransform>();
            targetRectTransform.anchoredPosition = spawnPosition;

            Vector2 direction = playerAnchoredPosition - spawnPosition;

            Sprite randomSprite = targetSprites[UnityEngine.Random.Range(0, targetSprites.Length)];
            target.Initialize(randomSprite, scrollSpeed, direction);

            activeTargets.Add(target);
        }

        private void UpdateActiveTargets()
        {
            Vector2 playerAnchoredPosition = playerPosition != null ? playerPosition.anchoredPosition : Vector2.zero;

            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                StageTarget target = activeTargets[i];

                if (target == null)
                {
                    activeTargets.RemoveAt(i);
                    continue;
                }

                // La cible a dépassé le joueur de plus que la zone Ok sans avoir été cliquée : ratée.
                if (target.SignedDistanceAlongDirection(playerAnchoredPosition) < -okRadius)
                {
                    RegisterJudgement(StageHitJudgement.Miss, target.AnchoredPosition);
                    activeTargets.Remove(target);
                    Destroy(target.gameObject);
                }
            }
        }

        private void HandleInput()
        {
            bool clickedThisFrame = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

            if (clickedThisFrame)
            {
                JudgeClosestTarget();
            }
        }

        private void JudgeClosestTarget()
        {
            StageTarget closestTarget = FindClosestTarget(out float closestDistance);

            if (closestTarget == null)
            {
                return;
            }

            StageHitJudgement judgement = EvaluateDistance(closestDistance);
            RegisterJudgement(judgement, closestTarget.AnchoredPosition);

            activeTargets.Remove(closestTarget);
            Destroy(closestTarget.gameObject);
        }

        /// <summary>Détermine le jugement selon la distance entre la cible et le joueur, du centre vers l'extérieur.</summary>
        private StageHitJudgement EvaluateDistance(float distance)
        {
            if (distance <= perfectRadius)
            {
                return StageHitJudgement.Perfect;
            }

            if (distance <= goodRadius)
            {
                return StageHitJudgement.Good;
            }

            if (distance <= okRadius)
            {
                return StageHitJudgement.Ok;
            }

            return StageHitJudgement.Miss;
        }

        private StageTarget FindClosestTarget(out float closestDistance)
        {
            Vector2 playerAnchoredPosition = playerPosition != null ? playerPosition.anchoredPosition : Vector2.zero;

            StageTarget closestTarget = null;
            closestDistance = float.MaxValue;

            foreach (StageTarget target in activeTargets)
            {
                if (target == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(target.AnchoredPosition, playerAnchoredPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        /// <summary>
        /// Comptabilise un jugement (succès/échec, précision pondérée) et affiche le petit texte de
        /// feedback correspondant à l'endroit de la cible.
        /// </summary>
        private void RegisterJudgement(StageHitJudgement judgement, Vector2 position)
        {
            judgedCount++;
            qualitySum += GetQualityWeight(judgement);

            if (judgement == StageHitJudgement.Miss)
            {
                failureCount++;
            }
            else
            {
                successCount++;

                if (playerPlaceholder != null)
                {
                    playerPlaceholder.TriggerDanceAnimation();
                }
            }

            SpawnJudgementFeedback(judgement, position);
        }

        private static float GetQualityWeight(StageHitJudgement judgement)
        {
            switch (judgement)
            {
                case StageHitJudgement.Perfect:
                    return PerfectQualityWeight;
                case StageHitJudgement.Good:
                    return GoodQualityWeight;
                case StageHitJudgement.Ok:
                    return OkQualityWeight;
                default:
                    return MissQualityWeight;
            }
        }

        private void SpawnJudgementFeedback(StageHitJudgement judgement, Vector2 position)
        {
            if (feedbackPrefab == null)
            {
                return;
            }

            Transform parent = targetsParent != null ? (Transform)targetsParent : transform;
            StageJudgementFeedback feedback = Instantiate(feedbackPrefab, parent);

            RectTransform feedbackRectTransform = feedback.GetComponent<RectTransform>();
            feedbackRectTransform.anchoredPosition = position;

            feedback.Show(GetJudgementLabel(judgement), GetJudgementColor(judgement));
        }

        private static string GetJudgementLabel(StageHitJudgement judgement)
        {
            switch (judgement)
            {
                case StageHitJudgement.Perfect:
                    return "PARFAIT";
                case StageHitJudgement.Good:
                    return "BIEN";
                case StageHitJudgement.Ok:
                    return "OK";
                default:
                    return "RATE";
            }
        }

        private Color GetJudgementColor(StageHitJudgement judgement)
        {
            switch (judgement)
            {
                case StageHitJudgement.Perfect:
                    return perfectFeedbackColor;
                case StageHitJudgement.Good:
                    return goodFeedbackColor;
                case StageHitJudgement.Ok:
                    return okFeedbackColor;
                default:
                    return missFeedbackColor;
            }
        }

        private void UpdateSessionTimer()
        {
            sessionTimer += Time.deltaTime;

            if (sessionTimer >= sessionDuration)
            {
                EndSession();
            }
        }

        private void EndSession()
        {
            isRunning = false;
            ClearActiveTargets();

            Completed?.Invoke(BuildResult());
        }

        private void ClearActiveTargets()
        {
            foreach (StageTarget target in activeTargets)
            {
                if (target != null)
                {
                    Destroy(target.gameObject);
                }
            }

            activeTargets.Clear();
        }

        private MiniGameResult BuildResult()
        {
            // Précision pondérée par la qualité de chaque jugement (Parfait > Bien > Ok > raté),
            // et non plus un simple ratio de réussites binaires.
            float accuracy = judgedCount > 0 ? qualitySum / judgedCount : 0f;

            // Gain proportionnel à la précision : 0% -> StatGainAtZeroAccuracy (pénalité),
            // 100% -> StatGainAtFullAccuracy, interpolé linéairement entre les deux.
            float statGain = Mathf.Lerp(StatGainAtZeroAccuracy, StatGainAtFullAccuracy, accuracy);

            return new MiniGameResult
            {
                Id = MiniGameId.Stage,
                RawScore = successCount,
                Accuracy = accuracy,
                TargetStat = StatType.Stage,
                StatGain = Mathf.RoundToInt(statGain),
                Completed = true
            };
        }
    }
}
