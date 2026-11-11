using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
//using Shared.Core.Utils;
using UnityEngine;

public static class AssemblyExtensions {
	public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly) {
		if(assembly == null) {
			throw new ArgumentNullException("assembly");
		}

		try {
			return assembly.GetTypes();
		}
		catch(ReflectionTypeLoadException rtle) {
			Exception[] loaderExceptions = rtle.LoaderExceptions;
			foreach(Exception e in loaderExceptions) {
				Debug.LogError("LoaderException: " + e.Message);
			}
			return rtle.Types.Where(t => t != null);
		}
	}

	public static IEnumerable<Type> GetDefinedTypes(this Assembly assembly) {
		if(assembly == null) {
			throw new ArgumentNullException("assembly");
		}

		try {
			return assembly.DefinedTypes;
		}
		catch(ReflectionTypeLoadException rtle) {
			Exception[] loaderExceptions = rtle.LoaderExceptions;
			foreach(Exception e in loaderExceptions) {
				Debug.LogError("LoaderException: " + e.Message);
			}
			return rtle.Types.Where(t => t != null);
		}
	}

	public static IEnumerable<FieldInfo> GetMutableStaticFields(this Type type) {
		return type
			.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			.Where(f => !f.IsLiteral && // non-const
						(!f.IsInitOnly || (!f.FieldType.IsPrimitive && (f.FieldType != typeof(string)))) && // non-readonly or not primitive / string
						!(f.HasGeneratedName() || f.IsCompilerGenerated() || f.IsAnonymousMethodCacheField())); // not compiler generated
	}


	public static IEnumerable<PropertyInfo> GetMutableStaticProperties(this Type type) {
		return type
			.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			.Where(p => p.CanWrite);
	}

	public static bool IsCompilerGenerated(this MemberInfo memberInfo) {
		return memberInfo.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null;
	}

	public static bool HasGeneratedName(this MemberInfo memberInfo) {
		return memberInfo.Name.StartsWithFast("<") || memberInfo.Name.StartsWithFast("$");
	}

	public static bool IsAutomaticPropertyBackingField(this FieldInfo fieldInfo) {
		return IsCompilerGenerated(fieldInfo) && fieldInfo.Name.EndsWithFast("BackingField");
	}

	public static bool IsAnonymousMethodCacheField(this FieldInfo fieldInfo) {
		return fieldInfo.Name.StartsWithFast("CS$<>") || fieldInfo.Name.StartsWithFast("<>f__am");
	}

	public static bool IsClosureType(this Type type) {
		return type.HasGeneratedName() && type.IsCompilerGenerated() && (type.Name.Contains("DisplayClass") || type.Name.Contains("AnonStorey"));
	}
}
