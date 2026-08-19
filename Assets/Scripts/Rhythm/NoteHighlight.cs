using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Mise en avant colorée d'une note : tant qu'elle est active, le matériau de la
    /// note pulse entre sa couleur de base et une couleur d'accent pour signaler la
    /// fenêtre parfaite. Implémente <see cref="INoteHighlight"/> pour que le style de
    /// retour puisse être remplacé indépendamment.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class NoteHighlight : MonoBehaviour, INoteHighlight
    {
        [Tooltip("Couleur de la note hors de sa fenêtre parfaite.")]
        [SerializeField] private Color baseColor = Color.white;

        [Tooltip("Couleur vers laquelle la note pulse quand elle est mise en avant.")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Vitesse de la pulsation de mise en avant, en radians par seconde.")]
        [SerializeField] private float pulseSpeed = DefaultPulseSpeed;

        private Renderer noteRenderer;
        private MaterialPropertyBlock propertyBlock;
        private bool isHighlighted;

        // URP Lit and most shaders expose the base color under this property name.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private const float DefaultPulseSpeed = 10f;

        private void Awake()
        {
            noteRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();

            ApplyColor(baseColor);
        }

        /// <inheritdoc />
        public void SetHighlight(bool active)
        {
            isHighlighted = active;

            if (!active)
            {
                ApplyColor(baseColor);
            }
        }

        private void Update()
        {
            if (!isHighlighted)
            {
                return;
            }

            // Oscillate between base and highlight color to create the scintillation.
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            ApplyColor(Color.Lerp(baseColor, highlightColor, pulse));
        }

        private void ApplyColor(Color color)
        {
            if (noteRenderer == null)
            {
                return;
            }

            noteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            noteRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
