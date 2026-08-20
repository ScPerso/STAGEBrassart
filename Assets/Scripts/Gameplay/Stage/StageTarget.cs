using UnityEngine;
using UnityEngine.UI;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Cible de rythme qui défile horizontalement vers la gauche à vitesse constante.
    /// Reste purement passive : le déplacement et l'apparence sont gérés ici, tout le
    /// jugement (succès/échec) reste centralisé dans <see cref="StageRhythmManager"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class StageTarget : MonoBehaviour
    {
        private RectTransform selfRectTransform;
        private Image targetImage;
        private float scrollSpeedPerSecond;

        /// <summary>Position horizontale courante de la cible dans l'espace du Canvas.</summary>
        public float AnchoredX => selfRectTransform.anchoredPosition.x;

        private void Awake()
        {
            selfRectTransform = GetComponent<RectTransform>();
            targetImage = GetComponent<Image>();
        }

        /// <summary>Configure la cible juste après son instanciation : sprite aléatoire et vitesse de défilement.</summary>
        public void Initialize(Sprite sprite, float speedPerSecond)
        {
            targetImage.sprite = sprite;
            scrollSpeedPerSecond = speedPerSecond;
        }

        private void Update()
        {
            Vector2 anchoredPosition = selfRectTransform.anchoredPosition;
            anchoredPosition.x -= scrollSpeedPerSecond * Time.deltaTime;
            selfRectTransform.anchoredPosition = anchoredPosition;
        }
    }
}
