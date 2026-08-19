using Magma.Data;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Évaluation pure du timing d'une note. Les fenêtres sont fournies au constructeur
    /// pour rester modulables à l'exécution (par exemple élargies par la stat Musique),
    /// sans aucune dépendance à une scène ou un GameObject.
    /// </summary>
    public class NoteJudge
    {
        /// <summary>
        /// Fenêtre Perfect par défaut (secondes), valeur historique du projet. Sert aussi
        /// de fenêtre de glow par défaut aux notes (<see cref="FallingNote"/>).
        /// </summary>
        public const float PerfectWindowSeconds = 0.6f;

        /// <summary>Fenêtre Good par défaut (secondes), valeur historique du projet.</summary>
        public const float GoodWindowSeconds = 1.0f;

        /// <summary>Au-delà de ce retard (secondes), une note est expirée (Miss définitif).</summary>
        public const float MissWindowSeconds = 1.0f;

        private readonly float perfectWindow;
        private readonly float goodWindow;
        private readonly float missWindow;

        /// <summary>
        /// Crée un juge avec des fenêtres explicites, en secondes.
        /// </summary>
        /// <param name="perfectWindow">Erreur absolue maximale pour un Perfect.</param>
        /// <param name="goodWindow">Erreur absolue maximale pour un Good.</param>
        /// <param name="missWindow">Retard au-delà duquel une note est définitivement expirée.</param>
        public NoteJudge(float perfectWindow, float goodWindow, float missWindow)
        {
            this.perfectWindow = perfectWindow;
            this.goodWindow = goodWindow;
            this.missWindow = missWindow;
        }

        /// <summary>
        /// Juge un appui à partir du temps de la note et du temps de l'entrée joueur.
        /// </summary>
        /// <param name="noteTime">Temps cible de la note, en secondes.</param>
        /// <param name="inputTime">Temps de l'appui joueur, en secondes.</param>
        /// <param name="delta">Écart signé (inputTime - noteTime), en secondes.</param>
        /// <returns>Le jugement correspondant à l'erreur de timing.</returns>
        public Judgement Evaluate(float noteTime, float inputTime, out float delta)
        {
            delta = inputTime - noteTime;
            float error = Mathf.Abs(delta);

            if (error <= perfectWindow)
            {
                return Judgement.Perfect;
            }

            if (error <= goodWindow)
            {
                return Judgement.Good;
            }

            return Judgement.Miss;
        }

        /// <summary>
        /// Indique si une note en retard doit être considérée comme expirée (Miss définitif).
        /// </summary>
        /// <param name="noteTime">Temps cible de la note, en secondes.</param>
        /// <param name="currentTime">Temps courant du morceau, en secondes.</param>
        /// <returns>Vrai si la note est expirée.</returns>
        public bool IsExpired(float noteTime, float currentTime)
        {
            return currentTime - noteTime > missWindow;
        }
    }
}
