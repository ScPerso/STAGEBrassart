using Magma.Data;
using Magma.Rhythm;
using NUnit.Framework;

namespace Magma.Tests.EditMode
{
    /// <summary>Tests des fenêtres et de la symétrie du jugement de timing.</summary>
    public class NoteJudgeTests
    {
        private const float PerfectWindow = 0.055f;
        private const float GoodWindow = 0.12f;
        private const float MissWindow = 0.18f;
        private const float NoteTime = 1.0f;
        private const float Tolerance = 1e-5f;

        private static NoteJudge CreateBaseJudge()
        {
            return new NoteJudge(PerfectWindow, GoodWindow, MissWindow);
        }

        [Test]
        public void ExactHit_IsPerfect_WithZeroDelta()
        {
            NoteJudge judge = CreateBaseJudge();

            Judgement result = judge.Evaluate(NoteTime, NoteTime, out float delta);

            Assert.AreEqual(Judgement.Perfect, result);
            Assert.AreEqual(0f, delta, Tolerance);
        }

        [Test]
        public void EarlyBy54ms_IsStillPerfect()
        {
            NoteJudge judge = CreateBaseJudge();

            Judgement result = judge.Evaluate(NoteTime, NoteTime + 0.054f, out _);

            Assert.AreEqual(Judgement.Perfect, result);
        }

        [Test]
        public void By56ms_IsGood()
        {
            NoteJudge judge = CreateBaseJudge();

            Judgement result = judge.Evaluate(NoteTime, NoteTime + 0.056f, out _);

            Assert.AreEqual(Judgement.Good, result);
        }

        [Test]
        public void By119ms_IsGood()
        {
            NoteJudge judge = CreateBaseJudge();

            Judgement result = judge.Evaluate(NoteTime, NoteTime + 0.119f, out _);

            Assert.AreEqual(Judgement.Good, result);
        }

        [Test]
        public void By121ms_IsMiss()
        {
            NoteJudge judge = CreateBaseJudge();

            Judgement result = judge.Evaluate(NoteTime, NoteTime + 0.121f, out _);

            Assert.AreEqual(Judgement.Miss, result);
        }

        [Test]
        public void EarlyBy54ms_IsSymmetricPerfect()
        {
            NoteJudge judge = CreateBaseJudge();

            Judgement result = judge.Evaluate(NoteTime, NoteTime - 0.054f, out float delta);

            Assert.AreEqual(Judgement.Perfect, result);
            Assert.Less(delta, 0f);
        }

        [Test]
        public void WidenedWindows_TurnBaseGoodIntoPerfect()
        {
            // A hit at 0.1s is Good with the base windows...
            Judgement baseResult = CreateBaseJudge().Evaluate(NoteTime, NoteTime + 0.1f, out _);
            Assert.AreEqual(Judgement.Good, baseResult);

            // ...but becomes Perfect once the Music stat widens the perfect window.
            NoteJudge widened = new NoteJudge(0.15f, 0.25f, 0.35f);
            Judgement widenedResult = widened.Evaluate(NoteTime, NoteTime + 0.1f, out _);

            Assert.AreEqual(Judgement.Perfect, widenedResult);
        }
    }
}
