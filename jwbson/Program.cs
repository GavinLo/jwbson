using System;
using System.IO;
using System.Collections.Generic;

namespace jwbson {

	public class A {

		[jw.SerializedField]
		public Dictionary<string, float> MyDict = new Dictionary<string, float>() {
			{"aaa", 111.222f },
			{"bbb", 222.666f },
		};

		[jw.SerializedField]
		public Dictionary<string, B> MyObjInDict = new Dictionary<string, B>() {
			{"aaa", new B() },
			{"bbb", new B() },
		};

		[jw.SerializedField]
		public short MyInt = 234;

		[jw.SerializedField]
		public float MyFloat = 123.456f;

		[jw.SerializedField]
		public double MyDouble = 123.45678910234;

		[jw.SerializedField]
		public string MyString = "hello";

		[jw.SerializedField]
		public bool MyBool = false;

		[jw.SerializedField]
		public byte[] bsss = new byte[] {
			0x01, 0x02, 0x03, 0x04,
		};

		[jw.SerializedField]
		public string[] MyArray = new string[] {
			"me",
			"you",
			null,
			"she",
			"haha",
		};

		[jw.SerializedField]
		public B bbb = new B();

		[jw.SerializedField]
		public B MyNull = null;

		[jw.SerializedField]
		public B[] MyBs = new B[] {
			new B(),
			new B(),
			new B(),
		};

		[jw.SerializedField]
		public string[][] MyArrayInArray = new string[][] {
			new string[]{"i", "love", "you"},
			new string[]{"she", "love", "me"},
		};

	}

	public class B {
		[jw.SerializedField]
		public short MyInt = 2342;

		[jw.SerializedField]
		public float MyFloat = 1234.456f;

		[jw.SerializedField]
		public string MyString = "world";

		[jw.SerializedField]
		public bool MyBool = true;
	}

	public class C {
		[jw.SerializedField]
		public Dictionary<string, float> MyDict;

		[jw.SerializedField]
		public Dictionary<string, D> MyObjInDict;


		[jw.SerializedField]
		public short MyInt = 0;

		[jw.SerializedField]
		public float MyFloat = 0f;

		[jw.SerializedField]
		public string MyString = "";

		[jw.SerializedField]
		public bool MyBool = false;

		[jw.SerializedField]
		public List<string> MyArray = null;

		[jw.SerializedField("MyBs")]
		public List<D> MyDs = null;

		[jw.SerializedField("bbb")]
		public D ddd = null;

		[jw.SerializedField]
		// public string[][] MyArrayInArray = null;
		public List<string[]> MyArrayInArray = null;

		[jw.SerializedField]
		public byte[] bsss = new byte[] {
			0x01, 0x02, 0x03, 0x04,
		};

	}

	public class D {
		[jw.SerializedField]
		public short MyInt = 0;

		[jw.SerializedField]
		public float MyFloat = 0f;

		[jw.SerializedField]
		public string MyString = "";

		[jw.SerializedField]
		public bool MyBool = false;

	}
	
	class MainClass {
		
		public static void Main(string[] args) {
//			SavaBson();
			LoadBson();
		}

		public static void SavaBson() {
			var a = new A();
			var p = "test.bson";
			var f = File.Open(p, FileMode.Create);
			var b = new jw.Bson(jw.Bson.Context11);
			b.IsDebug = true;
			b.Serialize(a, f);
			f.Close();
			Console.WriteLine(b.DebugOutput);
		}

		public static void LoadBson() {
			var p = "test.bson";
			var f = File.Open(p, FileMode.Open);
			var c = new C();
			var b = new jw.Bson(jw.Bson.Context11);
			b.IsDebug = true;
			b.Deserialize(f, c);
			f.Close();
			Console.WriteLine(b.DebugOutput);
		}

	}
}
