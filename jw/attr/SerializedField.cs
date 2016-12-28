using System;

namespace jw {

	public class SerializedField : Attribute {
		
		private string mName;
		private SerializedPermission mPermission;
		
		public SerializedField(string name = null, SerializedPermission permission = SerializedPermission.ReadWrite) {
			mName = name;
			mPermission = permission;
		}
		
		public string Name {
			get {
				return mName;
			}
		}

		public SerializedPermission Permission {
			get {
				return mPermission;
			}
		}

		public bool UseToSerialize {
			get {
				return SerializedPermissionUtils.Test(mPermission, SerializedPermission.Write);
			}
		}

		public bool UseToDeserialize {
			get {
				return SerializedPermissionUtils.Test(mPermission, SerializedPermission.Read);
			}
		}

	}

}
