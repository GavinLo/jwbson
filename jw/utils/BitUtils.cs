using System;

namespace jw {

	public static class BitUtils {

        public static byte[] GetUShortBytes(ushort value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetUIntBytes(uint value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetULongBytes(ulong value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetFloatBytes(float value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetDoubleBytes(double value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetLongBytes(long value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetIntBytes(int value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetShortBytes(short value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetCharBytes(char value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

        public static byte[] GetBoolBytes(bool value, Endian endian = Endian.Big) {
			var bytes = BitConverter.GetBytes(value);
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return bytes;
			}
			Array.Reverse(bytes);
			return bytes;
		}

		public static bool ToBool(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToBoolean(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(bool));
			return BitConverter.ToBoolean(value, startIndex);
		}
		
        public static char ToChar(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToChar(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(char));
			return BitConverter.ToChar(value, startIndex);
		}

        public static double ToDouble(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToDouble(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(double));
			return BitConverter.ToDouble(value, startIndex);
		}

        public static short ToShort(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToInt16(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(short));
			return BitConverter.ToInt16(value, startIndex);
		}

        public static int ToInt(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToInt32(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(int));
			return BitConverter.ToInt32(value, startIndex);
		}

        public static long ToLong(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToInt64(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(long));
			return BitConverter.ToInt64(value, startIndex);
		}

        public static float ToFloat(byte[] value, int startIndex = 0, Endian endian = Endian.Big) {
			var systemEndian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
			if (systemEndian == endian) {
				return BitConverter.ToSingle(value, startIndex);
			}
			Array.Reverse(value, startIndex, sizeof(float));
			return BitConverter.ToSingle(value, startIndex);
		}

	}

}
