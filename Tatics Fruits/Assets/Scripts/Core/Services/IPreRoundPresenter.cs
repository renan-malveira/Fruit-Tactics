using Core.ScriptableObjects;
using New_GameplayCore;

namespace Core.Services
{
    public interface IPreRoundPresenter
    {
        PreRoundModel BuildModel(LevelConfigSo cfg, IDeckService deck, PlayerProfileService profileService);

        void OnStartClicked();
        void OnBackClicked();
        void OnToggleUseFixedSeed(bool value);
    }
}