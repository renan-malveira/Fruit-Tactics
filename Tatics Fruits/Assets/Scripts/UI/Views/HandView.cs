using System.Collections.Generic;
using Core.Services;
using MoreMountains.Feedbacks;
using New_GameplayCore;
using UnityEngine;

namespace UI.Views
{
    public class HandView : MonoBehaviour
    {
        [SerializeField] private Transform cardParent;
        [SerializeField] private CardView cardPrefab;

        private IHandService _hand;
        private IGameController _game;

        private readonly List<CardView> _spawned = new();

        public void Initialize(IHandService hand, IGameController game)
        {
            _hand = hand;
            _game = game;

            _hand.OnHandChanged += UpdateHand;
            UpdateHand(_hand.Cards);
        }

        private void OnDestroy()
        {
            if (_hand != null)
                _hand.OnHandChanged -= UpdateHand;
        }

        private void UpdateHand(IReadOnlyList<CardInstance> cards)
        {
            // Carta que o jogador está arrastando agora continua sendo a carta da mão.
            // Antes a mão era refeita no meio do arraste (outro dedo, troca, compra) e
            // ganhava uma cópia da mesma carta: ao soltar, ficavam duas iguais e uma delas
            // não jogava mais (ex.: jogar uma maçã e a outra maçã não entrar).
            var dragging = new Dictionary<string, CardView>();
            foreach (var cv in _spawned)
            {
                if (cv == null) continue;
                var drag = cv.GetComponent<CardDragHandler>();
                var id = cv.GetCardData().RollId;
                if (id != null && drag != null && drag.IsDragging && cv.transform.parent != cardParent)
                    dragging[id] = cv;
            }
            _spawned.Clear();

            // Tudo o que está na mão é refeito, inclusive sobras que ninguém rastreava.
            for (var i = cardParent.childCount - 1; i >= 0; i--)
            {
                var child = cardParent.GetChild(i);
                if (child.GetComponent<CardView>() != null)
                    Destroy(child.gameObject);
            }

            foreach (var card in cards)
            {
                if (card.RollId != null && dragging.TryGetValue(card.RollId, out var held))
                {
                    _spawned.Add(held);
                    continue;
                }

                var cv = Instantiate(cardPrefab, cardParent);
                cv.Initialize(card, _game.OnCardSelected);

                var drag = cv.GetComponent<CardDragHandler>();
                if (drag != null)
                    drag.DragEnded += HandleCardDragEnded;

                var feedback = cv.GetComponent<MMF_Player>();
                feedback?.PlayFeedbacks();
                _spawned.Add(cv);
            }
        }

        /// <summary>
        /// Carta que voltou para a mão mas não está mais nela (foi trocada, a rodada
        /// recomeçou ou era uma cópia) sai da tela em vez de ficar como carta fantasma.
        /// </summary>
        private void HandleCardDragEnded(CardDragHandler drag)
        {
            if (drag == null || drag.transform.parent != cardParent)
                return;

            var cv = drag.GetComponent<CardView>();
            if (cv == null)
                return;

            if (_spawned.Contains(cv) && IsInHand(cv.GetCardData()))
                return;

            _spawned.Remove(cv);
            Destroy(cv.gameObject);
        }

        private bool IsInHand(CardInstance card)
        {
            if (_hand == null) return false;

            foreach (var c in _hand.Cards)
                if (c.RollId == card.RollId)
                    return true;

            return false;
        }
    }
}
