namespace Magma.Gameplay.Stage
{
    /// <summary>
    /// Qualité du jugement d'une cible de prestation scénique, de la moins bonne à la meilleure,
    /// déterminée par la distance entre le centre de la cible et le centre du joueur au moment du
    /// clic/tap (zones circulaires concentriques, comme un oignon).
    /// </summary>
    public enum StageHitJudgement
    {
        Miss,
        Ok,
        Good,
        Perfect
    }
}
