using System.Collections;
using TMPro;
using UnityEngine;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Petit texte de jugement (RATE/OK/BIEN/PARFAIT) qui apparaît à l'endroit d'une cible jugée,
    /// flotte légèrement vers le haut en s'estompant, puis se détruit automatiquement.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class StageJudgementFeedback : MonoBehaviour
    {
        [Tooltip("Durée totale d'affichage avant disparition complète, en secondes.")]
        [SerializeField] private float lifetime = 0.6f;

        [Tooltip("Distance parcourue vers le haut pendant la durée de vie, en unités de Canvas.")]
        [SerializeField] private float floatUpDistance = 40f;

        private RectTransform selfRectTransform;
        private TextMeshProUGUI label;

        private void Awake()
        {
            selfRectTransform = GetComponent<RectTransform>();
            label = GetComponent<TextMeshProUGUI>();
        }

        /// <summary>Configure le texte et la couleur du jugement, puis démarre l'animation de disparition.</summary>
        public void Show(string text, Color color)
        {
            label.text = text;
            label.color = color;
            StartCoroutine(PlayAndDestroy());
        }

        private IEnumerator PlayAndDestroy()
        {
            Vector2 startPosition = selfRectTransform.anchoredPosition;
            Vector2 endPosition = startPosition + Vector2.up * floatUpDistance;
            Color startColor = label.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);

                selfRectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
                label.color = Color.Lerp(startColor, endColor, t);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
