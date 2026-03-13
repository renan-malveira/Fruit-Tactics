using UnityEngine;

namespace Core.ScriptableObjects
{
    [CreateAssetMenu(fileName = "DailyLoginConfig", menuName = "FruitTactics/DailyLoginConfig")]
    public class DailyLoginConfigSo : ScriptableObject
    {
        [SerializeField] private int[] dayRewards = {50, 75, 100, 150, 200, 300, 500};
        [SerializeField] private bool resetStreakOnMiss = true;
        
        public int DayCount => dayRewards.Length;
        public bool ResetStreakOnMiss => resetStreakOnMiss;

        public int GetReward(int dayIndex) => dayRewards[Mathf.Clamp(dayIndex, 0, dayRewards.Length - 1)];
        
        public int[] GetAllRewards() => (int[]) dayRewards.Clone();
    }
}