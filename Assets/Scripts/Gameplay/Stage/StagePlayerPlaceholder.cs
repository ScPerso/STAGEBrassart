using System.Collections;
using UnityEngine;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Représentation temporaire du personnage au centre de l'écran, en attendant
    /// l'intégration d'un vrai modèle animé. Réagit à chaque cible réussie par une
    /// courte pulsation d'échelle qui mime un mouvement de danse.
    /// </summary>
    public class StagePlayerPlaceholder : MonoBehaviour
    {
        [Tooltip("Multiplicateur d'échelle atteint au pic de la pulsation de danse.")]
        [SerializeField] private float danceScalePunch = 1.25f;

        [Tooltip("Durée totale de la pulsation de danse, en secondes.")]
        [SerializeField] private float danceDuration = 0.2f;

        private RectTransform selfRectTransform;
        private Vector3 baseScale;
        private Coroutine danceRoutine;

        private void Awake()
        {
            selfRectTransform = GetComponent<RectTransform>();
            baseScale = selfRectTransform.localScale;
        }

        /// <summary>Joue une pulsation d'échelle représentant la danse, déclenchée à chaque cible réussie.</summary>
        public void TriggerDanceAnimation()
        {
            if (danceRoutine != null)
            {
                StopCoroutine(danceRoutine);
            }

            danceRoutine = StartCoroutine(PlayDancePunch());
        }

        private IEnumerator PlayDancePunch()
        {
            float elapsed = 0f;
            float halfDuration = danceDuration * 0.5f;

            while (elapsed < danceDuration)
            {
                elapsed += Time.deltaTime;

                float pulseFactor = elapsed < halfDuration
                    ? elapsed / halfDuration
                    : 1f - (elapsed - halfDuration) / halfDuration;

                selfRectTransform.localScale = baseScale * Mathf.Lerp(1f, danceScalePunch, pulseFactor);
                yield return null;
            }

            selfRectTransform.localScale = baseScale;
            danceRoutine = null;
        }
    }
}
