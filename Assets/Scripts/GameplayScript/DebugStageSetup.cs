using System;
using Promotion.Core;
using Promotion.Data;
using UnityEngine;

namespace Promotion.Gameplay
{
    [RequireComponent(typeof(PieceRegistry))]
    public class DebugStageSetup : MonoBehaviour  // 테스트용으로 스테이지 시작 시 정해진 기물들을 보드에 미리 놓아 두는 클래스
    // 스테이지 에셋이 없을 때 StageDirector가 이 배치 정보를 보고 스테이지를 만들기도 함 -> 나중에 StageDefinition으로 옮길 예정
    {
        [Serializable]
        public class Placement  // 기물 하나를 어디에 놓을지에 대한 정보(인스펙터에서 입력)
        {
            public PieceDefinition definition;  // 놓을 기물의 종류
            public Team team = Team.Player; // 아군인지 적인지
            public Vector2Int coord;    // 놓을 칸의 좌표(x = 열, y = 줄)
        }

        public Placement[] placements = Array.Empty<Placement>();   // 시작할 때 놓을 기물 목록

        PieceRegistry _registry;    // 기물을 생성하고 관리하는 컴포넌트

        void Start()    // 첫 프레임에 실행 -> 목록에 있는 기물을 모두 보드에 생성
        {
            _registry = GetComponent<PieceRegistry>();

            foreach (var placement in placements)   // 배치 목록을 하나씩 확인해서
            {
                if (placement.definition == null)   // 기물 종류가 비어 있는 항목은 건너뜀
                    continue;

                _registry.Spawn(placement.definition, placement.team,
                    new Coord(placement.coord.x, placement.coord.y));   // 지정된 칸에 기물 생성
            }
        }
    }
}
