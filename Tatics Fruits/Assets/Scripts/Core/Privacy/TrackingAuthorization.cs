using System;
using AOT;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Core.Privacy
{
    /// <summary>
    /// Ponte para o App Tracking Transparency (iOS 14+).
    /// Nas outras plataformas responde "não se aplica" na hora.
    /// Implementação nativa: Assets/Plugins/iOS/FTTrackingAuthorization.mm
    /// </summary>
    public static class TrackingAuthorization
    {
        // Mesmos valores de ATTrackingManagerAuthorizationStatus.
        public enum Status
        {
            NotDetermined = 0,
            Restricted = 1,
            Denied = 2,
            Authorized = 3
        }

        private delegate void NativeCallback(int status);

        private static Action<Status> _pending;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int FT_GetTrackingAuthorizationStatus();
        [DllImport("__Internal")] private static extern void FT_RequestTrackingAuthorization(NativeCallback callback);
#endif

        public static Status Current
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return (Status)FT_GetTrackingAuthorizationStatus();
#else
                return Status.Authorized;
#endif
            }
        }

        /// <summary>
        /// Mostra o pedido do sistema só se o status ainda não foi decidido.
        /// O callback sempre é chamado, uma única vez.
        /// </summary>
        public static void RequestIfNeeded(Action<Status> onDone)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var current = Current;
            if (current != Status.NotDetermined)
            {
                onDone?.Invoke(current);
                return;
            }

            _pending = onDone;
            FT_RequestTrackingAuthorization(OnNativeResult);
#else
            onDone?.Invoke(Current);
#endif
        }

        [MonoPInvokeCallback(typeof(NativeCallback))]
        private static void OnNativeResult(int status)
        {
            var callback = _pending;
            _pending = null;
            Debug.Log($"[TrackingAuthorization] ATT status: {(Status)status}");
            callback?.Invoke((Status)status);
        }
    }
}
