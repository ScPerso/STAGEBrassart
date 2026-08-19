using Magma.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Magma.Gameplay
{
    /// <summary>Bouton du hub de mini-jeux : charge la scène du mini-jeu configuré lorsqu'il est cliqué.</summary>
    [RequireComponent(typeof(Button))]
    public class MiniGameHubButton : MonoBehaviour
    {
        /// <summary>Mini-jeu lancé par ce bouton.</summary>
        [SerializeField] private MiniGameId miniGameId;

        /// <summary>Charge la scène associée au mini-jeu configuré. Branché sur Button.onClick.</summary>
        public void LoadMiniGame()
        {
            SceneManager.LoadScene(SceneNames.ForMiniGame(miniGameId));
        }
    }
}
