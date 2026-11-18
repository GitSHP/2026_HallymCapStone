using Promotion.Data;
using UnityEngine;

namespace Promotion.Tutorial
{
    [CreateAssetMenu(menuName = "Promotion/Piece Guide", fileName = "Guide_")]
    public class PieceGuideData : ScriptableObject // 기물 한 종류의 설명(기물 가이드) 내용을 담는 데이터
    // -> 이번 런에서 그 기물을 처음 소환했을 때 PieceGuideView가 이 내용을 화면에 띄움
    // -> 설명할 기물이 늘어나도 코드는 그대로 두고 이 에셋만 하나 더 만들면 됨
    {
        [Tooltip("설명할 기물이다.")]
        public PieceDefinition piece; // 이 가이드가 설명하는 기물

        [Tooltip("적 기물 설명이면 체크한다. 적 그림을 보여 주고, 비용 대신 '한 턴에 N칸 전진' 같은 적 정보를, 이동 칸 그림에는 한 턴에 다가오는 범위를 그린다.")]
        public bool enemy; // 적 가이드인지 여부 (중갑병 등)

        [Tooltip("한 장씩 넘겨 가며 보여 줄 설명 문장이다. 마지막 장에서 [확인]을 누르면 닫힌다.")]
        [TextArea(2, 5)]
        public string[] pages = new string[0]; // 설명 페이지 목록 -> 예: ① 이동 규칙 ② 잡는 방법 ③ 활용 요령
    }
}
