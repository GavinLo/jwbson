#jwbson

##描述
* 一个bson/json的序列化/反序列化的简单实现。
* 支持常用的数据类型：double/float, string, array, bytes, bool, int/int32/int64等。

##Description
* A Simple Bson/Json Serializer
* Support data types: double/float, string, array, bytes, bool, int/int32/int64...

##用法 - Usage
###序列化 - Serialize
```csharp
public class A {
	[jw.SerializedField]
	public int MyInt = 123;
	
	[jw.SerializedField(“MyDouble”)]
	public float MyFloat = 123.456f;
}
	
// bson
var a = new A();
var f = File.Open(“test.bson”, FileMode.Create);
var b = new jw.Bson(jw.Bson.Context11);
b.Serialize(a, f);
f.Close();
	
// json
a = new A();
var f = File.Open(“test.json”, FileMode.Create);
var j = new jw.Jwon(jw.Jwon.ContextJson);
j.Serialize(a, f);
f.Close();
```
	
###反序列化 - Deserialize
	
```csharp
// bson
var f = File.Open(“test.bson”, FileMode.Open);
var a = new A();
var b = new jw.Bson(jw.Bson.Context11);
b.Deserialize(f, a);
f.Close();
	
// json
f = File.Open(“test.json”, FileMode.Open);
var a = new A();
var j = new jw.Jwon(jw.Jwon.ContextJson);
j.Deserialize(f, a);
f.Close();
```
	
###反序列化 - Deserialize
__不使用原数据结构，使用类似的数据结构 - deserialize to a new similar data structure__

```csharp
public class C {
	[jw.SerializedField]
	public int MyInt;
	
	[jw.SerializedField(“MyDouble”)]
	public float MyFloat;
}

// bson
var f = File.Open(“test.bson”, FileMode.Open);
var c = new C();
var b = new jw.Bson(jw.Bson.Context11);
b.Deserialize(f, c);
f.Close();
	
// json
f = File.Open(“test.json”, FileMode.Open);
c = new C();
var j = new jw.Jwon(jw.Jwon.ContextJson);
j.Deserialize(f, c);
f.Close();
```
