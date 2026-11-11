using UnityEngine;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System;

/// <summary>
/// Same with BinaryFileWriter, it use FileStream to Read/Write file to disk, too. But use the build-in JSONUlity
/// of Unity to serialize an object to a JSON (instead of use BinaryFormatter). Object's class must be marked with Serializable attribute.
/// NOTES : not like BinaryFormatter, JSONUtility alway convert success from JSON to object without any exception, all object's fields
/// that it cannot read from JSON will be init with default value, mind that.
/// </summary>
public class JsonFileWriter {
	#region Load,Save
	///<summary> Load file (with encryption optinal)</summary>
	public static T Load<T>(string path, string key = null) where T : class {
		var loaded = (key != null) ? Read(path, key) : Read(path);
		// not found file at path or cannot read
		if(loaded == null) {
			Debug.LogError(string.Format("Load FAIELD [{0}], Return default object", path));
			return default(T);
		}
		// ok, deserialize it
		else {
			var deserialized = ByteArrayToJSONThenToObject<T>(loaded);
			Debug.Log(string.Format("LoadJson [{0}] success", path));
			return deserialized;
		}
	}

	///<summary> Load file (with encryption optinal)</summary>
	public static void Save<T>(T data, string path, string key = null) {
		var toByteArray = ObjectToJSONThenToByteArray<T>(data);
		if(key != null) Write(path, toByteArray, key);
		else Write(path, toByteArray);
		Debug.Log(string.Format("SaveJson [{0}] success", path));
	}
	#endregion//Load,save


	#region Read, Write
	static byte[] Read(string path) {
		if(!Exist(path)) return null;
		try {
			var result = File.ReadAllBytes(path);
			return result;
		}
		catch(Exception e) {
			Debug.LogError(string.Format("Cannot read [{0}], error [{1}]", path, e.Message));
		}
		return null;
	}

	static byte[] Read(string path, string key) {
		var data = Read(path);
		// null data, then just return, no need to decrypt
		if(data == null) return data;
		// decrypt it
		var decrypted = Decrypt(data, key);
		return decrypted;
	}

	static void Write(string path, byte[] data) {
		try {
			File.WriteAllBytes(path, data);
		}
		catch(Exception e) {
			Debug.LogError(string.Format("Cannot Write [{0}], error [{1}]", path, e.Message));
		}
	}

	static void Write(string path, byte[] data, string key) {
		var encrypted = Encrypt(data, key);
		Write(path, encrypted);
	}
	#endregion//Read, Write


	#region Encrypt, Decrypt
	public static string ByteArrayToStringUTF8(byte[] byteArray) {
		return UTF8Encoding.UTF8.GetString(byteArray);
	}

	public static byte[] StringUTF8ToByteArray(string text) {
		return UTF8Encoding.UTF8.GetBytes(text);
	}

	///<summary> Check if the input key is acceptable for Rijndael algorythm, if not, fix it by add more or crop to get the acceptable length.
	/// Rijndael algorythm, only support the 128, 192, 256 bit key (32chars = 128bit .v.v..) </summary>
	static byte[] ValidateAndFixKey(string key) {
		const string FIX_CHAR = "x";
		var keyLength = key.Length;
		if(keyLength != 32 && keyLength != 48 && keyLength != 64) {
			//Debug.Log("Encrypt key must have 32,48,64 length. Fixed");
			// for simple, we crop string to 32 length
			if(keyLength > 32) {
				var subString = key.Substring(0, 32);
				return StringUTF8ToByteArray(subString);
			}
			// shorter than 32, than fill it up
			else {
				var builder = new StringBuilder(key, 32);
				for(int n = 0; n < 32 - keyLength; n++) {
					builder.Append(FIX_CHAR);
				}
				return StringUTF8ToByteArray(builder.ToString());
			}
		}
		// key ok
		return StringUTF8ToByteArray(key);
	}

	/// <summary> Encrypt the input byte[] array, using Rijndae algorythm.
	/// NOTES : https://stackoverflow.com/questions/2919228/specified-key-is-not-a-valid-size-for-this-algorithm
	/// RijndaelManaged supports 128, 192 and 256 bit keys </summary>
	public static byte[] Encrypt(byte[] toEncrypt, string key) {
		byte[] keyArray = ValidateAndFixKey(key);
		// 256-AES key
		RijndaelManaged rDel = new RijndaelManaged();
		try {
			rDel.Key = keyArray;
			rDel.Mode = CipherMode.ECB;
			// http://msdn.microsoft.com/en-us/library/system.security.cryptography.ciphermode.aspx
			rDel.Padding = PaddingMode.PKCS7;
		}
		catch(CryptographicException e) {
			Debug.LogError("Failed to encrypt : " + e.Message);
		}
		// better lang support
		ICryptoTransform cTransform = rDel.CreateEncryptor();
		return cTransform.TransformFinalBlock(toEncrypt, 0, toEncrypt.Length);
	}

	/// <summary> Encrypt the input byte[] array, using Rijndae algorythm.
	/// NOTES : https://stackoverflow.com/questions/2919228/specified-key-is-not-a-valid-size-for-this-algorithm
	/// RijndaelManaged supports 128, 192 and 256 bit keys </summary>
	public static byte[] Decrypt(byte[] toDecrypt, string key) {
		byte[] keyArray = ValidateAndFixKey(key);
		// AES-256 key
		RijndaelManaged rDel = new RijndaelManaged();
		try {
			rDel.Key = keyArray;
			rDel.Mode = CipherMode.ECB;
			// http://msdn.microsoft.com/en-us/library/system.security.cryptography.ciphermode.aspx
			rDel.Padding = PaddingMode.PKCS7;
		}
		catch(CryptographicException e) {
			Debug.LogError("Failed to decrypt : " + e.Message);
		}
		// better lang support
		ICryptoTransform cTransform = rDel.CreateDecryptor();
		byte[] resultArray = cTransform.TransformFinalBlock(toDecrypt, 0, toDecrypt.Length);
		/// if you want to get the string, try this function instead of Encoding.UTF8.GetString
		//var convertToString = Convert.ToBase64String(resultArray);
		return resultArray;
	}
	#endregion//Encrypt, Decrypt


	#region Utils
	///<summary> Simple checking exist file at path</summary>
	public static bool Exist(string path) {
		return File.Exists(path);
	}

	public static void DeleteFile(string path) {
		try {
			File.Delete(path);
		}
		catch {
			Debug.LogWarning(string.Format("Could not Delete exist file [{0}]", path));
		}
	}

	static byte[] ObjectToJSONThenToByteArray<T>(T data) {
		var toJSON = JsonUtility.ToJson(data);
		var toByteArray = StringUTF8ToByteArray(toJSON);
		return toByteArray;
	}

	static T ByteArrayToJSONThenToObject<T>(byte[] byteArray) {
		var loadedDataToString = ByteArrayToStringUTF8(byteArray);
		T deserialized;
		try {
			deserialized = JsonUtility.FromJson<T>(loadedDataToString);
		}
		catch (Exception e) {
			Log.Error(false, "JSONFileWriter, cannot deserialized data, create default data, error [{0}]", e.Message);
			return default(T);
		}
		return deserialized;
	}
	#endregion//Utils
}