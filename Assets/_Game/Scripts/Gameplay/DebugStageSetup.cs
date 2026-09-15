using System;
using Gambonanza.Core;
using Gambonanza.Data;
using UnityEngine;

namespace Gambonanza.Gameplay
{
    /// <summary>
    /// Temporary board filler for P2 so movement can be exercised by hand.
    /// P5 replaces this with a StageDefinition and the deploy phase.
    /// </summary>
    [RequireComponent(typeof(PieceRegistry))]
    public class DebugStageSetup : MonoBehaviour
    {
        [Serializable]
        public class Placement
        {
            public PieceDefinition definition;
            public Team team = Team.Player;
            public Vector2Int coord;
        }

        public Placement[] placements = Array.Empty<Placement>();

        PieceRegistry _registry;

        void Start()
        {
            _registry = GetComponent<PieceRegistry>();

            foreach (var placement in placements)
            {
                if (placement.definition == null)
                    continue;

                _registry.Spawn(placement.definition, placement.team,
                    new Coord(placement.coord.x, placement.coord.y));
            }
        }
    }
}
