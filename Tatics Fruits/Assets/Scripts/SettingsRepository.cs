using System.IO;

public static class SettingsRepository
{
    private const string FileName = "settings.json";
    private static GameSettingsModel _cache;

    /// <summary>
    /// true quando as configurações vieram do settings.json (o jogador já tem escolhas salvas).
    /// false na primeira abertura: o Localizer usa o idioma do aparelho.
    /// </summary>
    public static bool LoadedFromDisk { get; private set; }

    public static GameSettingsModel Get()
    {
        if (_cache != null)
            return _cache;

        LoadedFromDisk = JsonDataService.TryLoad(FileName, out _cache) && _cache != null;
        if (!LoadedFromDisk)
            _cache = new GameSettingsModel();
        return _cache;
    }

    /// <summary>Esquece as configurações em memória (usado na exclusão de conta).</summary>
    public static void ClearCache() => _cache = null;

    public static void Save(GameSettingsModel model = null)
    {
        if (model != null)
            _cache = model;
        JsonDataService.Save(FileName, _cache ?? new GameSettingsModel());
    }
}