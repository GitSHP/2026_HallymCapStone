using System;
using Promotion.Core;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Gameplay
{
    /// <summary>
    /// P2 단계에서 이동을 직접 시험해 볼 수 있도록 보드를 채우는 임시 코드다.
    /// P5에서 StageDefinition과 배치 단계로 대체된다.
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
