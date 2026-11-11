/* using Facebook.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// https://developers.facebook.com/docs/unity/reference/current/FB.ActivateApp/
/// Log event when user Launch app, and to ground. To measure playing duration ...
/// </summary>
public class FBSendActiveAppEvent : MonoBehaviour {

    void Start() {
        // this component need to dontdestroy, to log the AppliationPause event later
        DontDestroyOnLoad(gameObject);
        // check init to call
        if(FB.IsInitialized) {
            FB.ActivateApp();
        }
        else {
            //Handle FB.Init
            FB.Init(() => {
                FB.ActivateApp();
            });
        }
    }

    void OnApplicationPause(bool pauseStatus) {
        // Check the pauseStatus to see if we are in the foreground
        // or background
        if(!pauseStatus) {
            //app resume
            if(FB.IsInitialized) {
                FB.ActivateApp();
            }
            else {
                //Handle FB.Init
                FB.Init(() => {
                    FB.ActivateApp();
                });
            }
        }
    }
}
 */