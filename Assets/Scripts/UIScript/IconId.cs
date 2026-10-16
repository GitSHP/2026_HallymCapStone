namespace Promotion.UI
{
    public enum IconId  // UI에 쓰이는 아이콘 종류 목록 -> 이 값의 숫자를 UiArt.icons 배열의 인덱스로 사용함
    {
        None,   // 아이콘 없음 (0번)
        Pawn, Rook, Bishop, Knight, Queen, King,   // 체스 기물 아이콘
        Heart, Sword, Bolt, Coin, Shield, Star, Crown, Chevrons,   // 유물·재화 등 일반 아이콘
        Arrow, Bag, Check   // 화살표·가방·체크 표시 아이콘
        // 주의: 중간에 값을 끼워 넣으면 UiArt.icons 배열의 순서가 어긋나므로 항상 맨 뒤에 추가할 것
    }
}
