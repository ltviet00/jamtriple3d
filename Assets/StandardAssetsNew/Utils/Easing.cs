using System.Collections.Generic;
using UnityEngine;

/* 
 * Functions taken from Tween.js - Licensed under the MIT license
 * at https://github.com/sole/tween.js
 */
public class Easing {
	#region Cache
	public enum TweenType {
		linear = 0, easeOutQuad, easeInQuad, easeInOutQuad, easeInCubic, easeOutCubic, easeInOutCubic, easeInQuart, easeOutQuart, easeInOutQuart,
		easeInQuint, easeOutQuint, easeInOutQuint, easeInSine, easeOutSine, easeInOutSine, easeInExpo, easeOutExpo, easeInOutExpo, easeInCirc, easeOutCirc, easeInOutCirc, easeInBounce, easeOutBounce, easeInOutBounce, easeInBack, easeOutBack, easeInOutBack, easeInElastic, easeOutElastic, easeInOutElastic
	}

	// declare the delegate
	public delegate float FloatDelegate(float k);
	/// <summary> The cache delegate to all tween functions </summary>
	static Dictionary<TweenType, FloatDelegate> s_cacheTweenFunction = null;

	public static FloatDelegate FindTweenFunc(TweenType tweenType) {
		if(s_cacheTweenFunction == null) {
			s_cacheTweenFunction = new Dictionary<TweenType, FloatDelegate>();
			// linear
			s_cacheTweenFunction.Add(TweenType.linear, Linear);
			// quad
			s_cacheTweenFunction.Add(TweenType.easeInQuad, Quadratic.In);
			s_cacheTweenFunction.Add(TweenType.easeOutQuad, Quadratic.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutQuad, Quadratic.InOut);
			// cubic
			s_cacheTweenFunction.Add(TweenType.easeInCubic, Cubic.In);
			s_cacheTweenFunction.Add(TweenType.easeOutCubic, Cubic.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutCubic, Cubic.InOut);
			// Quart
			s_cacheTweenFunction.Add(TweenType.easeInQuart, Quartic.In);
			s_cacheTweenFunction.Add(TweenType.easeOutQuart, Quartic.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutQuart, Quartic.InOut);
			// Quint
			s_cacheTweenFunction.Add(TweenType.easeInQuint, Quintic.In);
			s_cacheTweenFunction.Add(TweenType.easeOutQuint, Quintic.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutQuint, Quintic.InOut);
			// Sine
			s_cacheTweenFunction.Add(TweenType.easeInSine, Sinusoidal.In);
			s_cacheTweenFunction.Add(TweenType.easeOutSine, Sinusoidal.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutSine, Sinusoidal.InOut);
			// Expo
			s_cacheTweenFunction.Add(TweenType.easeInExpo, Exponential.In);
			s_cacheTweenFunction.Add(TweenType.easeOutExpo, Exponential.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutExpo, Exponential.InOut);
			// Circ
			s_cacheTweenFunction.Add(TweenType.easeInCirc, Circular.In);
			s_cacheTweenFunction.Add(TweenType.easeOutCirc, Circular.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutCirc, Circular.InOut);
			// bouncing
			s_cacheTweenFunction.Add(TweenType.easeInBounce, Bounce.In);
			s_cacheTweenFunction.Add(TweenType.easeOutBounce, Bounce.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutBounce, Bounce.InOut);
			// Back
			s_cacheTweenFunction.Add(TweenType.easeInBack, Back.In);
			s_cacheTweenFunction.Add(TweenType.easeOutBack, Back.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutBack, Back.InOut);
			// Elastic
			s_cacheTweenFunction.Add(TweenType.easeInElastic, Elastic.In);
			s_cacheTweenFunction.Add(TweenType.easeOutElastic, Elastic.Out);
			s_cacheTweenFunction.Add(TweenType.easeInOutElastic, Elastic.InOut);
		}
		// not found, then return Linear
		if(!s_cacheTweenFunction.ContainsKey(tweenType)) {
			Log.Warning(false, $"Not cache function for {tweenType}, return Linear");
			return s_cacheTweenFunction[TweenType.linear];
		}
		return s_cacheTweenFunction[tweenType];
	}

	public static float EaseFloat(float from, float to, float k, TweenType tween) {
		var tweenFunc = FindTweenFunc(tween);
		k = Mathf.Clamp01(k);
		var ratio = tweenFunc(k);
		var result = Mathf.Lerp(from, to, ratio);
		return result;
	}

	public static Vector3 EaseVector3(Vector3 from, Vector3 to, float k, TweenType tween) {
		var tweenFunc = FindTweenFunc(tween);
		k = Mathf.Clamp01(k);
		var ratio = tweenFunc(k);
		var result = Vector3.Lerp(from, to, ratio);
		return result;
	}
	#endregion//Cache


	#region Function
	public static float Linear(float k) {
		return k;
	}

	public class Quadratic {
		public static float In(float k) {
			return k * k;
		}

		public static float Out(float k) {
			return k * (2f - k);
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return 0.5f * k * k;
			return -0.5f * ((k -= 1f) * (k - 2f) - 1f);
		}
	};

	public class Cubic {
		public static float In(float k) {
			return k * k * k;
		}

		public static float Out(float k) {
			return 1f + ((k -= 1f) * k * k);
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return 0.5f * k * k * k;
			return 0.5f * ((k -= 2f) * k * k + 2f);
		}
	};

	public class Quartic {
		public static float In(float k) {
			return k * k * k * k;
		}

		public static float Out(float k) {
			return 1f - ((k -= 1f) * k * k * k);
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return 0.5f * k * k * k * k;
			return -0.5f * ((k -= 2f) * k * k * k - 2f);
		}
	};

	public class Quintic {
		public static float In(float k) {
			return k * k * k * k * k;
		}

		public static float Out(float k) {
			return 1f + ((k -= 1f) * k * k * k * k);
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return 0.5f * k * k * k * k * k;
			return 0.5f * ((k -= 2f) * k * k * k * k + 2f);
		}
	};

	public class Sinusoidal {
		public static float In(float k) {
			return 1f - Mathf.Cos(k * Mathf.PI / 2f);
		}

		public static float Out(float k) {
			return Mathf.Sin(k * Mathf.PI / 2f);
		}

		public static float InOut(float k) {
			return 0.5f * (1f - Mathf.Cos(Mathf.PI * k));
		}
	};

	public class Exponential {
		public static float In(float k) {
			return Mathf.Approximately(k, 0f) ? 0f : Mathf.Pow(1024f, k - 1f);
		}

		public static float Out(float k) {
			return Mathf.Abs(k - 1f) < float.Epsilon ? 1f : 1f - Mathf.Pow(2f, -10f * k);
		}

		public static float InOut(float k) {
			if(Mathf.Approximately(k, 0f)) return 0f;
			if(Mathf.Approximately(k, 1f)) return 1f;
			if((k *= 2f) < 1f) return 0.5f * Mathf.Pow(1024f, k - 1f);
			return 0.5f * (-Mathf.Pow(2f, -10f * (k - 1f)) + 2f);
		}
	};

	public class Circular {
		public static float In(float k) {
			return 1f - Mathf.Sqrt(1f - k * k);
		}

		public static float Out(float k) {
			return Mathf.Sqrt(1f - ((k -= 1f) * k));
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return -0.5f * (Mathf.Sqrt(1f - k * k) - 1);
			return 0.5f * (Mathf.Sqrt(1f - (k -= 2f) * k) + 1f);
		}
	};

	public class Elastic {
		public static float In(float k) {
			if(Mathf.Approximately(k, 0f)) return 0;
			if(Mathf.Approximately(k, 1f)) return 1;
			return -Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f);
		}

		public static float Out(float k) {
			if(Mathf.Approximately(k, 0f)) return 0;
			if(Mathf.Approximately(k, 1f)) return 1;
			return Mathf.Pow(2f, -10f * k) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) + 1f;
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return -0.5f * Mathf.Pow(2f, 10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f);
			return Mathf.Pow(2f, -10f * (k -= 1f)) * Mathf.Sin((k - 0.1f) * (2f * Mathf.PI) / 0.4f) * 0.5f + 1f;
		}
	};

	public class Back {
		static float s = 1.70158f;
		static float s2 = 2.5949095f;

		public static float In(float k) {
			return k * k * ((s + 1f) * k - s);
		}

		public static float Out(float k) {
			return (k -= 1f) * k * ((s + 1f) * k + s) + 1f;
		}

		public static float InOut(float k) {
			if((k *= 2f) < 1f) return 0.5f * (k * k * ((s2 + 1f) * k - s2));
			return 0.5f * ((k -= 2f) * k * ((s2 + 1f) * k + s2) + 2f);
		}
	};

	public class Bounce {
		public static float In(float k) {
			return 1f - Out(1f - k);
		}

		public static float Out(float k) {
			if(k < (1f / 2.75f)) {
				return 7.5625f * k * k;
			}
			else if(k < (2f / 2.75f)) {
				return 7.5625f * (k -= (1.5f / 2.75f)) * k + 0.75f;
			}
			else if(k < (2.5f / 2.75f)) {
				return 7.5625f * (k -= (2.25f / 2.75f)) * k + 0.9375f;
			}
			else {
				return 7.5625f * (k -= (2.625f / 2.75f)) * k + 0.984375f;
			}
		}

		public static float InOut(float k) {
			if(k < 0.5f) return In(k * 2f) * 0.5f;
			return Out(k * 2f - 1f) * 0.5f + 0.5f;
		}
	};
	#endregion// Function


	#region Utils
	///<summary> Do tween function to find float between from - to, using tweenFunc from TweenType, + ratio </summary>
	public static float Tween(float from, float to, TweenType tweenType, float ratio) {
		var tweenFunc = FindTweenFunc(tweenType);
		return tweenFunc(ratio) * (to - from) + from;
	}

	///<summary> Do tween function to find point between from - to, using tweenFunc from TweenType, + ratio </summary>
	public static Vector2 Tween(Vector2 from, Vector2 to, TweenType tweenType, float ratio) {
		var tweenFunc = FindTweenFunc(tweenType);
		return Vector(from, to, tweenFunc, ratio);
	}

	static Vector2 Vector(Vector2 from, Vector2 to, FloatDelegate tweenFunc, float ratio) {
		var result = new Vector2();
		var easingValue = tweenFunc(ratio);
		result.x = easingValue * (to.x - from.x) + from.x;
		result.y = easingValue * (to.y - from.y) + from.y;
		return result;
	}
	#endregion//Utils
}