using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

///<summary> Follow setup https://firebase.google.com/docs/unity/setup </summary>
public class FirebaseCheckFixDependencies : MonoBehaviour {
	#region Init, config
	[Header("Event out")]
	[SerializeField] GameEvent firebaseReadyEvent;

	[Header("Validation")]
	[SerializeField] bool isFailedConfig;
	void OnValidate() {
		isFailedConfig = this.CheckNullAllSerializedFields();
	}

	void Start() {
        if(isFailedConfig)
            return;
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if(dependencyStatus == Firebase.DependencyStatus.Available) {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                var app = Firebase.FirebaseApp.DefaultInstance;
                // Set a flag here to indicate whether Firebase is ready to use by your app.
                Log.Info("--- Firebase is ready to use");
                firebaseReadyEvent.Raise();
            }
            else {
                UnityEngine.Debug.LogError(System.String.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        },
        // use this to let it back to mainThread when finish
        TaskScheduler.FromCurrentSynchronizationContext());
    }
	#endregion// Init config
}
