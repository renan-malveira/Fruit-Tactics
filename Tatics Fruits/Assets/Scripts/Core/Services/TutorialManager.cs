using System;
using New_GameplayCore;
using New_GameplayCore.Views;
using UI.Views;
using UnityEngine;

namespace Core.Services
{
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private TutorialPanel tutorialPopup;
        [SerializeField] private int targetLevel = 1;

        private const string TutorialFile = "tutorial_data.json";
        private TutorialData _data;

        public event Action OnTutorialFinished;

        private void Awake()
        {
            _data = JsonDataService.Load<TutorialData>(TutorialFile) ?? new TutorialData();

            if (tutorialPopup != null)
                tutorialPopup.OnTutorialFinished += HandlePopupFinished;
        }

        public bool TryShowTutorial(int currentLevel)
        {
            if (tutorialPopup == null) return false;
            if (HasCompletedTutorial()) return false;

            var isFirstLevel  = currentLevel <= 1;
            var isTargetLevel = currentLevel == targetLevel;

            if (!isFirstLevel && !isTargetLevel) return false;

            tutorialPopup.Show();
            return true;
        }

        public bool HasCompletedTutorial()
            => _data != null && _data.tutorialCompleted;

        public void MarkCompletedFromRemote()
        {
            if (_data == null) _data = new TutorialData();
            if (_data.tutorialCompleted) return;

            _data.tutorialCompleted = true;
            JsonDataService.Save(TutorialFile, _data);
        }

        public void ResetForTesting()
        {
            _data = new TutorialData();
            JsonDataService.Save(TutorialFile, _data);
        }

        private void HandlePopupFinished()
        {
            _data.tutorialCompleted = true;
            _data.firstLoginDone    = true;
            JsonDataService.Save(TutorialFile, _data);

            SyncCompletionToFirebase();

            OnTutorialFinished?.Invoke();
        }

        private void SyncCompletionToFirebase()
        {
            var dataSaver = FindFirstObjectByType<Managers.DataSaver>();
            if (dataSaver?.dataToSave == null) return;

            dataSaver.dataToSave.tutorialCompleted = true;
            dataSaver.SaveData(force: true);
        }

        private void OnDestroy()
        {
            if (tutorialPopup != null)
                tutorialPopup.OnTutorialFinished -= HandlePopupFinished;
        }
    }
}
