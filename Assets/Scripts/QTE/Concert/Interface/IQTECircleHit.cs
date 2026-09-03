/// <summary>
/// Reçoit la résolution d'un QTE circulaire de concert.
/// </summary>
public interface IQTECircleHit
{
    /// <summary>Traite une réussite avec une précision comprise entre 0 et 100.</summary>
    void Hit(float accuracy);

    /// <summary>Traite un QTE qui a expiré sans action du joueur.</summary>
    void Miss();
}
