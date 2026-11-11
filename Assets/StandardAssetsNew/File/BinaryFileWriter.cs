using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Security.Cryptography;
using System;

/// <summary>
/// Binary file writer. In project, we often use BinaryFormatter and FileStream to Read/Write file to disk.
/// This is a simple Utils class with Load/Save function for reading/writing obejct (mark with Serializable)
/// to/from devices's storage.
/// </summary>
public class BinaryFileWriter {
	#region Load, Save
	/// <summary> Load object type T (marked System.Serializable) at path. Return true if Load successful.
	/// By default, we will delete the file at path if cannot deserialze it </summary>
	public static bool Load<T>(string path, out T result, bool deleteIfCannotDeserialization = true) {
		ActiveEnvironmentVariableForIOSPlatform();
		// if exist the previous save file
		if(File.Exists(path)) {
			try {
				//get file from save location
				BinaryFormatter binary = new BinaryFormatter();
				FileStream reader = File.Open(path, FileMode.Open);
				//apply data to our instance
				try {
					result = (T)binary.Deserialize(reader);
					Debug.Log(string.Format("Load [{0}] successful", path));
					return true;
				}
				catch(SerializationException e) {
					Debug.LogError(string.Format("Load [{0} failed, Cannot serialize [{1}]]", path, e.Message));
					if(deleteIfCannotDeserialization)
						File.Delete(path);
				}
			}
			catch(IOException e) {
				Debug.LogError(string.Format("Load [{0}] failed, IOException [{1}]]", path, e.Message));
				if(deleteIfCannotDeserialization) {
					Debug.LogWarning(string.Format("Could not Load the exist file [{0}], delete it", path));
					DeleteFile(path);
				}
			}
			catch(Exception e) {
				Debug.LogError(string.Format("Load [{0}] failed, error [{1}]", path, e.Message));
				if(deleteIfCannotDeserialization) {
					Debug.LogWarning(string.Format("Could not Load the exist file [{0}], delete it", path));
					DeleteFile(path);
				}
			}
		}
		// not found file at path
		Debug.LogError(string.Format("Load FAIELD, not exist [{0}]]", path));
		result = default(T);
		return false;
	}

	/// <summary> Save the data (marked System.Serializable) at path. Return true if Write file successful</summary>
	public static bool Save<T>(T data, string path) {
		ActiveEnvironmentVariableForIOSPlatform();
		try {
			// create a binary file to save
			BinaryFormatter binary = new BinaryFormatter();
			FileStream writer = File.Create(path);
			// set data
			binary.Serialize(writer, data);
			writer.Close();//flush, write to file
			Debug.Log(string.Format("Save successful [{0}] !!!", path));
			return true;
		}
		catch(IOException e) {
			Debug.LogError(string.Format("Save FAILED [{0}], IOException : {1}", path, e.Message));
			return false;
		}
		catch(Exception e) {
			Debug.LogError(string.Format("Save FAILED [{0}], message : {1}", path, e.Message));
			return false;
		}
	}

	/// <summary> Load the file which was encrypted by key. </summary>
	public static bool Load<T>(string path, string key, out T result, bool deleteIfCannotDeserialization = true) where T : class {
		ActiveEnvironmentVariableForIOSPlatform();
		// if exist the previous save file
		if(File.Exists(path)) {
			try {
				// read file, decrypt and deserialize
				var byteArray = File.ReadAllBytes(path);
				var decrypted = Decrypt(byteArray, key);
				result = DeserializeByteArrayToObject<T>(decrypted);
				Debug.Log(string.Format("Load [{0}] successful", path));
				return true;
			}
			catch(SerializationException e) {
				Debug.LogError(string.Format("Load [{0} failed, Cannot serialize [{1}]]", path, e.Message));
				if(deleteIfCannotDeserialization)
					File.Delete(path);
			}
			catch(IOException e) {
				Debug.LogError(string.Format("Load [{0}] failed, error [{1}]]", path, e.Message));
				if(deleteIfCannotDeserialization) {
					Debug.LogWarning(string.Format("Could not Load the exist file [{0}], delete it", path));
					DeleteFile(path);
				}
			}
			catch(Exception e) {
				Debug.LogError(string.Format("Load [{0}] failed, Exception : [{1}]", path, e.Message));
				if(deleteIfCannotDeserialization) {
					Debug.LogWarning(string.Format("Could not Load the exist file [{0}], delete it", path));
					DeleteFile(path);
				}
			}
		}
		// not found file at path
		Debug.LogError(string.Format("Load FAILED, not exist [{0}]]", path));
		result = default(T);
		return false;
	}

	/// <summary> Save the data, encrypted it using key </summary>
	public static bool Save<T>(T data, string key, string path) {
		ActiveEnvironmentVariableForIOSPlatform();
		try {
			// create a binary file to save, encrypt it
			var userDataInByteArray = ObjectToByteArray(data);
			var encrypted = Encrypt(userDataInByteArray, key);
			File.WriteAllBytes(path, encrypted);
			Debug.Log(string.Format("Save successful : [{0}]", path));
			return true;
		}
		catch(IOException e) {
			Debug.LogError(string.Format("Save FAILED [{0}], IOException : {1}", path, e.Message));
			return false;
		}
		catch(SerializationException e) {
			Debug.LogError(string.Format("Save FAILED [{0}], SerializationException : {1}", path, e.Message));
			return false;
		}
		catch(Exception e) {
			Debug.LogError(string.Format("Save FAILED [{0}], Exception : {1}", path, e.Message));
			return false;
		}
	}

	public static void CreateDirectoryIfNotExist(string directory) {
		if(!Directory.Exists(directory)) {
			Directory.CreateDirectory(directory);
		}
	}

	///<summary> On IOS device, BinaryFormatter will be strip assembly. Solution is:
	///- Addd link.xml file to let System.IO.* library not be stripped
	///- Use System.Environment.SetEnvironmentVarialbe() to fix something regard to FileStream and Reflection in runtime
	///Readmore in some post here : 
	///https://answers.unity.com/questions/725419/filestream-binaryformatter-from-c-to-ios-doesnt-wo.html
	///https://answers.unity.com/questions/1332440/saving-game-data-to-ios-device.html
	///https://stackoverflow.com/questions/27806113/saving-and-loading-game-data-not-working
	///
	///https://docs.unity3d.com/Manual/iphone-playerSizeOptimization.html
	///https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html </summary>
	static void ActiveEnvironmentVariableForIOSPlatform() {
#if UNITY_IOS
		const string VARIABLE_NAME = "MONO_REFLECTION_SERIALIZER";
		const string VALUE = "yes";
		System.Environment.SetEnvironmentVariable(VARIABLE_NAME, VALUE);
#endif//UNITY_IOS
	}
	#endregion//Load, Save


	#region Encrypt, Decrypt
	public static string ByteArrayToStringUTF8(byte[] byteArray) {
		return UTF8Encoding.UTF8.GetString(byteArray);
	}

	public static byte[] StringUTF8ToByteArray(string text) {
		return UTF8Encoding.UTF8.GetBytes(text);
	}

	///<summary> Check if the input key is acceptable for Rijndael algorythm, if not, fix it by add more or crop to get
	/// the acceptable length. Rijndael algorythm, only support the 128, 192, 256 bit key (32chars = 128bit .v.v..) </summary>
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


	#region Read, Write
	static byte[] Read(string path) {
		if(!Exist(path))
			return null;
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
		if(data == null)
			return data;
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


	#region Utils
	///<summary> Simple checking exist file at path </summary>
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

	///<summary> Convert object to byte[], using BinaryFormatter</summary>
	public static byte[] ObjectToByteArray(object obj) {
		if(obj == null) {
			return null;
		}
		var formatter = new BinaryFormatter();
		using(var stream = new MemoryStream()) {
			formatter.Serialize(stream, obj);
			return stream.ToArray();
		}
	}

	///<summary> Convert byte[] to object, which was serialize using BinaryFormatter
	///before (via ObjectToByteArray func above)</summary>
	public static T DeserializeByteArrayToObject<T>(byte[] byteArray) where T : class {
		if(byteArray == null) {
			return null;
		}
		using(var stream = new MemoryStream()) {
			var formatter = new BinaryFormatter();
			stream.Write(byteArray, 0, byteArray.Length);
			stream.Seek(0, SeekOrigin.Begin);
			var obj = (T)formatter.Deserialize(stream);
			return obj;
		}
	}
	#endregion//Utils
}