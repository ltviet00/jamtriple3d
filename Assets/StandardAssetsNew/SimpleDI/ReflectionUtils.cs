using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
//using Shared.Core.Utils;
using UnityEngine;

public static class ReflectionUtils {
	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, Type[]> SubtypesCache = new Dictionary<Type, Type[]>();
	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, Type[]> IncludingSelfSubtypesCache = new Dictionary<Type, Type[]>();

	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, string[]> EnumNamesCache = new Dictionary<Type, string[]>();
	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, Array> EnumValuesCache = new Dictionary<Type, Array>();

	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, List<FieldInfo>> FieldInfosCache = new Dictionary<Type, List<FieldInfo>>();
	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, List<PropertyInfo>> PropertyInfosCache = new Dictionary<Type, List<PropertyInfo>>();
	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, List<MemberInfo>> PropertyAndFieldInfosCache = new Dictionary<Type, List<MemberInfo>>();
	//[Core.OnLogoutPersist]
	static readonly Dictionary<Type, List<MethodInfo>> MethodInfosCache = new Dictionary<Type, List<MethodInfo>>();

	public class TypeReflectionData {
		public Dictionary<string, System.Type> NameToType;
	}

	public static object GetValue(object parentObject, string childName) {
		if(parentObject == null)
			return null;

		Type parentType = parentObject.GetType();

		FieldInfo childAsField = parentType.GetField(childName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if(childAsField != null)
			return childAsField.GetValue(parentObject);

		PropertyInfo childAsProperty = parentType.GetProperty(childName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if(childAsProperty != null)
			return childAsProperty.GetValue(parentObject, null);

		return null;
	}

	public static object GetIndexValue(object parentObject, string collectionName, int collectionIndex) {
		IEnumerable collection = parentObject as IEnumerable;
		IEnumerator enumerator = collection.GetEnumerator();

		int count = 0;
		while(count++ <= collectionIndex) {
			enumerator.MoveNext();
		}

		return enumerator.Current;
	}

	public static void SetValue(object parentObject, string childName, object childValue) {
		if(parentObject == null)
			return;

		Type parentType = parentObject.GetType();

		FieldInfo childAsField = parentType.GetField(childName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if(childAsField != null)
			childAsField.SetValue(parentObject, childValue);

		PropertyInfo childAsProperty = parentType.GetProperty(childName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		if(childAsProperty != null)
			childAsProperty.SetValue(parentObject, childValue, null);
	}

	public static void SetMethodValue(object parent, string methodName, object[] methodValue) {
		MethodInfo mInfo = parent.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		mInfo.Invoke(parent, methodValue);
	}

	public static Type[] GetSubclassesOfTypeInSameAssembly(Type type, bool includeOwnType = false, bool includeAbstract = false) {
		if(type == null) {
			return new Type[] { };
		}

		Dictionary<Type, Type[]> relevantDictionary = includeOwnType ? IncludingSelfSubtypesCache : SubtypesCache;

		if(relevantDictionary.ContainsKey(type))
			return relevantDictionary[type];

		Type[] types = (from assemblyType in type.Assembly.GetTypes()
						where type.IsAssignableFrom(assemblyType)
						where includeOwnType || (type != assemblyType)
						where includeAbstract || !assemblyType.IsAbstract
						select assemblyType).ToArray();

		relevantDictionary[type] = types;

		return types;
	}

	public static string[] GetEnumNames(Type enumType) {
		string[] toReturn = null;
		if(!EnumNamesCache.TryGetValue(enumType, out toReturn)) {
			toReturn = Enum.GetNames(enumType);
			EnumNamesCache[enumType] = toReturn;
		}
		return toReturn;
	}

	public static Array GetEnumValues(Type enumType) {
		Array toReturn = null;
		if(!EnumValuesCache.TryGetValue(enumType, out toReturn)) {
			toReturn = Enum.GetValues(enumType);
			EnumValuesCache[enumType] = toReturn;
		}
		return toReturn;
	}

	public static object TryParseEnum(Type enumType, string value) {
		if(enumType == null) {
			Debug.LogError("[ENUM PARSE FAILED] Null enum type.");
			return null;
		}

		if(string.IsNullOrEmpty(value)) {
			Debug.LogError("[ENUM PARSE FAILED] Null or empty value. type " + enumType.Name);
			return null;
		}

		try {
			return Enum.Parse(enumType, value);
		}
		catch(ArgumentException argE) {
			Array values = Enum.GetValues(enumType);
			if(values.Length == 0) {
				Debug.LogError($"[ENUM PARSE FAILED] Zero length enum. [{value}] in type [{enumType.Name}]");
				return argE;
			}
			else {
				Debug.LogError($"[ENUM PARSE FAILED] Could not find value.. [{value}] in type [{enumType.Name}]");
				return Enum.GetValues(enumType).GetValue(0);
			}
		}
	}

	public static List<MethodInfo> GetMethodInfos(Type type) {
		List<MethodInfo> toReturn = null;
		if(!MethodInfosCache.TryGetValue(type, out toReturn)) {
			toReturn = new List<MethodInfo>();

			BindingFlags allMembers = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |BindingFlags.DeclaredOnly;
			Type currentType = type;
			while(currentType != null && currentType != typeof(object)) {
				MethodInfo[] methods = currentType.GetMethods(allMembers);
				toReturn = toReturn.Concat(methods.Where(m => !m.IsSpecialName)).ToList();
				currentType = currentType.BaseType;
			}
			MethodInfosCache[type] = toReturn;
		}

		return toReturn;
	}

	public static List<FieldInfo> GetFieldInfos(Type type) {
		List<FieldInfo> toReturn = null;
		if(!FieldInfosCache.TryGetValue(type, out toReturn)) {
			toReturn = new List<FieldInfo>();

			BindingFlags allMembers = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			Type currentType = type;
			while(currentType != null && currentType != typeof(object)) {
				FieldInfo[] fields = currentType.GetFields(allMembers);
				toReturn = toReturn.Concat(fields).ToList();

				currentType = currentType.BaseType;
			}
			FieldInfosCache[type] = toReturn;
		}

		return toReturn;
	}

	public static List<PropertyInfo> GetPropertyInfos(Type type) {
		List<PropertyInfo> toReturn = null;
		if(!PropertyInfosCache.TryGetValue(type, out toReturn)) {
			toReturn = new List<PropertyInfo>();

			BindingFlags allMembers = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

			Type currentType = type;
			while(currentType != null && currentType != typeof(object)) {
				PropertyInfo[] properties = currentType.GetProperties(allMembers);
				toReturn = toReturn.Concat(properties).ToList();

				currentType = currentType.BaseType;
			}

			PropertyInfosCache[type] = toReturn;
		}

		return toReturn;
	}

	public static List<MemberInfo> GetPropertyAndFieldInfos(Type type) {
		List<MemberInfo> toReturn = null;
		if(!PropertyAndFieldInfosCache.TryGetValue(type, out toReturn)) {
			toReturn = new List<MemberInfo>();

			toReturn = toReturn.Concat(GetPropertyInfos(type)).Concat(GetFieldInfos(type)).ToList();

			PropertyAndFieldInfosCache[type] = toReturn;
		}

		return toReturn;
	}

	public static List<Tuple<MemberInfo, T>> GetMemberInfosWithAttribute<T>(Type type) where T : Attribute {
		List<Tuple<MemberInfo, T>> toReturn = new List<Tuple<MemberInfo, T>>();

		foreach(MemberInfo member in GetPropertyAndFieldInfos(type)) {
			if(member is FieldInfo) {
				FieldInfo field = member as FieldInfo;
				T attrib = field.GetCustomAttribute<T>();
				if(attrib != null)
					toReturn.Add(new Tuple<MemberInfo, T>(field, attrib));
			}

			if(member is PropertyInfo) {
				PropertyInfo property = member as PropertyInfo;
				T attrib = property.GetCustomAttribute<T>();
				if(attrib != null)
					toReturn.Add(new Tuple<MemberInfo, T>(property, attrib));
			}
		}

		return toReturn;
	}

	public static List<MemberInfo> GetMemberInfosOfType<T>(Type type) {
		List<MemberInfo> toReturn = new List<MemberInfo>();

		foreach(MemberInfo member in GetPropertyAndFieldInfos(type)) {
			if(member is FieldInfo) {
				FieldInfo field = member as FieldInfo;
				if(field.FieldType == typeof(T))
					toReturn.Add(field);
			}

			if(member is PropertyInfo) {
				PropertyInfo property = member as PropertyInfo;
				if(property.PropertyType == typeof(T))
					toReturn.Add(property);
			}
		}

		return toReturn;
	}

	public static Type GetMemberType(this MemberInfo member) {
		switch(member.MemberType) {
		case MemberTypes.Field:
			return ((FieldInfo)member).FieldType;
		case MemberTypes.Property:
			return ((PropertyInfo)member).PropertyType;
		default:
			throw new InvalidCastException("Unexpected MemberInfo.MemberType: " + member.MemberType);
		}
	}

	public static object GetMemberValue(this MemberInfo member, object obj) {
		switch(member.MemberType) {
		case MemberTypes.Field:
			return ((FieldInfo)member).GetValue(obj);
		case MemberTypes.Property:
			return ((PropertyInfo)member).GetValue(obj);
		default:
			throw new InvalidCastException("Unexpected MemberInfo.MemberType: " + member.MemberType);
		}
	}

	public static void SetMemberValue(this MemberInfo member, object obj, object value) {
		switch(member.MemberType) {
		case MemberTypes.Field:
			((FieldInfo)member).SetValue(obj, value);
			break;
		case MemberTypes.Property:
			((PropertyInfo)member).SetValue(obj, value);
			break;
		default:
			throw new InvalidCastException("Unexpected MemberInfo.MemberType: " + member.MemberType);
		}
	}

	public static bool IsEnumerableType(Type type) {
		return (type.GetInterface(nameof(IEnumerable)) != null);
	}

	public static bool IsGenericList(Type type) {
		return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)));
	}

	public static bool IsGeneric(Type type) {
		return type.IsGenericType;
	}
}
