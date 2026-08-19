using Magma.Data;
using Magma.Rhythm;
using NUnit.Framework;

namespace Magma.Tests.EditMode
{
    /// <summary>Tests du score, de la précision et du score sans compétences.</summary>
    public class RhythmScorerTests
    {
        private const float Tolerance = 1e-4f;

        private static void Register(RhythmScorer scorer, Judgement judgement, int count)
        {
            for (int i = 0; i < count; i++)
            {
                scorer.Register(judgement, 1f);
            }
        }

        [Test]
        public void TenPerfects_GiveAccuracyOne()
        {
            RhythmScorer scorer = new RhythmScorer();

            Register(scorer, Judgement.Perfect, 10);

            Assert.AreEqual(1f, scorer.Accuracy, Tolerance);
        }

        [Test]
        public void TenMisses_GiveAccuracyZero()
        {
            RhythmScorer scorer = new RhythmScorer();

            Register(scorer, Judgement.Miss, 10);

            Assert.AreEqual(0f, scorer.Accuracy, Tolerance);
        }

        [Test]
        public void FivePerfectsFiveGoods_GiveAccuracy0_75()
        {
            RhythmScorer scorer = new RhythmScorer();

            Register(scorer, Judgement.Perfect, 5);
            Register(scorer, Judgement.Good, 5);

            Assert.AreEqual(0.75f, scorer.Accuracy, Tolerance);
        }

        [Test]
        public void ScoreWithoutStats_IsNeverAboveTotalScore()
        {
            RhythmScorer scorer = new RhythmScorer();

            // Register hits with combo multipliers above 1.
            scorer.Register(Judgement.Perfect, 2f);
            scorer.Register(Judgement.Good, 1.5f);
            scorer.Register(Judgement.Perfect, 3f);

            Assert.LessOrEqual(scorer.ComputeScoreWithoutStats(), scorer.TotalScore);
        }
    }
}
