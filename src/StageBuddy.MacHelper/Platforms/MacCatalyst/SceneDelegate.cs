using Foundation;

namespace StageBuddy.MacHelper
{
    // Registered in Info.plist (UIApplicationSceneManifest). Apps built with the iOS/Mac Catalyst 27 SDK
    // must use the UIKit scene lifecycle or they crash at launch on OS 27+.
    [Register("SceneDelegate")]
    public class SceneDelegate : MauiUISceneDelegate
    {
    }
}
