
namespace jw {

	public sealed class Flags {

		private int mFlags;
		public static Flags Zero = new Flags();

		public Flags() {
			mFlags = 0;
		}

		public Flags(int f) {
			mFlags = f;
		}

		public int Value {
			get {
				return mFlags;
			}
		}

		public void Add(int flag) {
			mFlags |= flag;
		}

		public void Remove(int flag) {
			mFlags &= ~flag;
		}

		public bool Test(int flag) {
			//return ((mFlags & flag) == flag) && (flag != 0 || mFlags == flag);
			return Test(mFlags, flag);
		}

		public static int Add(int flag, int addFlag) {
			return flag | addFlag;
		}

		public static int Remove(int flag, int removeFlag) {
			return flag & ~removeFlag;
		}

		public void Set(int offset, bool value) {
			if (offset < 0) {
				return;
			}
			int op = 0x1 << offset;
			if (value) {
				Add(op);
			} else {
				Remove(op);
			}
		}

		public static bool Test(int flag, int testFlag) {
			return ((flag & testFlag) == testFlag) && (flag != 0x00000000);
		}

		public bool IsSet(int offset) {
			if (offset < 0) {
				return false;
			}
			int test = 0x1 << offset;
			return (mFlags & test) != 0;
		}

		public static bool Equals(int flag, int testFlag) {
			return flag == testFlag;
		}

		public void Clear() {
			mFlags = 0;
		}
	}
	
}
