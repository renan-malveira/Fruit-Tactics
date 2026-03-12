using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Firebase.Database;
using UnityEngine;

namespace Managers
{
    [Serializable]
    public class DataToSave
    {
        public string userName;
        public int uniquePlayerId;
        public int totalCoins;
        public int crrLevel;
        public int highScore;
        public long lastUpdatedTicks;
        public bool isVip;
        public long vipExpirationTicks;
        public bool removeAds;

        public List<string> ownedCards = new List<string>();
        public List<string> equippedDeck = new List<string>();
        public List<int> unlockedAvatar = new List<int> { 0 };
        public List<int> purchasedAvatar = new List<int>();
        public Dictionary<string, int> BestScores = new Dictionary<string, int>();

        public bool musicOn = true;
        public bool sfxOn = true;
        public bool vfxOn = true;
        public string language = "pt_BR";
        public string dailyDayKey;
        public string lastLoginDayKey;
    }

    public class DataSaver : MonoBehaviour
    {
        [Header("Player Data")]
        [SerializeField] private string userId;
        [SerializeField] public DataToSave dataToSave = new DataToSave();

        [Header("Sync Settings")]
        [Tooltip("Se true, quando não existir dado na nuvem, cria um perfil padrão e salva no Firebase.")]
        [SerializeField] private bool createCloudIfMissing = true;

        private DatabaseReference _databaseReference;
        private string _localFilePath;
        
        public bool IsDirty { get; private set; }

        public event Action<DataToSave> OnDataLoaded;
        public event Action OnDataNotFound;
        public event Action<Exception> OnLoadFailed;
        public event Action OnSavedToCloud;
        public event Action<DataToSave> OnRemoteDataChanged;
        
        [Header("Real-Time Sync")]
        [SerializeField] private bool enableRealtimeSync = true;
        [SerializeField] private bool smartMerger = true;

        private bool _isListening = false;
        private bool _isLocalChange = false;
        

        private void Awake()
        {
            _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
            _localFilePath = Path.Combine(Application.persistentDataPath, "playerData.json");
        }

        public void SetUserId(string uid)
        {
            userId = uid;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[DataSaver] UserId set: {userId}");
#endif
        }

        #region Public API

        /// <summary>
        /// Load READ-ONLY: carrega cloud + local, resolve conflito e salva apenas LOCAL.
        /// Não grava no Firebase durante o load (para não sobrescrever edição do console).
        /// </summary>
        public void LoadData()
        {
            if (!ValidateUserId()) return;
            StartCoroutine(LoadDataCoroutine());
        }

        /// <summary>
        /// Marca que houve alteração e deve salvar no próximo autosave.
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Salva completo (local + cloud) apenas se estiver Dirty (ou force = true).
        /// Use isso para “snapshot completo”.
        /// </summary>
        public void SaveData(bool force = false)
        {
            if (!ValidateUserId()) return;
            if (!force && !IsDirty)
                return;

            SaveLocal();
            SaveFullToCloud();
        }

        /// <summary>
        /// Patch: atualiza apenas alguns campos no Firebase (sem sobrescrever o nó inteiro).
        /// Também salva no JSON local.
        /// </summary>
        public void SavePatch(Dictionary<string, object> patch, bool updateLocalTicks = true)
        {
            if (!ValidateUserId()) return;
            if (patch == null || patch.Count == 0) return;

            if (dataToSave == null)
                dataToSave = new DataToSave();

            if (updateLocalTicks)
            {
                var ticks = DateTime.UtcNow.Ticks;
                dataToSave.lastUpdatedTicks = ticks;
                patch["lastUpdatedTicks"] = ticks;
            }

            ApplyPatchLocally(patch);
            SaveLocal();

            _databaseReference
                .Child("users")
                .Child(userId)
                .UpdateChildrenAsync(patch)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                        Debug.LogError($"[DataSaver] Erro ao salvar PATCH no Firebase: {task.Exception}");
                    else if (task.IsCompletedSuccessfully)
                        OnSavedToCloud?.Invoke();
                });

            IsDirty = false;
        }
        
        public void SetCoins(int coins)
        {
            dataToSave.totalCoins = Mathf.Max(0, coins);
            MarkDirty();
        }

        public void AddCoins(int amount)
        {
            dataToSave.totalCoins = Mathf.Max(0, dataToSave.totalCoins + amount);
            MarkDirty();
        }

        public void SetRemoveAds(bool value)
        {
            dataToSave.removeAds = value;
            Ads.AdGatingService.Instance?.NotifyStatusChanged();
            SaveData();
        }


        public void SetVip(bool isVip, int durationDays = 30)
        {
            dataToSave.isVip = isVip;
            dataToSave.vipExpirationTicks = isVip ? DateTime.UtcNow.AddDays(durationDays).Ticks : 0;
            MarkDirty();
        }

        #endregion

        #region Load

        private IEnumerator LoadDataCoroutine()
        {
            var serverDataTask = _databaseReference.Child("users").Child(userId).GetValueAsync();
            var localData = LoadLocal();

            yield return new WaitUntil(() => serverDataTask.IsCompleted);

            DataToSave cloudData = null;

            if (serverDataTask.IsFaulted)
            {
                Debug.LogError($"[DataSaver] Error loading from Firebase: {serverDataTask.Exception}");
                OnLoadFailed?.Invoke(serverDataTask.Exception);
                yield break;
            }

            var snapshot = serverDataTask.Result;

            if (snapshot != null && snapshot.Exists)
            {
                var jsonData = snapshot.GetRawJsonValue();
                if (!string.IsNullOrEmpty(jsonData))
                    cloudData = JsonUtility.FromJson<DataToSave>(jsonData);
            }

            dataToSave = ResolveDataConflict(localData, cloudData);

            if (dataToSave != null)
            {
                if (dataToSave.lastUpdatedTicks == 0)
                    dataToSave.lastUpdatedTicks = DateTime.UtcNow.Ticks;

                SaveLocal();
                IsDirty = false;
                
                OnDataLoaded?.Invoke(dataToSave);
                StartRealtimeListener();
                yield break;
            }

            dataToSave = CreateNewDefaultData();
            SaveLocal();
            IsDirty = false;

            if (createCloudIfMissing)
                SaveFullToCloud();

            OnDataNotFound?.Invoke();
            StartRealtimeListener();
        }

        #endregion

        #region Save (Full)

        private void SaveFullToCloud()
        {
            if (dataToSave == null)
                dataToSave = new DataToSave();

            dataToSave.lastUpdatedTicks = DateTime.UtcNow.Ticks;
            _isLocalChange = true;

            var json = JsonUtility.ToJson(dataToSave);

            _databaseReference
                .Child("users")
                .Child(userId)
                .SetRawJsonValueAsync(json)
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"[DataSaver] Erro ao salvar FULL no Firebase: {task.Exception}");
                        _isLocalChange = false;
                    }
                    else if (task.IsCompletedSuccessfully)
                    {
                        IsDirty = false;
                        OnSavedToCloud?.Invoke();
                    }
                });
        }

        #endregion

        #region Local JSON

        public void SaveLocal()
        {
            try
            {
                var json = JsonUtility.ToJson(dataToSave);
                File.WriteAllText(_localFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataSaver] Erro ao salvar localmente: {e}");
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
                Debug.LogError($"[DataSaver] Erro ao carregar dados locais: {e}");
                return null;
            }
        }

        #endregion

        #region Conflict + Defaults

        private DataToSave ResolveDataConflict(DataToSave local, DataToSave cloud)
        {
            if (cloud != null)
                return cloud;

            return local;
        }

        private DataToSave CreateNewDefaultData()
        {
            return new DataToSave
            {
                userName = "Guest",
                uniquePlayerId = UnityEngine.Random.Range(100000, 999999),
                totalCoins = 0,
                crrLevel = 1,
                highScore = 0,
                removeAds = false,
                isVip = false,
                vipExpirationTicks = 0,
                lastUpdatedTicks = DateTime.UtcNow.Ticks,
                ownedCards = new List<string>(),
                equippedDeck = new List<string>(),
                unlockedAvatar = new List<int> { 0 },
                purchasedAvatar = new List<int>(),
                BestScores = new Dictionary<string, int>(),
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
                Debug.LogError("[DataSaver] userId está vazio! Configure o ID do jogador antes de salvar/carregar.");
                return false;
            }
            return true;
        }

        #endregion

        #region Patch Local Apply (mínimo útil)

        private void ApplyPatchLocally(Dictionary<string, object> patch)
        {
            // Mantive bem simples: só os campos mais comuns.
            // Se quiser, eu completo com todos.
            foreach (var kv in patch)
            {
                switch (kv.Key)
                {
                    case "userName":
                        dataToSave.userName = kv.Value?.ToString();
                        break;
                    case "uniquePlayerId":
                        dataToSave.uniquePlayerId = Convert.ToInt32(kv.Value);
                        break;
                    case "totalCoins":
                        dataToSave.totalCoins = Convert.ToInt32(kv.Value);
                        break;
                    case "crrLevel":
                        dataToSave.crrLevel = Convert.ToInt32(kv.Value);
                        break;
                    case "highScore":
                        dataToSave.highScore = Convert.ToInt32(kv.Value);
                        break;
                    case "removeAds":
                        dataToSave.removeAds = Convert.ToBoolean(kv.Value);
                        break;
                    case "isVip":
                        dataToSave.isVip = Convert.ToBoolean(kv.Value);
                        break;
                    case "vipExpirationTicks":
                        dataToSave.vipExpirationTicks = Convert.ToInt64(kv.Value);
                        break;
                    case "musicOn":
                        dataToSave.musicOn = Convert.ToBoolean(kv.Value);
                        break;
                    case "sfxOn":
                        dataToSave.sfxOn = Convert.ToBoolean(kv.Value);
                        break;
                    case "vfxOn":
                        dataToSave.vfxOn = Convert.ToBoolean(kv.Value);
                        break;
                    case "language":
                        dataToSave.language = kv.Value?.ToString();
                        break;
                    case "dailyDayKey":
                        dataToSave.dailyDayKey = kv.Value?.ToString();
                        break;
                    case "lastLoginDayKey":
                        dataToSave.lastLoginDayKey = kv.Value?.ToString();
                        break;
                    case "lastUpdatedTicks":
                        dataToSave.lastUpdatedTicks = Convert.ToInt64(kv.Value);
                        break;
                }
            }
        }

        #endregion

        #region Real-Time Firebase Listener

        public void StartRealtimeListener()
        {
            if (!enableRealtimeSync || _isListening || !ValidateUserId())
                return;

            var userRef = _databaseReference.Child("users").Child(userId);
            userRef.ValueChanged += OnFirebaseValueChanged;
            _isListening = true;
        }

        public void StopRealtimeListener()
        {
            if (!_isListening || !ValidateUserId())
                return;
            
            var userRef = _databaseReference.Child("users").Child(userId);
            userRef.ValueChanged -= OnFirebaseValueChanged;
            _isListening = false;
        }

        private void OnFirebaseValueChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[DataSaver] Firebase listener error: {args.DatabaseError.Message}");
                return;
            }

            if (_isLocalChange)
            {
                _isLocalChange = false;
                return;
            }

            var snapshot = args.Snapshot;
            if (snapshot == null || !snapshot.Exists)
                return;
            
            var jsonData = snapshot.GetRawJsonValue();
            if (string.IsNullOrEmpty(jsonData))
                return;
            
            var remoteData = JsonUtility.FromJson<DataToSave>(jsonData);

            if (smartMerger)
                ApplyRemoteChanges(remoteData);
            else
            {
                dataToSave = remoteData;
                SaveLocal();
                OnRemoteDataChanged?.Invoke(dataToSave);
            }
        }

        private void ApplyRemoteChanges(DataToSave remoteData)
        {
            if (remoteData.lastUpdatedTicks <= dataToSave.lastUpdatedTicks)
                return;

            dataToSave = remoteData;
            SaveLocal();
            OnRemoteDataChanged?.Invoke(dataToSave);
        }

        #endregion

        public void UpdateDailyLogin(string todayKey)
        {
            if (dataToSave != null && dataToSave.lastLoginDayKey != todayKey)
            {
                dataToSave.lastLoginDayKey = todayKey;
                SaveData();
            }
        }

        #region Debug & Testing Utilities

        [ContextMenu("Debug: Print Current Data")]
        public void DebugPrintCurrentData()
        {
            if (dataToSave == null)
            {
                Debug.Log("[DataSaver] ❌ No data loaded");
                return;
            }

            Debug.Log("========== CURRENT PLAYER DATA ==========");
            Debug.Log($"User Name: {dataToSave.userName}");
            Debug.Log($"User ID (internal): {userId}");
            Debug.Log($"Total Coins: {dataToSave.totalCoins}");
            Debug.Log($"Current Level: {dataToSave.crrLevel}");
            Debug.Log($"High Score: {dataToSave.highScore}");
            Debug.Log($"Remove Ads: {dataToSave.removeAds}");
            Debug.Log($"VIP Status: {dataToSave.isVip}");
            Debug.Log($"Owned Cards: {dataToSave.ownedCards.Count}");
            Debug.Log($"Equipped Deck: {dataToSave.equippedDeck.Count}");
            Debug.Log($"Unlocked Avatars: {dataToSave.unlockedAvatar.Count}");
            Debug.Log($"Last Updated: {new DateTime(dataToSave.lastUpdatedTicks)}");
            Debug.Log("========================================");
        }

        [ContextMenu("Debug: Force Reload from Firebase")]
        public void DebugForceReloadFromFirebase()
        {
            if (!ValidateUserId())
            {
                Debug.LogError("[DataSaver] Cannot reload - User ID not set!");
                return;
            }

            Debug.Log("[DataSaver] 🔄 Forcing reload from Firebase...");
            LoadData();
        }

        [ContextMenu("Debug: Clear Local Cache Only")]
        public void DebugClearLocalCache()
        {
            try
            {
                if (File.Exists(_localFilePath))
                {
                    File.Delete(_localFilePath);
                    Debug.Log($"[DataSaver] ✅ Local cache deleted: {_localFilePath}");
                    Debug.Log("[DataSaver] Restart the game to reload from Firebase");
                }
                else
                {
                    Debug.Log("[DataSaver] No local cache file found");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataSaver] Error deleting local cache: {e}");
            }
        }

        public void ForceLoadFromFirebase()
        {
            if (!ValidateUserId()) return;
            StartCoroutine(ForceLoadFromFirebaseCoroutine());
        }

        private IEnumerator ForceLoadFromFirebaseCoroutine()
        {
            Debug.Log("[DataSaver] 🔄 Force loading ONLY from Firebase (ignoring local data)...");

            var serverDataTask = _databaseReference.Child("users").Child(userId).GetValueAsync();
            yield return new WaitUntil(() => serverDataTask.IsCompleted);

            if (serverDataTask.IsFaulted)
            {
                Debug.LogError($"[DataSaver] ❌ Failed to load from Firebase: {serverDataTask.Exception}");
                yield break;
            }

            var snapshot = serverDataTask.Result;

            if (snapshot != null && snapshot.Exists)
            {
                var jsonData = snapshot.GetRawJsonValue();
                if (!string.IsNullOrEmpty(jsonData))
                {
                    dataToSave = JsonUtility.FromJson<DataToSave>(jsonData);
                    SaveLocal();
                    IsDirty = false;

                    Debug.Log($"[DataSaver] ✅ Force loaded from Firebase - Coins: {dataToSave.totalCoins}, Level: {dataToSave.crrLevel}");
                    OnDataLoaded?.Invoke(dataToSave);
                }
                else
                {
                    Debug.LogError("[DataSaver] ❌ Firebase data is empty");
                }
            }
            else
            {
                Debug.LogError("[DataSaver] ❌ No Firebase data found for this user");
            }
        }

        #endregion
    }
}
