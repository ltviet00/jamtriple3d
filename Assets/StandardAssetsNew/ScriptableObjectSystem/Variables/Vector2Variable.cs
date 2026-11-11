using System;
using UnityEngine;

[CreateAssetMenu(menuName = "SO architect/Vector2Variable")]
public class Vector2Variable : StructVariable<Vector2> {
	public float RandomBetweenXY() {
		return UnityEngine.Random.Range(Value.x, Value.y);
	}
}
