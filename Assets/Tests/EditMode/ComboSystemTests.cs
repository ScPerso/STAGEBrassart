using Magma.Data;
using Magma.Rhythm;
using NUnit.Framework;

namespace Magma.Tests.EditMode
{
    /// <summary>Tests du combo et du multiplicateur de score.</summary>
    public class ComboSystemTests
    {
        private const float Tolerance = 1e-4f;

        private static void RegisterPerfects(ComboSystem combo, int count)
        {
            for (int i = 0; i < count; i++)
            {
                combo.RegisterHit(Judgement.Perfect);
            }
        }

        [Test]
        public void TenPerfects_GiveMultiplier1_25()
        {
            ComboSystem combo = new ComboSystem();

            RegisterPerfects(combo, 10);

            Assert.AreEqual(1.25f, combo.Multiplier, Tolerance);
        }

        [Test]
        public void Multiplier_IsCappedAtFour()
        {
            ComboSystem combo = new ComboSystem();

            // With the formula 1 + floor(combo/10)*0.25 the cap (4) is reached at combo 120;
            // a very high combo must never exceed 4.
            RegisterPerfects(combo, 40);
            Assert.LessOrEqual(combo.Multiplier, 4f);

            RegisterPerfects(combo, 200);
            Assert.AreEqual(4f, combo.Multiplier, Tolerance);
        }

        [Test]
        public void Miss_ResetsCombo_ButKeepsMaxCombo()
        {
            ComboSystem combo = new ComboSystem();

            RegisterPerfects(combo, 5);
            Assert.AreEqual(5, combo.Combo);

            combo.RegisterHit(Judgement.Miss);

            Assert.AreEqual(0, combo.Combo);
            Assert.AreEqual(5, combo.MaxCombo);
        }

        [Test]
        public void RampSpeedTwo_Reaches1_25InFiveHits()
        {
            ComboSystem combo = new ComboSystem { RampSpeed = 2f };

            RegisterPerfects(combo, 5);

            Assert.AreEqual(1.25f, combo.Multiplier, Tolerance);
        }
    }
}
