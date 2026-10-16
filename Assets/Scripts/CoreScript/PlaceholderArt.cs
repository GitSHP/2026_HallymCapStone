using UnityEngine;

namespace Promotion.Core
{
    public static class PlaceholderArt   // 보드·칸·기물을 그릴 때 공용으로 쓰는 재질을 만들어 주는 클래스
    // -> 예전에는 임시 스프라이트(사각형, 원, 둥근 사각형)도 코드로 만들었지만, 지금은 Assets/UI/Sprites의 PNG를 UiArt로 불러 씀
    {
        static Material _unlit;   // 조명 영향을 받지 않는 공용 재질 (처음 쓸 때 한 번만 생성)

        public static Material UnlitMaterial   // 조명 없이 원래 색 그대로 그리는 재질
        // -> 보드는 조명 설계가 없는 평면 2D 화면인데, 조명용(lit) 셰이더는 씬에 2D 조명이 없으면 에러도 없이 검게 그려짐
        // -> 그래서 unlit으로 그림. 나중에 조명이 필요하면 lit 재질을 직접 지정하면 됨
        {
            get
            {
                if (_unlit == null)   // 만약 아직 재질이 없다면 새로 만듦
                {
                    var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")   // URP 2D용 unlit 셰이더를 찾고
                                 ?? Shader.Find("Sprites/Default");   // 없다면 기본 스프라이트 셰이더를 사용
                    _unlit = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };   // 씬에 저장되지 않는 임시 재질로 생성
                }
                return _unlit;
            }
        }
    }
}
