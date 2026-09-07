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
        /// Fenêtre Perfect par défaut (secondes). Calibrée pour rester musicale : à 100 BPM
        /// un temps vaut 0,6 s, donc ±0,11 s représente environ un cinquième de temps.
        /// Sert aussi de fenêtre de glow par défaut aux notes (<see cref="FallingNote"/>).
        /// </summary>
        public const float PerfectWindowSeconds = 0.11f;

        /// <summary>
        /// Fenêtre Good par défaut (secondes). Volontairement généreuse pour un public large,
        /// tout en restant sous la moitié de l'écart minimal entre deux notes d'une colonne
        /// (0,6 s en chart facile) afin qu'un appui ne puisse jamais viser deux notes à la fois.
        /// </summary>
        public const float GoodWindowSeconds = 0.22f;

        /// <summary>Au-delà de ce retard (secondes), une note est expirée (Miss définitif).</summary>
        public const float MissWindowSeconds = 0.22f;

        private readonly float perfectWindow;
        private readonly float goodWindow;
        private readonly float missWindow;

        /// <summary>Fenêtre Perfect effectivement utilisée par ce juge, en secondes.</summary>
        public float PerfectWindow => perfectWindow;

        /// <summary>Fenêtre Good effectivement utilisée par ce juge, en secondes.</summary>
        public float GoodWindow => goodWindow;

        /// <summary>Retard au-delà duquel ce juge considère une note expirée, en secondes.</summary>
        public float MissWindow => missWindow;

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
