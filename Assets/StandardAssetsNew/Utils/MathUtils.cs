using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MathUtils {

	#region Float
	//------------------------------
	//------------ float
	//------------------------------

	public static bool IsEqual(this float first, float second, float accuracy = float.Epsilon) {
		return Mathf.Abs(first - second) < accuracy;
	}

	public static bool IsNotEqual(this float first, float second, float accuracy = float.Epsilon) {
		return !IsEqual(first, second, accuracy);
	}

	public static float Sqr(this float number) {
		return number * number;
	}

	public static float Sqrt(this float number) {
		return Mathf.Sqrt(number);
	}

	public static float Abs(this float number) {
		return Mathf.Abs(number);
	}

	/// Determine the number is between left and right segment
	public static bool IsBetween(this float number, float left, float right) {
		// swap to make sure left < right
		if(left > right) {
			var temp = left;
			left = right;
			right = temp;
		}
		return number >= left && number <= right;
	}
	#endregion// Float


	#region Vector2
	//------------------------------
	//------------ vector2
	//------------------------------

	public static bool IsEqual(this Vector2 first, Vector2 second, float accuracy = float.Epsilon) {
		return first.x.IsEqual(second.x, accuracy)
		&& first.y.IsEqual(second.y, accuracy);
	}

	public static bool IsNotEqual(this Vector2 first, Vector2 second, float accuracy = float.Epsilon) {
		return !first.x.IsEqual(second.x, accuracy)
		|| !first.y.IsEqual(second.y, accuracy);
	}

	public static bool IsLengthEqual(this Vector2 first, Vector2 second, float accuracy = float.Epsilon) {
		return first.sqrMagnitude.IsEqual(second.sqrMagnitude, accuracy);
	}

	public static bool IsLonger(this Vector2 vector, float length) {
		return vector.sqrMagnitude > length * length;
	}

	public static bool IsAnyCoordinateBiggerThan(this Vector2 vector, float length) {
		return vector.x > length || vector.y > length;
	}

	public static Vector3 ToVector3(this Vector2 vector, float z = 0f) {
		return new Vector3(vector.x, vector.y, z);
	}

	/// Modify the position by add a (-randomX, randomX) to Xcoor and (-randomY, randomY) to Ycoor
	public static Vector2 RandomTranslate(this Vector2 vector, float randomX, float randomY) {
		var result = new Vector2(vector.x, vector.y);
		result.x += Random.Range(-randomX, randomX);
		result.y += Random.Range(-randomY, randomY);
		return result;
	}
	#endregion//Vector2


	#region Vector3
	//------------------------------
	//------------ vector3
	//------------------------------

	public static bool IsEqual(this Vector3 first, Vector3 second, float accuracy = float.Epsilon) {
		return first.x.IsEqual(second.x, accuracy)
		&& first.y.IsEqual(second.y, accuracy)
		&& first.z.IsEqual(second.z, accuracy);
	}

	public static bool IsNotEqual(this Vector3 first, Vector3 second, float accuracy = float.Epsilon) {
		return !first.x.IsEqual(second.x, accuracy)
		|| !first.y.IsEqual(second.y, accuracy)
		|| !first.z.IsEqual(second.z, accuracy);
	}

	public static bool IsLengthEqual(this Vector3 first, Vector3 second, float accuracy = float.Epsilon) {
		return first.sqrMagnitude.IsEqual(second.sqrMagnitude, accuracy);
	}

	public static bool IsLonger(this Vector3 vector, float length) {
		return vector.sqrMagnitude > length * length;
	}

	public static bool IsAnyCoordinateBiggerThan(this Vector3 vector, float length) {
		return vector.x > length || vector.y > length || vector.z > length;
	}

	public static Vector2 XY(this Vector3 vector) {
		return new Vector2(vector.x, vector.y);
	}

	public static float SqrLength(this Vector3[] spline) {
		var result = 0f;
		for(int n = 0; n < spline.Length - 1; n++) {
			result += (spline[n] - spline[n + 1]).sqrMagnitude;
		}
		return result;
	}

	public static float Magnitude(this Vector3[] spline) {
		var result = 0f;
		for(int n = 0; n < spline.Length - 1; n++) {
			result += (spline[n] - spline[n + 1]).magnitude;
		}
		return result;
	}

	
	public static bool IsLonger(this Vector3[] first, Vector3[] second) {
		// if the two array have the same point count, then we can use SqrLength for better performance
		if(first.Length == second.Length) return first.SqrLength() > second.SqrLength();
		// they not same point count, then we have to use magnitude, a bit longer than SqrMagnitude
		return first.Magnitude() > second.Magnitude();
	}
	#endregion//Vector3


	#region Quaternion
	//------------------------------
	//------------ Quaternion
	//------------------------------

	public static bool IsAnyCoordinateBiggerThan(this Quaternion vector, float length) {
		return vector.x > length || vector.y > length || vector.z > length;
	}

	public static bool IsNotEqual(this Quaternion first, Quaternion second, float accuracy = float.Epsilon) {
		return !first.x.IsEqual(second.x, accuracy)
		|| !first.y.IsEqual(second.y, accuracy)
		|| !first.z.IsEqual(second.z, accuracy);
	}

	public static bool IsEqual(this Quaternion first, Quaternion second, float accuracy = float.Epsilon) {
		return first.x.IsEqual(second.x, accuracy)
		&& first.y.IsEqual(second.y, accuracy)
		&& first.z.IsEqual(second.z, accuracy);
	}

	public static Vector3 XYZ(this Quaternion quaternion) {
		var result = new Vector3();
		result.x = quaternion.x;
		result.y = quaternion.y;
		result.z = quaternion.z;
		return result;
	}
	#endregion//Quaternion


	#region Array
	//------------------------------
	//------------ Array, generic
	//------------------------------

	/// <summary>
	/// Just revert the order of the input array, NOT create new one !!! If you want new array ? then use CloneAndRevert() instead.
	/// In case you use this syntax : array1st = array2nd.Revert, then you still have only one array, but 2 reference to it (array1st, array2nd)
	/// </summary>
	public static void Revert<T>(this T[] array) {
		T temp;
		for(int n = 0, halfLength = array.Length / 2; n < halfLength; n++) {
			var swapIndex = array.Length - 1 - n;
			temp = array[n];
			array[n] = array[swapIndex];
			array[swapIndex] = temp;
		}
	}

	public static T[] Clone<T>(this T[] array) {
		var result = new T[array.Length];
		for(int n = 0; n < array.Length; n++) {
			result[n] = array[n];
		}
		return result;
	}

	public static T[] CloneAndRevert<T>(this T[] array) {
		var result = new T[array.Length];
		for(int n = 0; n < array.Length; n++) {
			result[n] = array[array.Length - 1 - n];
		}
		return result;
	}

	public static void Revert<T>(this List<T> array) {
		T temp;
		for(int n = 0, halfLength = array.Count / 2; n < halfLength; n++) {
			var swapIndex = array.Count - 1 - n;
			temp = array[n];
			array[n] = array[swapIndex];
			array[swapIndex] = temp;
		}
	}

	public static List<T> Clone<T>(this List<T> array) {
		var result = new List<T>(array.Count);
		for(int n = 0; n < array.Count; n++) {
			result.Add(array[n]);
		}
		return result;
	}

	public static List<T> CloneAndRevert<T>(this List<T> array) {
		var result = new List<T>(array.Count);
		for(int n = 0; n < array.Count; n++) {
			result[n] = array[array.Count - 1 - n];
		}
		return result;
	}

	public static T PickRandom<T>(this List<T> list) {
		var randomIndex = Random.Range(0, list.Count);
		return list[randomIndex];
	}

	///<summary> The normal remove function, would take the element in the middle, and shift the rest to one step.
	///This function simply move the item to last, and remove it, so save some performance.
	/// Use this for cases that we don't care about the order of the list </summary>
	public static void RemoveElementWithoutShifting<T>(this List<T> list, T target) where T : class {
		for(int n = 0; n < list.Count; n++) {
			var item = list[n];
			if(item == target) {
				// swap to last and remove last
				var lastItem = list[list.Count - 1];
				list[list.Count - 1] = item;
				list[n] = lastItem;
				list.RemoveAt(list.Count - 1);
				return;
			}
		}
		Log.Warning(false, $"Not found item [{target}] in the list to remove");
	}
	#endregion//Array


	#region List, generic
	//------------------------------
	//------------ List, generic
	//------------------------------

	///<summary> Let say we want a list of 10 random intergers, but none of them larger than 5, the result list will be sort like this:
	/// [0, 3, 1, 4, 2, 0, 4, 1, 3, 2], then call this function with listSize = 10, and maxValue = 5</summary>
	public static List<int> CreateRandomIntegerListWithMaxValue(int listSize, int maxValue) {
		// create a list with maxValue size, then pick a random index, swap to the last, take it out of the list.
		// that how we random pick from a list and make sure no duplicate items
		var listToPick = new List<int>(listSize);
		// first loop, alway has at least a list with maxValue size (in case the listSize < maxValue, we still need a list with maxValue to pick)
		for(int n = 0; n < maxValue; n++) {
			listToPick.Add(n);
		}
		// NOTES: if listToPick.size < resutlSize, then we add need to add more. Let loop back until we reach the desire size
		// eg: we need listSize = 7, but maxValue=4,  then must have [0, 1, 2, 3, 0, 1, 2] to pick
		while(listToPick.Count <= listSize) {
			for(int n = 0; n < maxValue; n++) {
				listToPick.Add(n);
				// check break here
				if(listToPick.Count >= listSize)
					break;
			}
		}
		// picking
		var result = new List<int>(listSize);
		for(int n = 0; n < listSize; n++) {
			// pick random
			var randomIndex = Random.Range(0, listToPick.Count);
			var lastItemIndex = listToPick.Count - 1;
			var valueAtRandomIndex = listToPick[randomIndex];
			result.Add(valueAtRandomIndex);
			// swap to last, and remove it out of the list to pick
			listToPick[randomIndex] = listToPick[lastItemIndex];
			listToPick.RemoveAt(lastItemIndex);
		}
		return result;
	}

	public static List<T> CloneAndDoubleList<T>(List<T> list) {
		var result = new List<T>(list.Count * 2);
		for(int n = 0; n < list.Count; n++) {
			var original = list[n];
			result.Add(original);
			result.Add(original);
			// NOTES: the list is empty, should Add, not direct assign like this
			//result[n * 2] original;
			//result[n * 2 + 1] = original;
		}
		return result;
	}

	/// <summary> Loop from 0 -> (count-1), pick a random index from (n+1 - count), swap to it </summary>
	public static void ShuffleList<T>(List<T> list) {
		for(int n = 0; n < list.Count - 1; n++) {
			// pick random index to swap to
			var randomPickIndex = Random.Range(n + 1, list.Count);
			// swap
			var temp = list[n];
			list[n] = list[randomPickIndex];
			list[randomPickIndex] = temp;
		}
	}

	///<summary> Input [0,1,2,3,4,5], amount = 3 then the result is a list [3], random pick from original [1,4,5] </summary>
	public static List<T> CreateSubListWithRandomElement<T>(List<T> originalList, int amount) {
		if(amount > originalList.Count) {
			Log.Error(false, $"Cannot create sublist with size[{amount}] > original size [{originalList.Count}]");
			return null;
		}
		if(amount == originalList.Count) {
			Log.Info($"Request sublist size = original size [{amount}], so just return original list");
			return originalList;
		}
		var result = new List<T>(amount);
		var indexList = CreateRandomIntegerListWithMaxValue(amount, originalList.Count);
		for(int n = 0; n < amount; n++) {
			var index = indexList[n];
			result.Add(originalList[index]);
		}
		return result;
	}

	///<summary> Input [0,1,2,3,4,5], amount = 3 then the result is a list [3], random pick from original [1,4,5], and the original list remain [0,2,3] </summary>
	public static List<T> TakeOutSubListWithRandomElement<T>(List<T> originalList, int takeOutAmount) {
		if(takeOutAmount > originalList.Count) {
			Log.Error(false, $"Cannot create sublist with size[{takeOutAmount}] > original size [{originalList.Count}]");
			return null;
		}
		var result = new List<T>(takeOutAmount);
		// select random item, swap it to last of list, take it out, add to result, this better performance
		for(int n = 0; n < takeOutAmount; n++) {
			var randomIndex = Random.Range(0, originalList.Count);
			// swap to last
			var temp = originalList[randomIndex];
			originalList[randomIndex] = originalList[originalList.Count - 1];
			originalList[originalList.Count - 1] = temp;
			// take it out
			result.Add(temp);
			originalList.RemoveAt(originalList.Count - 1);
		}
		return result;
	}

	///<summary> Want a list of 4 integer whose sum = 100, then input = 4, sum = 100, result somthing like this [20, 35, 15, 30].
	///More about the algorithm in this thread  https://stackoverflow.com/questions/2640053/getting-n-random-numbers-whose-sum-is-m/2640067 </summary>
	public static int[] CreateRandomIntegerListWhoseSumIsDefined(int number, int targetSum) {
		var result = new int[number];
		int sum = 0;
		// carefully check. Then just fill zero for array
		if(targetSum == 0) {
			for(int n = 0; n < result.Length; n++) {
				result[n] = 0;
			}
			return result;
		}
		// edge case : targetSum < number. Eg: int[5], but sum = 3, then [1,1,1,0,0]
		if(targetSum <= number) {
			for(int n = 0; n < number; n++) {
				result[n] = 1;
			}
			return result;
		}
		// first loop, random result
		for(int n = 0; n < number; n++) {
			// let just temp pick this range
			result[n] = Random.Range(targetSum, targetSum * 3);
			sum += result[n];
		}
		// 2nd loop, normalize them to get the targetSum
		for(int n = 0; n < number; n++) {
			var normalize = result[n];
			result[n] = (int)(((float)normalize / (float)sum) * targetSum);
		}
		// careful a bit, if sum of first [n-1] not = targetSum, then add up for the last one
		sum = 0;
		for(int n = 0; n < number - 1; n++) {
			sum += result[n];
		}
		result[number - 1] = targetSum - sum;
		return result;
	}

	///<summary> Turn [3,4,5,6] to [6,3,4,5] </summary>
	public static void BringLastToFirst<T>(this List<T> list) {
		var last = list[list.Count - 1];
		for(int n = list.Count - 2; n >= 0; n--) {
			list[n + 1] = list[n];
		}
		list[0] = last;
	}

	///<summary> The Hashset -> TOList is LINQ extension, we don't want to use LINQ, so do it manually </summary>
	public static List<T> HashsetToList<T>(HashSet<T> set) {
		var result = new List<T>(set.Count);
		foreach(var item in set) {
			result.Add(item);
		}
		return result;
	}

	///<summary> We has the list [1, 4, 3, 6, 7], and we want the [7, 6, 3], then call this with amount = 3. this will create new list, no touch the original list </summary>
	public static List<T> CreateListOfItemsFromTheEnd<T>(List<T> original, int amount) {
		if(amount > original.Count) {
			Log.Warning(false, $"CreateListOfItemsFromTheEnd with amount > original [{original.Count}], return the original itself");
			return original;
		}

		var result = new List<T>(amount);
		for(int n = 0; n < amount; n++) {
			result.Add(original[original.Count - n - 1]);
		}
		return result;
	}
	#endregion//List, generic


	#region Dictionary
	public static TKey PickRandomMapKey<TKey, TValue>(this Dictionary<TKey, TValue> map) where TKey : class {
		var randomIndex = Random.Range(0, map.Count);
		var count = 0;
		foreach(var item in map) {
			if(count == randomIndex)
				return item.Key;
			count++;
		}
		return null;
	}

	public static TValue PickRandomMapValue<TKey, TValue>(this Dictionary<TKey, TValue> map) where TValue : class {
		var randomIndex = Random.Range(0, map.Count);
		var count = 0;
		foreach(var item in map) {
			if(count == randomIndex)
				return item.Value;
			count++;
		}
		return null;
	}
	#endregion//Dictionary
}