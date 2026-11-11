// ----------------------------------------------------------------------------
// Unite 2017 - Game Architecture with Scriptable Objects
// 
// Author: Ryan Hipple
// Date:   10/04/17
// ----------------------------------------------------------------------------

// the developerDescription in private will thrown a warning. We dont need that,
// we just need a description to read in Inspector, and dont want any class access it.
// So disable this warning
#pragma warning disable 0414
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "SO architect/FloatVariable")]
public class FloatVariable : StructVariable<float> {
	protected override bool IsNewValueDifferentFromCurrentValue(float newValue) {
		return newValue.IsNotEqual(_value);
	}
}
#pragma warning restore 0414