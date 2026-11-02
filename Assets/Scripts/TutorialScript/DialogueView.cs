using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Feel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Promotion.Tutorial
{
    public class DialogueView : MonoBehaviour // 튜토리얼의 대사 화면을 담당하는 클래스
    // -> 모드가 두 가지 있음
    //    Talk: 화자 초상화가 있는 비주얼 노벨식 대화창 -> 플레이어가 클릭해 넘길 때까지 화면이 어두워지고 보드 입력이 잠김
    //    Hint: 플레이어가 행동하는 동안 보드 아래에 뜨는 짧은 한 줄 안내 -> 직접 해 보는 것이 학습이므로 아무것도 막지 않음
    // -> 레이아웃은 전투 씬에 꺼진 상태로 놓인 프리팹이고, 튜토리얼이 시작될 때 이를 켜고 스크립트의 화자 정보를 채움
    {
        static readonly Color TalkDim = new Color(0f, 0f, 0f, 0.7f); // 대화 중 화면을 어둡게 덮는 색 (반투명 검정)
        static readonly Color StoryDim = new Color(0f, 0f, 0f, 0.88f); // 만화(스토리)가 떠 있을 때 덮는 색 -> 보드가 아니라 만화에 시선이 가도록 더 어둡게

        [Header("대화")]
        [SerializeField] GameObject _talk; // 대화창 전체 오브젝트 (켜고 끄는 용도)
        [SerializeField] Image _backdrop; // 대화 중 화면 전체를 어둡게 덮는 이미지
        [SerializeField] Button _backdropButton; // 어두운 배경 아무 곳이나 클릭하면 대사를 넘기기 위한 버튼
        [SerializeField] TextMeshProUGUI _speakerName; // 화자 이름 텍스트
        [SerializeField] TextMeshProUGUI _talkText; // 대사 본문 텍스트
        [SerializeField] GameObject _continueMark; // 대사가 다 나왔을 때 "다음으로 넘기기" 표시 오브젝트
        [SerializeField] Image _portrait; // 화자 초상화 이미지
        [Tooltip("스크립트에 일러스트가 없을 때 초상화 대신 보여 준다.")]
        [SerializeField] GameObject _portraitPlaceholder; // 초상화 일러스트가 없을 때 대신 보여 주는 임시 틀

        [Header("보드 가림막")]
        [SerializeField] CanvasGroup _curtain; // 스토리 장면 동안 보드와 HUD를 가리는 가림막 (투명도로 페이드)
        [SerializeField] Image _background; // 가림막에 깔리는 스토리 배경 그림
        [SerializeField] AspectRatioFitter _backgroundFitter; // 배경 그림의 비율을 유지한 채 화면을 채우게 해 주는 컴포넌트
        [SerializeField] Image _shade; // 배경 그림 위에 덮는 음영 -> 만화가 떠 있을 때만 어두워짐

        [Header("스토리")]
        [SerializeField] GameObject _story; // 만화(스토리) 페이지 전체 오브젝트
        [SerializeField] Image _storyBack; // 이전 만화 프레임 (뒤에 깔려 있음)
        [SerializeField] Image _storyFront; // 새 만화 프레임 (앞에서 서서히 나타남)

        [Header("힌트")]
        [SerializeField] GameObject _hint; // 힌트(한 줄 안내) 오브젝트
        [SerializeField] TextMeshProUGUI _hintText; // 힌트 문구 텍스트

        [Header("말풍선 (상점·지도 단계)")]
        [Tooltip("화면 요소를 테두리로 강조하고 그 옆에 말풍선을 띄우는 묶음이다. 큰 대화창은 상점·지도를 가려 버리므로 이것을 쓴다.")]
        [SerializeField] GameObject _callout; // 말풍선 묶음 전체 (켜고 끄는 용도)
        [SerializeField] RectTransform _calloutArea; // 좌표 계산의 기준이 되는 전체 화면 크기의 영역
        [Tooltip("강조 영역 바깥을 어둡게 덮는 네 장이다. 순서: 위, 아래, 왼쪽, 오른쪽.")]
        [SerializeField] RectTransform[] _calloutDims = new RectTransform[4]; // 강조 영역을 뚫고 나머지만 어둡게 덮는 네 장의 판
        [SerializeField] RectTransform _calloutHole; // 강조 영역 위에 덮는 투명한 막 -> 설명하는 동안 강조된 버튼이 실수로 눌리지 않게 함
        [SerializeField] RectTransform _frame; // 강조 영역을 둘러싸는 테두리
        [SerializeField] RectTransform _bubble; // 말풍선 상자
        [SerializeField] TextMeshProUGUI _bubbleName; // 말풍선 위쪽의 화자 이름
        [SerializeField] TextMeshProUGUI _bubbleText; // 말풍선 본문
        [SerializeField] GameObject _bubbleContinue; // 말풍선의 "넘기기" 표시

        const float FramePadding = 14f; // 강조 테두리가 대상보다 바깥으로 벌어지는 여백(픽셀)
        const float BubbleGap = 22f; // 강조 영역과 말풍선 사이 간격(픽셀)
        const float ScreenMargin = 24f; // 말풍선이 화면 가장자리에 붙지 않도록 두는 여백(픽셀)

        readonly List<RectTransform> _targets = new(); // 지금 말풍선이 가리키는 화면 요소들
        bool _calloutBlocking; // 지금 말풍선이 클릭해서 넘기는 대사인지 (false면 행동을 기다리는 힌트)
        bool _breathing; // 강조 테두리의 숨쉬기 연출을 이미 시작했는지 여부 -> 무한 반복 연출이 겹치지 않게 한 번만 시작

        Sprite _defaultPortrait; // 표정을 따로 정하지 않은 대사에 쓰는 기본 초상화
        float _charactersPerSecond = 40f; // 대사 타이핑 속도 (초당 글자 수)
        float _storyShade = 0.8f; // 만화가 떠 있을 때 배경을 어둡게 하는 정도
        bool _covered; // 지금 가림막이 보드를 가리고 있는지 여부
        bool _typing; // 지금 대사가 한 글자씩 타이핑되는 중인지 여부
        bool _advance; // 플레이어가 다음 대사로 넘기기를 눌렀는지 여부
        bool _blocking; // 지금 보드 입력을 막고 있는지 여부 (InputGate에 Push한 상태인지)

        public bool IsTalking  // 지금 대화창 또는 말풍선(클릭해서 넘기는 대사)이 떠 있는지 여부 -> 기물 가이드가 대화가 끝날 때까지 기다리는 데 사용
        {
            get
            {
                bool talking = _talk != null && _talk.activeInHierarchy;
                bool pointing = _callout != null && _callout.activeInHierarchy && _calloutBlocking;
                return talking || pointing;
            }
        }

        public void SetSortingOrder(int order)
        // 대사 화면이 그려지는 순서를 바꾸는 함수 -> 상점처럼 대사 화면보다 위에 그려지는 화면을 설명할 때 그 위로 올림
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = order;
        }

        public void Init(TutorialScript script)
        // 튜토리얼용으로 대사 화면을 켜고 화자 이름·초상화·스토리 배경을 채우는 함수
        {
            Init(script.speakerName, script.portrait, script.storyBackground,
                script.storyBackgroundShade, script.charactersPerSecond);
        }

        public void Init(string speakerName, Sprite portrait, Sprite storyBackground, float storyShade, float charactersPerSecond)
        // 대사 화면을 켜고 화자 이름·초상화·스토리 배경을 채우는 함수 -> 튜토리얼 대본이 아닌 대사(최종전 보스 등)도 이 함수로 씀
        // speakerName: 화자 이름 / portrait: 기본 초상화 / storyBackground: 보드를 가릴 때 깔 배경(없으면 검정)
        // storyShade: 만화가 떠 있을 때 배경 음영 / charactersPerSecond: 타이핑 속도
        {
            _defaultPortrait = portrait;
            _storyShade = storyShade;
            _charactersPerSecond = Mathf.Max(1f, charactersPerSecond);
            gameObject.SetActive(true); // 꺼져 있던 대사 화면을 켬

            _backdropButton.onClick.RemoveListener(Continue); // 혹시 이미 연결되어 있다면 먼저 빼고 (중복 연결 방지)
            _backdropButton.onClick.AddListener(Continue); // 배경을 클릭하면 Continue가 실행되도록 연결

            _speakerName.text = speakerName; // 화자 이름을 표시하고
            ShowPortrait(portrait); // 기본 초상화를 보여 줌
            InitCallout(speakerName); // 말풍선의 클릭 연결과 화자 이름도 준비함

            bool hasBackground = storyBackground != null; // 스토리 배경 그림이 있는지
            _background.gameObject.SetActive(hasBackground); // 배경 그림이 있을 때만 배경과 음영을 켬
            _shade.gameObject.SetActive(hasBackground);
            if (hasBackground) // 만약 배경 그림이 있다면
            {
                _background.sprite = storyBackground; // 배경 그림을 넣고
                _backgroundFitter.aspectRatio = storyBackground.rect.width / storyBackground.rect.height; // 그림의 가로/세로 비율을 알려 줌 -> 어떤 화면비에서도 늘리지 않고 잘라 내는 방식으로 화면을 채움
                _shade.color = Color.clear; // 만화가 시작될 때까지 음영은 투명하게 둠 (SetShade 참고)
            }

            HideAll(); // 처음에는 대화창·힌트·스토리를 모두 숨긴 상태로 시작
        }

        public IEnumerator Say(string text, Sprite expression = null, Sprite storyFrame = null)
        // 대화창에 대사 한 줄을 타이핑 효과로 보여 주고, 플레이어가 넘길 때까지 기다리는 것을 순서대로 진행하는 코루틴
        // -> expression: 이 대사에서 쓸 초상화 (null이면 기본 초상화), storyFrame: 함께 띄울 만화 프레임 (null이면 만화 없음)
        {
            ShowPortrait(expression != null ? expression : _defaultPortrait); // 표정 그림이 있으면 그것을, 없으면 기본 초상화를 보여 주고
            ShowStory(storyFrame); // 만화 프레임을 보여 주거나 숨김

            _hint.SetActive(false); // 힌트는 숨기고
            HideCallout(); // 말풍선도 숨기고
            _talk.SetActive(true); // 대화창을 켜고
            SetBlocking(true); // 대화 중에는 보드 입력을 막음

            yield return TypeAndWait(_talkText, _continueMark, text); // 대사를 타이핑하듯 보여 주고 넘길 때까지 기다림
        }

        IEnumerator TypeAndWait(TextMeshProUGUI label, GameObject continueMark, string text)
        // 대사를 한 글자씩 보여 준 뒤 플레이어가 넘기기를 누를 때까지 기다리는 코루틴 -> 큰 대화창과 말풍선이 함께 사용
        {
            label.text = text; // 대사 전체를 텍스트에 넣되
            label.maxVisibleCharacters = 0; // 처음에는 한 글자도 보이지 않게 하고
            label.ForceMeshUpdate(); // 글자 수를 정확히 알기 위해 텍스트 정보를 즉시 갱신
            int total = label.textInfo.characterCount; // 실제로 보여 줄 전체 글자 수 (태그 등은 제외된 값)

            continueMark.SetActive(false); // 타이핑 중에는 넘기기 표시를 숨기고
            _advance = false; // 넘기기 입력을 초기화하고
            _typing = true; // 타이핑 시작

            // 타이핑 효과: 매 프레임 보이는 글자 수를 조금씩 늘림
            // -> 다른 곳의 히트스톱(Time.timeScale 조절) 때문에 대사가 멈추면 안 되므로 unscaled 시간을 사용
            float shown = 0f; // 지금까지 보여 준 글자 수 (소수점까지 누적)
            while (_typing && shown < total) // 플레이어가 건너뛰지 않았고 아직 다 보여 주지 않았다면 반복
            {
                shown += Time.unscaledDeltaTime * _charactersPerSecond; // 지난 프레임 시간 × 초당 글자 수만큼 늘리고
                label.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown)); // 그만큼의 글자만 화면에 보이게 함
                yield return null; // 다음 프레임까지 대기
            }

            // 타이핑이 끝났거나 플레이어가 건너뛰었다면 -> 대사 전체를 한 번에 보여 줌
            _typing = false;
            label.maxVisibleCharacters = total;
            continueMark.SetActive(true); // 넘기기 표시를 켜고
            _advance = false; // 건너뛰기에 쓴 입력이 바로 넘기기로 처리되지 않도록 초기화

            while (!_advance) // 플레이어가 넘기기를 누를 때까지 기다림
                yield return null;
        }

        // ---------- 말풍선 (상점·지도 단계) ----------

        public IEnumerator Point(string text, IReadOnlyList<RectTransform> targets)
        // 화면 요소(targets)를 테두리로 강조하고 그 옆 말풍선에 대사를 보여 준 뒤, 플레이어가 넘길 때까지 기다리는 코루틴
        // -> 강조 영역 바깥은 어둡게 덮여 클릭이 막히므로, 설명하는 동안 실수로 상품을 사거나 노드를 고르지 않음
        // -> targets가 비어 있으면 강조 없이 화면 가운데에 말풍선만 띄움
        {
            if (_callout == null) // 말풍선이 없는 프리팹이라면 큰 대화창으로 대신 보여 줌 -> 튜토리얼이 멈추지는 않게
            {
                yield return Say(text);
                yield break;
            }

            FitBubble(text); // 대사 길이에 맞게 말풍선 높이를 먼저 정하고
            ShowCallout(targets, true); // 클릭해서 넘기는 말풍선으로 띄우고
            SetBlocking(true); // 보드 입력도 막음
            yield return TypeAndWait(_bubbleText, _bubbleContinue, text);
        }

        public void PointHint(string text, IReadOnlyList<RectTransform> targets)
        // 화면 요소(targets)를 강조하고 그 옆에 행동 안내를 띄우는 함수 -> 아무것도 막지 않으므로 플레이어가 바로 그 요소를 누를 수 있음
        {
            if (_callout == null) // 말풍선이 없는 프리팹이라면 한 줄 힌트로 대신 보여 줌
            {
                ShowHint(text);
                return;
            }

            FitBubble(text); // 안내 길이에 맞게 말풍선 높이를 정하고
            ShowCallout(targets, false); // 막지 않는 말풍선으로 띄우고
            SetBlocking(false); // 입력 막기를 풀고
            _bubbleText.text = text; // 안내 문구를 한 번에 보여 줌 (행동을 기다리는 동안 타이핑 효과는 필요 없음)
            _bubbleText.maxVisibleCharacters = int.MaxValue;
            _bubbleContinue.SetActive(false); // 클릭해서 넘기는 단계가 아니므로 넘기기 표시는 숨김
        }

        void FitBubble(string text)
        // 말풍선 높이를 대사 길이에 맞추는 함수 -> 한 줄짜리 안내에 큰 빈 상자가 뜨지 않게 함
        // -> 본문 칸은 말풍선에 늘어나도록 붙어 있으므로, 말풍선 높이만 바꾸면 본문 칸도 따라 바뀜
        {
            var bodyRect = _bubbleText.rectTransform;
            float chrome = _bubble.rect.height - bodyRect.rect.height; // 본문 칸을 뺀 나머지 높이 (위쪽 이름 줄 + 아래쪽 여백)
            float textHeight = _bubbleText.GetPreferredValues(text, bodyRect.rect.width, 0f).y; // 이 대사가 본문 칸 너비에서 차지하는 높이
            float height = Mathf.Max(BubbleMinHeight, chrome + textHeight + 4f); // 너무 납작해지지 않도록 최소 높이를 지킴
            _bubble.sizeDelta = new Vector2(_bubble.sizeDelta.x, height);
        }

        const float BubbleMinHeight = 120f; // 말풍선의 최소 높이(픽셀)

        void InitCallout(string speakerName)
        // 말풍선의 화자 이름을 채우고, 어두운 판을 누르면 대사가 넘어가도록 연결하는 함수
        {
            if (_callout == null) // 말풍선이 없는 프리팹이라면 할 일이 없음
                return;

            _bubbleName.text = speakerName;

            foreach (var dim in _calloutDims) // 어두운 판 네 장과
                Listen(dim);
            Listen(_calloutHole); // 강조 영역의 투명한 막 모두 누르면 대사가 넘어가게 함

            if (!_breathing) // 강조 테두리가 숨 쉬듯 커졌다 작아지게 함 -> 무한 반복이므로 한 번만 시작
            {
                _breathing = true;
                Fx.Breathe(_frame);
            }
        }

        void Listen(RectTransform clickable) // 그 영역을 누르면 Continue가 실행되도록 연결하는 함수
        {
            if (clickable == null)
                return;

            var button = clickable.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.RemoveListener(Continue); // 중복 연결 방지
            button.onClick.AddListener(Continue);
        }

        void ShowCallout(IReadOnlyList<RectTransform> targets, bool blocking)
        // 큰 대화창·힌트·만화를 끄고 말풍선을 켜는 함수 -> 강조 대상을 기억해 두고 바로 한 번 배치함
        {
            ForceUncover(); // 상점·지도를 설명하는 중이므로 가림막은 걷고
            ShowStory(null); // 만화와
            _talk.SetActive(false); // 큰 대화창과
            _hint.SetActive(false); // 힌트는 숨김

            _targets.Clear(); // 이번에 가리킬 대상을 새로 기억함
            if (targets != null)
                foreach (var target in targets)
                    if (target != null)
                        _targets.Add(target);

            _calloutBlocking = blocking;
            _callout.SetActive(true);
            LayoutCallout(); // 첫 프레임부터 올바른 위치에 보이도록 바로 배치
        }

        void HideCallout() // 말풍선을 숨기는 함수
        {
            if (_callout != null)
                _callout.SetActive(false);
            _calloutBlocking = false;
            _targets.Clear();
        }

        void LateUpdate()
        // 말풍선이 떠 있는 동안 매 프레임 위치를 다시 맞추는 함수
        // -> 상점 카드가 날아 들어오거나 지도가 스크롤되는 등 대상이 움직여도 테두리가 따라감
        {
            if (_callout != null && _callout.activeSelf)
                LayoutCallout();
        }

        void LayoutCallout()
        // 강조 테두리·어두운 판·말풍선을 대상 위치에 맞게 배치하는 함수
        {
            bool hasTarget = TryGetTargetBounds(out var bounds); // 가리킬 대상들을 모두 감싸는 영역을 구함
            var area = _calloutArea.rect; // 화면 전체 영역 (말풍선 묶음 기준 좌표)

            var hole = hasTarget
                ? Rect.MinMaxRect(bounds.xMin - FramePadding, bounds.yMin - FramePadding,
                    bounds.xMax + FramePadding, bounds.yMax + FramePadding) // 대상보다 조금 넓게
                : new Rect(area.center, Vector2.zero); // 대상이 없으면 크기 0인 구멍 -> 화면 전체가 어둡게 덮임

            _frame.gameObject.SetActive(hasTarget); // 대상이 있을 때만 테두리를 보여 주고
            if (hasTarget)
                Place(_frame, hole);

            LayoutDims(area, hole); // 구멍 바깥을 어둡게 덮고
            LayoutBubble(area, hasTarget, hole); // 말풍선을 구멍 옆에 놓음
        }

        void LayoutDims(Rect area, Rect hole)
        // 구멍(hole)만 남기고 화면을 위·아래·왼쪽·오른쪽 네 장으로 덮는 함수 -> 클릭해서 넘기는 대사일 때만 보임
        {
            for (int i = 0; i < _calloutDims.Length; i++)
                if (_calloutDims[i] != null)
                    _calloutDims[i].gameObject.SetActive(_calloutBlocking);
            if (_calloutHole != null)
                _calloutHole.gameObject.SetActive(_calloutBlocking && hole.width > 0f);

            if (!_calloutBlocking) // 행동을 기다리는 힌트라면 아무것도 덮지 않음
                return;

            float left = Mathf.Max(area.xMin, hole.xMin); // 구멍이 화면 밖으로 나가도 판이 뒤집히지 않도록 화면 안으로 자름
            float right = Mathf.Min(area.xMax, hole.xMax);
            float bottom = Mathf.Max(area.yMin, hole.yMin);
            float top = Mathf.Min(area.yMax, hole.yMax);

            PlaceDim(0, Rect.MinMaxRect(area.xMin, top, area.xMax, area.yMax)); // 위: 구멍 위쪽 전부
            PlaceDim(1, Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, bottom)); // 아래: 구멍 아래쪽 전부
            PlaceDim(2, Rect.MinMaxRect(area.xMin, bottom, left, top)); // 왼쪽: 구멍 높이만큼
            PlaceDim(3, Rect.MinMaxRect(right, bottom, area.xMax, top)); // 오른쪽: 구멍 높이만큼

            if (_calloutHole != null && hole.width > 0f)
                Place(_calloutHole, hole); // 구멍 자리에는 투명한 막을 덮어 클릭을 받음
        }

        void PlaceDim(int index, Rect rect) // index번 어두운 판을 rect 크기로 놓는 함수 (크기가 음수가 되면 0으로)
        {
            if (index >= _calloutDims.Length || _calloutDims[index] == null)
                return;

            rect.width = Mathf.Max(0f, rect.width);
            rect.height = Mathf.Max(0f, rect.height);
            Place(_calloutDims[index], rect);
        }

        void LayoutBubble(Rect area, bool hasTarget, Rect hole)
        // 말풍선을 놓는 함수 -> 대상 아래에 자리가 있으면 아래, 없으면 위, 대상이 없으면 화면 가운데
        {
            var size = _bubble.rect.size; // 말풍선 크기
            var center = area.center; // 대상이 없을 때의 위치 = 화면 가운데

            if (hasTarget)
            {
                float below = hole.yMin - BubbleGap - size.y * 0.5f; // 대상 바로 아래에 놓을 때의 중심 높이
                float above = hole.yMax + BubbleGap + size.y * 0.5f; // 대상 바로 위에 놓을 때의 중심 높이
                bool fitsBelow = below - size.y * 0.5f >= area.yMin + ScreenMargin; // 아래에 놓아도 화면 안에 들어오는지

                center = new Vector2(hole.center.x, fitsBelow ? below : above);
            }

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;
            center.x = Mathf.Clamp(center.x, area.xMin + ScreenMargin + halfW, area.xMax - ScreenMargin - halfW); // 화면 밖으로 나가지 않게 자름
            center.y = Mathf.Clamp(center.y, area.yMin + ScreenMargin + halfH, area.yMax - ScreenMargin - halfH);

            _bubble.anchoredPosition = center - area.center; // 말풍선은 화면 가운데 기준으로 놓임
        }

        bool TryGetTargetBounds(out Rect bounds)
        // 강조할 대상들을 모두 감싸는 사각형을 말풍선 묶음 기준 좌표로 구하는 함수 -> 대상이 하나도 보이지 않으면 false
        // -> 모든 UI가 Screen Space Overlay 캔버스라 대상의 월드 좌표가 곧 화면 픽셀 좌표임
        {
            bounds = default;
            bool found = false;
            var corners = new Vector3[4];

            foreach (var target in _targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy) // 사라졌거나 꺼진 대상은 건너뜀
                    continue;

                target.GetWorldCorners(corners); // 대상의 네 꼭짓점 화면 좌표를 구해서
                foreach (var corner in corners)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_calloutArea, corner, null, out var local); // 말풍선 묶음 기준 좌표로 바꾸고
                    if (!found) // 첫 점이라면 그 점에서 시작
                    {
                        bounds = new Rect(local, Vector2.zero);
                        found = true;
                    }
                    else // 그 뒤로는 모든 점을 감싸도록 넓힘
                    {
                        bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, local.x), Mathf.Min(bounds.yMin, local.y),
                            Mathf.Max(bounds.xMax, local.x), Mathf.Max(bounds.yMax, local.y));
                    }
                }
            }

            return found;
        }

        void Place(RectTransform target, Rect rect)
        // UI 요소를 말풍선 묶음 기준 좌표의 rect 위치·크기에 정확히 맞추는 함수
        // -> 요소들은 화면 가운데를 기준점(앵커)으로 두므로, 위치는 화면 가운데에서 얼마나 떨어졌는지로 넣음
        {
            target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = rect.size;
            target.anchoredPosition = rect.center - _calloutArea.rect.center;
        }

        public void ShowHint(string text)
        // 플레이어가 직접 행동하는 동안 보여 주는 짧은 한 줄 안내를 띄우는 함수 -> 아무 입력도 막지 않음
        {
            ForceUncover(); // 플레이어가 곧 보드에서 행동해야 하므로 어떤 경우에도 가림막을 걷음
            HideCallout(); // 말풍선을 끄고
            _talk.SetActive(false); // 대화창을 끄고
            SetBlocking(false); // 보드 입력 막기를 풀고
            ShowStory(null); // 만화도 숨김

            _hintText.text = text; // 힌트 문구를 넣고
            _hint.SetActive(true); // 힌트를 화면에 띄움
        }

        public void HideAll() // 가림막·만화·대화창·힌트·말풍선을 모두 숨기고 입력 막기를 푸는 함수
        {
            ForceUncover();
            ShowStory(null);
            _talk.SetActive(false);
            _hint.SetActive(false);
            HideCallout();
            SetBlocking(false);
        }

        public void SetBoardCovered(bool covered)
        // 스토리 장면을 위해 보드와 HUD를 가림막 뒤로 숨기거나 다시 드러내는 함수
        // -> 가릴 때는 즉시 가리고, 걷을 때는 서서히 사라지게 해서 전장이 뚝 끊겨 나타나지 않도록 함
        {
            bool showing = _curtain.gameObject.activeSelf; // 지금 가림막이 켜져 있는지 (바꾸기 전 상태)
            _covered = covered; // 새 상태를 저장하고
            UpdateDim(); // 어둡게 덮는 색도 상태에 맞게 갱신

            if (covered) // 만약 가려야 한다면
            {
                _curtain.gameObject.SetActive(true); // 가림막을 켜고
                _curtain.alpha = 1f; // 즉시 완전히 불투명하게 함
                return;
            }

            if (showing) // 걷어야 하고 가림막이 켜져 있었다면
                Fx.FadeOutCurtain(_curtain); // 서서히 사라지게 함
        }

        void ForceUncover() // 연출 없이 가림막을 즉시 걷는 함수
        {
            _covered = false;
            _curtain.gameObject.SetActive(false);
            UpdateDim();
        }

        void ShowPortrait(Sprite sprite)
        // 초상화를 보여 주는 함수 -> 일러스트가 없으면 대신 임시 틀을 보여 줌
        {
            bool hasArt = sprite != null; // 보여 줄 일러스트가 있는지
            _portrait.gameObject.SetActive(hasArt); // 일러스트가 있을 때만 초상화를 켜고
            if (_portraitPlaceholder != null) // 임시 틀이 지정되어 있다면
                _portraitPlaceholder.SetActive(!hasArt); // 일러스트가 없을 때만 임시 틀을 켬
            if (hasArt) // 만약 일러스트가 있다면 초상화 이미지를 교체
                _portrait.sprite = sprite;
        }

        void UpdateDim()
        // 대화 중 화면을 어둡게 덮는 색을 상황에 맞게 정하는 함수
        // -> 가림막(스토리 배경)이 있을 때는 어둡게 덮으면 그림을 묻어 버리므로 투명하게 두고, 배경 자체의 음영으로만 어둡게 함
        {
            if (_covered) // 만약 가림막이 보드를 가리고 있다면
                _backdrop.color = Color.clear; // 덮는 색은 투명
            else
                _backdrop.color = _story.activeSelf ? StoryDim : TalkDim; // 만화가 떠 있으면 더 어둡게, 아니면 기본 대화 어둡기
        }

        void ShowStory(Sprite frame)
        // 만화 한 페이지(프레임)를 보여 주는 함수 (frame이 null이면 만화를 숨김)
        // -> 각 프레임에는 지금까지의 모든 컷이 그려져 있음
        // -> 새 프레임을 이전 프레임 위에 겹쳐 서서히 나타나게 하면 다음 컷이 새로 등장하는 것처럼 보임
        {
            if (frame == null) // 만약 보여 줄 프레임이 없다면 -> 만화를 숨김
            {
                if (_story.activeSelf) // 만화가 떠 있었다면
                    SetShade(false); // 배경 음영을 다시 밝히고

                _story.SetActive(false); // 만화를 끄고
                _storyFront.sprite = null; // 앞 프레임을 비움 -> 다음에 처음부터 다시 시작
                UpdateDim();
                return;
            }

            if (_story.activeSelf && _storyFront.sprite == frame) // 이미 같은 프레임이 떠 있다면 다시 연출하지 않음
                return;

            if (!_story.activeSelf) // 만화가 처음 나타나는 순간이라면
                SetShade(true); // 배경 음영을 어둡게 함

            var previous = _story.activeSelf ? _storyFront.sprite : null; // 직전에 보이던 프레임 (처음이면 없음)
            _story.SetActive(true); // 만화를 켜고
            UpdateDim();

            _storyBack.sprite = previous; // 직전 프레임을 뒤쪽에 깔아 두고
            _storyBack.enabled = previous != null; // 직전 프레임이 없다면 뒤쪽 이미지는 끔

            _storyFront.sprite = frame; // 새 프레임을 앞쪽에 넣고
            Fx.FadeIn(_storyFront); // 서서히 나타나게 함 -> 다음 컷이 등장하는 것처럼 보임
        }

        void SetShade(bool on)
        // 만화가 떠 있는 동안 배경 그림을 어둡게 하고, 사라지면 다시 밝히는 함수
        {
            if (_shade != null && _shade.gameObject.activeSelf) // 음영 오브젝트가 있고 켜져 있을 때만 (배경 그림이 있을 때만)
                Fx.FadeTo(_shade, on ? _storyShade : 0f); // 켜면 정해 둔 어둡기로, 끄면 투명하게 서서히 바꿈
        }

        void Update() // 매 프레임 키보드 입력을 검사해서 대사를 넘기는 함수
        {
            bool pointing = _callout != null && _callout.activeSelf && _calloutBlocking; // 클릭해서 넘기는 말풍선이 떠 있는지
            if (!_talk.activeSelf && !pointing) // 대화창도 말풍선 대사도 없다면 검사할 필요 없음
                return;

            var keyboard = Keyboard.current; // 현재 연결된 키보드 (신 Input System)
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) // 만약 이번 프레임에 스페이스나 엔터를 눌렀다면
                Continue(); // 대사를 넘김 (클릭과 같은 처리)
        }

        void Continue()
        // 넘기기 입력을 처리하는 함수 -> 첫 입력은 타이핑을 즉시 끝내고, 다음 입력은 다음 대사로 넘어감
        {
            if (_typing) // 만약 아직 타이핑 중이라면
                _typing = false; // 타이핑을 멈춤 -> Say에서 대사 전체가 한 번에 표시됨
            else
                _advance = true; // 타이핑이 끝났다면 다음 대사로 넘어가도록 표시
        }

        void SetBlocking(bool blocking)
        // 보드 입력 막기를 켜고 끄는 함수 -> InputGate는 Push/Pop 횟수로 관리되므로 상태가 바뀔 때만 한 번씩 호출해야 함
        {
            if (_blocking == blocking) // 이미 같은 상태라면 아무것도 하지 않음 -> Push/Pop이 중복되지 않도록
                return;

            _blocking = blocking; // 새 상태를 저장하고
            if (blocking) // 막아야 한다면
                InputGate.Push(); // 입력 막기 횟수를 하나 늘리고
            else
                InputGate.Pop(); // 풀어야 한다면 하나 줄임
        }

        void OnDestroy() // 대사 화면이 사라질 때 입력 막기가 남아 있지 않도록 반드시 풀어 주는 함수
        {
            SetBlocking(false);
        }
    }
}
