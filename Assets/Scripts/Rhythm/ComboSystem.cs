using System;
using Magma.Data;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Suit le combo de notes réussies consécutives et en dérive un multiplicateur de
    /// score plafonné. Classe pure, instanciable et testable sans scène ni GameObject.
    /// </summary>
    public class ComboSystem
    {
        private const float MultiplierStep = 0.25f;
        private const float MaxMultiplierValue = 4f;
        private const int ComboPerStep = 10;
        private const float DefaultRampSpeed = 1f;

        private int combo;
        private int maxCombo;
        private float rampSpeed = DefaultRampSpeed;

        /// <summary>Combo courant (réussites consécutives).</summary>
        public int Combo => combo;

        /// <summary>Combo maximum atteint depuis le dernier Reset.</summary>
        public int MaxCombo => maxCombo;

        /// <summary>Vitesse de montée du combo (1 par défaut).</summary>
        public float RampSpeed
        {
            get => rampSpeed;
            set => rampSpeed = value;
        }

        /// <summary>Multiplicateur de score courant, plafonné à 4.</summary>
        public float Multiplier
        {
            get
            {
                float steps = Mathf.Floor(combo * rampSpeed / ComboPerStep);
                return Mathf.Min(1f + steps * MultiplierStep, MaxMultiplierValue);
            }
        }

        /// <summary>Déclenché à chaque changement de combo, avec la valeur courante.</summary>
        public event Action<int> OnComboChanged;

        /// <summary>Déclenché lorsqu'un Miss casse le combo.</summary>
        public event Action OnComboBroken;

        /// <summary>
        /// Enregistre un jugement : incrémente le combo sur un succès, le remet à zéro sur un Miss.
        /// </summary>
        /// <param name="j">Jugement de la note.</param>
        public void RegisterHit(Judgement j)
        {
            if (j == Judgement.Miss)
            {
                if (combo > 0)
                {
                    combo = 0;
                    OnComboBroken?.Invoke();
                    OnComboChanged?.Invoke(combo);
                }

                return;
            }

            combo++;

            if (combo > maxCombo)
            {
                maxCombo = combo;
            }

            OnComboChanged?.Invoke(combo);
        }

        /// <summary>Remet le combo courant et le combo maximum à zéro.</summary>
        public void Reset()
        {
            combo = 0;
            maxCombo = 0;
        }
    }
}
