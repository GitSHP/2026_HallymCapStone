using System;
using UnityEngine;

namespace Gambonanza.Data
{
    /// <summary>
    /// The placeholder icon vocabulary. Lives in Data rather than UI so a
    /// definition can name its own icon without depending on the interface.
    /// </summary>
    public enum IconId
    {
        None,
        Pawn, Rook, Bishop, Knight, Queen, King,
        Heart, Sword, Bolt, Coin, Shield, Star, Crown, Chevrons,
        Arrow, Bag, Check
    }

    /// <summary>
    /// One effect stated as a symbol and a number — "+1" next to a sword — rather
    /// than as a sentence. Cards are read at a glance in a shop, and a row of
    /// chips survives translation and small screens in a way prose does not.
    /// </summary>
    [Serializable]
    public class EffectChip
    {
        public IconId icon = IconId.Star;
        public string value = "+1";
    }

    /// <summary>
    /// A passive run-modifier bought in the shop. This pass carries identity, art
    /// and price only — what an artifact actually does is applied once the battle
    /// and action-point systems exist to hang effects on.
    /// </summary>
    [CreateAssetMenu(menuName = "Gambonanza/Artifact Definition", fileName = "Artifact")]
    public class ArtifactDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Artifact";

        [Tooltip("Placeholder icon drawn in code. Ignored when a sprite is assigned below.")]
        public IconId icon = IconId.Star;

        [Tooltip("Real art. Assigning this overrides the placeholder icon.")]
        public Sprite art;

        [Tooltip("Long form, kept for tooltips and the codex. The card shows the chips instead.")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("Card face")]
        public Color accentColor = new Color(0.42f, 0.58f, 1f);

        [Tooltip("What the card actually shows: up to three symbol-and-number chips.")]
        public EffectChip[] effects = Array.Empty<EffectChip>();

        [Header("Shop")]
        [Min(0)] public int cost = 4;

        [Tooltip("Higher weight appears in the shop more often.")]
        [Min(0.01f)] public float weight = 1f;

        [Tooltip("Only one copy can be owned per run.")]
        public bool unique = true;
    }
}
