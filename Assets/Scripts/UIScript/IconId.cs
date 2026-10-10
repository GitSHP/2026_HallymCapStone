namespace Promotion.UI
{
    /// <summary>
    /// [UI 공용] 코드로 그려내는 임시 아이콘의 이름표. 실제 아트가 준비되면 각
    /// 데이터 에셋에 스프라이트를 넣는 것으로 대체되며, 그 경우 스프라이트가 우선한다.
    /// 실제 그림은 UiSprites 가 만든다.
    /// </summary>
    public enum IconId
    {
        None,
        Pawn, Rook, Bishop, Knight, Queen, King,
        Heart, Sword, Bolt, Coin, Shield, Star, Crown, Chevrons,
        Arrow, Bag, Check
    }
}
