using System.Collections.Generic;
using Core.Services;
using DG.Tweening;
using MoreMountains.Feedbacks;
using New_GameplayCore;
using UI.Views;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay
{
    public class PlayAreaDropZone : MonoBehaviour, IDropHandler
    {
        [Header("Slots")]
        [SerializeField] private Transform dropContent;
        [SerializeField] private Transform matchedPile;

        [Header("Pile layout")]
        [SerializeField] private Vector2 pileStep = new Vector2(12f, -8f);
        [SerializeField] private float pileFanAngle = 8f;
        [SerializeField] private float pileScale = 0.9f;
        [SerializeField] private float pileAnimDuration = 0.2f;

        [Header("Feel / Match VFX")]
        [SerializeField] private MMF_Player matchFeedback;
        [SerializeField] private float matchFeedbackIntensity = 1f;

        [Header("Feel / Par errado")]
        [Tooltip("Opcional: som ou efeito quando o jogador junta duas frutas diferentes.")]
        [SerializeField] private MMF_Player mismatchFeedback;
        [SerializeField] private float mismatchPunchAngle = 14f;
        [SerializeField] private float mismatchPunchDuration = 0.35f;

        [Header("Refs")]
        [SerializeField] private GameControllerInitializer bootstrap;

        private IRuleEngine _rules;
        private IGameController _controller;
        private IHandService _hand;

        private readonly List<(CardView view, CardDragHandler drag, CardInstance data)> _staged = new(2);
        private int _pileCount = 0;

        void Start()
        {
            _rules = bootstrap.RuleEngine;
            _controller = bootstrap.Controller;
            _hand = bootstrap.Hand;

            if (!dropContent) dropContent = transform;
            if (!matchedPile) matchedPile = transform;
        }

        private void OnDestroy()
        {
            foreach (var staged in _staged)
                if (staged.drag != null)
                    staged.drag.Tapped -= HandleStagedCardTapped;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var go = eventData.pointerDrag;
            if (go == null) return;

            var drag = go.GetComponent<CardDragHandler>();
            var view = go.GetComponent<CardView>();
            if (drag == null || view == null) return;

            if (_controller == null || !_controller.IsPlaying)
            {
                drag.ReturnToOrigin();
                return;
            }

            var data = view.GetCardData();

            if (_staged.Count >= 2)
            {
                drag.ReturnToOrigin();
                return;
            }

            if (_staged.Count == 0)
            {
                if (!_hand.TryRemove(data))
                {
                    drag.ReturnToOrigin();
                    return;
                }

                drag.AcceptDrop(dropContent);
                PlayDropFlip(view.transform);
                _staged.Add((view, drag, data));
                drag.Tapped += HandleStagedCardTapped;
                return;
            }

            var first = _staged[0];

            // A própria carta que já espera o par: só volta para onde estava.
            if (first.data.RollId == data.RollId)
            {
                drag.ReturnToOrigin();
                return;
            }

            var isPair = _rules.IsValidPair(first.data, data);

            if (!isPair)
            {
                drag.ReturnToOrigin();
                _controller.ReportInvalidPair();
                PlayMismatch(first.view, view);
                return;
            }

            if (matchFeedback)
            {
                var a = first.view.transform.position;
                var b = view.transform.position;
                var mid = (a + b) * 0.5f;

                matchFeedback.PlayFeedbacks(mid, Random.Range(0.9f, 1.1f) * matchFeedbackIntensity);
            }

            drag.AcceptDrop(dropContent);
            PlayDropFlip(view.transform);
            PlayScaleUp(view.transform);

            first.drag.Tapped -= HandleStagedCardTapped;
            _staged.Clear();

            _controller.TryResolvePair(first.data, data);

            MoveToPile(first.view);
            MoveToPile(view);
        }

        /// <summary>
        /// Tocar na carta que espera o par devolve ela para a mão.
        /// Antes ela ficava presa na área de jogo até aparecer a fruta igual.
        /// </summary>
        private void HandleStagedCardTapped(CardDragHandler drag)
        {
            if (_staged.Count != 1 || _staged[0].drag != drag)
                return;

            var staged = _staged[0];
            if (_controller == null || !_controller.IsPlaying)
                return;

            if (!_controller.ReturnCardToHand(staged.data))
                return;

            drag.Tapped -= HandleStagedCardTapped;
            _staged.Clear();

            if (staged.view != null)
            {
                staged.view.transform.DOKill();
                Destroy(staged.view.gameObject);
            }
        }

        private void PlayMismatch(CardView staged, CardView dropped)
        {
            mismatchFeedback?.PlayFeedbacks();
            PunchCard(staged ? staged.transform : null);
            PunchCard(dropped ? dropped.transform : null);
        }

        private void PunchCard(Transform card)
        {
            if (!card) return;

            // Só rotação: a posição das cartas da mão é controlada pelo layout.
            card.DOComplete();
            card.DOPunchRotation(new Vector3(0f, 0f, mismatchPunchAngle), mismatchPunchDuration, 12, 0.6f);
        }

        private void PlayDropFlip(Transform t)
        {
            t.DOKill();
            t.localRotation = Quaternion.identity;
            t.DOLocalRotate(new Vector3(0f, 0f, 45f), 0.30f);
        }

        private void PlayScaleUp(Transform card)
        {
            if (!card) return;

            card.DOKill();
            card.DOScale(1.3f, 0.25f).SetEase(Ease.OutElastic);
        }

        private void MoveToPile(CardView view)
        {
            if (!view) return;

            var drag = view.GetComponent<CardDragHandler>();
            if (drag) drag.enabled = false;

            var cg = view.GetComponent<CanvasGroup>();
            if (!cg) cg = view.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            view.transform.SetParent(matchedPile, worldPositionStays: false);

            var targetPos = new Vector3(pileStep.x * _pileCount, pileStep.y * _pileCount, 0f);
            var rotZ = Random.Range(-pileFanAngle, pileFanAngle);

            view.transform.DOKill();
            view.transform.DOLocalMove(targetPos, pileAnimDuration).SetEase(Ease.OutQuad);
            view.transform.DOLocalRotate(new Vector3(0, 0, rotZ), pileAnimDuration).SetEase(Ease.OutQuad);
            view.transform.DOScale(pileScale, 0.25f).SetEase(Ease.OutQuad);


            view.transform.SetAsLastSibling();
            _pileCount++;
        }
    }
}
