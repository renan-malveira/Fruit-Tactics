using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gameplay.Utils
{
    [DefaultExecutionOrder(-100)]
    public class Localizer : MonoBehaviour
    {
        private static Localizer _instance;
        public static Localizer Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<Localizer>();
                
                    if (_instance == null)
                    {
                        var go = new GameObject("Localizer");
                        _instance = go.AddComponent<Localizer>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
    
        public static bool IsReady => _instance != null && _instance._isInitialized;
    
        private readonly Dictionary<string, string> _table = new();
        private bool _isInitialized = false;

        public string CurrentLanguage { get; private set; } = "pt-BR";
    
        public delegate void LanguageChanged();
        public event LanguageChanged OnLanguageChanged;
    
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
        
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        public static readonly string[] SupportedLanguages = { "pt-BR", "en-US", "es-ES" };
        private const string FallbackLanguage = "pt-BR";
        private const float AndroidLoadTimeoutSeconds = 3f;

        private void Initialize()
        {
            var cfg = SettingsRepository.Get();
            var language = cfg.language;

            // Primeira abertura (sem settings.json): usa o idioma do aparelho e grava a escolha.
            // Quem já jogou mantém o idioma salvo.
            if (!SettingsRepository.LoadedFromDisk)
            {
                language = DetectDeviceLanguage();
                cfg.language = language;
                SettingsRepository.Save(cfg);
            }

            SetLanguage(NormalizeLanguage(language), save: false);
            _isInitialized = true;
        }

        public static string DetectDeviceLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Portuguese: return "pt-BR";
                case SystemLanguage.Spanish: return "es-ES";
                default: return "en-US";
            }
        }

        private static string NormalizeLanguage(string language)
        {
            foreach (var supported in SupportedLanguages)
                if (string.Equals(supported, language, StringComparison.OrdinalIgnoreCase))
                    return supported;
            return FallbackLanguage;
        }

        public void SetLanguage(string language, bool save = true)
        {
            string oldLanguage = CurrentLanguage;
            CurrentLanguage = language;
            LoadTable(language);
            if (save)
            {
                var s = SettingsRepository.Get();
                s.language = language;
                SettingsRepository.Save(s);
            }
        
            if (oldLanguage != language)
            {
                Managers.AnalyticsManager.Instance?.TrackLanguageChanged(oldLanguage, language);
            }
        
            OnLanguageChanged?.Invoke();
        }

        public string TrFormat(string key, string fallback, params object[] args)
        {
            var fmt = Tr(key, fallback);
            try
            {
                return string.Format(fmt, args);
            }
            catch (FormatException)
            {
                return fmt;
            }
        }

        private void LoadTable(string language)
        {
            _table.Clear();
        
            string path = Path.Combine(Application.streamingAssetsPath, "i18n", $"{language}.json");
        
#if UNITY_ANDROID && !UNITY_EDITOR
            // No Android o StreamingAssets fica dentro do APK (jar:), então File não lê.
            // O arquivo é pequeno e local; a espera tem limite para nunca travar o jogo.
            string json;
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(path))
            {
                request.SendWebRequest();
                var deadline = DateTime.UtcNow.AddSeconds(AndroidLoadTimeoutSeconds);
                while (!request.isDone && DateTime.UtcNow < deadline)
                    System.Threading.Thread.Sleep(1);

                if (!request.isDone)
                {
                    request.Abort();
                    Debug.LogError($"[Localizer] Tempo esgotado ao ler {path}");
                    return;
                }
                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Localizer] {request.error} ({path})");
                    return;
                }
                json = request.downloadHandler.text;
            }
#else
            if (!File.Exists(path))
            {
                Debug.LogError($"Missing locale + {path}");
                return;
            }
            var json = File.ReadAllText(path); 
#endif
            var wrapper = JsonUtility.FromJson<LocalizationWrapper>(Wrap(json));
            if (wrapper != null && wrapper.entries != null)
            {
                foreach (var e in wrapper.entries)
                {
                    _table[e.key] = e.value;
                }
            }
        }
    
        [System.Serializable] private class LocalizationEntry { public string key; public string value; }
        [System.Serializable] private class LocalizationWrapper { public LocalizationEntry[] entries; }

        private string Wrap(string raw)
        {
            var dict = MiniJson.Deserialize(raw) as Dictionary<string, object>;
            var list = new List<LocalizationEntry>();
            foreach (var kv in dict) list.Add(new LocalizationEntry { key = kv.Key, value = kv.Value.ToString() });
            return JsonUtility.ToJson(new LocalizationWrapper { entries = list.ToArray() }, false);
        }

        public string Tr(string key, string fallback = "")
            => _table.TryGetValue(key, out var v) ? v : (string.IsNullOrEmpty(fallback) ? key : fallback);
    }
}