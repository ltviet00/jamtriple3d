using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public static class AdmobIosAttBridge {
	public static void RequestTrackingAuthorization(float timeoutSeconds, Action onFinished) {
#if UNITY_IOS && !UNITY_EDITOR
		var runner = AdmobRuntimeRunner.Instance;
		runner.StartCoroutine(RequestTrackingAuthorizationRoutine(timeoutSeconds, onFinished));
#else
		onFinished?.Invoke();
#endif
	}

#if UNITY_IOS && !UNITY_EDITOR
	static IEnumerator RequestTrackingAuthorizationRoutine(float timeoutSeconds, Action onFinished) {
		var bindingType = Type.GetType("Unity.Advertisement.IosSupport.ATTrackingStatusBinding, Unity.Advertisement.IosSupport");
		if(bindingType == null) {
			Log.Warning(false, "ATT package is missing. Install Unity iOS 14 Advertising Support if you need ATT prompt.");
			onFinished?.Invoke();
			yield break;
		}

		var getStatus = bindingType.GetMethod("GetAuthorizationTrackingStatus", BindingFlags.Public | BindingFlags.Static);
		var request = bindingType.GetMethod("RequestAuthorizationTracking", BindingFlags.Public | BindingFlags.Static);
		if(getStatus == null || request == null) {
			onFinished?.Invoke();
			yield break;
		}

		var status = Convert.ToInt32(getStatus.Invoke(null, null));
		if(status != 0) {
			onFinished?.Invoke();
			yield break;
		}

		request.Invoke(null, null);
		var timer = Mathf.Max(1f, timeoutSeconds);
		while(timer > 0f) {
			yield return null;
			timer -= Time.unscaledDeltaTime;
			status = Convert.ToInt32(getStatus.Invoke(null, null));
			if(status != 0) {
				break;
			}
		}
		onFinished?.Invoke();
	}
#endif
}

public sealed class AdmobRuntimeRunner : MonoBehaviour {
	static AdmobRuntimeRunner _instance;

	public static AdmobRuntimeRunner Instance {
		get {
			if(_instance != null) {
				return _instance;
			}
			var go = new GameObject("[AdmobRuntimeRunner]");
			UnityEngine.Object.DontDestroyOnLoad(go);
			_instance = go.AddComponent<AdmobRuntimeRunner>();
			return _instance;
		}
	}
}
