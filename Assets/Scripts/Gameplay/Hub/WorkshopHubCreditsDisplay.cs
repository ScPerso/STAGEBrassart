using TMPro;
using UnityEngine;

namespace Magma.Gameplay
{
    /// <summary>
    /// Affiche le nombre de crédits de formation restants dans le hub. Chaque mini-jeu
    /// terminé consomme un crédit (voir <see cref="GameFlowManager.ReportMiniGameResult"/>) ;
    /// ce texte se remet à jour chaque fois que le hub est réaffiché.
    /// </summary>
    public class WorkshopHubCreditsDisplay : MonoBehaviour
    {
        [Tooltip("Texte affichant le nombre de crédits restants.")]
        [SerializeField] private TextMeshProUGUI creditsLabel;

        private void OnEnable()
        {
            RefreshDisplay();
        }

        /// <summary>Relit les crédits restants depuis le GameFlowManager et met à jour le texte.</summary>
        public void RefreshDisplay()
        {
            if (creditsLabel == null)
            {
                return;
            }

            int creditsRemaining = GameFlowManager.Instance != null ? GameFlowManager.Instance.CreditsRemaining : 0;
            creditsLabel.text = "Credits : " + creditsRemaining;
        }
    }
}
