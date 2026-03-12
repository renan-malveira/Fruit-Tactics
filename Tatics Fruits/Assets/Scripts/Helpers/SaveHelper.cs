using Core.Services;
using Gameplay.Controllers;
using Managers;
using UnityEngine;

public static class SaveHelper
{
    public static void SaveAll()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SavePlayerData();
        }

        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.SaveProfile();
        }
        
        SyncToFirebase();
        Debug.Log("[SaveHelper] ✅ All save systems triggered (local + Firebase)");
    }

    public static void SaveToLocal()
    {
        SaveAll();
    }

    public static void SyncToFirebase()
    {
        var dataSaver = Object.FindFirstObjectByType<Managers.DataSaver>();
        if (dataSaver != null)
        {
            dataSaver.SaveData(force: true);
        }
    }

    public static void OnLevelComplete(int levelIndex, int coinsEarned, int score, int stars = 3)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.OnLevelComplete(levelIndex, coinsEarned, score, stars);
        }
        else
        {
            Debug.LogWarning("[SaveHelper] PlayerProfileController not found!");
        }
    }

    public static void OnPurchaseAvatar(int avatarId, int price)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.PurchaseAvatar(avatarId, price);
        }
        else
        {
            Debug.LogWarning("[SaveHelper] PlayerProfileController not found!");
        }
    }

    public static void OnPurchaseCard(string cardId, int price)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.TryPurchaseCard(cardId, price);
        }
        else
        {
            Debug.LogWarning("[SaveHelper] PlayerProfileController not found!");
        }
    }

    public static void OnSettingsChanged(bool music, bool sfx, bool vfx, string language)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.SetMusicEnabled(music);
            profileController.SetSfxEnabled(sfx);
            profileController.SetVfxEnabled(vfx);
            profileController.SetLanguage(language);
        }
    }

    public static void OnRemoveAdsPurchased()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetRemoveAds(true);
            PlayerDataManager.Instance.SavePlayerData();
        }

        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
            profileController.SetRemoveAds(true);

        Ads.AdGatingService.Instance?.NotifyStatusChanged();
    }


    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.AddGold(amount);
        }
        
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoins(amount);
        }
    }

    public static bool TrySpendCoins(int amount)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            return profileController.TrySpendGold(amount);
        }

        if (PlayerDataManager.Instance != null)
        {
            return PlayerDataManager.Instance.SpendCoins(amount);
        }

        return false;
    }

    public static int GetCoins()
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            return profileController.Data.gold;
        }

        if (PlayerDataManager.Instance != null)
        {
            return PlayerDataManager.Instance.GetCoins();
        }

        return 0;
    }

    public static void UnlockAvatar(int avatarId)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.UnlockAvatar(avatarId);
        }

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.UnlockAvatar(avatarId);
            PlayerDataManager.Instance.SavePlayerData();
        }
    }

    public static void OnDailyRewardClaimed(string dayKey, int rewardCoins)
    {
        var dataSaver = Object.FindFirstObjectByType<Managers.DataSaver>();
        if (dataSaver != null)
        {
            dataSaver.UpdateDailyLogin(dayKey);
        }
        
        Debug.Log($"[SaveHelper] ✅ Daily reward claimed - Day: {dayKey}, Coins: {rewardCoins}");
    }

    public static void OnDeckChanged(System.Collections.Generic.List<string> equippedDeck)
    {
        var profileController = Object.FindFirstObjectByType<PlayerProfileController>();
        if (profileController != null)
        {
            profileController.SaveProfile();
        }
        
        Debug.Log($"[SaveHelper] ✅ Deck saved with {equippedDeck.Count} cards");
    }

    public static void LogSaveSystemStatus()
    {
        Debug.Log("========== SAVE SYSTEM STATUS ==========");
        Debug.Log($"PlayerDataManager: {(PlayerDataManager.Instance != null ? "✅" : "❌")}");
        Debug.Log($"PlayerProfileController: {(Object.FindFirstObjectByType<PlayerProfileController>() != null ? "✅" : "❌")}");
        Debug.Log($"DataSaver (Firebase): {(Object.FindFirstObjectByType<Managers.DataSaver>() != null ? "✅" : "❌")}");
        
        var dataSaver = Object.FindFirstObjectByType<Managers.DataSaver>();
        if (dataSaver != null && dataSaver.dataToSave != null)
        {
            Debug.Log($"  └─ Firebase Coins: {dataSaver.dataToSave.totalCoins}");
            Debug.Log($"  └─ Firebase Level: {dataSaver.dataToSave.crrLevel}");
        }
        
        Debug.Log($"Coins (from helpers): {GetCoins()}");
        Debug.Log("========================================");
    }
}
