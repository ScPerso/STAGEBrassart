namespace Magma.Data
{
    /// <summary>
    /// Résumé immuable d'un mini-jeu terminé, remonté à la couche gestion pour être
    /// converti en gain de statistique. Le mini-jeu ignore tout des artistes :
    /// il rapporte seulement sa performance.
    /// </summary>
    public struct MiniGameResult
    {
        /// <summary>Mini-jeu concerné.</summary>
        public MiniGameId Id;

        /// <summary>Score brut avant conversion en statistique.</summary>
        public int RawScore;

        /// <summary>Précision normalisée de 0 (tout raté) à 1 (parfait).</summary>
        public float Accuracy;

        /// <summary>Statistique visée par ce mini-jeu.</summary>
        public StatType TargetStat;

        /// <summary>Gain de statistique accordé à l'issue du mini-jeu.</summary>
        public int StatGain;

        /// <summary>Vrai si le mini-jeu a été mené à son terme.</summary>
        public bool Completed;
    }
}
