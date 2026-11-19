using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Botão Voltar do Android. Desde a v2.2 não fecha mais o jogo: repassa para o
/// Core.BackButtonRouter (pausa, fecha popup ou manda o app para segundo plano).
/// </summary>
public class AndroidQuitHandler : MonoBehaviour
{
    private static AndroidQuitHandler _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_ANDROID || UNITY_EDITOR
    private void Update()
    {
        bool backPressed = false;

        // Sistema antigo (Input Manager)
        if (Input.GetKeyDown(KeyCode.Escape))
            backPressed = true;

        // Sistema novo (Input System)
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            backPressed = true;
        #endif

        if (backPressed)
            Core.BackButtonRouter.HandleBack();
    }
#endif
}
