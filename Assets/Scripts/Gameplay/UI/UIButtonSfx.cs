using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Magma.Gameplay.UI
{
    /// <summary>
    /// Joue les sons d'interaction d'un bouton : un son de survol et un son de clic. Le survol
    /// n'est joué qu'à la souris (ou au stylet) car sur les appareils tactiles l'entrée du
    /// pointeur précède immédiatement le clic et ferait sonner le survol à chaque appui ; seul le
    /// clic est donc audible au tactile. Le clip de clic peut être remplacé par un son spécifique
    /// pour un bouton particulier (ex. validation de personnage) directement dans l'inspecteur.
    /// Les deux clips sont joués via le <see cref="SfxPlayer"/> global (persistant entre les
    /// scènes) plutôt qu'une AudioSource locale, pour que le son de clic ne soit pas coupé net
    /// lorsque le bouton déclenche un changement de scène immédiat.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Clips")]
        [Tooltip("Son joué au survol de la souris. Ignoré sur les appareils tactiles.")]
        [SerializeField] private AudioClip hoverClip;

        [Tooltip("Son joué au clic, sur souris comme sur tactile.")]
        [SerializeField] private AudioClip clickClip;

        [Header("Volumes (réglables indépendamment)")]
        [Tooltip("Volume du son de survol.")]
        [Range(0f, 1f)]
        [SerializeField] private float hoverVolume = 1f;

        [Tooltip("Volume du son de clic.")]
        [Range(0f, 1f)]
        [SerializeField] private float clickVolume = 1f;

        /// <summary>Joue le son de survol, uniquement lorsque le pointeur est une souris ou un stylet.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverClip == null || !IsMouseOrPenPointer(eventData))
            {
                return;
            }

            SfxPlayer.EnsureInstance().PlayOneShot(hoverClip, hoverVolume);
        }

        /// <summary>Joue le son de clic, quel que soit le type de pointeur (souris ou tactile).</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickClip == null)
            {
                return;
            }

            SfxPlayer.EnsureInstance().PlayOneShot(clickClip, clickVolume);
        }

        private static bool IsMouseOrPenPointer(PointerEventData eventData)
        {
            if (eventData is ExtendedPointerEventData extendedEventData)
            {
                return extendedEventData.pointerType == UIPointerType.MouseOrPen;
            }

            // Repli si le module d'entrée actif ne fournit pas de type de pointeur étendu :
            // un identifiant de pointeur négatif correspond à la souris avec les modules historiques.
            return eventData.pointerId < 0;
        }
    }
}
