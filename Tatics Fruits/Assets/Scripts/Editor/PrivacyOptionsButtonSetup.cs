using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Editor
{
    /// <summary>
    /// Cria o botão "Opções de privacidade" no painel de Configurações (cena MainMenu)
    /// a partir de uma cópia do botão "Termos" e liga em SettingsMenu.privacyOptionsButton.
    /// O botão só aparece no jogo onde a lei exige (EEE, Reino Unido, Suíça); ver ConsentManager.
    ///
    /// Menu: Tools/Release/4. Criar botão Opções de privacidade
    /// </summary>
    public static class PrivacyOptionsButtonSetup
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ButtonName = "Privacy Options Button";
        private const string LocalizationKey = "privacy_options";
        private const string FallbackLabel = "Opções de privacidade";

        [MenuItem("Tools/Release/4. Criar botão Opções de privacidade")]
        public static void Create()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var menu = Object.FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
            if (menu == null)
            {
                Fail("SettingsMenu não encontrado na cena MainMenu.");
                return;
            }

            var so = new SerializedObject(menu);
            var privacyProp = so.FindProperty("privacyOptionsButton");
            var termsProp = so.FindProperty("termsButton");
            var deleteProp = so.FindProperty("deleteAccountButton");

            if (privacyProp.objectReferenceValue != null)
            {
                Selection.activeObject = privacyProp.objectReferenceValue;
                EditorUtility.DisplayDialog("Opções de privacidade",
                    "O botão já existe e está ligado no SettingsMenu. Nada foi alterado.", "OK");
                return;
            }

            var terms = termsProp.objectReferenceValue as Button;
            if (terms == null)
            {
                Fail("SettingsMenu.termsButton está vazio; não há botão para copiar.");
                return;
            }

            // Cópia do botão Termos, no mesmo contêiner, logo depois dele.
            var clone = Object.Instantiate(terms.gameObject, terms.transform.parent);
            clone.name = ButtonName;
            clone.transform.SetSiblingIndex(terms.transform.GetSiblingIndex() + 1);
            Undo.RegisterCreatedObjectUndo(clone, "Criar botão Opções de privacidade");

            var button = clone.GetComponent<Button>();

            // O botão Termos tem chamadas fixas no Inspector; o novo só usa a do SettingsMenu.
            for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);

            // Embaixo, no centro: espelho do botão Excluir (que fica no topo).
            var rect = (RectTransform)clone.transform;
            var deleteRect = (deleteProp.objectReferenceValue as Button)?.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = deleteRect != null ? deleteRect.sizeDelta : rect.sizeDelta;
            rect.anchoredPosition = new Vector2(0f, deleteRect != null ? -deleteRect.anchoredPosition.y : 10f);

            // Texto traduzido (chave privacy_options em StreamingAssets/i18n).
            var label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = FallbackLabel;
                if (!label.enableAutoSizing)
                {
                    label.fontSizeMax = label.fontSize;
                    label.fontSizeMin = Mathf.Max(12f, label.fontSize * 0.5f);
                    label.enableAutoSizing = true;
                }

                var localized = label.GetComponent<LocalizedText>();
                if (localized != null)
                {
                    localized.key = LocalizationKey;
                    localized.fallback = FallbackLabel;
                    EditorUtility.SetDirty(localized);
                }
                EditorUtility.SetDirty(label);
            }

            privacyProp.objectReferenceValue = button;
            so.ApplyModifiedProperties();

            var container = (RectTransform)terms.transform.parent;
            var overlaps = container.rect.height < rect.sizeDelta.y * 3f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = clone;
            EditorGUIUtility.PingObject(clone);

            Debug.Log($"[PrivacyOptionsButtonSetup] Criado '{ButtonName}' em {container.name} e ligado em SettingsMenu.privacyOptionsButton.");
            EditorUtility.DisplayDialog("Opções de privacidade",
                "Botão criado embaixo, no centro do painel de Configurações, e ligado no SettingsMenu. Cena salva.\n\n" +
                "Ele fica escondido no jogo e só aparece para jogadores do EEE, Reino Unido e Suíça." +
                (overlaps ? "\n\nAtenção: o contêiner parece baixo para três linhas de botões. Confira a posição na cena." : string.Empty),
                "OK");
        }

        private static void Fail(string message)
        {
            Debug.LogError("[PrivacyOptionsButtonSetup] " + message);
            EditorUtility.DisplayDialog("Opções de privacidade", message, "OK");
        }
    }
}
