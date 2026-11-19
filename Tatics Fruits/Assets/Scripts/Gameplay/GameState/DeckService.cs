using System;
using System.Collections.Generic;
using Core.ScriptableObjects;
using Core.Services;
using UnityEngine;

namespace New_GameplayCore.Services
{
    public class DeckService : IDeckService
    {
        private readonly List<CardInstance> _deck = new();
        private readonly List<CardInstance> _discard = new();
        private System.Random _rng = new();

        public int DeckCount => _deck.Count;
        public int DiscardCount => _discard.Count;
        public int TotalInitialCount { get; private set; }
        
        
        public event Action<int, int> OnDeckChanged;

        private void Notify() => OnDeckChanged?.Invoke(DeckCount, DiscardCount);
        public void Build(DeckConfigSo config, System.Random rng)
        {
            _deck.Clear();
            _discard.Clear();
            TotalInitialCount = 0;

            // Usa o gerador recebido: com useFixedSeed a fase sai sempre com a mesma ordem de cartas.
            if (rng != null)
                _rng = rng;

            if (config == null)
            {
                return;
            }

            if (config.entries == null || config.entries.Length == 0)
            {
                return;
            }

            foreach (var e in config.entries)
            {
                if (e.type == null)
                {
                    Debug.LogError($"[DeckService] Entrada inválida no build! {e}");
                    continue;
                }

                for (int i = 0; i < e.quantity; i++)
                {
                    string id = Guid.NewGuid().ToString();
                    _deck.Add(new CardInstance(id, e.type, e.type.baseValue));
                }

                TotalInitialCount += Mathf.Max(0, e.quantity);
            }

            Shuffle(_deck);
            Notify();
        }


        public bool TryDraw(out CardInstance card)
        {
            if (_deck.Count == 0)
            {
                card = default;
                return false;
            }

            int idx = _deck.Count - 1;
            card = _deck[idx];
            _deck.RemoveAt(idx);
            return true;
        }

        public int DrawMany(int count, IList<CardInstance> buffer)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (!TryDraw(out var c)) break;
                buffer.Add(c);
                drawn++;
            }
            return drawn;
        }

        public void Discard(CardInstance card) => _discard.Add(card);

        public void DiscardMany(IReadOnlyList<CardInstance> cards)
        {
            foreach (var c in cards)
                _discard.Add(c);
        }

        public bool TryRefillFromDiscard()
        {
            if (_discard.Count == 0) return false;
            _deck.AddRange(_discard);
            _discard.Clear();
            Shuffle(_deck);
            return true;
        }

        private void Shuffle(List<CardInstance> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = _rng.Next(i, list.Count);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}