using System;
using DG.Tweening;
using New_GameplayCore.Views;
using UI.Views;
using UnityEngine;
using UnityEngine.EventSystems;

namespace New_GameplayCore
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        /// <summary>Toque sem arrastar (devolve à mão a carta que espera o par).</summary>
        public event Action<CardDragHandler> Tapped;

        [Header("Refs")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform dragRoot;
        [SerializeField] private GameControllerInitializer bootstrap;

        private RectTransform _rt;
        private CanvasGroup _cg;

        private Transform _originParent;
        private int _originSibling;
        private Vector2 _originAnchoredPos;

        private bool _accepted;
        private Transform _acceptedParent;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _cg = GetComponent<CanvasGroup>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _accepted = false;
            _acceptedParent = null;

            _originParent = transform.parent;
            _originSibling = transform.GetSiblingIndex();
            _originAnchoredPos = _rt.anchoredPosition;
        
            Transform dragParent = dragRoot != null ? dragRoot : canvas.transform;
            transform.SetParent(dragParent, worldPositionStays: true);

            _cg.blocksRaycasts = false;
            _cg.alpha = 0.9f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            
            _rt.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _cg.blocksRaycasts = true;
            _cg.alpha = 1f;

            if (_accepted && _acceptedParent != null)
            {
                transform.SetParent(_acceptedParent, worldPositionStays: false);

                var rt = (RectTransform)transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.DOAnchorPos(Vector2.zero, 0.15f).SetEase(Ease.OutCubic);

                return;
            }
            
            transform.SetParent(_originParent, worldPositionStays: false);
            transform.SetSiblingIndex(_originSibling);
            _rt.anchoredPosition = _originAnchoredPos;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!enabled || eventData.dragging) return;
            Tapped?.Invoke(this);
        }

        public void AcceptDrop(Transform newParent)
        {
            _accepted = true;
            _acceptedParent = newParent;
        }
    
        public void ReturnToOrigin()
        {
            _accepted = false;
            _acceptedParent = null;
            transform.SetParent(_originParent, worldPositionStays: false);
            transform.SetSiblingIndex(_originSibling);
            _rt.anchoredPosition = _originAnchoredPos;
        }
    }
}
