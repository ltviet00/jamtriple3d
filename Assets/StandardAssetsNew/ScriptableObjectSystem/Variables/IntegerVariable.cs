using UnityEngine;
using System.Collections;
using System;

[CreateAssetMenu(menuName = "SO architect/IntegerVariable")]
public class IntegerVariable : StructVariable<int> {
	protected override bool IsNewValueDifferentFromCurrentValue(int newValue) {
		return newValue != this._value;
	}

	/// <summary> Use this to increase the value, often use in Editor, via button Unity event ... </summary>
	public void In_Add(int amount) {
		this.Value += amount;
	}

	/// <summary> Use this to decrease the value, often use in Editor, via button Unity event ... </summary>
	public void In_Subtract(int amount) {
		this.Value -= amount;
	}

	public void In_Reset() {
		this.Value = 0;
	}
}