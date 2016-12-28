
namespace jw {

	public enum SerializedPermission {
		Read = 0x1,
		Write = 0x2,
		ReadOnly = Read,
		ReadWrite = Read | Write,
		WriteOnly = Write,
	}

	public static class SerializedPermissionUtils {
		public static bool Test(SerializedPermission permission, SerializedPermission testPermission) {
			return Flags.Test((int)permission, (int)testPermission);
		}
	}

}
