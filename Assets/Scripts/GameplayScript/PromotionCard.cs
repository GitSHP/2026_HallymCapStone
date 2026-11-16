using System;
using System.Collections.Generic;
using Promotion.Data;
using Promotion.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Gameplay
{
    public class PromotionCard : MonoBehaviour
    // 폰 승격 선택창의 카드 한 장 (기물 그림 + 이름 + 행마법 한 줄)
    // -> 모양은 PromotionCard 프리팹이 원본이고, 여기서는 내용만 채움
    {
        [SerializeField] UiButton _button;
        [SerializeField] Image _art;
        [SerializeField] TextMeshProUGUI _name;
        [SerializeField] TextMeshProUGUI _move;

        Action _onPick;

        void Awake()
        {
            _button.Clicked += OnClicked;
        }

        public void Fill(PieceDefinition def, Action onPick)
        {
            _onPick = onPick;
            _art.sprite = def.itemImage;
            _art.enabled = def.itemImage != null;
            _name.text = def.itemName;
            _move.text = MoveSummary(def);
        }

        public void PlayIn(int index)
        {
            _button.PlayIn(index);
        }

        void OnClicked()
        {
            var callback = _onPick;
            _onPick = null;     // 두 번 눌러도 한 번만 처리
            callback?.Invoke();
        }

        public static string MoveSummary(PieceDefinition def)
        // 기물 데이터의 행마법(MovePattern)으로 한 줄 설명을 만드는 함수 -> 새 기물을 추가해도 따로 적을 필요 없음
        {
            bool slideStraight = false;
            bool slideDiagonal = false;
            bool stepStraight = false;
            bool stepDiagonal = false;
            bool knight = false;
            bool leapOther = false;

            foreach (var pattern in def.patterns)
            {
                if (pattern == null)
                    continue;
                foreach (var d in pattern.directions)
                {
                    int ax = Mathf.Abs(d.x);
                    int ay = Mathf.Abs(d.y);
                    bool straight = ax == 0 || ay == 0;
                    bool diagonal = ax == ay;
                    if (pattern.sliding && straight)
                        slideStraight = true;
                    else if (pattern.sliding && diagonal)
                        slideDiagonal = true;
                    else if (Mathf.Max(ax, ay) == 1 && straight)
                        stepStraight = true;
                    else if (Mathf.Max(ax, ay) == 1 && diagonal)
                        stepDiagonal = true;
                    else if ((ax == 1 && ay == 2) || (ax == 2 && ay == 1))
                        knight = true;
                    else
                        leapOther = true;
                }
            }

            var parts = new List<string>();
            if (slideStraight && slideDiagonal)
                parts.Add("모든 방향으로 멀리");
            else if (slideStraight)
                parts.Add("가로·세로로 멀리");
            else if (slideDiagonal)
                parts.Add("대각선으로 멀리");

            if (stepStraight && stepDiagonal)
                parts.Add("주변 한 칸");
            else if (stepStraight && !slideStraight)
                parts.Add("가로·세로 한 칸");
            else if (stepDiagonal && !slideDiagonal)
                parts.Add("대각선 한 칸");

            if (knight)
                parts.Add("L자로 뛰어넘기");
            if (leapOther)
                parts.Add("먼 칸으로 뛰어넘기");

            return parts.Count > 0 ? string.Join(" + ", parts) : "";
        }
    }
}
