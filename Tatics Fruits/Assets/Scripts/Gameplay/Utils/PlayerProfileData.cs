using System;
using System.Collections.Generic;
using Core.SaveSystem;

namespace Gameplay.Utils
{
    #region Missões diárias (igual ao seu)
    [Serializable]
    public class DailyMissionState
    {
        public string missionId;
        public string description;
        public int progress;
        public int target;
        public int rewardGold;
        public bool completed;
        public bool claimed;
    }
    #endregion

    #region Login diário 7/7 (novo)
    [Serializable]
    public class DailyLoginData
    {
        public string lastClaimDayKey = "";
    
        public int cycleIndex = 0;
    
        public List<bool> claimed = new List<bool> { false,false,false,false,false,false,false };
    
        public List<int> rewards = new List<int> { 50, 75, 100, 150, 200, 300, 500 };
    }
    #endregion

    [Serializable]
    public class DailySystemData
    {
        public string dayKey;
    
        public List<DailyMissionState> missions = new List<DailyMissionState>();
    
        public DailyLoginData login = new DailyLoginData();
    
        [Obsolete("Use Daily.Login em vez de lastLoginRewardDayKey.")]
        public string lastLoginRewardDayKey;

        [Obsolete("Use Daily.Login.rewards em vez de loginRewardGold.")]
        public int loginRewardGold = 50;
    }

    [Serializable]
    public class PlayerProfileData : ISaveData, UnityEngine.ISerializationCallbackReceiver
    {
        public string playerName = "Jogador";
        public int avatarIndex = 0;
        public int gold = 0;
        public int currentLevelIndex = 0;
        public int highestLevelUnlocked = 0;

        public bool musicOn = true;
        public bool sfxOn = true;
        public bool vfxOn = true;
        public string language = "pt_BR";

        public bool hasAcceptedLGPD = false;
        public bool removeAds = false;
    
        public DailySystemData daily = new DailySystemData();

        // Recorde e estrelas por fase (chave = LevelConfigSo.LevelKey). Os Dictionary não são
        // gravados pelo JsonUtility: o conteúdo vai para as listas abaixo (ver IntMapSerializer).
        [NonSerialized] public Dictionary<string, int> BestScores = new Dictionary<string, int>();
        [NonSerialized] public Dictionary<string, int> LevelStars = new Dictionary<string, int>();
        public List<string> bestScoreKeys = new List<string>();
        public List<int> bestScoreValues = new List<int>();
        public List<string> levelStarKeys = new List<string>();
        public List<int> levelStarValues = new List<int>();

        /// <summary>true depois que o levels_progress.json antigo foi incorporado a este perfil.</summary>
        public bool levelProgressMigrated;

        public List<string> ownedCards = new List<string>();
        public List<string> equippedDeck = new List<string>();
        public List<int> unlockedAvatars =  new List<int> {0};
        public List<int> purchasedAvatars =  new List<int>();
        public string firebaseUserId;
        
        public long lastUpdatedTicks;

        public string GetFileName() => "player_profile.json";

        public void OnBeforeSave()
        {
            lastUpdatedTicks = DateTime.UtcNow.Ticks;
        }

        public void OnAfterLoad()
        {
            if (ownedCards == null) ownedCards = new List<string>();
            if (equippedDeck == null) equippedDeck = new List<string>();
            if (daily == null) daily = new DailySystemData();
            if (daily.login == null) daily.login = new DailyLoginData();
            if (BestScores == null) BestScores = new Dictionary<string, int>();
            if (LevelStars == null) LevelStars = new Dictionary<string, int>();
            if (currentLevelIndex < 0) currentLevelIndex = 0;
            if (highestLevelUnlocked < 0) highestLevelUnlocked = 0;
        }

        public void OnBeforeSerialize()
        {
            bestScoreKeys ??= new List<string>();
            bestScoreValues ??= new List<int>();
            levelStarKeys ??= new List<string>();
            levelStarValues ??= new List<int>();
            IntMapSerializer.Write(BestScores, bestScoreKeys, bestScoreValues);
            IntMapSerializer.Write(LevelStars, levelStarKeys, levelStarValues);
        }

        public void OnAfterDeserialize()
        {
            BestScores = IntMapSerializer.Read(bestScoreKeys, bestScoreValues);
            LevelStars = IntMapSerializer.Read(levelStarKeys, levelStarValues);
        }
    }
}