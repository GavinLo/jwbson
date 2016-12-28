using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace jw {

	public interface BsonContext {

		byte TypeDouble { get; }
		byte TypeString { get; }
		byte TypeDocument { get; }
		byte TypeArray { get; }
		byte TypeBinary { get; }
		byte TypeBool { get; }
		byte TypeInt32 { get; }
		byte TypeInt64 { get; }

		byte SubTypeGeneric { get; }

		byte ValueEnd { get; }
		byte ValueFalse { get; }
		byte ValueTrue { get; }

		Endian Endian { get; }

		bool Float32AsDouble { get; } // NOTE 非Bson标准，是个优化
	}

	public class BsonContext11 : BsonContext {

		public byte TypeDouble {
			get {
				return 0x01; // "\x01" e_name double	64-bit binary floating point
			}
		}

		public byte TypeString {
			get {
				return 0x02; // "\x02" e_name string	UTF-8 string
			}
		}

		public byte TypeDocument {
			get {
				return 0x03; // "\x03" e_name document	Embedded document
			}
		}

		public byte TypeArray {
			get {
				return 0x04; // "\x04" e_name document	Array
			}
		}

		public byte TypeBinary {
			get {
				return 0x05; // "\x05" e_name binary	Binary data
			}
		}

		public byte TypeBool {
			get {
				return 0x08; // "\x08" e_name "\x00"	Boolean "false" 
			}
		}

		public byte TypeInt32 {
			get {
				return 0x10; // "\x10" e_name int32	32-bit integer
			}
		}

		public byte TypeInt64 {
			get {
				return 0x12; // "\x12" e_name int64	64-bit integer
			}
		}

		public byte SubTypeGeneric {
			get {
				return 0x00; // "\x00"	Generic binary subtype
			}
		}

		public byte ValueEnd {
			get {
				return 0x00; // "\x00"
			}
		}

		public byte ValueFalse {
			get {
				return 0x00; // "\x08" e_name "\x00"	Boolean "false" 
			}
		}

		public byte ValueTrue {
			get {
				return 0x01; // "\x08" e_name "\x01"	Boolean "true"
			}
		}

		public Endian Endian {
			get {
				return Endian.Little;
			}
		}

		public virtual bool Float32AsDouble {
			get {
				return false;
			}
		}

	}

	/// <summary>
    /// 标准定义在这里
	/// http://bsonspec.org/spec.html
    /// </summary>
	public class Bson {

		public static BsonContext11 Context11 = new BsonContext11();

		private BsonContext mContext;

		public bool IsDebug = false;
		private StringBuilder mDebugString;

		public Bson(BsonContext context = null) {
			if (context == null) {
				mContext = Context11;
			} else {
				mContext = context;
			}
		}

		public bool Serialize(object obj, Stream stream) {
			if (obj == null || stream == null) {
				return false;
			}
			if (!stream.CanWrite || !stream.CanSeek) {
				return false;
			}
			if (IsDebug) {
				if (mDebugString == null) {
					mDebugString = new StringBuilder();
				}
				mDebugString.Length = 0;
			}
			serializeObject(null, obj, stream);
			return true;
		}

		public bool Deserialize(Stream stream, object obj) {
			if (obj == null || stream == null) {
				return false;
			}
			if (!stream.CanRead || !stream.CanSeek) {
				return false;
			}
			if (IsDebug) {
				if (mDebugString == null) {
					mDebugString = new StringBuilder();
				}
				mDebugString.Length = 0;
			}
			mTempArrayListLevel = 0;
			deserializeObject(stream, obj);
			return true;
		}

		public string DebugOutput {
			get {
				if (mDebugString == null) {
					return null;
				}
				return mDebugString.ToString();
			}
		}

		#region serialize
		private void serializeObject(string objKey, object obj, Stream stream) {
			if (obj == null) {
				return;
			}

			if (objKey != null) {
				if (IsDebug) {
					appendDebugByte(mContext.TypeDocument);
				}
				stream.WriteByte(mContext.TypeDocument);
				writeString(stream, objKey);
			}

			long sizePosition;
			int debugStringPosition;
			writeBeginObject(stream, out sizePosition, out debugStringPosition);

			var type = obj.GetType();
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fields != null) {
				foreach (var field in fields) {
					var serializedField = ReflectUtils.GetCustomAttribute<SerializedField>(field);
					if (serializedField == null || !serializedField.UseToSerialize) {
						continue;
					}
					var key = serializedField.Name == null ? field.Name : serializedField.Name;
					serializeKeyValue(key, field.GetValue(obj), stream);
				}
			}
			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (properties != null) {
				foreach (var property in properties) {
					var serializedField = ReflectUtils.GetCustomAttribute<SerializedField>(property);
					if (serializedField == null || !serializedField.UseToSerialize) {
						continue;
					}
					var key = serializedField.Name == null ? property.Name : serializedField.Name;
					serializeKeyValue(key, property.GetValue(obj, null), stream);
				}
			}

			writeEndObject(stream, sizePosition, debugStringPosition);
		}

		private void serializeKeyValue(string key, object value, Stream stream) {
			if (key == null || value == null) {
				return;
			}
			byte valueType;
			byte subType;
			byte[] valueBytes;
			bool valueNeedSize;
			bool valueNeedEnd;
			if (!getValueBytes(value, out valueType, out subType, out valueBytes, out valueNeedSize, out valueNeedEnd)) {
				if (value is IList || value.GetType().IsArray) {
					serializeArray(key, value as IEnumerable, stream);
				} else if (value is IDictionary) {
					serializeDict(key, value as IDictionary, stream);
				} else {
					serializeObject(key, value, stream);
				}
				return;
			}
			if (IsDebug) {
				appendDebugByte(valueType);
			}
			stream.WriteByte(valueType);
			writeString(stream, key);
			if (valueNeedSize) {
				byte[] valueSizeBytes = null;
				if (valueNeedEnd) {
					valueSizeBytes = BitUtils.GetIntBytes(valueBytes.Length + 1, mContext.Endian); // NOTE 包含'\x00'	
				} else {
					valueSizeBytes = BitUtils.GetIntBytes(valueBytes.Length, mContext.Endian);
				}
				if (IsDebug) {
					appendDebugBytes(valueSizeBytes);
				}
				stream.Write(valueSizeBytes, 0, valueSizeBytes.Length);
			}
			if (subType != 0xff) {
				stream.WriteByte(subType);
				if (IsDebug) {
					appendDebugByte(subType);
				}
			}
			if (IsDebug) {
				if (value is string) {
					mDebugString.Append(value as string);
				} else {
					appendDebugBytes(valueBytes);
					if (!valueNeedEnd) {
						mDebugString.AppendLine();
					}
				}
			}
			stream.Write(valueBytes, 0, valueBytes.Length);
			if (valueNeedEnd) {
				writeEnd(stream);
			}
		}

		private void serializeArray(string key, IEnumerable array, Stream stream) {
			if (IsDebug) {
				appendDebugByte(mContext.TypeArray);
			}
			stream.WriteByte(mContext.TypeArray);
			writeString(stream, key);

			long sizePosition;
			int debugStringPosition;
			writeBeginObject(stream, out sizePosition, out debugStringPosition);

			var i = 0;
			foreach (var a in array) {
				serializeKeyValue(i.ToString(), a, stream);
				i++;
			}

			writeEndObject(stream, sizePosition, debugStringPosition);
		}

		private void serializeDict(string key, IDictionary dict, Stream stream) {
			var genericTypes = dict.GetType().GetGenericArguments();
			if (genericTypes[0] != typeof(string)) { // NOTE 不支持string以外的key
				return;
			}

			if (IsDebug) {
				appendDebugByte(mContext.TypeDocument);
			}
			stream.WriteByte(mContext.TypeDocument);
			writeString(stream, key);

			long sizePosition;
			int debugStringPosition;
			writeBeginObject(stream, out sizePosition, out debugStringPosition);

			var keys = dict.Keys;
			foreach (var d in keys) {
				var k = d.ToString();
				serializeKeyValue(k, dict[d], stream);
			}

			writeEndObject(stream, sizePosition, debugStringPosition);
		}

		private void writeBeginObject(Stream stream, out long sizePosition, out int debugStringPosition) {
			// document ::= int32 e_list "\x00" 
			// BSON Document. int32 is the total number of bytes comprising the document.
			// NOTE 预留子文档大小位置
			sizePosition = stream.Position;
			var sizeBytes = BitUtils.GetIntBytes(0, mContext.Endian);
			debugStringPosition = 0;
			if (IsDebug) {
				debugStringPosition = mDebugString.Length;
				appendDebugBytes(sizeBytes);
				mDebugString.AppendLine();
			}
			stream.Write(sizeBytes, 0, sizeBytes.Length);
		}

		private void writeEndObject(Stream stream, long sizePosition, int debugStringPosition) {
			// 结尾符
			writeEnd(stream);
			var endPosition = stream.Position;
			// 回填子文档大小
			var sizeBytes = BitUtils.GetIntBytes((int)(endPosition - sizePosition), mContext.Endian);
			stream.Seek(sizePosition, SeekOrigin.Begin);
			if (IsDebug) {
				replaceDebugBytes(debugStringPosition, sizeBytes);
			}
			stream.Write(sizeBytes, 0, sizeBytes.Length);
			stream.Seek(endPosition, SeekOrigin.Begin);
		}

		private void writeEnd(Stream stream) {
			if (IsDebug) {
				appendDebugByte(mContext.ValueEnd);
				mDebugString.AppendLine();
			}
			stream.WriteByte(mContext.ValueEnd);
		}

		private void writeString(Stream stream, string str) {
			if (IsDebug) {
				mDebugString.Append(str);
			}
			var strBytes = Encoding.UTF8.GetBytes(str);
			stream.Write(strBytes, 0, strBytes.Length);
			writeEnd(stream);
		}

		private bool getValueBytes(object value, out byte type, out byte subType, out byte[] bytes, out bool needSize, out bool needEnd) {
			type = 0x00;
			subType = 0xff;
			bytes = null;
			needSize = false;
			needEnd = false;
			if (value is int || value is short) {
				type = mContext.TypeInt32;
				var v = (int)Convert.ChangeType(value, typeof(int));
				bytes = BitUtils.GetIntBytes(v, mContext.Endian);
				return true;
			} else if (value is uint || value is ushort) {
				type = mContext.TypeInt32;
				var v = (uint)Convert.ChangeType(value, typeof(uint));
				bytes = BitUtils.GetUIntBytes(v, mContext.Endian);
				return true;
			} else if (value is long) {
				type = mContext.TypeInt64;
				var v = (long)Convert.ChangeType(value, typeof(long));
				bytes = BitUtils.GetLongBytes(v, mContext.Endian);
				return true;
			} else if (value is ulong) {
				type = mContext.TypeInt64;
				var v = (ulong)Convert.ChangeType(value, typeof(ulong));
				bytes = BitUtils.GetULongBytes(v, mContext.Endian);
				return true;
			} else if (value is float || value is double) {
				if (!mContext.Float32AsDouble) {
					type = mContext.TypeDouble;
					var v = (double)Convert.ChangeType(value, typeof(double));
					bytes = BitUtils.GetDoubleBytes(v, mContext.Endian);
					return true;
				} else {
					type = mContext.TypeDouble;
					var v = (float)Convert.ChangeType(value, typeof(float));
					bytes = BitUtils.GetFloatBytes(v, mContext.Endian);
					return true;
				}
			} else if (value is decimal) {
				// "\x13" e_name decimal128	128-bit decimal floating point
				// type = 0x13;
				// TODO 
				return false;
			} else if (value is string) {
				type = mContext.TypeString;
				bytes = Encoding.UTF8.GetBytes(value as string);
				needSize = true;
				needEnd = true;
				return true;
			} else if (value is bool) {
				type = mContext.TypeBool;
				var b = (bool)value;
				bytes = new byte[]{ b ? mContext.ValueTrue : mContext.ValueFalse };
				return true;
			} else if (value is byte[]) {
				type = mContext.TypeBinary;
				bytes = value as byte[];
				subType = mContext.SubTypeGeneric;
				needSize = true;
				return true;
			}
			return false;
		}

		private void appendDebugByte(byte b) {
			mDebugString.Append("\\x" + b.ToString("X2"));
		}

		private void appendDebugBytes(byte[] bs) {
			foreach (var b in bs) {
				appendDebugByte(b);
			}
		}

		private void replaceDebugBytes(int index, byte[] bs) {
			var i = index;
			foreach (var b in bs) {
				mDebugString[i++] = '\\';
				mDebugString[i++] = 'x';
				var bb = b.ToString("X2");
				mDebugString[i++] = bb[0];
				mDebugString[i++] = bb[1];
			}
		}
		#endregion

		#region deserialize
		private byte[] mReadBuffer;
		private int deserializeObject(Stream stream, object obj) {
			int numBytesRead = 0;
			int size;
			int n;
			if (!readSize(stream, out size, out n)) {
				return 0; // NOTE 表示结束
			}
			size -= n;
			numBytesRead += n;
			if (size <= 1) {
				return numBytesRead;
			}
			var type = obj.GetType();
			while (size > 1) {
				n = deserializeKeyValue(stream, obj, type);
				if (n <= 0) {
					return 0; // NOTE 表示结束
				}
				size -= n;
				numBytesRead += n;
			}
			stream.Seek(stream.Position + 1, SeekOrigin.Begin);
			numBytesRead++;
			return numBytesRead;
		}

		private int deserializeKeyValue(Stream stream, object obj, Type objType) {
			byte valueType;
			string key;
			int numBytesRead;
			int totalBytesRead = 0;
			if (!readKey(stream, out valueType, out key, out numBytesRead)) {
				return numBytesRead;
			}
			totalBytesRead += numBytesRead;

			Type targetValueType = null;
			FieldInfo field = null;
			PropertyInfo property = null;
			var isDict = obj is IDictionary;
			IDictionary dict = null;
			if (isDict) {
				var genericTypes = objType.GetGenericArguments();
				if (genericTypes[0] != typeof(string)) { // NOTE 不支持string以外的key
					return -1;
				}
				targetValueType = genericTypes[1];
				dict = obj as IDictionary;
			} else {
				var serializedField = getSerializedField(objType, key, out field);
				if (serializedField != null && !serializedField.UseToDeserialize) {
					return totalBytesRead;
				}
				serializedField = getSerializedField(objType, key, out property);
				if (serializedField != null && !serializedField.UseToDeserialize) {
					return totalBytesRead;
				}
				targetValueType = getFieldType(field, property);
			}

			var value = readValue(stream, valueType, targetValueType, out numBytesRead);
			if (value == null) {
				if (valueType == mContext.TypeArray || valueType == mContext.TypeDocument) {
					if (!isDict && field == null && property == null) { // NOTE 找不到内嵌对象的，直接跳过，IDictionary除外
						int size;
						int n;
						if (!readSize(stream, out size, out n)) {
							return 0; // NOTE 表示结束
						}
						size -= n;
						stream.Seek(stream.Position + size, SeekOrigin.Begin);
						totalBytesRead += size;
						return totalBytesRead;
					}
					if (valueType == mContext.TypeArray) {
						numBytesRead = deserializeArray(stream, obj, field, property);
					} else {
						var o = Activator.CreateInstance(targetValueType);
						numBytesRead = deserializeObject(stream, o);
						if (isDict) {
							dict[key] = o;
						} else {
							setFieldValue(obj, field, property, o);
						}
					}
				}
			} else {
				if (isDict) {
					dict[key] = value;
				} else {
					setFieldValue(obj, field, property, value);
				}
			}
			totalBytesRead += numBytesRead;
			return totalBytesRead;
		}

		private int deserializeArray(Stream stream, object obj, FieldInfo field, PropertyInfo property) {
			var targetType = getFieldType(field, property);
			int numBytesRead;
			var array = readArray(stream, targetType, out numBytesRead);
			if (numBytesRead <= 0) {
				return 0; // NOTE 表示结束
			}
			setFieldValue(obj, field, property, array);
			return numBytesRead;
		}

		private SerializedField getSerializedField(Type type, string name, out FieldInfo field) {
			SerializedField serializedField;
			field = ReflectUtils.GetFieldCustomAttribute<SerializedField>(type, name, (FieldInfo f, string n) => {
				var a = ReflectUtils.GetCustomAttribute<SerializedField>(f);
				if (a == null) {
					return null;
				}
				if (a.Name == n || (a.Name == null && f.Name == n)) {
					return a;
				}
				return null;
			}, out serializedField);
			return serializedField;
		}

		private SerializedField getSerializedField(Type type, string name, out PropertyInfo property) {
			SerializedField serializedField;
			property = ReflectUtils.GetPropertyCustomAttribute<SerializedField>(type, name, (PropertyInfo f, string n) => {
				var a = ReflectUtils.GetCustomAttribute<SerializedField>(f);
				if (a == null) {
					return null;
				}
				if (a.Name == n || (a.Name == null && f.Name == n)) {
					return a;
				}
				return null;
			}, out serializedField);
			return serializedField;
		}

		private bool readKey(Stream stream, out byte valueType, out string key, out int numBytesRead) {
			key = null;
			valueType = 0x00;
			numBytesRead = 0;
			var b = stream.ReadByte();
			if (b < 0) {
				return false;
			}
			valueType = (byte)b;
			numBytesRead++;

			int n;
			if (!StreamUtils.ReadString(stream, Encoding.UTF8, 0, out key, out n)) {
				return false;
			}
			numBytesRead += n;

			if (IsDebug) {
				mDebugString.Append("Find Key=" + key + " Type=0x" + valueType.ToString("X2") + "\n");
			}
			return true;
		}

		private bool readSize(Stream stream, out int size, out int numBytesRead) {
			return StreamUtils.ReadInt32(stream, mContext.Endian, out size, out numBytesRead);
		}

		private bool readSubType(Stream stream, out byte subType, out int numBytesRead) {
			subType = 0x00;
			var b = stream.ReadByte();
			if (b < 0) {
				numBytesRead = 0;
				return false;
			}
			numBytesRead = 1;
			subType = (byte)b;
			return true;
		}
		
		private object readValue(Stream stream, byte valueType, Type targetType, out int numBytesRead) {
			numBytesRead = 0;
			object value = null;
			if (valueType == mContext.TypeInt32) {
				var valueBytes = readValueBytes(stream, sizeof(int), out numBytesRead);
				if (valueBytes == null) {
					return false;
				}
				value = BitUtils.ToInt(valueBytes, 0, mContext.Endian);
				if (targetType != typeof(int)) {
					try {
						value = Convert.ChangeType(value, targetType);
					} catch (Exception) {
						value = null;
					}
				}
			} else if (valueType == mContext.TypeInt64) {
				var valueBytes = readValueBytes(stream, sizeof(long), out numBytesRead);
				if (valueBytes == null) {
					return false;
				}
				value = BitUtils.ToLong(valueBytes, 0, mContext.Endian);
				if (targetType != typeof(long)) {
					try {
						value = Convert.ChangeType(value, targetType);
					} catch (Exception) {
						value = null;
					}
				}
			} else if (valueType == mContext.TypeDouble) {
				if (!mContext.Float32AsDouble) {
					var valueBytes = readValueBytes(stream, sizeof(double), out numBytesRead);
					if (valueBytes == null) {
						return false;
					}
					value = BitUtils.ToDouble(valueBytes, 0, mContext.Endian);
					if (targetType != typeof(double)) {
						try {
							value = Convert.ChangeType(value, targetType);
						} catch (Exception) {
							value = null;
						}
					}
				} else {
					var valueBytes = readValueBytes(stream, sizeof(float), out numBytesRead);
					if (valueBytes == null) {
						return false;
					}
					value = BitUtils.ToFloat(valueBytes, 0, mContext.Endian);
					if (targetType != typeof(float)) {
						try {
							value = Convert.ChangeType(value, targetType);
						} catch (Exception) {
							value = null;
						}
					}
				}
			} else if (valueType == mContext.TypeString) {
				int size;
				if (!readSize(stream, out size, out numBytesRead)) {
					return false;
				}
				int n;
				var valueBytes = readValueBytes(stream, size, out n);
				numBytesRead += n;
				if (valueBytes == null) {
					return false;
				}
				value = Encoding.UTF8.GetString(valueBytes, 0, size - 1 /** 去除结束符 **/);
			} else if (valueType == mContext.TypeBool) { 
				var valueBytes = readValueBytes(stream, 1, out numBytesRead);
				if (valueBytes == null) {
					return false;
				}
				value = valueBytes[0] == 0x01 ? true : false;
				if (targetType != typeof(bool)) {
					try {
						value = Convert.ChangeType(value, targetType);
					} catch (Exception) {
						value = null;
					}
				}
			} else if (valueType == mContext.TypeBinary) {
				int size;
				if (!readSize(stream, out size, out numBytesRead)) {
					return false;
				}
				int n;
				byte subType;
				if (!readSubType(stream, out subType, out n)) {
					return false;
				}
				numBytesRead += n;

				var valueBytes = readValueBytes(stream, size, out n);
				numBytesRead += n;
				if (valueBytes == null) {
					return false;
				}
				var v = new byte[n];
				Buffer.BlockCopy(valueBytes, 0, v, 0, n);
				value = v;
			}
			// TODO decimal
			return value;
		}

		private byte[] readValueBytes(Stream stream, int size, out int numBytesRead) {
			var readBuffer = getReadBuffer(size);
			if (!StreamUtils.ReadBytes(stream, readBuffer, 0, size, out numBytesRead)) {
				return null;
			}
			return readBuffer;
		}

		private object readArray(Stream stream, Type arrayType, out int numBytesRead) {
			int size;
			numBytesRead = 0;
			if (!readSize(stream, out size, out numBytesRead)) {
				numBytesRead = 0; // NOTE 表示结束
				mTempArrayListLevel--;
				return null;
			}
			var numBytesToRead = size - numBytesRead;

			Type elementType = null;
			if (arrayType.IsArray) {
				elementType = arrayType.GetElementType();
			} else if (arrayType.GetGenericTypeDefinition() == typeof(List<>)) {
				elementType = arrayType.GetGenericArguments()[0];
			} else {
				numBytesRead = numBytesToRead;
				stream.Seek(stream.Position + numBytesToRead, SeekOrigin.Begin);
				mTempArrayListLevel--;
				return null;
			}

			var tempArrayList = getTempArrayList(mTempArrayListLevel);
			mTempArrayListLevel++;
			while (numBytesToRead > 1) {
				byte valueType;
				string key;
				int n;
				if (!readKey(stream, out valueType, out key, out n)) {
					numBytesRead = 0; // NOTE 表示结束
					mTempArrayListLevel--;
					return null;
				}
				numBytesToRead -= n;
				numBytesRead += n;
				// 数组视为这样的子文档{"0":ele0, "1":ele1, "2":ele2,...}，直接忽略顺序
				var element = readValue(stream, valueType, arrayType.GetElementType(), out n);
				numBytesToRead -= n;
				numBytesRead += n;
				if (element == null) {
					if (valueType == 0x04) { // "\x04" e_name document	Array
						element = readArray(stream, elementType, out n);
						numBytesToRead -= n;
						numBytesRead += n;
					} else if (valueType == 0x03) { // "\x03" e_name document	Embedded document
						element = Activator.CreateInstance(elementType);
						n = deserializeObject(stream, element);
						if (n <= 0) {
							numBytesRead = 0; // NOTE 表示结束
							mTempArrayListLevel--;
							return null;
						}
						numBytesToRead -= n;
						numBytesRead += n;
					}
					if (element == null) {
						continue;
					}
				}
				tempArrayList.Add(element);
			}
			stream.Seek(stream.Position + numBytesToRead, SeekOrigin.Begin);
			numBytesRead += numBytesToRead;

			object result = null;
			if (arrayType.IsArray) {
				var array = Array.CreateInstance(arrayType.GetElementType(), tempArrayList.Count);
				for (var i = 0; i < tempArrayList.Count; i++) {
					var a = tempArrayList[i];
					array.SetValue(a, i);
				}
				result = array;
			} else if (arrayType.GetGenericTypeDefinition() == typeof(List<>)) {
				var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
				foreach (var a in tempArrayList) {
					list.Add(a);
				}
				result = list;
			}
			return result;
		}

		private List<List<object>> mTempArrayLists;
		private int mTempArrayListLevel = 0;
		private List<object> getTempArrayList(int level) {
			if (mTempArrayLists == null) {
				mTempArrayLists = new List<List<object>>();
			}
			List<object> al = null;
			if (mTempArrayLists.Count <= level) {
				al = new List<object>();
				mTempArrayLists.Add(al);
			} else {
				al = mTempArrayLists[level];
			}
			al.Clear();
			return al;
		}

		private Type getFieldType(FieldInfo field, PropertyInfo property) {
			if (field != null) {
				return field.FieldType;
			} else if (property != null) {
				return property.PropertyType;
			}
			return null;
		}

		private void setFieldValue(object obj, FieldInfo field, PropertyInfo property, object value) {
			if (field != null) {
				field.SetValue(obj, value);
				if (IsDebug) {
					setValueDebugString(obj.GetType().Name, field.Name, value);
				}
			} else if (property != null) {
				property.SetValue(obj, value, null);
				if (IsDebug) {
					setValueDebugString(obj.GetType().Name, property.Name, value);
				}
			}
		}

		private byte[] getReadBuffer(int size) {
			if (size <= 0) {
				return null;
			}
			if (mReadBuffer == null || size > mReadBuffer.Length) {
				mReadBuffer = new byte[size];
				return mReadBuffer;
			}
			return mReadBuffer;
		}

		private void setValueDebugString(string objName, string fieldName, object value) {
			if (value is Array) {
				mDebugString.Append(objName + "." + fieldName + "=[");
				var vs = value as Array;
				foreach (var v in vs) {
					mDebugString.Append(v + ",");
				}
				mDebugString.Append("]\n");
			} else {
				mDebugString.Append(objName + "." + fieldName + "=" + value + "\n");
			}
		}
		#endregion

	}

}
