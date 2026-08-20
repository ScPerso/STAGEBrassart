using System.Collections;
using Magma.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// HUD de session (IMGUI, placeholder léger) et bilan de fin (UI réelle) du mini-jeu de
    /// prestation scénique. Le bilan affiche le score, la précision, puis anime la jauge de
    /// Prestation Scénique de sa valeur actuelle vers sa nouvelle valeur pour rendre visible le
    /// gain (ou la perte) obtenu. Un seul bouton "Continuer" remonte le résultat à la boucle de
    /// jeu : il n'y a pas de rejeu depuis cet écran.
    /// </summary>
    [RequireComponent(typeof(StageRhythmManager))]
    public class StageResultUI : MonoBehaviour
    {
        /// <summary>Valeur maximale d'une jauge de statistique, pour convertir une valeur en fraction 0-1.</summary>
        private const float MaxStatValue = 100f;

        /// <summary>Durée de l'animation de remplissage de la jauge pendant le bilan, en secondes.</summary>
        private const float GaugeAnimationDuration = 1.2f;

        [Header("Panneau de bilan")]
        [Tooltip("Racine du panneau de bilan, masquée tant que la session n'est pas terminée.")]
        [SerializeField] private GameObject resultPanel;

        [SerializeField] private TextMeshProUGUI scoreLabel;
        [SerializeField] private TextMeshProUGUI accuracyLabel;

        [Header("Jauge de Prestation Scénique (barre verticale, remplissage bas -> haut)")]
        [SerializeField] private Image stageGauge;
        [SerializeField] private TextMeshProUGUI stageGaugeValueLabel;

        [Header("Pourcentage qui se déverse dans la jauge")]
        [Tooltip("Affiche le pourcentage de réussite, qui décroît vers 0 pendant que la jauge se remplit, pour matérialiser le transfert.")]
        [SerializeField] private TextMeshProUGUI transferValueLabel;

        [Tooltip("Bouton Continuer, désactivé pendant l'animation pour forcer à voir le résultat avant de continuer.")]
        [SerializeField] private Button continueButton;

        private StageRhythmManager stageRhythmManager;
        private MiniGameResult result;
        private Coroutine gaugeAnimationRoutine;

        private void Awake()
        {
            stageRhythmManager = GetComponent<StageRhythmManager>();
            ConfigureVerticalGauge(stageGauge);

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            stageRhythmManager.Completed += OnSessionCompleted;
        }

        private void OnDisable()
        {
            stageRhythmManager.Completed -= OnSessionCompleted;
        }

        private static void ConfigureVerticalGauge(Image gauge)
        {
            if (gauge == null)
            {
                return;
            }

            // Rectangle bleu plein sans sprite : la hauteur (anchorMax.y) représente la valeur,
            // sans passer par Image.Type.Filled pour éviter tout artefact d'étirement.
            gauge.type = Image.Type.Simple;
            gauge.sprite = null;

            RectTransform rect = gauge.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }

        private void OnSessionCompleted(MiniGameResult sessionResult)
        {
            result = sessionResult;

            int previousStageValue = GameFlowManager.Instance != null
                ? GameFlowManager.Instance.GetStat(StatType.Stage)
                : 0;
            int previewStageValue = Mathf.Clamp(previousStageValue + result.StatGain, 0, (int)MaxStatValue);
            float accuracyPercent = result.Accuracy * 100f;

            if (scoreLabel != null)
            {
                scoreLabel.text = "Reussites : " + result.RawScore;
            }

            if (accuracyLabel != null)
            {
                accuracyLabel.text = "Precision : " + accuracyPercent.ToString("F0") + "%";
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            if (continueButton != null)
            {
                // Le bouton reste inactif tant que le transfert du pourcentage vers la jauge n'est pas visible.
                continueButton.interactable = false;
            }

            if (gaugeAnimationRoutine != null)
            {
                StopCoroutine(gaugeAnimationRoutine);
            }

            gaugeAnimationRoutine = StartCoroutine(PourAccuracyIntoGauge(accuracyPercent, previousStageValue, previewStageValue));
        }

        /// <summary>
        /// Anime simultanément le remplissage de la jauge (de sa valeur actuelle vers la nouvelle) et
        /// la décroissance du pourcentage affiché vers 0, pour matérialiser visuellement le pourcentage
        /// de réussite qui "se déverse" dans la jauge de Prestation Scénique.
        /// </summary>
        private IEnumerator PourAccuracyIntoGauge(float accuracyPercent, float fromGaugeValue, float toGaugeValue)
        {
            ApplyGaugeValue(fromGaugeValue);
            ApplyTransferValue(accuracyPercent);

            float elapsed = 0f;

            while (elapsed < GaugeAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / GaugeAnimationDuration);
                ApplyGaugeValue(Mathf.Lerp(fromGaugeValue, toGaugeValue, t));
                ApplyTransferValue(Mathf.Lerp(accuracyPercent, 0f, t));
                yield return null;
            }

            ApplyGaugeValue(toGaugeValue);
            ApplyTransferValue(0f);
            gaugeAnimationRoutine = null;

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }

        private void ApplyGaugeValue(float value)
        {
            if (stageGauge != null)
            {
                Vector2 anchorMax = stageGauge.rectTransform.anchorMax;
                anchorMax.x = 1f;
                anchorMax.y = Mathf.Clamp01(value / MaxStatValue);
                stageGauge.rectTransform.anchorMax = anchorMax;
            }

            if (stageGaugeValueLabel != null)
            {
                stageGaugeValueLabel.text = Mathf.RoundToInt(value).ToString();
            }
        }

        private void ApplyTransferValue(float percentRemaining)
        {
            if (transferValueLabel != null)
            {
                transferValueLabel.text = Mathf.RoundToInt(percentRemaining) + "%";
            }
        }

        /// <summary>Remonte le résultat à la boucle de jeu. Branché sur le bouton "Continuer" du bilan.</summary>
        public void OnContinueClicked()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ReportMiniGameResult(result);
            }
            else
            {
                Debug.LogWarning("StageResultUI: no GameFlowManager instance found, cannot report the mini-game result.");
            }
        }

        private void OnGUI()
        {
            if (resultPanel == null || !resultPanel.activeSelf)
            {
                DrawHud();
            }
        }

        private void DrawHud()
        {
            string hudText = "Reussites : " + stageRhythmManager.SuccessCount
                + "   Rates : " + stageRhythmManager.FailureCount
                + "   Temps : " + stageRhythmManager.TimeRemaining.ToString("F0") + "s";

            GUI.Label(new Rect(0f, 12f, Screen.width, 30f), hudText, BuildCenteredStyle());
        }

        private static GUIStyle BuildCenteredStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
