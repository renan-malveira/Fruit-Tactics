using Core.ScriptableObjects;
using Gameplay.Controllers;
using New_GameplayCore;
using New_GameplayCore.Services;
using UnityEngine;

namespace Core.Services
{
    public class PreRoundPresenter : IPreRoundPresenter
    {
        private readonly GameController _controller;
        private readonly LevelConfigSo _cfg;
        private readonly IDeckService _deck;
        private readonly PlayerProfileService _profileService;
        private PreRoundModel _model;

        public System.Action<PreRoundModel> OnModelReady;
        public System.Action OnRequestClose;

        public PreRoundPresenter(GameController controller, LevelConfigSo cfg, IDeckService deck,
            PlayerProfileService profileService)
        {
            _controller     = controller;
            _cfg            = cfg;
            _deck           = deck;
            _profileService = profileService;
        }

        public PreRoundModel BuildModel(LevelConfigSo cfg, IDeckService deck, PlayerProfileService profileService)
        {
            var total = deck.TotalInitialCount;
            var list = new System.Collections.Generic.List<DeckEntrySummary>();

            if (cfg.deck != null && cfg.deck.entries != null)
            {
                foreach (var e in cfg.deck.entries)
                    list.Add(new DeckEntrySummary { type = e.type, quantity = e.quantity });
            }

            // Estrelas da v2.2: 1 = meta, 2 = meta dentro do tempo-alvo, 3 = isso sem errar par.
            var levelId = cfg.LevelKey;
            var s1 = cfg.targetScore;
            var s2 = cfg.parTimeSeconds > 0 ? cfg.parTimeSeconds : StarRules.FallbackTimeLeftSeconds(cfg);
            var s3 = 0;

            var best = profileService.GetBestScore(levelId);

            _model = new PreRoundModel()
            {
                levelId = levelId,
                displayName = string.IsNullOrEmpty(cfg.displayName) ? cfg.name : cfg.displayName,
                description = cfg.description,

                targetScore = cfg.targetScore,
                star1Score = s1,
                star2Score = s2,
                star3Score = s3,

                initialTimeSec = cfg.initialTimeSeconds,
                handSize = cfg.handSize,
                timeBonusOnPair = cfg.timeBonusOnPair,
                swapAllPenalty = cfg.swapAllTimePenalty,
                swapRandomPenalty = cfg.swapRandomTimePenalty,
                allowRefillFromDiscard = cfg.allowEmptyDeckRefill,

                deckTotalCount = total,
                composition = list.ToArray(),
                
                bestScore = best,
                
                useFixedSeed = cfg.useFixedSeed,
                effectiveSeed = cfg.useFixedSeed ? cfg.fixedSeed : Random.Range(int.MinValue, int.MaxValue),

                parTimeSeconds = cfg.parTimeSeconds,
                bestStars = profileService.GetLevelStars(levelId),
            };
            
            OnModelReady?.Invoke(_model);
            return _model;
        }

        public void OnStartClicked()
        {
            _controller.BeginPlayFromPreRound(_model);
            OnRequestClose?.Invoke();
        }

        public void OnBackClicked()
        {
            _controller.BackToLevelSelect();
            OnRequestClose?.Invoke();
        }

        public void OnToggleUseFixedSeed(bool value)
        {
            _model.useFixedSeed = value;
            _model.effectiveSeed = value ? _cfg.fixedSeed : Random.Range(int.MinValue, int.MaxValue);
            OnModelReady?.Invoke(_model);
        }
    }
}
