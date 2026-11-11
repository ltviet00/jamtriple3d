using UnityEngine;
using System.Collections;

public struct QuadraticBezierCurve {
	Vector3 _begin, _middle, _end;

	public QuadraticBezierCurve(Vector3 begin, Vector3 middle, Vector3 end) {
		_begin = begin;
		_middle = middle;
		_end = end;
	}

	public Vector3 GetPointInCurve(float percent) {
		percent = Mathf.Clamp01(percent);
		var runPoint1 = Vector3.Lerp(_begin, _middle, percent);
		var runPoint2 = Vector3.Lerp(_middle, _end, percent);
		return Vector3.Lerp(runPoint1, runPoint2, percent);
	}
}