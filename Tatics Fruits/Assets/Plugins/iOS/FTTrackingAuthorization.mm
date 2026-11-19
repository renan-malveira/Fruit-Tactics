// App Tracking Transparency bridge for Fruit Tactics.
// C# side: Assets/Scripts/Core/Privacy/TrackingAuthorization.cs
// The AppTrackingTransparency framework is linked by StoreReleaseSetup (iOS post-process).

#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

typedef void (*FTTrackingCallback)(int status);

extern "C" {

int FT_GetTrackingAuthorizationStatus()
{
    if (@available(iOS 14, *)) {
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    }
    return 3; // authorized: ATT does not exist before iOS 14
}

void FT_RequestTrackingAuthorization(FTTrackingCallback callback)
{
    if (@available(iOS 14, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            dispatch_async(dispatch_get_main_queue(), ^{
                if (callback != NULL) callback((int)status);
            });
        }];
        return;
    }

    if (callback != NULL) callback(3);
}

}
