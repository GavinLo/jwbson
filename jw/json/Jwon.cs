using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace jw {

	public delegate string JwonContextBytesEncodingFunc(byte[] bytes);

	public interface JwonContext {
		/// <summary>
        /// 对象开始符
        /// </summary>
        /// <returns></returns>
		char ObjectStart { get; }

		/// <summary>
        /// 对象结束符
        /// </summary>
        /// <returns></returns>
		char ObjectEnd { get; }

		/// <summary>
        /// 数组开始符
        /// </summary>
        /// <returns></returns>
		char ArrayStart { get; }

		/// <summary>
        /// 数组结束符
        /// </summary>
        /// <returns></returns>
		char ArrayEnd { get; }

		/// <summary>
        /// 是否把数组序列化成对象，相应的key为0、1、2、3……
        /// </summary>
        /// <returns></returns>
		bool ArrayAsObject { get; }

		/// <summary>
        /// key开始符
        /// </summary>
        /// <returns></returns>
		char KeyStart { get; }

		/// <summary>
        /// key结束符
        /// </summary>
        /// <returns></returns>
		char KeyEnd { get; }

		/// <summary>
        /// value开始符
        /// </summary>
        /// <returns></returns>
		char ValueStart { get; }

		/// <summary>
        /// value结束符
        /// </summary>
        /// <returns></returns>
		char ValueEnd { get; }

		/// <summary>
        /// key与value之间的分隔符
        /// </summary>
        /// <returns></returns>
		char KeyValueSeparator { get; }

		/// <summary>
        /// value之间的分隔符，key-value对之间的分隔符
        /// </summary>
        /// <returns></returns>
		char ValueSeparator { get; }

		/// <summary>
        /// 定义引号，一般用于字符串的value
        /// </summary>
        /// <returns></returns>
		char Quote { get; }

		/// <summary>
        /// 字符串类型的value是否需要引号包起来
        /// </summary>
        /// <returns></returns>
		bool StringNeedQuote { get; }

		/// <summary>
        /// 输出文本的编码方式
        /// </summary>
        /// <returns></returns>
		Encoding Encoding { get; }

		/// <summary>
        /// 如何序列化二进制流
        /// </summary>
        /// <returns></returns>
		JwonContextBytesEncodingFunc BytesEncodingFunc { get; }

		/// <summary>
        /// 32位浮点数保留位数，0表示无限制
        /// </summary>
        /// <returns></returns>
		int FloatReserve { get; }

		/// <summary>
        /// 64位浮点数保留位数，0表示无限制
        /// </summary>
        /// <returns></returns>
		int DoubleReserve { get; }

	}

	public class JsonContext : JwonContext {

		public char ObjectStart {
			get {
				return '{';
			}
		}

		public char ObjectEnd {
			get {
				return '}';
			}
		}

		public char ArrayStart {
			get {
				return '[';
			}
		}

		public char ArrayEnd {
			get {
				return ']';
			}
		}

		public bool ArrayAsObject {
			get {
				return false;
			}
		}

		public char KeyStart {
			get {
				return '\"';
			}
		}

		public char KeyEnd {
			get {
				return '\"';
			}
		}

		public char ValueStart {
			get {
				return Constants.CharZero;
			}
		}

		public char ValueEnd {
			get {
				return Constants.CharZero;
			}
		}

		public char KeyValueSeparator {
			get {
				return ':';
			}
		}

		public char ValueSeparator { 
			get {
				return ',';
			}
		}

		public char Quote {
			get {
				return '\"';
			}
		}

		public bool StringNeedQuote { 
			get {
				return true;
			}
		}

		public Encoding Encoding {
			get {
				return Encoding.UTF8;
			}
		}

		public JwonContextBytesEncodingFunc BytesEncodingFunc {
			get {
				return (byte[] bytes) => {
					return Convert.ToBase64String(bytes);
				};
			}
		}

		public int FloatReserve { 
			get {
				return 3;
			}
		}

		public int DoubleReserve {
			get {
				return 6;
			}
		}

	}

	/// <summary>
    /// JunWen Object Notation
    /// </summary>
	public class Jwon {

		public static JwonContext ContextJson = new JsonContext();

		private JwonContext mContext;
		private string mFloatFormat;
		private string mDoubleFormat;

		public Jwon(JwonContext context = null) {
			if (context == null) {
				mContext = ContextJson;
			} else {
				mContext = context;
			}

			mFloatFormat = "{0:#.";
			for (var i = 0; i < mContext.FloatReserve; i++) {
				mFloatFormat += "#";
			}
			mFloatFormat += "}";

			mDoubleFormat = "{0:#.";
			for (var i = 0; i < mContext.DoubleReserve; i++) {
				mDoubleFormat += "#";
			}
			mDoubleFormat += "}";
		}

		public bool Serialize(object obj, Stream stream, string name = null) {
			if (obj == null || stream == null) {
				return false;
			}
			if (!stream.CanWrite) {
				return false;
			}
			var sw = new StreamWriter(stream, mContext.Encoding);
			serializeObject(name, obj, sw, false);
			sw.Flush();
			return true;
		}

		public string SerializeText(object obj, string name = null) {
			if (obj == null) {
				return null;
			}
			var ms = new MemoryStream();
			if (!Serialize(obj, ms, name)) {
				return null;
			}
			return mContext.Encoding.GetString(ms.ToArray());
		}

		public bool Deserialize(Stream stream, object obj) {
			if (obj == null || stream == null) {
				return false;
			}
			if (!stream.CanRead) {
				return false;
			}
			var sr = new StreamReader(stream, mContext.Encoding);
			mTempArrayListLevel = 0;
			try {
				deserializeObject(sr, obj);
			} catch {
				return false;
			}
			return true;
		}

		public bool DeserializeText(string str, object obj) {
			if (obj == null || str == null) {
				return false;
			}
			var ms = new MemoryStream(mContext.Encoding.GetBytes(str));
			return Deserialize(ms, obj);
		}

		#region serialize
		private bool serializeObject(string objKey, object obj, StreamWriter stream, bool needValueSeparator) {
			if (obj == null) {
				return false;
			}

			if (needValueSeparator) {
				writeChar(stream, mContext.ValueSeparator);
			}

			writeObjectStart(stream, objKey);

			var type = obj.GetType();
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fields != null) {
				needValueSeparator = false;
				foreach (var field in fields) {
					var serializedField = ReflectUtils.GetCustomAttribute<SerializedField>(field);
					if (serializedField == null || !serializedField.UseToSerialize) {
						continue;
					}
					var key = serializedField.Name == null ? field.Name : serializedField.Name;
					if (serializeKeyValue(key, field.GetValue(obj), stream, needValueSeparator)) {
						needValueSeparator = true;
					}
				}
			}
			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (properties != null) {
				needValueSeparator = false;
				foreach (var property in properties) {
					var serializedField = ReflectUtils.GetCustomAttribute<SerializedField>(property);
					if (serializedField == null || !serializedField.UseToSerialize) {
						continue;
					}
					var key = serializedField.Name == null ? property.Name : serializedField.Name;
					if (serializeKeyValue(key, property.GetValue(obj, null), stream, needValueSeparator)) {
						needValueSeparator = true;
					}
				}
			}

			writeObjectEnd(stream);
			return true;
		}

		private bool serializeKeyValue(string key, object value, StreamWriter stream, bool needValueSeparator) {
			if (value == null) {
				return false;
			}

			Type valueType;
			string valueString;
			if (!getValueString(value, out valueType, out valueString)) {
				var b = false;
				if (value is IList || value.GetType().IsArray) {
					b = serializeArray(key, value as IEnumerable, stream, needValueSeparator);
				} else if (value is IDictionary) {
					b = serializeDict(key, value as IDictionary, stream, needValueSeparator);
				} else {
					b = serializeObject(key, value, stream, needValueSeparator);
				}
				return b;
			}

			if (needValueSeparator) {
				writeChar(stream, mContext.ValueSeparator);
			}
			writeKeyValue(stream, key, valueString);
			return true;
		}

		private bool serializeArray(string key, IEnumerable array, StreamWriter stream, bool needValueSeparator) {
			if (needValueSeparator) {
				writeChar(stream, mContext.ValueSeparator);
			}

			writeArrayStart(stream, key);
			needValueSeparator = false;
			var i = 0;
			foreach (var a in array) {
				string k = null;
				if (mContext.ArrayAsObject) {
					k = i.ToString();
				}
				if (serializeKeyValue(k, a, stream, needValueSeparator)) {
					needValueSeparator = true;
					i++;
				}
			}
			writeArrayEnd(stream);
			return true;
		}

		private bool serializeDict(string key, IDictionary dict, StreamWriter stream, bool needValueSeparator) {
			var genericTypes = dict.GetType().GetGenericArguments();
			if (genericTypes[0] != typeof(string)) { // NOTE 不支持string以外的key
				return false;
			}
			
			if (needValueSeparator) {
				writeChar(stream, mContext.ValueSeparator);
			}

			writeObjectStart(stream, key);
			needValueSeparator = false;
			var keys = dict.Keys;
			foreach (var d in keys) {
				var k = d.ToString();
				if (serializeKeyValue(k, dict[d], stream, needValueSeparator)) {
					needValueSeparator = true;
				}
			}
			writeObjectEnd(stream);
			return true;
		}

		private void writeString(StreamWriter stream, string str, bool quote) {
			if (str == null) {
				return;
			}
			quote = quote && mContext.Quote != Constants.CharZero;
			if (quote) {
				stream.Write(mContext.Quote);
			}
			stream.Write(str);
			if (quote) {
				stream.Write(mContext.Quote);
			}
		}

		private void writeChar(StreamWriter stream, char c) {
			if (c == Constants.CharZero) {
				return;
			}
			stream.Write(c);
		}

		private void writeObjectStart(StreamWriter stream, string key) {
			writeKey(stream, key);
			writeChar(stream, mContext.ObjectStart);
		}

		private void writeObjectEnd(StreamWriter stream) {
			writeChar(stream, mContext.ObjectEnd);
		}

		private void writeArrayStart(StreamWriter stream, string key) {
			writeKey(stream, key);
			writeChar(stream, mContext.ArrayAsObject ? mContext.ObjectStart : mContext.ArrayStart);
		}

		private void writeArrayEnd(StreamWriter stream) {
			writeChar(stream, mContext.ArrayAsObject ? mContext.ObjectEnd : mContext.ArrayEnd);
		}

		private void writeKeyValue(StreamWriter stream, string key, string value) {
			if (value == null) {
				return;
			}
			writeKey(stream, key);
			writeValue(stream, value);
		}

		private void writeKey(StreamWriter stream, string key) {
			if (key == null) {
				return;
			}
			writeChar(stream, mContext.KeyStart);
			stream.Write(key);
			writeChar(stream, mContext.KeyEnd);
			writeChar(stream, mContext.KeyValueSeparator);
		}

		private void writeValue(StreamWriter stream, string value) {
			if (value == null) {
				return;
			}
			writeChar(stream, mContext.ValueStart);
			stream.Write(value);
			writeChar(stream, mContext.ValueEnd);
		}

		private StringBuilder mTempStringBuilder;
		private bool getValueString(object value, out Type type, out string bytes) {
			type = value.GetType();
			bytes = null;
			
			if (value is int || value is short 
			|| value is uint || value is ushort 
			|| value is long || value is ulong) {
				bytes = value.ToString();
				return true;
			} else if (value is float) {
				if (mContext.FloatReserve <= 0) {
					bytes = value.ToString();
				} else {
					var str = String.Format(mFloatFormat, value);
					bytes = str;
				}
				return true;
			} else if (value is double) {
				if (mContext.DoubleReserve <= 0) {
					bytes = value.ToString();
				} else {
					var str = String.Format(mDoubleFormat, value);
					bytes = str;
				}
				return true;
			} else if (value is decimal) {
				// "\x13" e_name decimal128	128-bit decimal floating point
				// type = 0x13;
				// TODO 
				return false;
			} else if (value is string) {
				if (mTempStringBuilder == null) {
					mTempStringBuilder = new StringBuilder();
				}
				mTempStringBuilder.Length = 0;
				var quote = mContext.Quote;
				if (mContext.StringNeedQuote && quote != Constants.CharZero) {
					mTempStringBuilder.Append(quote);
				}
				mTempStringBuilder.Append(value as string);
				if (mContext.StringNeedQuote && quote != Constants.CharZero) {
					mTempStringBuilder.Append(quote);
				}
				bytes = mTempStringBuilder.ToString();
				return true;
			} else if (value is bool) {
				var b = (bool)value;
				bytes = b ? "true" : "false";
				return true;
			} else if (value is byte[]) {
				var bytesEncodingFunc = mContext.BytesEncodingFunc;
				if (bytesEncodingFunc == null) {
					return false;
				}
				if (mTempStringBuilder == null) {
					mTempStringBuilder = new StringBuilder();
				}
				mTempStringBuilder.Length = 0;
				var valueBytes = value as byte[];
				var str = bytesEncodingFunc(valueBytes);
				var quote = mContext.Quote;
				if (quote != Constants.CharZero) {
					mTempStringBuilder.Append(quote);
				}
				mTempStringBuilder.Append(str);
				if (quote != Constants.CharZero) {
					mTempStringBuilder.Append(quote);
				}
				bytes = mTempStringBuilder.ToString();
				return true;
			}
			return false;
		}
		#endregion

		#region deserialize
		private static bool isValidChar(char c) {
			return c > 32;
		}

		private StringBuilder mKeyBuilder;
		private StringBuilder mValueBuilder;
		private void deserializeObject(StreamReader stream, object obj, Type parentElementType = null /** NOTE 这个主要是为了数组或者嵌套类型而使用的参数，因为重用的templist里面都是object类型，不通过参数无法知道真正的元素类型 **/) {
			if (mKeyBuilder == null) {
				mKeyBuilder = new StringBuilder();
			}
			if (mValueBuilder == null) {
				mValueBuilder = new StringBuilder();
			}
			var keyParsing = false;
			var valueParsing = false;
			FieldInfo field = null;
			PropertyInfo property = null;
			string key = null;
			object value = null;

			var ci = stream.Read();
			while (ci != -1) {
				var c = (char)ci;
				ci = stream.Read();

				if (c == mContext.ObjectStart) {
					Type valueType;
					getKeyInfo(obj, key, out valueType, out field, out property);
					if (parentElementType != null) {
						valueType = parentElementType;
					}
					if (valueType != null) {
						value = Activator.CreateInstance(valueType);
						deserializeObject(stream, value);
					}
				} else if (c == mContext.ObjectEnd) {
					// NOTE 递归调用，这里等于结束当前节点的解析
					break;
				} else if (c == mContext.ArrayStart) {
					Type arrayType;
					getKeyInfo(obj, key, out arrayType, out field, out property);
					if (parentElementType != null) {
						arrayType = parentElementType;
					}
					if (arrayType != null) {
						var elemType = getElementType(arrayType);
						var tempArrayList = getTempArrayList(mTempArrayListLevel); // NOTE 嵌套列表重用
						mTempArrayListLevel++;
						deserializeObject(stream, tempArrayList, elemType);
						mTempArrayListLevel--;

						// NOTE 重新生成容器对象，templist只作为缓存
						if (arrayType.IsArray) {
							var array = Array.CreateInstance(elemType, tempArrayList.Count);
							for (var i = 0; i < tempArrayList.Count; i++) {
								var a = tempArrayList[i];
								array.SetValue(a, i);
							}
							value = array;
						} else if (arrayType.GetGenericTypeDefinition() == typeof(List<>)) {
							var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType)) as IList;
							foreach (var a in tempArrayList) {
								list.Add(a);
							}
							value = list;
						}
					}
					continue;
				} else if (c == mContext.ArrayEnd) {
					// NOTE 递归调用，这里等于结束当前节点的解析
					break;
				}

				// NOTE 解析key，如果为数组则只解析value
				if (key == null && !valueParsing && !(obj is IList)) {
					key = parseKey(c, mKeyBuilder, ref keyParsing);
					if (key != null) {
						continue;
					}
				}
				// NOTE 解析value
				if (value == null && !keyParsing) {
					var val = parseValue(c, mValueBuilder, ref valueParsing);
					if (val != null) {
						Type valueType;
						getKeyInfo(obj, key, out valueType, out field, out property);
						if (parentElementType != null) {
							valueType = parentElementType;
						}
						if (valueType == null || !getValueOfType(val, valueType, out value)) {
							key = null;
							value = null;
						}
					}
				}
				// NOTE 反射到对象
				if (value != null) {
					if (obj is IDictionary) {
						var dict = obj as IDictionary;
						dict[key] = value;
					} else if (obj is IList) {
						var list = obj as IList;
						list.Add(value);
					} else {
						setFieldValue(obj, field, property, value);
					}
					key = null;
					value = null;
				}
			}
		}

		private string parseKey(char c, StringBuilder builder, ref bool parsing) {
			return parseWord(c, mContext.KeyStart, mContext.KeyEnd, builder, ref parsing);
		}

		private string parseValue(char c, StringBuilder builder, ref bool parsing) {
			return parseWord(c, mContext.ValueStart, mContext.ValueEnd, builder, ref parsing);
		}

		private string parseWord(char c, char start, char end, StringBuilder builder, ref bool parsing) {
			string word = null;
			if (!isValidChar(c)) { // NOTE 无效字符处理
				if (parsing) {
					if (c != ' ' && c != '\t') { // NOTE 在解析过程当中允许空格和tab，其他情况忽略
						word = builder.ToString();
						builder.Length = 0;
						parsing = false;
						return word;
					} else {
						return null;
					}
				} else {
					return null;
				}
			}
			if (start == Constants.CharZero) { // NOTE 开始符为空时，遇到任何有效非分割符，则开始解析
				if (!parsing) {
					if (c == mContext.KeyValueSeparator || c == mContext.ValueSeparator || c == mContext.ObjectEnd || c == mContext.ArrayEnd || c == mContext.ObjectStart || c == mContext.ArrayStart) {
						
					} else {
						builder.Length = 0;
						parsing = true;
					}
				}
			}
			if (end == Constants.CharZero) { // NOTE 结束符为空时，遇到任何分割符，则停止解析
				if (parsing) {
					if (c == mContext.KeyValueSeparator || c == mContext.ValueSeparator || c == mContext.ObjectEnd || c == mContext.ArrayEnd || c == mContext.ObjectStart || c == mContext.ArrayStart) {
						word = builder.ToString();
						builder.Length = 0;
						parsing = false;
						return word;
					} else {
						
					}
				}
			}
			if (c == end) { // NOTE 遇到结束符
				if (end == start) { // NOTE 但如果结束符与开始符相同，则通过parsing来判断为开始还是结束解析
					if (parsing) {
						word = builder.ToString();
						builder.Length = 0;
						parsing = false;
						return word;
					} else {
						builder.Length = 0;
						parsing = true;
						return word;
					}
				}
				// NOTE 解析结束
				word = builder.ToString();
				builder.Length = 0;
				parsing = false;
				return word;
			}
			if (parsing) { // NOTE 解析过程
				builder.Append(c);
			}
			if (c == start) { // NOTE 遇到开始符，开始解析，由于会改变parsing状态，故放在解析过程之后判断
				builder.Length = 0;
				parsing = true;
				return null;
			}
			return word;
		}

		/// <summary>
        /// 获取obj中key所指定的域信息和该域的类型信息，以便解析相应类型的值。如果obj为容器，则只获取其需要解析的值信息
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <param name="valueType"></param>
        /// <param name="field"></param>
        /// <param name="property"></param>
		private void getKeyInfo(object obj, string key, out Type valueType, out FieldInfo field, out PropertyInfo property) {
			valueType = null;
			field = null;
			property = null;
			var objType = obj.GetType();

			if (obj is IDictionary) {
				var genericTypes = objType.GetGenericArguments();
				if (genericTypes[0] != typeof(string)) { // NOTE 不支持string以外的key
					return;
				}
				valueType = genericTypes[1];
			} else if (objType.IsArray) {
				valueType = objType.GetElementType();
			} else if (obj is IList) {
				var genericTypes = objType.GetGenericArguments();
				valueType = genericTypes[0];
			} else {
				var serializedField = getSerializedField(objType, key, out field);
				if (serializedField != null && !serializedField.UseToDeserialize) {
					field = null;
				}
				serializedField = getSerializedField(objType, key, out property);
				if (serializedField != null && !serializedField.UseToDeserialize) {
					property = null;
				}
				valueType = getFieldType(field, property);
			}
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

		private Type getFieldType(FieldInfo field, PropertyInfo property) {
			if (field != null) {
				return field.FieldType;
			} else if (property != null) {
				return property.PropertyType;
			}
			return null;
		}

		private Type getElementType(Type arrayType) {
			Type elementType = null;
			if (arrayType.IsArray) {
				elementType = arrayType.GetElementType();
			} else if (arrayType.GetGenericTypeDefinition() == typeof(List<>)) {
				elementType = arrayType.GetGenericArguments()[0];
			}
			return elementType;
		}

		private void setFieldValue(object obj, FieldInfo field, PropertyInfo property, object value) {
			if (field != null) {
				field.SetValue(obj, value);
			} else if (property != null) {
				property.SetValue(obj, value, null);
			}
		}

		private bool getValueOfType(string value, Type targetType, out object result) {
			result = null;
			if (targetType == typeof(int)) {
				int r;
				if (!int.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(uint)) {
				uint r;
				if (!uint.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(short)) {
				short r;
				if (!short.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(ushort)) {
				ushort r;
				if (!ushort.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(long)) {
				long r;
				if (!long.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(ulong)) {
				ulong r;
				if (!ulong.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(float)) {
				float r;
				if (!float.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(double)) {
				double r;
				if (!double.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(decimal)) {
				decimal r;
				if (!decimal.TryParse(value, out r)) {
					return false;
				}
				result = r;
				return true;
			} else if (targetType == typeof(bool)) {
				result = value.ToLower() == "true";
				return true;
			} else if (targetType == typeof(string)) {
				if (mContext.StringNeedQuote) {
					result = removeStringQuote(value, mContext.Quote);
				} else {
					result = value;
				}
				return true;
			} else if (targetType == typeof(byte[])) {
				if (mContext.StringNeedQuote) {
					value = removeStringQuote(value, mContext.Quote);
				}
				try {
					result = Convert.FromBase64String(value);
				} catch {
					return false;
				}
				return true;
			}
			return false;
		}

		private static string removeStringQuote(string str, char quote) {
			var i = str.IndexOf(quote);
			if (i >= 0) {
				str = str.Remove(i, 1);
			}
			i = str.LastIndexOf(quote);
			if (i >= 0) {
				str = str.Remove(i);
			}
			return str;
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
		#endregion

	}

}
