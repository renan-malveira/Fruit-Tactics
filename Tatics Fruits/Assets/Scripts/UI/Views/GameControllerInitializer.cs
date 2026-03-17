using System;
using Ads;
using Core.ScriptableObjects;
using Core.Services;
using DefaultNamespace.New_GameplayCore;
using Gameplay.Controllers;
using Gameplay.GameState;
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
        
        private bool _hadInvalidPair;
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

        private void Awake()
        {
            Progress = new LevelProgressService();
            Progress.Load();
            
            _profileService = new PlayerProfileService();
            _profileService.Load();

            var currentIndex = _profileService.Data.currentLevelIndex;

            if (levelSet && levelSet.levels.Length > 0)
                levelConfig = levelSet.levels[Mathf.Clamp(currentIndex, 0, levelSet.levels.Length - 1)];
            
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
            _rule.OnInvalidPairAttempt += () => _hadInvalidPair = true;
            
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
                phaseLabel.text = $"Fase {Progress.CurrentIndex + 1}";

            _controller.StartLevel(levelConfig, deckConfig);
            hudView.Initialize(_time, _score, _swap, _combo, levelConfig, comboTierConfig);
            handView.Initialize(_hand, _controller);

            if (gameplaySettings != null)
            {
                gameplaySettings.Initialize(_time);
            }
            
            Managers.AnalyticsManager.Instance?.TrackLevelStarted(
                Progress.CurrentIndex + 1, 
                levelConfig.name
            );
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
            var totalScore = _score.Total;
            var levelId = levelConfig.levelId;
            if (string.IsNullOrEmpty(levelId))
                levelId = levelConfig.name;

            switch (cause)
            {
                case EndCause.TargetReached:
                    ShowVictory();
                    break;

                case EndCause.TimeUp:
                    ShowDefeat();
                    break;
            }
            
            InterstitialAdManager.Instance.OnMatchCompleted();
        }

        private void ShowVictory()
        {
            var presenter = new VictoryPresenter(
                levelConfig, _score, _time, _profileService, Progress, levelSet);

            VictoryModel model = default;
            presenter.OnModelReady += m => model = m;
            presenter.Build();

            var currentLevel = Progress.CurrentIndex;
            var totalScore   = _score.Total;
            var completed    = Progress.CanAdvance(levelConfig, totalScore, 0.75f);
            _profileService.SetLevel(currentLevel, completed);

            var levelId      = GetLevelId();
            var firebaseUid  = Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser?.UserId ?? string.Empty;
            var playerName   = _profileService.Data?.playerName ?? string.Empty;
            var previousBest = _profileService.GetBestScore(levelId);
            var lbRepository = new Services.FirebaseLeaderboardRepository();
            var lbPresenter  = new Services.LeaderboardPresenter(lbRepository);
            lbPresenter.SubmitIfNewBest(firebaseUid, playerName, totalScore, previousBest);

            var rewardGold = levelConfig.GetGoldReward(model.starsEarned);
            model.goldEarned = rewardGold;
            
            _profileService.AddGold(rewardGold);

            Progress.MarkNextUnlockedIfEligible(levelConfig, _score.Total, 0.75f);
            
            DailyMissionsController.Instance?.ReportWinLevel(Progress.CurrentIndex + 1);
            DailyMissionsController.Instance?.ReportScore(_score.Total);
            DailyMissionsController.Instance?.ReportWinWithoutMistakes(!_hadInvalidPair);
            _hadInvalidPair = false;

            var view = Instantiate(victoryPrefab, uiRoot);
            view.Bind(presenter, model);

            presenter.OnReplay += () =>
            {
                Progress.Replay();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            };
            presenter.OnNext += () =>
            {
                var currentIndex = Progress.CurrentIndex;
                var totalLevels  = levelSet.levels.Length;

                if (currentIndex >= totalLevels - 1)
                    ShowAllLevelsCompleted();
                else
                {
                    Progress.Advance(levelSet);
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            };
        }


        private void ShowAllLevelsCompleted()
        {
            var totalStars      = Progress.TotalStars(levelSet);
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
                    Progress.Reset();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                },
                onMenuClick: () =>
                {
                    SceneManager.LoadScene("MainMenu");
                },
                onLeaderboard: () =>
                {
                    SceneManager.LoadScene("MainMenu");
                });
        }


        private void ShowDefeat()
        {
            var presenter = new DefeatPresenter(levelConfig, _score, _time, _profileService);

            DefeatModel model = default;
            presenter.OnModelReady += m => model = m;
            presenter.Build();
            
            Progress.MarkNextUnlockedIfEligible(levelConfig, _score.Total, 0.75f);

            var view = Instantiate(defeatPrefab, uiRoot);
            view.Bind(presenter, model);

            presenter.OnReplay += () =>
            {
                Progress.Replay();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            };
            presenter.OnMenu += () =>
            {
                SceneManager.LoadScene("MainMenu");
            };
            
            var timeSpent = levelConfig.initialTimeSeconds - _time.TimeLeftSeconds;
            Managers.AnalyticsManager.Instance?.TrackLevelFailed(
                Progress.CurrentIndex + 1,
                _score.Total,
                timeSpent,
                "time_up"
            );
        }

        private void HandleEnterPreRound()
        {
            _preRoundPresenter = new PreRoundPresenter(
                _controller as GameController,
                levelConfig,
                _deck,
                _profileService);

            var model = _preRoundPresenter.BuildModel(levelConfig, _deck, _profileService);

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
    
        private string GetLevelId()
            => string.IsNullOrEmpty(levelConfig.levelId) ? levelConfig.name : levelConfig.levelId;
    }
}
