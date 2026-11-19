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

        /// <summary>
        /// Este serviço sobrevive à troca de cena, mas o DataSaver da MainMenu é recriado
        /// a cada volta ao menu. Usa a referência do Inspector enquanto ela existir; depois, a atual.
        /// </summary>
        private DataSaver Saver
        {
            get
            {
                if (dataSaver == null && DataSaver.Current != null)
                {
                    dataSaver = DataSaver.Current;
                    dataSaver.OnRemoteDataChanged -= OnRemoteDataChanged;
                    dataSaver.OnRemoteDataChanged += OnRemoteDataChanged;
                }
                return dataSaver;
            }
        }

        public bool ShouldShowAds()
        {
            var saver = Saver;
            if (saver == null || saver.dataToSave == null)
                return true;

            if (saver.dataToSave.removeAds)
                return false;

            if (!saver.dataToSave.isVip)
                return true;

            if (saver.dataToSave.vipExpirationTicks == 0)
                return false;

            return DateTime.UtcNow.Ticks >= saver.dataToSave.vipExpirationTicks;
        }

        public void GrantVip(long expirationTicks)
        {
            if (Saver?.dataToSave == null) return;

            Saver.dataToSave.isVip = true;
            Saver.dataToSave.vipExpirationTicks = expirationTicks;
            Saver.SaveData(force: true);

            VipNotificationScheduler.Schedule(expirationTicks);
            OnAdStatusChanged?.Invoke();
        }

        public void RevokeVip()
        {
            if (Saver?.dataToSave == null) return;

            Saver.dataToSave.isVip = false;
            Saver.dataToSave.vipExpirationTicks = 0;
            Saver.SaveData(force: true);

            VipNotificationScheduler.CancelAll();
            OnAdStatusChanged?.Invoke();
        }

        public void NotifyStatusChanged()
        {
            OnAdStatusChanged?.Invoke();
        }

        private void RescheduleIfVipActive()
        {
            if (Saver?.dataToSave == null) return;
            if (!Saver.dataToSave.isVip) return;
            if (Saver.dataToSave.vipExpirationTicks <= 0) return;

            VipNotificationScheduler.Schedule(Saver.dataToSave.vipExpirationTicks);
        }
    }
}
