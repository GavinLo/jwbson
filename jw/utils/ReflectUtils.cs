using System;
using System.Reflection;

namespace jw {

	public class ReflectUtils {

		public static T GetCustomAttribute<T>(FieldInfo field) where T : Attribute {
			var attrs = (T[])field.GetCustomAttributes(typeof(T), false);
			if (attrs == null || attrs.Length == 0) {
				return null;
			}
			var attr = attrs[0];
			return attr;
		}

		public static T GetCustomAttribute<T>(PropertyInfo property) where T : Attribute {
			var attrs = (T[])property.GetCustomAttributes(typeof(T), false);
			if (attrs == null || attrs.Length == 0) {
				return null;
			}
			var attr = attrs[0];
			return attr;
		}

		public delegate T MatchFieldDelegate<T>(FieldInfo field, string name);
		public static FieldInfo GetFieldCustomAttribute<T>(Type type, string name, MatchFieldDelegate<T> match, out T attr)  where T : Attribute {
			attr = null;
			if (type == null || name == null) {
				return null;
			}
			if (match == null) {
				return type.GetField(name);
			}
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fields == null || fields.Length == 0) {
				return null;
			}
			foreach (var field in fields) {
				var matchAttr = match(field, name);
				if (matchAttr != null) {
					attr = matchAttr;
					return field;
				}
			}
			return null;
		}

		public delegate T MatchPropertyDelegate<T>(PropertyInfo property, string name);
		public static PropertyInfo GetPropertyCustomAttribute<T>(Type type, string name, MatchPropertyDelegate<T> match, out T attr)  where T : Attribute {
			attr = null;
			if (type == null || name == null) {
				return null;
			}
			if (match == null) {
				return type.GetProperty(name);
			}
			var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (properties == null || properties.Length == 0) {
				return null;
			}
			foreach (var property in properties) {
				var matchAttr = match(property, name);
				if (matchAttr != null) {
					attr = matchAttr;
					return property;
				}
			}
			return null;
		}

	}

}
