using System;
using Ads;
using Core.ScriptableObjects;
using Core.Services;
using DefaultNamespace.New_GameplayCore;
using Gameplay.Controllers;
using Gameplay.GameState;
using Gameplay.Utils;
using Managers;
using New_GameplayCore;
using New_GameplayCore.GameState;
using New_GameplayCore.Services;
using New_GameplayCore.Views;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Views
{
    public class GameControllerInitializer : MonoBehaviour
    {
        [SerializeField] private LevelConfigSo levelConfig;
        [SerializeField] private DeckConfigSo deckConfig;
        [SerializeField] private HUDView hudView;
        [SerializeField] private HandView handView;
        [SerializeField] private PreRoundView preRoundView;
        [SerializeField] private Transform uiRoot;
        [SerializeField] private VictoryView victoryPrefab;
        [SerializeField] private DefeatView defeatPrefab;
        [SerializeField] private LevelSetSO levelSet;
        [SerializeField] private TextMeshProUGUI phaseLabel;
        [SerializeField] private PlayerProfileService _profileService;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private GameplaySettings gameplaySettings;
        [SerializeField] private CountdownView countdownView;
        [SerializeField] private AllLevelsCompletedView allLevelsCompletedView;
        [SerializeField] private ComboTierConfigSo comboTierConfig;
        [SerializeField] private PairMatchFeedback pairMatchFeedback;

        public IRuleEngine RuleEngine => _rule;
        public IGameController Controller => _controller;
        public bool IsReady { get; private set; }
        public event Action OnReady;
        public event Action<EndCause> OnLevelEnd;

        public IHandService Hand => _hand;
        public IDeckService Deck => _deck;
        public LevelConfigSo LevelConfig => levelConfig;
        public IScoreService Score => _score;
        public LevelProgressService Progress;
        public PlayerProfileService Profile => _profileService;
        public GameplaySettings PauseMenu => gameplaySettings;

        private GameStateMachine _fsm;
        private TimeManager _time;
        private ScoreService _score;
        private ComboTracker _combo;
        private DeckService _deck;
        private HandService _hand;
        private SwapService _swap;
        private RuleEngine _rule;
        private IGameController _controller;
        private PreRoundPresenter _preRoundPresenter;
        private PreRoundView _preRoundInstance;
        private bool _leavingScene;

        private int LevelNumber => Progress != null ? Progress.CurrentIndex + 1 : 1;

        private void Awake()
        {
            _profileService = new PlayerProfileService();
            _profileService.Load();

            // O progresso das fases mora no perfil (que vai para a nuvem).
            Progress = new LevelProgressService(_profileService);
            Progress.Load();

            var cfg = Progress.Current(levelSet);
            if (cfg != null)
                levelConfig = cfg;

            if (comboTierConfig == null)
            {
                comboTierConfig = Resources.Load<ComboTierConfigSo>("DefaultComboTierConfig");
            }

            _fsm   = new GameStateMachine();
            _time  = new TimeManager(levelConfig.initialTimeSeconds);
            _score = new ScoreService(levelConfig);
            _combo = new ComboTracker(levelConfig, comboTierConfig);
            _deck  = new DeckService();
            _hand  = new HandService(levelConfig.handSize);
            _swap  = new SwapService(_hand, _deck, _time, levelConfig);
            _rule  = new RuleEngine(_hand, _deck, _score, _time, _combo, levelConfig);
            _controller = new GameController(
                _fsm, _time, _deck, _hand, _rule, _swap, _combo, levelConfig, _score);

            _controller.OnEnterPreRound += HandleEnterPreRound;
            _controller.OnLevelEnded += HandleLevelEnded;
            _controller.OnPairResolved += HandlePairResolved;

            if (pairMatchFeedback != null)
                _controller.OnPairResolved += result => pairMatchFeedback.PlayMatchFeedback(result.ComboCountAfter);

            IsReady = true;
            OnReady?.Invoke();
        }

        private void Start()
        {
            if (Progress == null)
                return;

            if (tutorialManager == null)
            {
                StartGameplay();
                return;
            }

            DailyMissionsController.Instance?.ReportSessionStarted();

            var levelIndexForTutorial = Progress.CurrentIndex + 1;

            var shown = tutorialManager.TryShowTutorial(levelIndexForTutorial);

            if (!shown)
            {
                StartGameplay();
            }
            else
            {
                tutorialManager.OnTutorialFinished += HandleTutorialFinished;
            }
        }

        private void StartGameplay()
        {
            if (phaseLabel)
                phaseLabel.text = Localizer.Instance.TrFormat("level_title", "Fase {0}", LevelNumber);

            _controller.StartLevel(levelConfig, deckConfig);
            hudView.Initialize(_time, _score, _swap, _combo, levelConfig, comboTierConfig);
            handView.Initialize(_hand, _controller);

            if (gameplaySettings != null)
            {
                gameplaySettings.Initialize(_time);
            }

            Managers.AnalyticsManager.Instance?.TrackLevelStarted(LevelNumber, levelConfig.name);
        }

        private void HandleTutorialFinished()
        {
            tutorialManager.OnTutorialFinished -= HandleTutorialFinished;
            StartGameplay();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnEnterPreRound -= HandleEnterPreRound;
                _controller.OnLevelEnded -= HandleLevelEnded;
                _controller.OnPairResolved -= HandlePairResolved;
            }

            if (tutorialManager != null)
            {
                tutorialManager.OnTutorialFinished -= HandleTutorialFinished;
            }
        }

        private void HandlePairResolved(PairResult result)
        {
            DailyMissionsController.Instance?.ReportPairMade();
            DailyMissionsController.Instance?.ReportComboReached(result.ComboCountAfter);
        }

        private void HandleLevelEnded(EndCause cause)
        {
            // Conta a partida para o ritmo de anúncios; o anúncio em si só aparece
            // quando o jogador sai da tela de resultado (Próxima, Jogar de novo ou Menu).
            InterstitialAdManager.Instance?.RegisterMatchCompleted(LevelNumber);

            // Barra cheia termina a fase na hora (3 estrelas). Quando acaba o tempo ou as cartas,
            // vence quem chegou a pelo menos 1 estrela (1/3 da barra).
            var won = cause == EndCause.TargetReached || StarRules.Evaluate(levelConfig, _score.Total) >= 1;

            if (won)
                ShowVictory();
            else
                ShowDefeat(outOfCards: cause == EndCause.OutOfCards);

            OnLevelEnd?.Invoke(cause);
        }

        private void ShowVictory()
        {
            var levelIndex = Progress.CurrentIndex;
            var levelNumber = levelIndex + 1;
            var totalScore = _score.Total;
            var mistakes = _rule.Mistakes;
            var totalBestBefore = TotalBestScore();

            var presenter = new VictoryPresenter(
                levelConfig, _score, _time, _profileService, Progress, levelSet, mistakes);

            VictoryModel model = default;
            presenter.OnModelReady += m => model = m;
            presenter.Build();

            // Libera a próxima fase e já deixa ela como atual (Menu > Jogar continua dela).
            Progress.CompleteLevel(levelIndex, levelSet);

            SubmitLeaderboardIfImproved(totalBestBefore);

            var rewardGold = levelConfig.GetGoldReward(model.starsEarned);
            model.goldEarned = rewardGold;
            _profileService.AddGold(rewardGold);

            DailyMissionsController.Instance?.ReportWinLevel(levelNumber);
            DailyMissionsController.Instance?.ReportScore(totalScore);
            DailyMissionsController.Instance?.ReportWinWithoutMistakes(mistakes == 0);

            Managers.AnalyticsManager.Instance?.TrackLevelCompleted(
                levelNumber, totalScore, model.starsEarned, _time.ElapsedSeconds, true);

            var view = Instantiate(victoryPrefab, uiRoot);
            view.Bind(presenter, model);

            presenter.OnReplay += () =>
            {
                Progress.SetCurrentIndex(levelIndex);
                LeaveTo(SceneManager.GetActiveScene().buildIndex);
            };
            presenter.OnNext += () =>
            {
                if (levelIndex >= levelSet.levels.Length - 1)
                    ShowAllLevelsCompleted();
                else
                    LeaveTo(SceneManager.GetActiveScene().buildIndex);
            };
            presenter.OnMenu += () => LeaveTo("MainMenu");
        }

        private void ShowAllLevelsCompleted()
        {
            var totalStars       = Progress.TotalStars(levelSet);
            var maxStarsPossible = levelSet.levels.Length * 3;

            var model = new AllLevelsCompletedModel
            {
                TotalLevels      = levelSet.levels.Length,
                TotalStars       = totalStars,
                MaxStarsPossible = maxStarsPossible,
                FinalScore       = _score.Total
            };

            Managers.AnalyticsManager.Instance?.TrackButtonClicked(
                $"all_levels_completed_stars_{totalStars}of{maxStarsPossible}");

            var view = Instantiate(allLevelsCompletedView, uiRoot);
            view.Initialize(
                model,
                onPlayAgain: () =>
                {
                    // Volta para a fase 1 mantendo estrelas, recordes e fases liberadas.
                    Progress.Reset();
                    LeaveTo(SceneManager.GetActiveScene().buildIndex);
                },
                onMenuClick: () => LeaveTo("MainMenu"),
                onLeaderboard: () => LeaveTo("MainMenu"));
        }

        private void ShowDefeat(bool outOfCards)
        {
            var presenter = new DefeatPresenter(levelConfig, _score, _time, _profileService);

            DefeatModel model = default;
            presenter.OnModelReady += m => model = m;
            presenter.Build();
            model.outOfCards = outOfCards;

            var view = Instantiate(defeatPrefab, uiRoot);
            view.Bind(presenter, model);

            presenter.OnReplay += () =>
            {
                Progress.Replay();
                LeaveTo(SceneManager.GetActiveScene().buildIndex);
            };
            presenter.OnMenu += () => LeaveTo("MainMenu");

            Managers.AnalyticsManager.Instance?.TrackLevelFailed(
                LevelNumber,
                _score.Total,
                _time.ElapsedSeconds,
                outOfCards ? "out_of_cards" : "time_up"
            );
        }

        /// <summary>Mostra o intersticial (se for a hora) e só depois troca de cena.</summary>
        private void LeaveTo(int buildIndex) => LeaveTo(() => SceneManager.LoadScene(buildIndex));

        private void LeaveTo(string sceneName) => LeaveTo(() => SceneManager.LoadScene(sceneName));

        private void LeaveTo(Action load)
        {
            if (_leavingScene) return;
            _leavingScene = true;

            var ads = InterstitialAdManager.Instance;
            if (ads != null)
                ads.ShowIfDueThen(load);
            else
                load();
        }

        /// <summary>
        /// O ranking usa a soma dos recordes de todas as fases (comparável entre jogadores).
        /// Antes cada vitória enviava a pontuação da fase, e o "recorde anterior" era lido depois
        /// de gravado, então nada chegava ao ranking.
        /// </summary>
        private void SubmitLeaderboardIfImproved(int totalBestBefore)
        {
            var totalBestAfter = TotalBestScore();
            if (totalBestAfter <= totalBestBefore)
                return;

            var firebaseUid = Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(firebaseUid))
                return;

            try
            {
                var playerName  = _profileService.Data?.playerName ?? string.Empty;
                var lbPresenter = new Services.LeaderboardPresenter(new Services.FirebaseLeaderboardRepository());
                lbPresenter.SubmitIfNewBest(firebaseUid, playerName, totalBestAfter, totalBestBefore);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameControllerInitializer] Ranking não enviado: {e.Message}");
            }
        }

        private int TotalBestScore()
        {
            if (levelSet?.levels == null) return 0;

            var total = 0;
            foreach (var level in levelSet.levels)
                if (level != null)
                    total += _profileService.GetBestScore(level.LevelKey);
            return total;
        }

        private void HandleEnterPreRound()
        {
            _preRoundPresenter = new PreRoundPresenter(
                _controller as GameController,
                levelConfig,
                _deck,
                _profileService);

            var model = _preRoundPresenter.BuildModel(levelConfig, _deck, _profileService);
            model.levelNumber = LevelNumber;

            _preRoundInstance = Instantiate(preRoundView, uiRoot);
            _preRoundInstance.Bind(_preRoundPresenter, model);

            if (countdownView != null)
                _preRoundInstance.SetupCountdown(countdownView);
        }

        private void Update()
        {
            if (_controller != null)
            {
                _controller.UpdateTick(Time.deltaTime);
            }
        }
    }
}
