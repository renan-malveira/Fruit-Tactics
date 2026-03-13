using UnityEngine;

namespace Core.ScriptableObjects
{
    public enum MissionEventType
    {
        WinLevel          = 1,
        ScorePoints       = 2,
        MakePairs         = 3,
        ReachCombo        = 4,
        PlaySessions      = 5,
        WinWithoutMistakes = 6,
    }

    [CreateAssetMenu(menuName = "Game/Daily Mission", fileName = "DailyMission_")]
    public class DailyMissionSo : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public MissionEventType eventType = MissionEventType.WinLevel;

        [Header("Design")]
        public int target = 1;
        public int rewardGold = 50;
        public int levelParam = 0;
        public string missionType;

        [Header("Localization")]
        public string descriptionKey;
        [TextArea]
        public string descriptionTemplate = "Vença o nível {0}";
    }
}