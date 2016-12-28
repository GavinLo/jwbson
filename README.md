#jwbson

##描述
* 一个bson序列化/反序列化的简单实现。
* 支持常用的数据类型：double/float, string, array, bytes, bool, int/int32/int64等。

##Description
* A Simple Bson Serializer
* Support data types: double/float, string, array, bytes, bool, int/int32/int64...

##用法 - Usage
###序列化 - Serialize

	public class A {
		[jw.SerializedField]
		public int MyInt = 123;
		
		[jw.SerializedField(“MyDouble”)]
		public float MyFloat = 123.456f;
	}
	
	var a = new A();
	var f = File.Open(“test.bson”, FileMode.Create);
	var b = new uapp.Bson(uapp.Bson.Context11);
	b.Serialize(a, f);
	f.Close();
	
###反序列化 - Deserialize

	var f = File.Open(“test.bson”, FileMode.Open);
	var a = new A();
	var b = new uapp.Bson(uapp.Bson.Context11);
	b.Deserialize(f, a);
	f.Close();
	
###反序列化 - Deserialize
__不使用原数据结构，使用类似的数据结构 - deserialize to a new similar data structure__

	public class C {
		[jw.SerializedField]
		public int MyInt;
		
		[jw.SerializedField(“MyDouble”)]
		public float MyFloat;
	}

	var f = File.Open(“test.bson”, FileMode.Open);
	var c = new C();
	var b = new uapp.Bson(uapp.Bson.Context11);
	b.Deserialize(f, c);
	f.Close();
