using System;
using System.Collections.Generic;
using Magma.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Orchestrateur du mini-jeu de prestation scénique : fait défiler des cibles depuis
    /// le bord droit de l'écran, juge le timing des clics/taps du joueur par rapport à la
    /// position centrale, et renvoie le résultat final (score, précision) en fin de session.
    /// </summary>
    public class StageRhythmManager : MonoBehaviour
    {
        /// <summary>Gain de statistique accordé pour une session à 0% de précision (pénalité).</summary>
        private const float StatGainAtZeroAccuracy = -5f;

        /// <summary>Gain de statistique accordé pour une session à 100% de précision.</summary>
        private const float StatGainAtFullAccuracy = 15f;

        [Header("Cibles")]
        [Tooltip("Sprites piochés aléatoirement à chaque apparition de cible.")]
        [SerializeField] private Sprite[] targetSprites;

        [Tooltip("Prefab de cible instancié à chaque spawn (Image + StageTarget).")]
        [SerializeField] private StageTarget targetPrefab;

        [Tooltip("Parent des cibles instanciées (RectTransform du Canvas ou d'un conteneur dédié).")]
        [SerializeField] private RectTransform targetsParent;

        [Tooltip("Point d'apparition des cibles, au bord droit de l'écran.")]
        [SerializeField] private RectTransform spawnPoint;

        [Tooltip("Position du joueur, au centre de l'écran, contre laquelle le timing est jugé.")]
        [SerializeField] private RectTransform playerPosition;

        [Header("Réglages de rythme")]
        [Tooltip("Vitesse de défilement horizontal des cibles, en unités de Canvas par seconde.")]
        [SerializeField] private float scrollSpeed = 400f;

        [Tooltip("Intervalle initial entre deux apparitions de cible, en secondes, en début de session.")]
        [SerializeField] private float spawnInterval = 1f;

        [Tooltip("Intervalle minimal entre deux apparitions de cible, jamais franchi même en fin de session.")]
        [SerializeField] private float minimumSpawnInterval = 0.6f;

        [Tooltip("Réduction de l'intervalle de spawn par seconde de session écoulée (accélération progressive du rythme).")]
        [SerializeField] private float spawnIntervalAcceleration = 0.015f;

        [Tooltip("Tolérance de distance en X (unités de Canvas) pour valider un succès.")]
        [SerializeField] private float hitTolerance = 60f;

        [Tooltip("Durée totale d'une session, en secondes.")]
        [SerializeField] private float sessionDuration = 30f;

        [Header("Retour visuel")]
        [Tooltip("Placeholder temporaire représentant le personnage, tant qu'aucun modèle n'est intégré.")]
        [SerializeField] private StagePlayerPlaceholder playerPlaceholder;

        private readonly List<StageTarget> activeTargets = new List<StageTarget>();

        private float spawnTimer;
        private float sessionTimer;
        private bool isRunning;
        private int successCount;
        private int failureCount;

        /// <summary>Déclenché à la fin de la session, avec le résultat final du mini-jeu.</summary>
        public event Action<MiniGameResult> Completed;

        /// <summary>Nombre de cibles réussies depuis le début de la session courante.</summary>
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

        private void SpawnTarget()
        {
            if (targetPrefab == null || spawnPoint == null || targetSprites == null || targetSprites.Length == 0)
            {
                return;
            }

            Transform parent = targetsParent != null ? targetsParent : spawnPoint.parent;
            StageTarget target = Instantiate(targetPrefab, parent);

            RectTransform targetRectTransform = target.GetComponent<RectTransform>();
            targetRectTransform.anchoredPosition = spawnPoint.anchoredPosition;

            Sprite randomSprite = targetSprites[UnityEngine.Random.Range(0, targetSprites.Length)];
            target.Initialize(randomSprite, scrollSpeed);

            activeTargets.Add(target);
        }

        private void UpdateActiveTargets()
        {
            float playerX = playerPosition != null ? playerPosition.anchoredPosition.x : 0f;
            float missThresholdX = playerX - hitTolerance;

            for (int i = activeTargets.Count - 1; i >= 0; i--)
            {
                StageTarget target = activeTargets[i];

                if (target == null)
                {
                    activeTargets.RemoveAt(i);
                    continue;
                }

                if (target.AnchoredX < missThresholdX)
                {
                    RegisterFailure();
                    Destroy(target.gameObject);
                    activeTargets.RemoveAt(i);
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

            if (closestDistance <= hitTolerance)
            {
                RegisterSuccess();
            }
            else
            {
                RegisterFailure();
            }

            activeTargets.Remove(closestTarget);
            Destroy(closestTarget.gameObject);
        }

        private StageTarget FindClosestTarget(out float closestDistance)
        {
            float playerX = playerPosition != null ? playerPosition.anchoredPosition.x : 0f;

            StageTarget closestTarget = null;
            closestDistance = float.MaxValue;

            foreach (StageTarget target in activeTargets)
            {
                if (target == null)
                {
                    continue;
                }

                float distance = Mathf.Abs(target.AnchoredX - playerX);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        private void RegisterSuccess()
        {
            successCount++;

            if (playerPlaceholder != null)
            {
                playerPlaceholder.TriggerDanceAnimation();
            }
        }

        private void RegisterFailure()
        {
            failureCount++;
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
            int totalAttempts = successCount + failureCount;
            float accuracy = totalAttempts > 0 ? (float)successCount / totalAttempts : 0f;

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
