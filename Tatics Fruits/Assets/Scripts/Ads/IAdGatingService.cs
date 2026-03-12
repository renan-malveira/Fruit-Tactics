using System;

namespace Ads
{
    public interface IAdGatingService
    {
        bool ShouldShowAds();
        event Action OnAdStatusChanged;
    }
}