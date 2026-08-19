namespace Magma.Rhythm
{
    /// <summary>
    /// Abstraction du retour visuel de mise en avant d'une note (glow, changement de
    /// couleur) affiché tant que la note est dans sa fenêtre parfaite. Séparé du
    /// mouvement pour que le style puisse être remplacé sans toucher la logique.
    /// </summary>
    public interface INoteHighlight
    {
        /// <summary>
        /// Active ou désactive la mise en avant.
        /// </summary>
        /// <param name="active">Vrai tant que la note est dans sa fenêtre parfaite.</param>
        void SetHighlight(bool active);
    }
}
