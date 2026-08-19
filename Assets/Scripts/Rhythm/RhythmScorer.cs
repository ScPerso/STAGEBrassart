using Magma.Data;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Accumule les jugements et calcule le score (multiplicateur inclus), la précision
    /// et les compteurs. Classe pure, testable sans scène ni GameObject.
    /// </summary>
    public class RhythmScorer
    {
        /// <summary>Points de base d'un Perfect.</summary>
        public const int PerfectScore = 100;

        /// <summary>Points de base d'un Good.</summary>
        public const int GoodScore = 30;

        /// <summary>Points de base d'un Miss.</summary>
        public const int MissScore = 0;

        private int perfectCount;
        private int goodCount;
        private int missCount;
        private int totalRegistered;
        private int totalScore;
        private int baseScore;

        /// <summary>Score total accumulé, multiplicateurs de combo inclus.</summary>
        public int TotalScore => totalScore;

        /// <summary>Nombre de Perfect enregistrés.</summary>
        public int PerfectCount => perfectCount;

        /// <summary>Nombre de Good enregistrés.</summary>
        public int GoodCount => goodCount;

        /// <summary>Nombre de Miss enregistrés.</summary>
        public int MissCount => missCount;

        /// <summary>Nombre total de jugements enregistrés.</summary>
        public int TotalRegistered => totalRegistered;

        /// <summary>Précision normalisée : (Perfect + Good * 0.5) / total.</summary>
        public float Accuracy => totalRegistered > 0
            ? (perfectCount + goodCount * 0.5f) / totalRegistered
            : 0f;

        /// <summary>
        /// Enregistre un jugement en appliquant le multiplicateur de combo au score.
        /// </summary>
        /// <param name="j">Jugement de la note.</param>
        /// <param name="multiplier">Multiplicateur appliqué (1 = aucun bonus).</param>
        public void Register(Judgement j, float multiplier)
        {
            int points;

            switch (j)
            {
                case Judgement.Perfect:
                    perfectCount++;
                    points = PerfectScore;
                    break;

                case Judgement.Good:
                    goodCount++;
                    points = GoodScore;
                    break;

                default:
                    missCount++;
                    points = MissScore;
                    break;
            }

            totalRegistered++;
            baseScore += points;
            totalScore += Mathf.RoundToInt(points * multiplier);
        }

        /// <summary>
        /// Rejoue le score avec un multiplicateur de 1, pour isoler le bonus de compétences.
        /// </summary>
        /// <returns>Le score sans aucun multiplicateur.</returns>
        public int ComputeScoreWithoutStats()
        {
            return baseScore;
        }

        /// <summary>Remet tous les compteurs et scores à zéro.</summary>
        public void Reset()
        {
            perfectCount = 0;
            goodCount = 0;
            missCount = 0;
            totalRegistered = 0;
            totalScore = 0;
            baseScore = 0;
        }
    }
}
