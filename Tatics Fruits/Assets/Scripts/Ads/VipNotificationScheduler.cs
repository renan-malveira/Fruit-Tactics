using System;
using Unity.Notifications.Android;
using UnityEngine;

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace Ads
{
    public static class VipNotificationScheduler
    {
        private const string ChannelId = "vip_channel";
        private const string ChannelName = "VIP Status";

        private const int IdWarning3Days  = 5001;
        private const int IdWarning1Day   = 5002;
        private const int IdExpired       = 5003;

        public static void Schedule(long vipExpirationTicks)
        {
            CancelAll();

            if (vipExpirationTicks <= 0) return;

            var expiry = new DateTime(vipExpirationTicks, DateTimeKind.Utc).ToLocalTime();
            if (expiry <= DateTime.Now) return;

#if UNITY_ANDROID
            EnsureAndroidChannel();
            TryScheduleAndroid(IdWarning3Days, expiry.AddDays(-3),
                "Seu VIP expira em breve!",
                "Seu acesso VIP termina em 3 dias. Renove para continuar sem anúncios.");
            TryScheduleAndroid(IdWarning1Day, expiry.AddDays(-1),
                "Último dia do seu VIP!",
                "Seu VIP expira amanhã. Não perca seus benefícios exclusivos.");
            TryScheduleAndroid(IdExpired, expiry,
                "Seu VIP expirou",
                "Renove o VIP para voltar a jogar sem anúncios e com benefícios especiais.");
#elif UNITY_IOS
            TryScheduleIOS("vip_warn_3d", expiry.AddDays(-3),
                "Seu VIP expira em breve!",
                "Seu acesso VIP termina em 3 dias. Renove para continuar sem anúncios.");
            TryScheduleIOS("vip_warn_1d", expiry.AddDays(-1),
                "Último dia do seu VIP!",
                "Seu VIP expira amanhã. Não perca seus benefícios exclusivos.");
            TryScheduleIOS("vip_expired", expiry,
                "Seu VIP expirou",
                "Renove o VIP para voltar a jogar sem anúncios e com benefícios especiais.");
#endif
        }

        public static void CancelAll()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelScheduledNotification(IdWarning3Days);
            AndroidNotificationCenter.CancelScheduledNotification(IdWarning1Day);
            AndroidNotificationCenter.CancelScheduledNotification(IdExpired);
#elif UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification("vip_warn_3d");
            iOSNotificationCenter.RemoveScheduledNotification("vip_warn_1d");
            iOSNotificationCenter.RemoveScheduledNotification("vip_expired");
#endif
        }

#if UNITY_ANDROID
        private static void EnsureAndroidChannel()
        {
            var channel = new AndroidNotificationChannel
            {
                Id          = ChannelId,
                Name        = ChannelName,
                Importance  = Importance.High,
                Description = "Notificações sobre o status do seu VIP",
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

        private static void TryScheduleAndroid(int id, DateTime fireTime, string title, string text)
        {
            if (fireTime <= DateTime.Now) return;

            var notification = new AndroidNotification
            {
                Title    = title,
                Text     = text,
                FireTime = fireTime,
                SmallIcon = "icon_0",
                LargeIcon = "icon_1",
            };

            AndroidNotificationCenter.CancelScheduledNotification(id);
            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, id);
        }
#endif

#if UNITY_IOS
        private static void TryScheduleIOS(string identifier, DateTime fireTime, string title, string body)
        {
            if (fireTime <= DateTime.Now) return;

            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = fireTime - DateTime.Now,
                Repeats      = false,
            };

            var notification = new iOSNotification
            {
                Identifier    = identifier,
                Title         = title,
                Body          = body,
                ShowInForeground = false,
                Trigger       = trigger,
            };

            iOSNotificationCenter.RemoveScheduledNotification(identifier);
            iOSNotificationCenter.ScheduleNotification(notification);
        }
#endif
    }
}
