using System.Collections.Generic;
using UnityEngine;

namespace Gambonanza.Data
{
    /// <summary>
    /// What the shop may offer and how many slots it fills. Rolling lives here
    /// rather than in the screen so the randomness is data-driven and testable,
    /// and so a later stage/act can swap pools without touching UI code.
    /// </summary>
    [CreateAssetMenu(menuName = "Gambonanza/Shop Pool", fileName = "ShopPool")]
    public class ShopPool : ScriptableObject
    {
        [Header("Slots")]
        [Min(1)] public int artifactSlots = 3;
        [Min(1)] public int pieceSlots = 3;

        [Header("Pools")]
        public ArtifactDefinition[] artifacts = new ArtifactDefinition[0];
        [Tooltip("Pieces the player may buy. The king should not be listed here.")]
        public PieceDefinition[] pieces = new PieceDefinition[0];

        static readonly List<int> Candidates = new();

        /// <param name="ownedArtifacts">Unique artifacts already owned are not offered again.</param>
        public void Roll(List<ArtifactDefinition> artifactResults,
                         List<PieceDefinition> pieceResults,
                         IReadOnlyList<ArtifactDefinition> ownedArtifacts = null)
        {
            artifactResults.Clear();
            pieceResults.Clear();

            RollArtifacts(artifactResults, ownedArtifacts);
            RollPieces(pieceResults);
        }

        void RollArtifacts(List<ArtifactDefinition> results, IReadOnlyList<ArtifactDefinition> owned)
        {
            Candidates.Clear();
            for (int i = 0; i < artifacts.Length; i++)
            {
                var a = artifacts[i];
                if (a == null || a.weight <= 0f)
                    continue;
                if (a.unique && Owns(owned, a))
                    continue;
                Candidates.Add(i);
            }

            for (int slot = 0; slot < artifactSlots && Candidates.Count > 0; slot++)
            {
                int picked = PickWeighted(i => artifacts[i].weight);
                results.Add(artifacts[Candidates[picked]]);
                // Drawn without replacement: three identical cards on one screen is a bug, not variety.
                Candidates.RemoveAt(picked);
            }
        }

        static bool Owns(IReadOnlyList<ArtifactDefinition> owned, ArtifactDefinition artifact)
        {
            if (owned == null)
                return false;

            for (int i = 0; i < owned.Count; i++)
                if (owned[i] == artifact)
                    return true;

            return false;
        }

        void RollPieces(List<PieceDefinition> results)
        {
            Candidates.Clear();
            for (int i = 0; i < pieces.Length; i++)
            {
                var p = pieces[i];
                if (p == null || p.shopWeight <= 0f || p.isObjective)
                    continue;
                Candidates.Add(i);
            }

            // Duplicate pieces are fine — buying two pawns is a real choice.
            for (int slot = 0; slot < pieceSlots && Candidates.Count > 0; slot++)
            {
                int picked = PickWeighted(i => pieces[i].shopWeight);
                results.Add(pieces[Candidates[picked]]);
            }
        }

        /// <summary>Returns an index into <see cref="Candidates"/>, not into the source array.</summary>
        static int PickWeighted(System.Func<int, float> weightOf)
        {
            float total = 0f;
            foreach (var index in Candidates)
                total += weightOf(index);

            float roll = Random.value * total;
            for (int i = 0; i < Candidates.Count; i++)
            {
                roll -= weightOf(Candidates[i]);
                if (roll <= 0f)
                    return i;
            }
            return Candidates.Count - 1;
        }
    }
}
