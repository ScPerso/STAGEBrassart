using Magma.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Magma.Gameplay
{
    /// <summary>
    /// Pilote l'écran de sélection d'artiste : défilement parmi les fiches disponibles
    /// (une seule remplie pour l'instant, la structure supporte d'en ajouter d'autres)
    /// et validation du choix vers le premier rendez-vous d'accompagnement.
    /// </summary>
    [ExecuteAlways]
    public class ArtistSelectionController : MonoBehaviour
    {
        [Header("Fiches disponibles")]
        [SerializeField] private ArtistProfile[] artistProfiles;

        [Header("Affichage de la fiche")]
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI genreLabel;

        [Header("Jauges (barres verticales, remplissage bas -> haut)")]
        [SerializeField] private Image marketingGauge;
        [SerializeField] private Image musicGauge;
        [SerializeField] private Image stageGauge;

        [Header("Valeurs affichées sous chaque jauge")]
        [SerializeField] private TextMeshProUGUI marketingValueLabel;
        [SerializeField] private TextMeshProUGUI musicValueLabel;
        [SerializeField] private TextMeshProUGUI stageValueLabel;

        private int currentIndex;

        private void Awake()
        {
            // Les jauges sont de simples rectangles bleus dont la hauteur (anchorMax.y) représente
            // la statistique : pas de sprite, pas de type "Filled", pour éviter tout artefact visuel.
            ConfigureVerticalGauge(marketingGauge);
            ConfigureVerticalGauge(musicGauge);
            ConfigureVerticalGauge(stageGauge);
        }

        private void Start()
        {
            RefreshDisplay();
        }

        private static void ConfigureVerticalGauge(Image gauge)
        {
            if (gauge == null)
            {
                return;
            }

            gauge.type = Image.Type.Simple;
            gauge.sprite = null;

            RectTransform rect = gauge.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }

        /// <summary>Affiche la fiche artiste suivante (défilement circulaire). Branché sur le bouton "Suivant".</summary>
        public void ShowNext()
        {
            if (artistProfiles == null || artistProfiles.Length == 0)
            {
                return;
            }

            currentIndex = (currentIndex + 1) % artistProfiles.Length;
            RefreshDisplay();
        }

        /// <summary>Affiche la fiche artiste précédente (défilement circulaire). Branché sur le bouton "Précédent".</summary>
        public void ShowPrevious()
        {
            if (artistProfiles == null || artistProfiles.Length == 0)
            {
                return;
            }

            currentIndex = (currentIndex - 1 + artistProfiles.Length) % artistProfiles.Length;
            RefreshDisplay();
        }

        /// <summary>Valide la fiche affichée et démarre l'accompagnement. Branché sur le bouton "Valider le choix".</summary>
        public void ValidateChoice()
        {
            if (artistProfiles == null || artistProfiles.Length == 0)
            {
                return;
            }

            GameFlowManager.Instance.ConfirmArtistSelection(artistProfiles[currentIndex]);
        }

        private void RefreshDisplay()
        {
            if (artistProfiles == null || artistProfiles.Length == 0)
            {
                return;
            }

            ArtistProfile profile = artistProfiles[currentIndex];

            nameLabel.text = profile.DisplayName;
            genreLabel.text = profile.MusicGenre;

            SetGauge(marketingGauge, marketingValueLabel, profile.GetBaseStat(StatType.Marketing));
            SetGauge(musicGauge, musicValueLabel, profile.GetBaseStat(StatType.Music));
            SetGauge(stageGauge, stageValueLabel, profile.GetBaseStat(StatType.Stage));
        }

        private static void SetGauge(Image gauge, TextMeshProUGUI valueLabel, int value)
        {
            if (gauge != null)
            {
                gauge.rectTransform.anchorMax = new Vector2(1f, Mathf.Clamp01(value / 100f));
            }

            if (valueLabel != null)
            {
                valueLabel.text = value.ToString();
            }
        }
    }
}
