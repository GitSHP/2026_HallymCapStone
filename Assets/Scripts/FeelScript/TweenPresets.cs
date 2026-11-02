using PrimeTween;
using UnityEngine;

namespace Promotion.Feel
{
    [CreateAssetMenu(menuName = "Promotion/Tween Presets", fileName = "TweenPresets")]
    public class TweenPresets : ScriptableObject // 게임 연출에 쓰이는 모든 시간, 세기, 가속 곡선(Ease) 값을 한곳에 모은 데이터
    // -> 게임 코드를 건드리지 않고 인스펙터 하나에서 전체 손맛을 조정할 수 있음 (에셋은 Resources/TweenPresets 에 있어야 Fx가 불러옴)
    // 시간 값의 단위는 모두 초, Ease는 움직임의 가속 곡선 (OutBack = 살짝 지나쳤다 돌아옴, InQuad = 점점 빨라짐, OutQuad = 점점 느려짐)
    {
        [Header("호버")]
        public float hoverScale = 1.08f;    // 마우스를 기물 위에 올렸을 때 커지는 배율 (1.08 = 8% 커짐)
        public float hoverDuration = 0.12f; // 커지고 줄어드는 데 걸리는 시간
        public Ease hoverEase = Ease.OutQuad;   // 호버 크기 변화의 가속 곡선

        [Header("드래그")]
        public float dragScale = 1.18f; // 기물을 집어 들었을 때 커지는 배율 -> 손에 들린 느낌을 줌
        public float dragTiltDegrees = 6f;  // 집어 들었을 때 기울어지는 각도
        public float dragFollowSharpness = 18f; // 드래그 중 기물이 커서를 따라가는 민첩함 -> 클수록 빠릿하게, 작을수록 느릿하게 따라옴
        public float dragLiftDuration = 0.1f;   // 집어 드는 동작(커지고 기울어짐)에 걸리는 시간

        [Header("내려놓기 / 착지")]
        public float dropDuration = 0.14f;  // 기물이 칸 위로 내려앉는 데 걸리는 시간
        public Ease dropEase = Ease.InQuad; // 내려앉는 움직임의 가속 곡선 -> 점점 빨라지며 '쿵' 하고 떨어지는 느낌
        public Vector3 dropPunchStrength = new Vector3(0.35f, 0.35f, 0f);   // 착지 직후 기물이 튕기듯 커졌다 돌아오는 세기
        public float dropPunchDuration = 0.3f;  // 착지 튕김이 이어지는 시간
        public float dropPunchFrequency = 12f;  // 착지 튕김의 떨림 횟수 -> 클수록 잘게 떨림

        [Header("보드 흔들림 (착지 시)")]
        public Vector3 boardShakeStrength = new Vector3(0.08f, 0.08f, 0f);  // 기물이 착지할 때 체스판 전체가 흔들리는 세기
        public float boardShakeDuration = 0.25f;    // 판 흔들림이 이어지는 시간
        public float boardShakeFrequency = 14f; // 판 흔들림의 떨림 횟수

        [Header("이동 하이라이트")]
        public float highlightPopDuration = 0.18f;  // 이동 가능한 칸 표시가 톡 튀어나오는 데 걸리는 시간
        public float highlightStagger = 0.02f;  // 칸 표시들이 하나씩 차례로 나타나는 간격 -> 물결처럼 퍼져 보임
        public Ease highlightEase = Ease.OutBack;   // 칸 표시가 살짝 크게 튀었다가 제자리로 돌아오는 곡선

        [Header("전투")]
        public float lungeDistance = 0.35f; // 공격할 때 상대 쪽으로 돌진하는 거리 (월드 단위, 1 = 한 칸 정도)
        public float lungeDuration = 0.12f; // 돌진하는 데 걸리는 시간
        public Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f); // 맞은 기물이 번쩍이는 색 (빨간색)
        public float hitFlashDuration = 0.1f;   // 번쩍임 한 번에 걸리는 시간
        public Vector3 hitShakeStrength = new Vector3(0.12f, 0.05f, 0f);    // 맞은 기물이 부르르 떨리는 세기
        public float hitShakeDuration = 0.22f;  // 맞은 기물이 떨리는 시간

        [Header("사망")]
        public float deathDuration = 0.28f; // 잡힌 기물이 회전하며 작아져 사라지는 시간
        public float deathSpinDegrees = 200f;   // 사라지면서 도는 각도

        [Header("등장")]
        public float spawnDropHeight = 3f;  // 적이 나타날 때 위에서 떨어지기 시작하는 높이
        public float spawnDuration = 0.3f;  // 떨어져 내려오는 데 걸리는 시간
        public Ease spawnEase = Ease.InQuad;    // 떨어지는 움직임의 가속 곡선 -> 점점 빨라짐
        public float spawnStagger = 0.06f;  // 여러 적이 차례로 떨어지는 간격 (적이 한 칸씩 걸어올 때의 시차에도 사용)

        [Header("UI — 화면")]
        public float uiBackdropFade = 0.22f;    // 팝업 화면 뒤의 어두운 배경이 깔리는 시간
        public float uiPanelIn = 0.34f; // 팝업 패널이 나타나는 시간
        public float uiPanelOut = 0.18f;    // 팝업 패널이 사라지는 시간
        public Vector3 uiPanelFromScale = new Vector3(0.92f, 0.92f, 1f);   // 패널이 나타날 때 시작하는 크기 -> 여기서 원래 크기로 커짐
        public Ease uiPanelInEase = Ease.OutBack;   // 패널이 나타날 때의 곡선 (살짝 튀어나옴)
        public Ease uiPanelOutEase = Ease.InQuad;   // 패널이 사라질 때의 곡선

        [Header("UI — 카드")]
        public float uiCardIn = 0.3f;   // 상점 카드 하나가 나타나는 시간
        public float uiCardStagger = 0.05f; // 카드들이 차례로 나타나는 간격
        public float uiCardFromOffsetY = -40f;  // 카드가 등장할 때 시작하는 세로 위치 차이(픽셀) -> 아래에서 올라옴
        public Ease uiCardInEase = Ease.OutBack;    // 카드 등장 곡선

        [Header("UI — 상호작용")]
        public float uiHoverScale = 1.05f;  // UI 버튼/카드에 마우스를 올렸을 때 커지는 배율
        public float uiHoverLift = 10f; // 마우스를 올렸을 때 위로 떠오르는 높이(픽셀)
        public float uiHoverDuration = 0.14f;   // 호버 변화에 걸리는 시간
        public float uiPressScale = 0.95f;  // 버튼을 눌렀을 때 줄어드는 배율 -> 눌리는 느낌
        public float uiPressDuration = 0.07f;   // 눌림 변화에 걸리는 시간

        [Header("UI — 피드백")]
        public Vector3 uiPunchStrength = new Vector3(0.14f, 0.14f, 0f); // 구매 성공 등에서 UI가 통 튀는 세기
        public float uiPunchDuration = 0.35f;   // 튀는 시간
        public float uiPunchFrequency = 10f;    // 튀는 떨림 횟수
        public Vector3 uiDenyShake = new Vector3(14f, 0f, 0f);  // 골드 부족 등 거절될 때 좌우로 도리도리 흔들리는 세기(픽셀)
        public float uiDenyDuration = 0.3f; // 거절 흔들림 시간
        public float uiDenyFrequency = 18f; // 거절 흔들림 떨림 횟수
        public float uiCountDuration = 0.4f;    // 골드 같은 숫자가 바뀔 때 숫자가 올라가거나 내려가는 시간

        [Header("맵")]
        public float mapPulseScale = 1.18f; // 다음에 고를 수 있는 맵 노드가 숨 쉬듯 커지는 배율
        public float mapPulseDuration = 0.6f;   // 한 번 커졌다 작아지는 데 걸리는 시간
        public Ease mapPulseEase = Ease.InOutSine;  // 부드럽게 커지고 작아지는 곡선

        [Header("스토리")]
        public float storyFadeDuration = 0.5f;  // 튜토리얼/스토리 그림이나 글이 서서히 나타나는 시간
        public Ease storyFadeEase = Ease.OutQuad;   // 서서히 나타나는 곡선
        public float curtainFadeDuration = 0.8f;    // 화면 전체를 가린 검은 막이 걷히는 시간
        public Ease curtainFadeEase = Ease.InOutQuad;   // 막이 걷히는 곡선

        [Header("타이틀 연출")]
        [Tooltip("체스판 무늬가 깔리기 시작하는 시점(초).")]
        public float titleTileStart = 0.15f;    // 타이틀 화면에서 체스판 칸들이 깔리기 시작하는 시간
        [Tooltip("대각선 한 줄마다 늦어지는 시간. 클수록 물결이 천천히 번진다.")]
        public float titleTileStagger = 0.03f;  // 대각선 줄마다의 시차
        public float titleTileDuration = 0.35f; // 칸 하나가 커지며 나타나는 데 걸리는 시간
        [Tooltip("칸이 이 크기에서 제 크기로 커지며 깔린다.")]
        public float titleTileFromScale = 0.5f; // 칸이 시작하는 크기 (0.5 = 절반 크기)
        [Tooltip("킹 로고가 떨어지기 시작하는 시점(초).")]
        public float titleLogoDelay = 0.75f;    // 킹 로고가 떨어지기 시작하는 시간
        public float titleLogoDropHeight = 600f;    // 킹 로고가 떨어지기 시작하는 높이(픽셀) -> 화면 위쪽 바깥에서 떨어짐
        public float titleLogoDropDuration = 0.35f; // 킹 로고가 떨어지는 데 걸리는 시간
        [Tooltip("킹이 착지할 때 찌그러졌다 돌아오는 정도.")]
        public Vector3 titleLandSquash = new Vector3(0.22f, -0.22f, 0f);    // 착지 시 가로로 넓어지고 세로로 눌리는 정도 -> 말랑하게 '쿵' 하는 느낌
        public Vector3 titleShakeStrength = new Vector3(22f, 22f, 0f);  // 킹이 착지할 때 타이틀 화면 전체가 흔들리는 세기(픽셀)
        public float titleShakeDuration = 0.4f; // 화면 흔들림(과 찌그러짐) 시간
        public float titleShakeFrequency = 20f; // 화면 흔들림 떨림 횟수
        [Tooltip("제목 글자가 올라오기 시작하는 시점(초).")]
        public float titleLetterDelay = 1.1f;   // 제목 글자들이 올라오기 시작하는 시간
        [Tooltip("글자 하나마다 늦어지는 시간.")]
        public float titleLetterStagger = 0.06f;    // 글자마다의 시차 -> 앞 글자부터 차례로 솟아오름
        public float titleLetterDuration = 0.4f;    // 글자 하나가 올라오는 데 걸리는 시간
        [Tooltip("글자가 올라오기 시작하는 깊이(픽셀).")]
        public float titleLetterRise = 70f; // 글자가 원래 자리보다 이만큼 아래에서 시작해 올라옴
        [Tooltip("메뉴 버튼이 올라오기 시작하는 시점(초).")]
        public float titleMenuDelay = 1.75f;    // 시작/종료 등 메뉴 버튼이 나타나기 시작하는 시간
        [Tooltip("크레딧이 나타나기 시작하는 시점(초).")]
        public float titleCreditDelay = 2.2f;   // 하단 크레딧 문구가 나타나기 시작하는 시간
        public float titleCreditFade = 0.5f;    // 크레딧이 서서히 나타나는 데 걸리는 시간
        [Tooltip("조립이 끝난 뒤 킹 로고가 숨 쉬듯 커지는 정도.")]
        public float titleLogoBreatheScale = 1.04f; // 연출이 끝난 뒤 킹 로고가 계속 커졌다 작아지는 배율
        public float titleLogoBreatheDuration = 1.8f;   // 숨쉬기 한 번(커졌다 작아짐)의 시간

        [Header("기물 가이드")]
        [Tooltip("기물 가이드 창이 커지며 나타나는 시간.")]
        public float guideOpenDuration = 0.28f; // 가이드 창이 살짝 작은 크기에서 원래 크기로 커지는 시간
        [Tooltip("가이드 창이 시작하는 크기 비율.")]
        public float guideOpenScale = 0.9f; // 가이드 창이 나타날 때 시작하는 크기(원래 크기의 90%) -> 0에서 시작하지 않음
        [Tooltip("소환 연출이 끝나길 기다렸다가 가이드를 띄우기까지의 시간(실제 시간).")]
        public float guideShowDelay = 0.45f;    // 기물이 칸에 내려앉는 것을 보여 준 뒤 가이드를 띄우기 위한 대기 시간

        [Header("누적 킬 불꽃")]
        [Tooltip("한 스테이지에서 이 수만큼 잡으면 기물에 불이 붙는다.")]
        [Min(1)] public int killStreakThreshold = 3;    // 불이 붙는 스테이지 누적 킬 수
        [Tooltip("잡기 연출(돌진·착지)이 끝나길 기다렸다가 점화하기까지의 시간.")]
        public float fireIgniteDelay = 0.3f;    // 잡자마자 터뜨리면 착지 연출과 겹치므로 잠깐 기다렸다가 점화함
        public Vector3 fireIgnitePunch = new Vector3(0.3f, 0.3f, 0f);   // 점화 순간 기물이 '화르륵' 커졌다 돌아오는 세기
        public float fireIgnitePunchDuration = 0.35f;   // 점화 펀치가 이어지는 시간
        [Tooltip("점화 순간 보드 흔들림 세기 배율. 1이면 기물 착지 흔들림과 같다.")]
        public float fireIgniteShake = 0.6f;    // 점화 순간 체스판이 흔들리는 세기 배율 (약하게)
        public Color fireGlowColor = new Color(1f, 0.62f, 0.3f, 1f);    // 불붙은 기물 몸체가 깜빡이며 물드는 주황빛
        public float fireGlowPeriod = 0.5f; // 몸체가 주황빛으로 물들었다 돌아오는 데 걸리는 시간(편도)
        public float fireGlowFadeOut = 0.25f;   // 불이 꺼질 때 몸체가 원래 색으로 돌아오는 시간

        [Header("처치 배너")]
        [Tooltip("잡기 동작이 끝나 타격 이펙트가 터진 뒤, 처치 배너가 뜨기까지 기다리는 시간(실제 시간).")]
        public float calloutAfterBurstDelay = 0.35f;    // 이펙트를 먼저 보여 주고 배너를 띄우기 위한 대기 시간
        [Tooltip("배너가 자리보다 이만큼 아래에서 치고 올라온다(픽셀).")]
        public float calloutEnterRise = 90f;    // 등장할 때 올라오는 거리(픽셀, 1920x1080 기준)
        [Tooltip("배너가 이 배율에서 원래 크기로 줄어들며 자리 잡는다.")]
        public float calloutPopScale = 1.25f;   // 등장 시작 크기 배율 (0에서 시작하지 않음)
        public float calloutPopDuration = 0.28f;    // 치고 올라와 자리 잡는 데 걸리는 시간
        public float calloutHold = 0.9f;    // 자리 잡은 뒤 그대로 보여 주는 시간
        public float calloutFadeDuration = 0.35f;   // 사라지는 시간
        public float calloutExitRise = 30f; // 사라지면서 위로 떠오르는 거리(픽셀)
        public Color calloutFirstColor = Color.white;   // 첫 처치("처치!") 글자 색
        public Color calloutChainColor = new Color(1f, 0.62f, 0.2f, 1f);    // 두 번째 이후("연속처치!") 글자 색
        public Color calloutFireColor = new Color(1f, 0.85f, 0.25f, 1f);    // 불이 붙는 킬 수 이상일 때 글자 색 (금색)
        [Tooltip("불이 붙는 킬 수 이상일 때 배너 크기 배율.")]
        public float calloutFireScale = 1.12f;  // 불붙은 기물의 처치 배너는 조금 더 크게

        [Header("최종전 AP 잠김")]
        [Tooltip("최종전 대화가 끝나 체스판이 드러난 뒤 AP 패널이 잠기기까지 기다리는 시간.")]
        public float apLockDelay = 0.45f;   // 잠기기 전 대기 시간
        public float apLockDuration = 0.45f;    // 번쩍임·흔들림이 이어지는 시간
        public Color apLockFlashColor = new Color(1f, 0.32f, 0.25f, 1f);   // 잠기는 순간 패널이 번쩍이는 밝은 붉은색
        public Vector3 apLockPunch = new Vector3(0.12f, 0.12f, 0f);    // 잠기는 순간 패널이 툭 커지는 정도
        public Vector3 apLockShake = new Vector3(12f, 0f, 0f);  // 잠기는 순간 패널이 좌우로 덜컹 흔들리는 세기(픽셀)

        [Header("잡기 막힘 (중갑병)")]
        public Vector3 blockedShake = new Vector3(0.12f, 0f, 0f);   // 잡기를 막아 낸 기물이 좌우로 흔들리는 세기(월드 단위)
        public float blockedShakeDuration = 0.3f;   // 흔들림 시간
        public float blockedPopIn = 0.18f;  // "막힘!" 글자가 튀어나오는 시간
        public float blockedHold = 0.45f;   // "막힘!" 글자를 보여 주는 시간
        public float blockedFade = 0.3f;    // "막힘!" 글자가 사라지는 시간
        public float blockedRise = 0.35f;   // 사라지면서 떠오르는 높이(월드 단위)

        [Header("전역")]
        public float hitStopDuration = 0.08f;   // 타격 순간 시간이 거의 멈추는 '히트 스톱'의 길이 -> 타격감을 줌
    }
}
