using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.PushNotifications;
using UnityEngine;
using System;

namespace Ads
{
    public class PushNotificationManager : MonoBehaviour
    {
        void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            // A permissão de notificação só é pedida depois dos termos/consentimento,
            // nunca no primeiro frame do app.
            Core.Privacy.ConsentManager.WhenResolved(() =>
            {
                if (this == null) return;
                if (!Core.Privacy.ConsentManager.CanAskForNotifications) return;
                RegisterAsync();
            });
#endif
        }

#if UNITY_ANDROID || UNITY_IOS
        private async void RegisterAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();

                // O pacote de push depende do Analytics. Sem consentimento de analytics, não registra.
                if (!Core.Privacy.ConsentManager.CanCollectAnalytics)
                {
                    Debug.Log("[PushNotification] Sem consentimento de analytics; push não registrado.");
                    return;
                }

                await PushNotificationsService.Instance.RegisterForPushNotificationsAsync();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[PushNotification] Registrado para notificações.");
#endif
                PushNotificationsService.Instance.OnRemoteNotificationReceived += OnNotificationReceived;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PushNotification] Falha ao registrar: {e.Message}");
            }
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        void OnNotificationReceived(System.Collections.Generic.IDictionary<string, object> notificationData)
        {
            Debug.Log("[PushNotification] Received a notification!");
            foreach (var entry in notificationData)
            {
                Debug.Log($"[PushNotification] Data: {entry.Key} = {entry.Value}");
            }
        }
#endif

        void OnDestroy()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (PushNotificationsService.Instance != null)
            {
                PushNotificationsService.Instance.OnRemoteNotificationReceived -= OnNotificationReceived;
            }
#endif
        }
    }
}
