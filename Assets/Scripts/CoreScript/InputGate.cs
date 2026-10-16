namespace Promotion.Core
{
    public static class InputGate   // 보드 조작(호버, 기물 드래그)을 잠시 막아두는 전역 스위치
    // -> 보드 입력은 EventSystem을 거치지 않고 마우스 위치를 직접 읽기 때문에
    //    상점·알림창 같은 UI가 위에 떠 있어도 자동으로 막히지 않음 -> 그래서 이 클래스로 직접 막음
    {
        static int _blockers;   // 현재 보드 입력을 막고 있는 화면의 수
        // -> true/false가 아니라 숫자로 세기 때문에 창 두 개가 어떤 순서로 닫혀도 마지막 창이 닫혀야 입력이 풀림

        public static bool WorldInputEnabled => _blockers <= 0;   // 막고 있는 화면이 하나도 없으면 보드 조작 가능

        public static void Push() => _blockers++;   // 보드를 가리는 화면이 열릴 때 호출하는 함수 -> 차단 수 +1

        public static void Pop()   // 보드를 가리던 화면이 닫힐 때 호출하는 함수 -> 차단 수 -1
        {
            _blockers--;
            if (_blockers < 0)   // 만약 Push보다 Pop이 더 많이 불려 음수가 됐다면
                _blockers = 0;   // 0으로 고정 -> 음수로 남으면 다음 Push가 입력을 막지 못하기 때문
        }

        public static void Reset() => _blockers = 0;   // 차단 수를 0으로 초기화하는 함수
        // -> 씬을 새로 불러오면 Push했던 화면들이 사라지므로 카운트도 함께 비워야 함
    }
}
