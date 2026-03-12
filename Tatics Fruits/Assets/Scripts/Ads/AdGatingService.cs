using System;
using Managers;
using UnityEngine;

namespace Ads
{
    public class AdGatingService : MonoBehaviour, IAdGatingService
    {
        [SerializeField] private DataSaver dataSaver;
        
        public static AdGatingService Instance { get; private set; }
        
        public event Action OnAdStatusChanged;

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
            if (dataSaver != null)
                dataSaver.OnRemoteDataChanged += OnRemoteDataChanged;
        }

        private void OnDestroy()
        {
            if (dataSaver != null)
                dataSaver.OnRemoteDataChanged -= OnRemoteDataChanged;
        }

        private void OnRemoteDataChanged(DataToSave _)
        {
            OnAdStatusChanged?.Invoke();
        }

        public bool ShouldShowAds()
        {
            if (dataSaver?.dataToSave == null)
                return true;

            if (dataSaver.dataToSave.removeAds)
                return false;

            if (!dataSaver.dataToSave.isVip)
                return true;

            if (dataSaver.dataToSave.vipExpirationTicks == 0)
                return false;
            
            return DateTime.UtcNow.Ticks >= dataSaver.dataToSave.vipExpirationTicks;
        }
        
        public void NotifyStatusChanged()
        {
            OnAdStatusChanged?.Invoke();
        }
    }
}