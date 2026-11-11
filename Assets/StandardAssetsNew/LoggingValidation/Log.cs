#if UNITY_EDITOR
#define USE_LOG
#define ASSERT
#define LOG_LEVEL_DEBUG
#endif
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


public class Log {
	//---------------------------------
	//----------- Static, define
	//---------------------------------
	public enum LogLevel {
		//only error log will be show
		ERROR = 0,
		// show Warning and Error log only
		WARNING,
		// this's the highest level, will show all type of Log
		DEBUG,
	}

	#if LOG_LEVEL_ERROR
	public static LogLevel logLevel = LogLevel.ERROR;
	#elif LOG_LEVEL_WARNING
	public static LogLevel logLevel = LogLevel.WARNING;
	#elif LOG_LEVEL_DEBUG
	public static LogLevel logLevel = LogLevel.DEBUG;
	#else
	/// Default is the highest level DEBUG
	public static LogLevel logLevel = LogLevel.DEBUG;
	#endif


	static bool IsDebug() {
		return logLevel >= LogLevel.DEBUG;
	}
	static bool IsWarning() {
		return logLevel >= LogLevel.WARNING;
	}
	static bool IsError() {
		return logLevel >= LogLevel.ERROR;
	}

	// There're some types of Log() we often use (base on args) :
	// 1. Just string message
	// 2. message + Context (to ping it on hierachy)
	// 3. message which format
	// 4. message format + context
	// 5. message + MonoBehaviour info (script name, object name)
	// 6. message format + MonoBehaviour info
	// So we crreate n * overload funcs for Info(), Warning(), Error()

	#region Info
	//-------------------------------
	//------------ Info ----------
	//-------------------------------

	[Conditional("USE_LOG")]
	public static void Info(string message) {
		if(!IsDebug()) return;
		Debug.Log(message);
	}

	[Conditional("USE_LOG")]
	public static void Info(string message, Object context) {
		if(!IsDebug()) return;
		Debug.Log(message, context);
	}

	[Conditional("USE_LOG")]
	public static void Info(string format, params object[] args) {
		if(!IsDebug()) return;
		Debug.LogFormat(format, args);
	}

	[Conditional("USE_LOG")]
	public static void Info(Object context, string format, params object[] args) {
		if(!IsDebug()) return;
		Debug.LogFormat(context, format, args);
	}

	[Conditional("USE_LOG")]
	public static void Info(MonoBehaviour script, string message) {
		if(!IsDebug()) return;
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.Log(msgFormatted, script.gameObject);
	}

	[Conditional("USE_LOG")]
	public static void Info(MonoBehaviour script, string format, params object[] args) {
		if(!IsDebug()) return;
		var message = string.Format(format, args);
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.Log(msgFormatted, script.gameObject);
	}

	[Conditional("USE_LOG")]
	public static void Info(ScriptableObject asset, string message) {
		if(!IsDebug())
			return;
		var msgFormatted = CreateObjectInfoString(message, asset);
		Debug.Log(msgFormatted, asset);
	}

	[Conditional("USE_LOG")]
	public static void Info(ScriptableObject asset, string format, params object[] args) {
		if(!IsDebug())
			return;
		var message = string.Format(format, args);
		var msgFormatted = CreateObjectInfoString(message, asset);
		Debug.Log(msgFormatted, asset);
	}

	//-------------------------------
	//------------ Info with framecount
	//-------------------------------

	[Conditional("USE_LOG")]
	public static void InfoF(string message, Object context = null) {
		if(!IsDebug()) return;
		var text = CreateObjectInfoStringWithFrameCount(message, context);
		Debug.Log(text, context);
	}
	#endregion


	#region Warning
	//-------------------------------
	//------------ Warning ----------
	//-------------------------------

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, string message) {
		if(!IsWarning()) return;
		if(condition) return;
		Debug.LogWarning(message);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, string message, Object context) {
		if(!IsWarning()) return;
		if(condition) return;
		var messageWithobjectInfo = CreateObjectInfoString(message, context);
		Debug.LogWarning(messageWithobjectInfo, context);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, string format, params object[] args) {
		if(!IsWarning()) return;
		if(condition) return;
		Debug.LogWarningFormat(format, args);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, Object context, string format, params object[] args) {
		if(!IsWarning()) return;
		if(condition) return;
		var baseMessage = string.Format(format, args);
		var finalMessage = CreateObjectInfoString(baseMessage, context);
		Debug.LogWarning(finalMessage, context);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, MonoBehaviour script, string message) {
		if(!IsWarning()) return;
		if(condition) return;
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.LogWarning(msgFormatted, script.gameObject);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, MonoBehaviour script, string format, params object[] args) {
		if(!IsWarning()) return;
		if(condition) return;
		var message = string.Format(format, args);
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.LogWarning(msgFormatted, script.gameObject);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, ScriptableObject asset, string message) {
		if(!IsWarning())
			return;
		if(condition)
			return;
		var msgFormatted = CreateObjectInfoString(message, asset);
		Debug.LogWarning(msgFormatted, asset);
	}

	[Conditional("USE_LOG")]
	public static void Warning(bool condition, ScriptableObject script, string format, params object[] args) {
		if(!IsWarning())
			return;
		if(condition)
			return;
		var message = string.Format(format, args);
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.LogWarning(msgFormatted, script);
	}

	//-------------------------------
	//------------ Warning with FrameCount
	//-------------------------------

	[Conditional("USE_LOG")]
	public static void WarningF(bool condition, string message, Object context) {
		if(!IsWarning()) return;
		if(condition) return;
		var text = CreateObjectInfoStringWithFrameCount(message, context);
		Debug.LogWarning(text);
	}
	#endregion


	#region Error
	//-------------------------------
	//------------ Error ----------
	//-------------------------------

	[Conditional("USE_LOG")]
	public static void Error(bool condition, string message) {
		if(!IsError()) return;
		if(condition) return;
		Debug.LogError(message);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, string message, Object context) {
		if(!IsError()) return;
		if(condition) return;
		Debug.LogError(message, context);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, string format, params object[] args) {
		if(!IsError()) return;
		if(condition) return;
		Debug.LogErrorFormat(format, args);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, Object context, string format, params object[] args) {
		if(!IsError()) return;
		if(condition) return;
		Debug.LogErrorFormat(context, format, args);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, MonoBehaviour script, string message) {
		if(!IsError()) return;
		if(condition) return;
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.LogError(msgFormatted, script.gameObject);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, MonoBehaviour script, string format, params object[] args) {
		if(!IsError()) return;
		if(condition) return;
		var message = string.Format(format, args);
		var msgFormatted = CreateObjectInfoString(message, script);
		Debug.LogError(msgFormatted, script.gameObject);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, ScriptableObject asset, string message) {
		if(!IsError())
			return;
		if(condition)
			return;
		var msgFormatted = CreateObjectInfoString(message, asset);
		Debug.LogError(msgFormatted, asset);
	}

	[Conditional("USE_LOG")]
	public static void Error(bool condition, ScriptableObject asset, string format, params object[] args) {
		if(!IsError())
			return;
		if(condition)
			return;
		var message = string.Format(format, args);
		var msgFormatted = CreateObjectInfoString(message, asset);
		Debug.LogError(msgFormatted, asset);
	}

	//-------------------------------
	//------------ Error with FrameCount
	//-------------------------------

	[Conditional("USE_LOG")]
	public static void ErrorF(bool condition, string message, Object context = null) {
		if(!IsError()) return;
		if(condition) return;
		var text = CreateObjectInfoStringWithFrameCount(message, context);
		Debug.LogError(text, context);
	}
	#endregion


	#region Validate
	//-------------------------------
	//------------ Error ----------
	//-------------------------------

	/// <summary> If the condition dont meet, then throw a warning message. </summary>
	[Conditional("USE_LOG")]
	public static void ValidateNotNull(object objToValidate, UnityEngine.Object target, string message) {
		if(objToValidate == null) {
			const string FORMAT = "Null:[{0}]";
			var msg = string.Format(FORMAT, message);
			var finalLog = CreateObjectInfoString(msg, target);
			Debug.LogWarning(finalLog, target);

		}
	}
	#endregion//Validate


	#region Static private
	///<summary> Create a message that contain script name, object, root name, scene name (if it not a prefab) </summary>
	static string CreateObjectInfoString(string message, UnityEngine.Object target) {
		string result;
		if(target is Component) {
			var castToMono = target as Component;
			var sceneName = castToMono.gameObject.scene.name;
			// if scene name == null mean this log come from a component inside a prefab. Let show prefab name
			if(string.IsNullOrEmpty(sceneName)) {
				var rootParentName = castToMono.transform.root.name;
				result = $"[{message}], object[{castToMono.name}], script[{castToMono.GetType()}], prefab[{rootParentName}]";
			}
			else {
				result = $"[{message}], object[{castToMono.name}], script[{castToMono.GetType()}], scene[{castToMono.gameObject.scene.name}]";
			}
		}
		// not Component, maybe ScriptableObject or something
		else {
			result = $"[{message}], object[{target?.name}], script[{target?.GetType()}]";
		}
		return result;
	}

	///<summary> Create a message that contain script name, object, root name, scene name (if it not a prefab), and frameCount</summary>
	static string CreateObjectInfoStringWithFrameCount(string message, UnityEngine.Object target) {
		string result;
		if(target is Component) {
			var castToMono = target as Component;
			var sceneName = castToMono.gameObject.scene.name;
			// if scene name == null mean this log come from a component inside a prefab. Let show prefab name
			if(string.IsNullOrEmpty(sceneName)) {
				var rootParentName = castToMono.transform.root.name;
				result = $"[{message}], object[{castToMono.name}], script[{castToMono.GetType()}], prefab[{rootParentName}], at frame [{Time.frameCount}]";
			}
			else {
				result = $"[{message}], object[{castToMono.name}], script[{castToMono.GetType()}], scene[{castToMono.gameObject.scene.name}], at frame [{Time.frameCount}]";
			}
		}
		// null not Component, maybe ScriptableObject or something
		else {
			result = $"[{message}], object[{target?.name}], script[{target?.GetType()}], at frame [{Time.frameCount}]";
		}
		return result;
	}
	#endregion//static private
}



//---------------------------------------------
//------------- Assert ------------------------

/// Thown an exception if condition = false
//[Conditional("ASSERT")]
//public static void Assert(bool condition) {
//	if(!condition) throw new UnityException();
//}

///// Thown an exception if condition = false, show message on console's log
//[Conditional("ASSERT")]
//public static void Assert(bool condition, string message) {
//	if(!condition) throw new UnityException(message);
//}

///// Thown an exception if condition = false, show message on console's log
//[Conditional("ASSERT")]
//public static void Assert(bool condition, string format, params object[] args) {
//	if(!condition) throw new UnityException(string.Format(format, args));
//}

///// Throw exception and also hightlight object in Hierarchy windows
//[Conditional("ASSERT")]
//public static void Assert(bool condition, Object context = null) {
//	if(!condition) {
//		// use LogWarning instead of LogError : dont show too many "red" color debug console text
//		if(context != null) Debug.LogWarning("Error from this object !!!", context);
//		throw new UnityException();
//	}
//}

///// Throw exception and also hightlight object in Hierarchy windows
//[Conditional("ASSERT")]
//public static void Assert(bool condition, string message, Object context) {
//	if(!condition) {
//		// use LogWarning instead of LogError : dont show too many "red" color debug console text
//		if(context != null) Debug.LogWarning("Error from this object !!! : " + message, context);
//		throw new UnityException(message);
//	}
//}
