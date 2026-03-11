using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

namespace Managers
{
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }
        
        private bool _isInitialized = false;

        private void Awake()
        {
            if(Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await InitializeUnityServices();
        }

        private async Task InitializeUnityServices()
        {
            try
            {
                await UnityServices.InitializeAsync();
                GiveConsent();
                _isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Analytics] Failed to initialize Unity Services: {e.Message}");
            }
        }

        private void GiveConsent()
        {
            AnalyticsService.Instance.StartDataCollection();
        }

        public void TrackLevelStarted(int levelNumber, string levelName = "")
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("level_started")
            {
                { "level_number", levelNumber },
                { "level_name", string.IsNullOrEmpty(levelName) ? $"Level_{levelNumber}" : levelName }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackLevelCompleted(int levelNumber, int score, int stars, float timeSpent, bool victory)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("level_completed")
            {
                { "level_number", levelNumber },
                { "score", score },
                { "stars", stars },
                { "time_spent", timeSpent },
                { "victory", victory }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackLevelFailed(int levelNumber, int score, float timeSpent, string failReason)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("level_failed")
            {
                { "level_number", levelNumber },
                { "score", score },
                { "time_spent", timeSpent },
                { "fail_reason", failReason }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackDailyMissionCompleted(string missionId, string missionType, int rewardAmount)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("daily_mission_completed")
            {
                { "mission_id", missionId },
                { "mission_type", missionType },
                { "reward_amount", rewardAmount }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackDailyLoginClaimed(int dayNumber, int rewardAmount)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("daily_login_claimed")
            {
                { "day_number", dayNumber },
                { "reward_amount", rewardAmount }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackStoreOpened()
        {
            if (!_isInitialized) return;
            
            AnalyticsService.Instance.RecordEvent(new CustomEvent("store_opened"));
        }

        public void TrackItemPurchased(string itemId, string itemType, int price, string currency = "gold")
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("item_purchased")
            {
                { "item_id", itemId },
                { "item_type", itemType },
                { "price", price },
                { "currency", currency }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackCurrencyEarned(int amount, string source, string currency = "gold")
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("currency_earned")
            {
                { "amount", amount },
                { "source", source },
                { "currency", currency }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackCurrencySpent(int amount, string reason, string currency = "gold")
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("currency_spent")
            {
                { "amount", amount },
                { "reason", reason },
                { "currency", currency }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackPowerUpUsed(string powerUpType, int levelNumber)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("powerup_used")
            {
                { "powerup_type", powerUpType },
                { "level_number", levelNumber }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackAdWatched(string adType, string placement, bool completed)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("ad_watched")
            {
                { "ad_type", adType },
                { "placement", placement },
                { "completed", completed }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackSettingsChanged(string settingName, object settingValue)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("settings_changed")
            {
                { "setting_name", settingName },
                { "setting_value", settingValue.ToString() }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackLanguageChanged(string oldLanguage, string newLanguage)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("language_changed")
            {
                { "old_language", oldLanguage },
                { "new_language", newLanguage }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackMenuOpened(string menuName)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("menu_opened")
            {
                { "menu_name", menuName }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackTutorialStep(int stepNumber, string stepName, bool completed)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("tutorial_step")
            {
                { "step_number", stepNumber },
                { "step_name", stepName },
                { "completed", completed }
            };
            
            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackCombo(int comboCount, int scoreEarned)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("combo_performed")
            {
                { "combo_count", comboCount },
                { "score_earned", scoreEarned }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackSceneTransition(string fromScene, string toScene)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("scene_transition")
            {
                { "from_scene", fromScene },
                { "to_scene", toScene }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackButtonClicked(string buttonName)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("button_clicked")
            {
                { "button_name", buttonName }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackAdStarted(string adType, string placement)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("ad_started")
            {
                { "ad_type", adType },
                { "placement", placement }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        public void TrackAdCompleted(string adType, bool completed)
        {
            if (!_isInitialized) return;

            var customEvent = new CustomEvent("ad_completed")
            {
                { "ad_type", adType },
                { "completed", completed }
            };

            AnalyticsService.Instance.RecordEvent(customEvent);
        }

        private void OnApplicationQuit()
        {
            if (_isInitialized)
            {
                try
                {
                    AnalyticsService.Instance.Flush();
                    Debug.Log("[Analytics] Flushed analytics data on quit");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Analytics] Failed to flush on quit: {e.Message}");
                }
            }
        }
    }
}
