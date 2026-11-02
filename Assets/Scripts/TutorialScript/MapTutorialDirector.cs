using System.Collections;
using System.Collections.Generic;
using Promotion.Core;
using Promotion.Map;
using UnityEngine;

namespace Promotion.Tutorial
{
    public class MapTutorialDirector : MonoBehaviour // 지도 씬에서 튜토리얼의 지도 단계를 진행하는 클래스
    // -> 전투·상점 단계를 마친 튜토리얼이 지도로 넘어왔을 때만 동작함 (TutorialSession.Phase == Map)
    // -> 지도의 읽는 법(층·골드, 갈 수 있는 길, 보스)을 말풍선으로 짚어 준 뒤 플레이어가 첫 행선지를 고르면 튜토리얼을 끝냄
    // -> 이 클래스는 지도를 직접 조작하지 않고, MapScreen이 내주는 위치 정보만 받아서 가리킴
    {
        TutorialScript _script; // 진행할 튜토리얼 대본
        MapScreen _map; // 지도 화면
        DialogueView _view; // 대사·말풍선을 보여 주는 컴포넌트

        readonly List<RectTransform> _targets = new(); // 말풍선이 가리킬 화면 요소를 잠시 담는 리스트

        void Awake()
        {
            if (!TutorialSession.IsIn(TutorialPhase.Map)) // 튜토리얼의 지도 단계가 아니라면 (일반 런의 지도)
            {
                enabled = false; // 아무 동작도 하지 않음
                return;
            }

            _script = TutorialSession.Active;
        }

        IEnumerator Start()
        // 지도 단계(mapSteps)를 처음부터 끝까지 순서대로 진행하는 코루틴
        {
            if (_script == null) // 튜토리얼이 아니라면 시작하지 않음
                yield break;

            _map = FindAnyObjectByType<MapScreen>();
            _view = FindAnyObjectByType<DialogueView>(FindObjectsInactive.Include); // 꺼져 있는 대사 화면까지 포함해서 찾음
            if (_map == null || _view == null) // 지도나 대사 화면이 없다면 진행할 수 없으므로 튜토리얼을 끝내 둠
            {
                Debug.LogError("[MapTutorialDirector] MapScreen or DialogueView missing - add the TutorialDialogue prefab to the Map scene.");
                TutorialSession.Complete();
                yield break;
            }

            yield return null; // MapScreen이 Start에서 지도를 그릴 때까지 한 프레임 기다림
            _view.Init(_script); // 대사 화면을 켜고 화자 정보를 채움

            foreach (var step in _script.mapSteps)
            {
                if (step == null)
                    continue;

                ResolveFocus(step.focus, _targets); // 이 단계가 가리킬 화면 요소를 구하고

                if (step.wait == TutorialWait.Click) // 대사 단계라면 넘길 때까지 기다리고
                {
                    yield return _view.Point(step.text, _targets);
                }
                else // 행동 단계라면 안내를 띄우고 그 행동을 할 때까지 기다림
                {
                    _view.PointHint(step.text, _targets);
                    yield return new WaitUntil(() => IsMet(step.wait));
                }

                if (step.focus == TutorialFocus.MapBoss) // 보스를 보여 주느라 위로 스크롤했다면 현재 위치로 되돌림
                    _map.ShowCurrent();

                if (Chosen) // 행선지를 이미 골랐다면 (곧 전투 씬으로 넘어감) 남은 단계는 진행하지 않음
                    break;
            }

            Finish();
        }

        bool Chosen // 플레이어가 지도에서 행선지를 골랐는지 여부
        {
            get { return RunState.Instance.CurrentMapNode >= 0; }
        }

        bool IsMet(TutorialWait wait) // 단계가 기다리는 조건을 만족했는지 검사하는 함수
        {
            switch (wait)
            {
                case TutorialWait.MapNodeChosen: // 지도에서 갈 곳을 골랐는지
                    return Chosen;
                default: // 지도에서 쓸 수 없는 조건은 기다리지 않고 바로 통과
                    return true;
            }
        }

        void ResolveFocus(TutorialFocus focus, List<RectTransform> results)
        // 단계가 가리키는 대상(focus)을 지도 화면의 실제 UI 요소로 바꿔 results에 담는 함수
        {
            results.Clear();
            switch (focus)
            {
                case TutorialFocus.MapStatus: // 위쪽 층·골드 표시
                    results.Add(_map.StatusRect);
                    break;
                case TutorialFocus.MapChoices: // 지금 갈 수 있는 노드들
                    _map.GetSelectableNodes(results);
                    break;
                case TutorialFocus.MapBoss: // 보스 노드 -> 화면 밖에 있을 수 있으므로 먼저 맨 위로 스크롤함
                {
                    _map.ShowBoss();
                    var boss = _map.BossNode;
                    if (boss != null)
                        results.Add(boss);
                    break;
                }
            }
        }

        void Finish() // 지도 단계를 마치고 튜토리얼 전체를 끝내는 함수 -> 이제부터는 실제 런
        {
            _view.HideAll();
            TutorialSession.Complete();
        }

        void OnDestroy()
        // 지도 씬이 사라질 때(전투 씬으로 이동 등) 지도 단계가 남아 있다면 튜토리얼을 끝내 둠
        // -> 어떤 이유로든 Finish를 거치지 못해도 다음 전투가 튜토리얼로 시작되지 않게 하는 안전장치
        {
            if (TutorialSession.IsIn(TutorialPhase.Map))
                TutorialSession.Complete();
        }
    }
}
