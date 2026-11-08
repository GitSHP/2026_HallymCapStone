using System;
using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Data;
using Promotion.Feel;
using Promotion.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.Title
{
    public class FairyRouletteView : MonoBehaviour  // 타이틀의 특수 기물 뽑기(룰렛) 창을 담당하는 클래스
    // -> [첫 단어] + [둘째 단어] = [기물 그림]. 검은 왕관 1개로 한 번 돌리고, 두 단어가 멈추면 그 조합이 뜻하는 특수 기물을 해금함
    // -> 해금한 기물은 MetaProgress에 기록되어 다음 런부터 상점에 나옴 (ShopPool이 FairyRoulette.UnlockedPieces를 더함)
    // -> 화면 배치는 프리팹(FairyRoulette)에 있고, 이 스크립트는 내용 채우기·켜고 끄기·연출 실행만 함
    {
        [SerializeField] RectTransform _panel;  // 가운데 창 (등장 연출 대상)
        [SerializeField] TextMeshProUGUI _crowns;   // 오른쪽 위 "검은 왕관 : N개"
        [SerializeField] RectTransform _leftReel;   // 첫 번째 룰렛 칸
        [SerializeField] TextMeshProUGUI _leftWord; // 첫 번째 룰렛 글자
        [SerializeField] RectTransform _rightReel;  // 두 번째 룰렛 칸
        [SerializeField] TextMeshProUGUI _rightWord;    // 두 번째 룰렛 글자
        [SerializeField] Image _art;    // 결과 기물 그림
        [SerializeField] TextMeshProUGUI _artPlaceholder;   // 결과가 나오기 전 기물 칸에 보이는 "?"
        [SerializeField] Image _glow;   // 기물 뒤에서 깜빡이는 파란 빛
        [SerializeField] UiEmbers _embers;  // 기물 아래에서 피어오르는 파란 입자
        [SerializeField] TextMeshProUGUI _result;   // 결과 안내 문구
        [SerializeField] UiButton _spin;    // [룰렛 돌리기!]
        [SerializeField] UiButton _close;   // [닫기] -> 도는 중에는 Close()가 무시함 (어두운 버튼이라 SetInteractable로 색을 바꾸지 않음)

        public event Action Closed; // 창이 닫혔을 때 -> 타이틀이 버튼 상태를 다시 그림

        bool _spinning; // 룰렛이 도는 중인지 -> 도는 동안에는 닫기·다시 돌리기를 막음

        public bool IsOpen
        {
            get { return gameObject.activeSelf; }
        }

        void Awake()
        {
            _spin.Clicked += OnSpin;
            _close.Clicked += Close;
        }

        public void Open()  // 창을 여는 함수 -> 이전 결과는 지우고 처음 상태로 보여 줌
        {
            gameObject.SetActive(true);
            UiFx.BackdropIn(UiBuilder.Group(gameObject));
            UiFx.PanelIn(_panel, UiBuilder.Group(_panel.gameObject));

            _leftWord.text = "?";
            _rightWord.text = "?";
            ClearArt();
            _result.text = "";
            Refresh();
        }

        public void Close() // 창을 닫는 함수 (도는 중에는 닫지 않음)
        {
            if (_spinning || !IsOpen)
                return;
            ClearArt();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void Refresh()  // 왕관 수와 버튼 상태를 다시 그리는 함수
        {
            _crowns.text = "검은 왕관 : " + MetaProgress.Crowns + "개";

            var roulette = FairyRoulette.Instance;
            int locked = roulette != null ? roulette.LockedEntries().Count : 0;
            if (locked == 0)    // 다 뽑았다면 더 돌릴 수 없음
            {
                _spin.SetCaption("모두 획득함");
                _spin.SetInteractable(false);
            }
            else
            {
                _spin.SetCaption("룰렛 돌리기!  (왕관 1개)");
                _spin.SetInteractable(!_spinning && MetaProgress.Crowns > 0);   // 왕관이 없으면 누를 수 없음
            }
        }

        void OnSpin()   // [룰렛 돌리기!]를 눌렀을 때 -> 왕관 1개를 쓰고 룰렛을 돌림
        {
            if (_spinning)
                return;

            var roulette = FairyRoulette.Instance;
            if (roulette == null)
                return;
            var locked = roulette.LockedEntries();
            if (locked.Count == 0 || !MetaProgress.TrySpendCrown())
                return;

            var pick = locked[UnityEngine.Random.Range(0, locked.Count)];  // 아직 없는 특수 기물 중에서 하나를 뽑고
            MetaProgress.Unlock(pick.piece.name);   // 바로 해금 기록 -> 연출 도중 게임을 꺼도 결과는 남음
            StartCoroutine(Spin(roulette, pick));
        }

        IEnumerator Spin(FairyRoulette roulette, FairyRouletteEntry pick)
        // 두 룰렛을 함께 돌리다가 첫 번째가 먼저, 두 번째가 나중에 멈추고, 그 뒤 기물 그림을 보여 주는 코루틴
        {
            _spinning = true;
            ClearArt();
            _result.text = "";
            Refresh();

            var p = Fx.P;
            var leftWords = roulette.Words(true);
            var rightWords = roulette.Words(false);
            float start = Time.unscaledTime;
            float nextLeft = 0f;
            float nextRight = 0f;
            bool leftDone = false;
            bool rightDone = false;

            while (!rightDone)
            {
                float elapsed = Time.unscaledTime - start;

                if (!leftDone)
                {
                    if (elapsed >= p.reelLeftSpinTime)  // 시간이 다 되면 결과 단어에서 멈춤
                    {
                        leftDone = true;
                        Land(_leftReel, _leftWord, pick.left);
                    }
                    else if (elapsed >= nextLeft)
                    {
                        Roll(_leftWord, leftWords);
                        nextLeft = elapsed + Interval(elapsed / p.reelLeftSpinTime);
                    }
                }

                if (elapsed >= p.reelRightSpinTime)
                {
                    rightDone = true;
                    Land(_rightReel, _rightWord, pick.right);
                }
                else if (elapsed >= nextRight)
                {
                    Roll(_rightWord, rightWords);
                    nextRight = elapsed + Interval(elapsed / p.reelRightSpinTime);
                }

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.25f); // 두 번째 칸이 멈춘 것을 잠깐 보여 준 뒤

            ShowArt(pick.piece);
            _result.text = "<color=#6FC3FF>" + pick.piece.itemName + "</color> 획득!\n다음 런부터 상점에 등장합니다.";
            Sfx.Play(SfxId.Castle); // 맑은 종소리

            _spinning = false;
            Refresh();
        }

        static float Interval(float progress)   // 돌기 시작할 땐 빠르게, 멈출 때가 다가올수록 느리게 글자를 바꾸는 간격
        {
            var p = Fx.P;
            return Mathf.Lerp(p.reelFastInterval, p.reelSlowInterval, progress * progress);
        }

        static void Roll(TextMeshProUGUI label, List<string> words)    // 다음 단어로 바꾸는 함수 (같은 단어가 연달아 나오지 않게)
        {
            if (words.Count == 0)
                return;
            string next = words[UnityEngine.Random.Range(0, words.Count)];
            if (words.Count > 1 && next == label.text)
                next = words[(words.IndexOf(next) + 1) % words.Count];
            label.text = next;
            Fx.ReelTick(label.transform);
        }

        static void Land(RectTransform reel, TextMeshProUGUI label, string word)   // 룰렛 한 칸이 결과 단어에서 멈추는 함수
        {
            label.text = word;
            Fx.ReelTick(label.transform);
            Fx.ReelStop(reel);
            Sfx.Play(SfxId.Move);   // '탁'
        }

        void ShowArt(PieceDefinition piece) // 결과 기물 그림과 파란 이펙트를 켜는 함수
        {
            _artPlaceholder.gameObject.SetActive(false);
            _art.gameObject.SetActive(true);
            _art.sprite = piece.itemImage;
            Fx.RevealPiece(_art.transform, UiBuilder.Group(_art.gameObject));

            _glow.gameObject.SetActive(true);
            Fx.GlowPulse(_glow);
            _embers.Play();
        }

        void ClearArt() // 결과 그림과 이펙트를 끄고 "?"로 되돌리는 함수
        {
            Fx.GlowStop(_glow);
            _glow.gameObject.SetActive(false);
            _embers.Stop();
            _art.gameObject.SetActive(false);
            _artPlaceholder.gameObject.SetActive(true);
        }
    }
}
