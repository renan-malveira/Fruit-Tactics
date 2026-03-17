using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core.SaveSystem;
using Firebase.Database;
using Gameplay.Controllers;
using Gameplay.Utils;
using UnityEngine;

namespace Core.Services
{
    [Serializable]
    public class DataToSave
    {
        public string userName;
        public string userId;
        public int totalCoins;
        public int crrLevel;
        public int highScore;
        public long lastUpdatedTicks;
        public bool isVip;
        public bool tutorialCompleted;
        public long vipExpirationTicks;
        public List<string> ownedCards = new List<string>();
        public List<string> equippedDeck = new List<string>();
        public List<int> unlockedAvatar =  new List<int>{0};
        public List<int> purchasedAvatar = new List<int>();
        public Dictionary<string, int> bestScores =  new Dictionary<string, int>();

        public bool musicOn = true;
        public bool sfxOn = true;
        public bool vfxOn = true;
        public string language = "pt_BR";
        public string dailyDayKey;
        public string lastLoginDayKey;
    }
    public class FirebaseProfileSyncService : MonoBehaviour
    {
        [Header("Player Data")]
        [SerializeField] private string userId;
        [SerializeField] public DataToSave dataToSave = new DataToSave();

        [Header("References")]
        [SerializeField] private PlayerProfileController _profileController;

        private DatabaseReference _databaseReference;
        private string _localFilePath;

        public event Action<DataToSave> OnDataLoaded;
        public event Action OnDataNotFound;
        public event Action<Exception> OnLoadFailed;

        private void Awake()
        {
            _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
            _localFilePath = Path.Combine(Application.persistentDataPath, "playerData.json");
        }

        public void SetProfileController(PlayerProfileController controller)
        {
            _profileController = controller;
        }

        /// <summary>
        /// Salva dados no local (JSON) e tenta salvar no Firebase.
        /// </summary>
        public void SaveData()
        {
            if (!ValidateUserId()) return;

            if (dataToSave == null)
                dataToSave = new DataToSave();
            
            dataToSave.lastUpdatedTicks = DateTime.UtcNow.Ticks;
            
            SaveLocal();
            
            var json = JsonUtility.ToJson(dataToSave);
            _databaseReference
                .Child("users")
                .Child(userId)
                .SetRawJsonValueAsync(json)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                        Debug.LogError($"[DataSaver] Erro ao salvar dados no Firebase: {task.Exception}");
                });
        }

        public void SetUserId(string uid)
        {
            userId = uid;

            if (_profileController != null)
                _profileController.SetFirebaseUserId(uid);
        }

        /// <summary>
        /// Carrega dados do Firebase + Local, resolve conflito e sincroniza.
        /// </summary>
        public void LoadData()
        {
            if (!ValidateUserId()) return;
            StartCoroutine(LoadDataCoroutine());
        }

        private IEnumerator LoadDataCoroutine()
        {
            var serverDataTask = _databaseReference.Child("users").Child(userId).GetValueAsync();
            var localData = LoadLocal();

            yield return new WaitUntil(() => serverDataTask.IsCompleted);

            DataToSave cloudData = null;

            if (serverDataTask.IsFaulted)
            {
                Debug.LogError($"[FirebaseProfileSync] Error loading from Firebase: {serverDataTask.Exception}");
                OnLoadFailed?.Invoke(serverDataTask.Exception);
            }
            else
            {
                var snapshot = serverDataTask.Result;

                if (snapshot != null && snapshot.Exists)
                {
                    var jsonData = snapshot.GetRawJsonValue();
                    if (!string.IsNullOrEmpty(jsonData))
                        cloudData = JsonUtility.FromJson<DataToSave>(jsonData);
                }
            }

            dataToSave = ResolveDataConflict(localData, cloudData);

            if (dataToSave != null)
            {
                if (dataToSave.lastUpdatedTicks == 0)
                    dataToSave.lastUpdatedTicks = DateTime.UtcNow.Ticks;

                SaveLocal();
                _databaseReference.Child("users").Child(userId).SetRawJsonValueAsync(JsonUtility.ToJson(dataToSave));

                OnDataLoaded?.Invoke(dataToSave);

                ApplyCloudDataToProfile(dataToSave);
            }
        }

        private void ApplyCloudDataToProfile(DataToSave data)
        {
            if (_profileController == null || _profileController.Data == null)
                return;
            
            bool cloudIsNewer = data.lastUpdatedTicks > _profileController.Data.lastUpdatedTicks;

            _profileController.Data.playerName = data.userName;

            if (cloudIsNewer)
            {
                _profileController.Data.gold = data.totalCoins;
                _profileController.Data.currentLevelIndex = data.crrLevel;
                _profileController.Data.highestLevelUnlocked = data.highScore;

                if (data.ownedCards != null)
                    _profileController.Data.ownedCards = new List<string>(data.ownedCards);

                if (data.equippedDeck != null)
                    _profileController.Data.equippedDeck = new List<string>(data.equippedDeck);

                if (data.unlockedAvatar != null)
                    _profileController.Data.unlockedAvatars = new List<int>(data.unlockedAvatar);

                if (data.purchasedAvatar != null)
                    _profileController.Data.purchasedAvatars = new List<int>(data.purchasedAvatar);

                if (data.bestScores != null)
                    _profileController.Data.BestScores = new Dictionary<string, int>(data.bestScores);

                _profileController.Data.musicOn = data.musicOn;
                _profileController.Data.sfxOn = data.sfxOn;
                _profileController.Data.vfxOn = data.vfxOn;
                _profileController.Data.language = data.language;

                if (_profileController.Data.daily != null)
                {
                    _profileController.Data.daily.dayKey = data.dailyDayKey ?? "";
                    if (_profileController.Data.daily.login != null)
                        _profileController.Data.daily.login.lastClaimDayKey = data.lastLoginDayKey ?? "";
                }
            }

            _profileController.Data.firebaseUserId = userId;
            _profileController.SetFirebaseUserId(userId);
            _profileController.SaveProfile();
        }

        #region Local JSON

        private void SaveLocal()
        {
            try
            {
                var json = JsonUtility.ToJson(dataToSave);
                File.WriteAllText(_localFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseProfileSync] Failed to write local save file: {e}");
            }
        }

        private DataToSave LoadLocal()
        {
            try
            {
                if (!File.Exists(_localFilePath))
                    return null;

                var json = File.ReadAllText(_localFilePath);
                if (string.IsNullOrEmpty(json))
                    return null;

                return JsonUtility.FromJson<DataToSave>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseProfileSync] Failed to read local save file: {e}");
                return null;
            }
        }

        #endregion

        #region Helpers

        private DataToSave ResolveDataConflict(DataToSave local, DataToSave cloud)
        {
            if (cloud != null)
                return cloud;

            return local;
        }

        private DataToSave CreateNewDefaultData()
        {
            return new DataToSave()
            {
                userName = "Guest",
                totalCoins = 0,
                crrLevel = 1,
                highScore = 0,
                lastUpdatedTicks = DateTime.Now.Ticks,
                isVip = false,
                vipExpirationTicks = 0,
                ownedCards = new List<string>(),
                equippedDeck = new List<string>(),
                unlockedAvatar = new List<int> { 0 },
                purchasedAvatar = new List<int>(),
                bestScores = new Dictionary<string, int>(),
                musicOn = true,
                sfxOn = true,
                vfxOn = true,
                language = "pt_BR",
                dailyDayKey = "",
                lastLoginDayKey = ""
            };
        }

        private bool ValidateUserId()
        {
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            return true;
        }

        #endregion
        
        private PlayerProfileData _data;

        public PlayerProfileData Data => _data;

        public void Load()
        {
            _data = SaveManager.Instance.Load<PlayerProfileData>();
        }
        
        public void Save()
        {
            SaveManager.Instance.Save(_data);
        }

        public void MarkDirty()
        {
            SaveManager.Instance.MarkDirty<PlayerProfileData>();
        }

        public void AddGold(int amount)
        {
            _data.gold += amount;
            if (_data.gold < 0)
                _data.gold = 0;
            Save();
        }

        public void SetLevel(int index, bool completed = false)
        {
            _data.currentLevelIndex = index;
            if (completed && index >= _data.highestLevelUnlocked)
                _data.highestLevelUnlocked = index + 1;
            Save();
        }

        public void SetSettings(bool music, bool sfx, bool vfx, string lang)
        {
            _data.musicOn = music;
            _data.sfxOn = sfx;
            _data.vfxOn = vfx;
            _data.language = lang;
            Save();
        }

        public void UpdateDailyLogin(string todayKey)
        {
            if (_data.daily.login.lastClaimDayKey != todayKey)
            {
                _data.daily.login.lastClaimDayKey = todayKey;
                Save();
            }
        }

        public void UpdateDailyMissions(List<DailyMissionState> missions)
        {
            _data.daily.missions = missions;
            Save();
        }

        public void RegisterBestScore(string levelId, int score)
        {
            if(string.IsNullOrEmpty(levelId))
                return;
            
            if(!_data.BestScores.ContainsKey(levelId))
                _data.BestScores[levelId] = score;
            else if(score > _data.BestScores[levelId])
                _data.BestScores[levelId] = score;

            Save();
        }

        public int GetBestScore(string levelId)
        {
            if(string.IsNullOrEmpty(levelId))
                return 0;
            
            return _data.BestScores.TryGetValue(levelId, out int best) ? best : 0;
        }

        public void SetVipStatus(bool isVip, int durationDays = 30)
        {
            dataToSave.isVip = isVip;

            if (isVip)
            {
                dataToSave.vipExpirationTicks = DateTime.UtcNow.AddDays(durationDays).Ticks;
            }
            else
            {
                dataToSave.vipExpirationTicks = 0;
            }
            
            SaveData();
        }

        public bool IsVipActive()
        {
            if (!dataToSave.isVip)
                return false;

            if (dataToSave.vipExpirationTicks == 0)
                return true;

            bool isActive = DateTime.UtcNow.Ticks < dataToSave.vipExpirationTicks;

            if (!isActive)
            {
                dataToSave.isVip = false;
                SaveData();
            }

            return isActive;
        }

        public void UnlockCard(string cardId)
        {
            if (!dataToSave.ownedCards.Contains(cardId))
            {
                dataToSave.ownedCards.Add(cardId);
                SaveData();
            }
        }

        public void UnlockAvatar(int avatarId)
        {
            if (!dataToSave.unlockedAvatar.Contains(avatarId))
            {
                dataToSave.unlockedAvatar.Add(avatarId);
                SaveData();
            }
        }
    }
}