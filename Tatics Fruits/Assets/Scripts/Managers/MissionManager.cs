using System.Collections.Generic;
using Gameplay.Controllers;
using UnityEngine;

namespace Managers
{
    public class MissionManager : MonoBehaviour
    {
        [SerializeField] private PlayerProfileController profileController;

        public static MissionManager Instance { get; private set; }

        private readonly List<DefaultNamespace.Mission> _activeMissions = new List<DefaultNamespace.Mission>();

        public IReadOnlyList<DefaultNamespace.Mission> ActiveMissions => _activeMissions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GenerateMissions();
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        private void UnsubscribeAll()
        {
            foreach (var mission in _activeMissions)
                mission.OnMissionCompleted -= HandleMissionCompleted;
        }

        public void GenerateMissions()
        {
            UnsubscribeAll();
            _activeMissions.Clear();

            _activeMissions.Add(new DefaultNamespace.Mission("Marque 5000 pontos", "Faça 5000 pontos na partida", DefaultNamespace.MissionType.ScorePoints, 5000, 100, "Gold"));
            _activeMissions.Add(new DefaultNamespace.Mission("Jogue 3 partidas", "Complete 3 partidas", DefaultNamespace.MissionType.PlayXMatches, 3, 1, "NewCard"));

            foreach (var mission in _activeMissions)
                mission.OnMissionCompleted += HandleMissionCompleted;
        }

        private void HandleMissionCompleted(DefaultNamespace.Mission mission)
        {
            if (profileController == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[MissionManager] ProfileController not assigned — reward cannot be delivered.");
#endif
                return;
            }

            if (mission._rewardType == "Gold")
            {
                profileController.AddGold(mission._rewardAmount);
            }
            else if (mission._rewardType == "NewCard")
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[MissionManager] NewCard reward granted for mission: {mission._missionName}");
#endif
            }
        }

        public void UpdateMissionProgress(DefaultNamespace.MissionType type, int amount)
        {
            foreach (var mission in _activeMissions)
            {
                if (mission._type == type)
                    mission.UpdateProgress(amount);
            }
        }
    }
}