using Core.ScriptableObjects;
using UnityEngine;

namespace New_GameplayCore
{
    [CreateAssetMenu(menuName = "Create LevelSetSO", fileName = "LevelSet", order = 0)]
    public class LevelSetSO :ScriptableObject
    {
        public LevelConfigSo[] levels;
    }
}