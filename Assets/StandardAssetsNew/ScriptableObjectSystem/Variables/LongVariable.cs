using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "SO architect/LongVariable")]
public class LongVariable : StructVariable<long> {
	protected override bool IsNewValueDifferentFromCurrentValue(long newValue) {
		return newValue != this._value;
	}
}
