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

            RescheduleIfVipActive();
        }

        private void OnDestroy()
        {
            if (dataSaver != null)
                dataSaver.OnRemoteDataChanged -= OnRemoteDataChanged;
        }

        private void OnRemoteDataChanged(DataToSave _)
        {
            OnAdStatusChanged?.Invoke();
            RescheduleIfVipActive();
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

        public void GrantVip(long expirationTicks)
        {
            if (dataSaver?.dataToSave == null) return;

            dataSaver.dataToSave.isVip = true;
            dataSaver.dataToSave.vipExpirationTicks = expirationTicks;
            dataSaver.SaveData(force: true);

            VipNotificationScheduler.Schedule(expirationTicks);
            OnAdStatusChanged?.Invoke();
        }

        public void RevokeVip()
        {
            if (dataSaver?.dataToSave == null) return;

            dataSaver.dataToSave.isVip = false;
            dataSaver.dataToSave.vipExpirationTicks = 0;
            dataSaver.SaveData(force: true);

            VipNotificationScheduler.CancelAll();
            OnAdStatusChanged?.Invoke();
        }

        public void NotifyStatusChanged()
        {
            OnAdStatusChanged?.Invoke();
        }

        private void RescheduleIfVipActive()
        {
            if (dataSaver?.dataToSave == null) return;
            if (!dataSaver.dataToSave.isVip) return;
            if (dataSaver.dataToSave.vipExpirationTicks <= 0) return;

            VipNotificationScheduler.Schedule(dataSaver.dataToSave.vipExpirationTicks);
        }
    }
}
