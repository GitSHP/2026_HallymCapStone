using System.Collections;
using Promotion.Data;
using Promotion.Tutorial;   // [추가] 기존 기물 가이드 스크립트(PieceGuideDirector, PieceGuideView) 사용
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.UI
{
    public class GuideBookUIManager : MonoBehaviour // 아이템 도감의 모든 창을 켜고 끄는 관리자
    // -> 가이드 캔버스가 자식이 아니어도 참조로 연결해서 함께 관리함 -> "지금 열린 창"을 이 스크립트 한 곳에서만 기억함
    // -> 기물 설명창은 새로 만들지 않고 기존 기물 가이드(PieceGuideDirector + PieceGuideView)를 그대로 빌려 씀
    {
        enum PieceGuideCloseReason  // [추가] 기물 설명창이 어떻게 닫혔는지 -> 다음에 어느 창으로 갈지 정하는 데 사용
        {
            Confirm,    // 마지막 장에서 [확인] (PieceGuideView가 스스로 닫음)
            Back,       // 도감용 뒤로 가기 버튼
            Close       // 도감용 닫기 버튼
        }

        [Header("도감 열기 버튼")]
        [SerializeField] Button _openButton;

        [Header("도감 종류 선택 창")]
        [SerializeField] GameObject _selectPanel;
        [SerializeField] Button _selectArtifactButton;  // "아티팩트" 버튼
        [SerializeField] Button _selectPieceButton;     // "기물" 버튼
        [SerializeField] Button _selectCloseButton;

        [Header("아티팩트 리스트 창")]
        [SerializeField] GameObject _artifactListPanel;
        [SerializeField] Button _artifactListBackButton;
        [SerializeField] Button _artifactListCloseButton;

        [Header("기물 리스트 창")]
        [SerializeField] GameObject _pieceListPanel;
        [SerializeField] Button _pieceListBackButton;
        [SerializeField] Button _pieceListCloseButton;

        [Header("아티팩트 설명 창")]
        [SerializeField] ArtifactGuideView _artifactGuide;

        [Header("기물 설명 창")]    // [변경] 새 창 대신 기존 PieceGuide 오브젝트를 연결
        [Tooltip("씬의 PieceGuide 오브젝트에 붙은 PieceGuideDirector -> 게임 정지와 창 열기를 맡김")]
        [SerializeField] PieceGuideDirector _pieceGuideDirector;
        [Tooltip("PieceGuide 오브젝트의 PieceGuideView -> 창을 닫거나 닫혔는지 확인하는 데만 사용")]
        [SerializeField] PieceGuideView _pieceGuideView;
        [Tooltip("PieceGuide 프리팹의 설명 상자 안에 추가한 뒤로 가기 버튼 (도감에서 열 때만 보임)")]
        [SerializeField] Button _pieceGuideBackButton;
        [Tooltip("PieceGuide 프리팹의 설명 상자 안에 추가한 닫기 버튼 (도감에서 열 때만 보임)")]
        [SerializeField] Button _pieceGuideCloseButton;

        GameObject _currentListPanel;   // 지금(또는 마지막으로) 보던 리스트 창 -> 설명창에서 뒤로 갈 때 이 창으로 돌아감
        PieceGuideCloseReason _pieceGuideCloseReason;   // [추가] 이번에 기물 설명창이 닫힌 이유
        bool _waitingPieceGuide;    // [추가] 도감에서 연 기물 설명창이 떠 있는 중인지

        void Awake()    // 모든 버튼에 할 일을 연결하고, 시작할 때는 도감을 전부 닫아 둠
        {
            _openButton.onClick.AddListener(OnOpenButtonClicked);

            _selectArtifactButton.onClick.AddListener(OpenArtifactList);
            _selectPieceButton.onClick.AddListener(OpenPieceList);
            _selectCloseButton.onClick.AddListener(CloseAll);

            _artifactListBackButton.onClick.AddListener(OpenSelect);    // 리스트 -> 종류 선택 창
            _artifactListCloseButton.onClick.AddListener(CloseAll);

            _pieceListBackButton.onClick.AddListener(OpenSelect);
            _pieceListCloseButton.onClick.AddListener(CloseAll);

            _pieceGuideBackButton.onClick.AddListener(OnPieceGuideBackClicked);     // [추가]
            _pieceGuideCloseButton.onClick.AddListener(OnPieceGuideCloseClicked);   // [추가]
            SetPieceGuideButtonsVisible(false); // [추가] 자동 가이드, 인벤토리 [설명]에서는 이 버튼들이 보이면 안 되므로 숨겨 둠

            CloseAll();
        }

        // ───── 창 열기 ─────

        void OnOpenButtonClicked()  // ? 버튼 -> 닫혀 있으면 열고, 이미 열려 있으면 닫음
        {
            if (_waitingPieceGuide) // [추가] 기물 설명창이 떠 있는 동안에는 무시 (설명창의 닫기 버튼으로 닫음)
            {
                return;
            }

            if (IsOpen())
            {
                CloseAll();
            }
            else
            {
                OpenSelect();
            }
        }

        public void OpenSelect()    // 도감 종류 선택 창만 보여 줌
        {
            HideAll();
            _selectPanel.SetActive(true);
        }

        public void OpenArtifactList()
        {
            OpenList(_artifactListPanel);
        }

        public void OpenPieceList()
        {
            OpenList(_pieceListPanel);
        }

        void OpenList(GameObject listPanel) // 리스트 창 하나만 보여 주고, 어떤 리스트였는지 기억해 둠
        {
            HideAll();
            _currentListPanel = listPanel;
            listPanel.SetActive(true);
        }

        public void ShowItem(ItemData item) // [변경] 리스트의 아이콘 버튼이 호출 -> 아이템 종류를 보고 알맞은 설명창을 띄움
        {
            if (item == null)
            {
                return;
            }

            PieceDefinition piece = item as PieceDefinition;    // 기물 데이터인지 확인 (기물이 아니면 null)
            if (piece != null)
            {
                ShowPieceGuide(piece);
                return;
            }

            HideAll();
            _artifactGuide.Show(item);
        }

        // ───── 기물 설명창 (기존 기물 가이드 재활용) ─────

        void ShowPieceGuide(PieceDefinition piece)  // [추가] 기존 Director에게 설명창을 열어 달라고 요청
        {
            if (_waitingPieceGuide) // 이미 열려 있으면 무시
            {
                return;
            }

            bool opened = _pieceGuideDirector.ShowGuide(piece);
            // -> 인벤토리 [설명]과 같은 기존 함수: 게임 정지 + 입력 차단 + PieceGuideView 열기, 본 기록은 건드리지 않음
            // -> 가이드 데이터가 없거나 자동 가이드가 처리 중이면 false

            if (!opened)    // 못 열었으면 리스트를 그대로 둠
            {
                Debug.LogWarning("[GuideBookUIManager] 기물 설명창을 열 수 없습니다: " + piece.name);
                return;
            }

            HideAll();  // 리스트 창을 숨기고
            _pieceGuideCloseReason = PieceGuideCloseReason.Confirm; // 기본은 [확인] -> 뒤로/닫기를 누르면 그 값으로 바뀜
            SetPieceGuideButtonsVisible(true);  // 도감용 뒤로/닫기 버튼을 보여 줌
            StartCoroutine(WaitPieceGuideClosed());
        }

        IEnumerator WaitPieceGuideClosed()
        // [추가] 기물 설명창이 꺼질 때까지 기다렸다가, 닫힌 이유에 따라 다음 창을 정하는 코루틴
        // -> PieceGuideView는 [확인] 또는 Hide()로 닫히면 자기 오브젝트를 끄므로, 그걸 보고 닫힘을 알아냄
        // -> 게임이 멈춰 있어도(timeScale 0) yield return null은 매 프레임 계속 돌아감
        {
            _waitingPieceGuide = true;

            while (_pieceGuideView.gameObject.activeSelf)
            {
                yield return null;
            }

            _waitingPieceGuide = false;
            SetPieceGuideButtonsVisible(false); // 다음에 자동 가이드로 열릴 때 보이지 않게 다시 숨김

            if (_pieceGuideCloseReason == PieceGuideCloseReason.Close)  // 닫기 -> 도감 전체를 닫음
            {
                CloseAll();
            }
            else    // 뒤로 가기, 마지막 장 [확인] -> 기물 리스트로 돌아감
            {
                BackFromGuide();
            }
        }

        void OnPieceGuideBackClicked()  // [추가] 설명창의 뒤로 가기 -> 이유를 기록하고 기존 Hide()로 창을 닫음
        {
            _pieceGuideCloseReason = PieceGuideCloseReason.Back;
            _pieceGuideView.Hide(); // Director가 기다리던 창이 닫히므로 Director가 기존 코드대로 게임을 다시 진행시킴
        }

        void OnPieceGuideCloseClicked() // [추가] 설명창의 닫기 -> 이유를 기록하고 창을 닫음
        {
            _pieceGuideCloseReason = PieceGuideCloseReason.Close;
            _pieceGuideView.Hide();
        }

        void SetPieceGuideButtonsVisible(bool visible)  // [추가] 도감용 뒤로/닫기 버튼을 보이거나 숨김
        {
            _pieceGuideBackButton.gameObject.SetActive(visible);
            _pieceGuideCloseButton.gameObject.SetActive(visible);
        }

        // ───── 뒤로 가기 / 닫기 ─────

        public void BackFromGuide() // 설명창의 뒤로 가기 -> 보던 리스트 창으로 돌아감
        {
            if (_currentListPanel == null)  // 기억해 둔 리스트가 없으면 (예외 상황) 종류 선택 창으로
            {
                OpenSelect();
                return;
            }
            OpenList(_currentListPanel);
        }

        public void CloseAll()  // 도감을 완전히 닫음
        {
            HideAll();
            _currentListPanel = null;
        }

        void HideAll()  // [변경] 도감 창을 모두 끔 -> 기물 설명창은 Director가 관리하므로 여기서 끄지 않음
        {
            _selectPanel.SetActive(false);
            _artifactListPanel.SetActive(false);
            _pieceListPanel.SetActive(false);
            _artifactGuide.Hide();
        }

        bool IsOpen()   // [변경] 도감 창 중 하나라도 열려 있는지
        {
            if (_selectPanel.activeSelf)
            {
                return true;
            }
            if (_artifactListPanel.activeSelf)
            {
                return true;
            }
            if (_pieceListPanel.activeSelf)
            {
                return true;
            }
            if (_artifactGuide.IsShowing())
            {
                return true;
            }
            if (_waitingPieceGuide)
            {
                return true;
            }
            return false;
        }
    }
}
