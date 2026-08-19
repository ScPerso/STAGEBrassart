using Magma.Data;
using UnityEngine;

namespace Magma.Gameplay.Rehearsal
{
    /// <summary>
    /// Écran de fin de répétition temporaire dessiné en IMGUI (placeholder existant,
    /// hors périmètre de cette extraction). Écoute la fin de la répétition et affiche
    /// le score, la précision et le détail des jugements avec un bouton Rejouer.
    /// </summary>
    [RequireComponent(typeof(RehearsalManager))]
    public class RehearsalResultUI : MonoBehaviour
    {
        private RehearsalManager rehearsalManager;
        private MiniGameResult result;
        private bool hasResult;

        private const float PanelWidth = 360f;
        private const float PanelHeight = 240f;

        private void Awake()
        {
            rehearsalManager = GetComponent<RehearsalManager>();
        }

        private void OnEnable()
        {
            rehearsalManager.Completed += OnRehearsalCompleted;
        }

        private void OnDisable()
        {
            rehearsalManager.Completed -= OnRehearsalCompleted;
        }

        private void OnRehearsalCompleted(MiniGameResult miniGameResult)
        {
            result = miniGameResult;
            hasResult = true;
        }

        private void OnGUI()
        {
            if (!hasResult)
            {
                return;
            }

            float x = (Screen.width - PanelWidth) * 0.5f;
            float y = (Screen.height - PanelHeight) * 0.5f;

            GUILayout.BeginArea(
                new Rect(x, y, PanelWidth, PanelHeight),
                GUI.skin.box
            );

            GUILayout.Label("Repetition terminee");
            GUILayout.Space(8f);

            GUILayout.Label("Score : " + result.RawScore + " / " + rehearsalManager.MaxScore);
            GUILayout.Label("Precision : " + (result.Accuracy * 100f).ToString("F0") + "%");
            GUILayout.Space(8f);

            GUILayout.Label("Perfect : " + rehearsalManager.PerfectCount);
            GUILayout.Label("Good    : " + rehearsalManager.GoodCount);
            GUILayout.Label("Miss    : " + rehearsalManager.MissCount);
            GUILayout.Space(12f);

            if (GUILayout.Button("Rejouer", GUILayout.Height(32f)))
            {
                hasResult = false;
                rehearsalManager.Restart();
            }

            GUILayout.EndArea();
        }
    }
}
