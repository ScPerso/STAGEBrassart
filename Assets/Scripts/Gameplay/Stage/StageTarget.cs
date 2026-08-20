using UnityEngine;
using UnityEngine.UI;

namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Cible de rythme qui défile en ligne droite vers la position du joueur, depuis n'importe
    /// quel point de départ (haut, bas, gauche, droite). Reste purement passive : le déplacement
    /// et l'apparence sont gérés ici, tout le jugement (succès/échec) reste centralisé dans
    /// <see cref="StageRhythmManager"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class StageTarget : MonoBehaviour
    {
        private RectTransform selfRectTransform;
        private Image targetImage;
        private Vector2 moveDirection;
        private float scrollSpeedPerSecond;

        /// <summary>Position courante de la cible dans l'espace du Canvas.</summary>
        public Vector2 AnchoredPosition => selfRectTransform.anchoredPosition;

        private void Awake()
        {
            selfRectTransform = GetComponent<RectTransform>();
            targetImage = GetComponent<Image>();
        }

        /// <summary>
        /// Configure la cible juste après son instanciation : sprite aléatoire, vitesse de
        /// défilement et direction unitaire vers la position du joueur au moment du spawn.
        /// </summary>
        public void Initialize(Sprite sprite, float speedPerSecond, Vector2 direction)
        {
            targetImage.sprite = sprite;
            scrollSpeedPerSecond = speedPerSecond;
            moveDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.left;
        }

        private void Update()
        {
            selfRectTransform.anchoredPosition += moveDirection * (scrollSpeedPerSecond * Time.deltaTime);
        }

        /// <summary>
        /// Distance signée le long de la direction de déplacement de la cible entre celle-ci et
        /// <paramref name="referencePosition"/> : positive tant qu'elle n'a pas atteint cette
        /// position, négative une fois qu'elle l'a dépassée (utilisé pour détecter un raté quelle
        /// que soit la direction d'arrivée de la cible).
        /// </summary>
        public float SignedDistanceAlongDirection(Vector2 referencePosition)
        {
            return Vector2.Dot(referencePosition - AnchoredPosition, moveDirection);
        }
    }
}

