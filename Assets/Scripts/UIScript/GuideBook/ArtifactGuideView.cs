using Promotion.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Promotion.UI
{
    public class ArtifactGuideView : MonoBehaviour  // 아티팩트 설명 창 - 받은 아티팩트 정보를 화면에 채우기만 함 (창 전환은 매니저 담당)
    {
        [Header("연결")]
        [SerializeField] GuideBookUIManager _manager;
        [Tooltip("아티팩트 가격과 효과 설명을 읽어 올 상점 데이터 (Assets/Item/Shop/ShopPool)")]
        [SerializeField] ShopPool _shopPool;

        [Header("표시 UI")]
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _nameText;
        [SerializeField] TextMeshProUGUI _priceText;
        [SerializeField] TextMeshProUGUI _descriptionText;

        [Header("버튼")]
        [SerializeField] Button _backButton;    // 왼쪽 위
        [SerializeField] Button _closeButton;   // 오른쪽 위

        bool _listenersAdded;   // 버튼 연결을 한 번만 하기 위한 표시

        public void Show(ItemData artifact) // 매니저가 호출 -> 아티팩트 정보를 채우고 창을 켬
        {
            AddListenersOnce();

            _icon.sprite = artifact.itemImage;
            _icon.preserveAspect = true;
            _nameText.text = artifact.itemName;

            ShopOffer offer = FindOffer(artifact);
            if (offer != null)
            {
                _priceText.text = "가격: " + offer.cost + " 골드";
                _descriptionText.text = offer.blurb;
            }
            else    // ShopPool에 등록되지 않은 아티팩트라면 -> 정보가 없다고 표시
            {
                _priceText.text = "가격: -";
                _descriptionText.text = "설명이 등록되지 않은 아티팩트입니다.";
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public bool IsShowing()
        {
            return gameObject.activeSelf;
        }

        ShopOffer FindOffer(ItemData artifact)  // ShopPool의 유물 매물 중에서 이 아티팩트의 매물을 찾음 -> 없으면 null
        {
            if (_shopPool == null)
            {
                return null;
            }

            foreach (ShopOffer offer in _shopPool.artifactOffers)
            {
                if (offer != null && offer.item == artifact)
                {
                    return offer;
                }
            }
            return null;
        }

        void AddListenersOnce() // 뒤로 가기/닫기 버튼 연결 -> 창이 꺼진 채로 시작해도 처음 Show 때 확실히 연결되도록 여기서 함
        {
            if (_listenersAdded)
            {
                return;
            }
            _backButton.onClick.AddListener(OnBackClicked);
            _closeButton.onClick.AddListener(OnCloseClicked);
            _listenersAdded = true;
        }

        void OnBackClicked()
        {
            _manager.BackFromGuide();
        }

        void OnCloseClicked()
        {
            _manager.CloseAll();
        }
    }
}
