using UnityEngine;
using UnityEngine.UI;

namespace Magma.Gameplay
{
    /// <summary>
    /// Pilote le menu Options du menu principal : affichage d'un panneau qui bloque les clics sur
    /// les boutons en dessous, et réglage du volume général du jeu (sauvegardé entre les sessions).
    /// </summary>
    public class OptionsMenuController : MonoBehaviour
    {
        /// <summary>Clé de sauvegarde du volume choisi par le joueur.</summary>
        private const string VolumePrefsKey = "Magma.MasterVolume";

        /// <summary>Volume par défaut (sur 100) avant tout réglage du joueur.</summary>
        private const float DefaultVolumePercent = 50f;

        [Header("Panneau")]
        [Tooltip("Racine du panneau Options à afficher/masquer. Bloque les clics vers le menu principal quand actif.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Volume")]
        [SerializeField] private Slider volumeSlider;

        private void Awake()
        {
            float savedVolumePercent = PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolumePercent);
            ApplyVolume(savedVolumePercent);

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(savedVolumePercent);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        /// <summary>Ouvre le menu Options par-dessus le menu principal. Branché sur le bouton "Options".</summary>
        public void Open()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        /// <summary>Ferme le menu Options et retrouve le menu principal. Branché sur la croix de fermeture.</summary>
        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        /// <summary>Applique et sauvegarde le volume général du jeu (0-100). Branché sur le slider de volume.</summary>
        public void OnVolumeChanged(float volumePercent)
        {
            ApplyVolume(volumePercent);
            PlayerPrefs.SetFloat(VolumePrefsKey, volumePercent);
        }

        private static void ApplyVolume(float volumePercent)
        {
            AudioListener.volume = Mathf.Clamp01(volumePercent / 100f);
        }
    }
}
