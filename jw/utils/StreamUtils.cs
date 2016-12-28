using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace jw {

	public class StreamUtils {
		
		private static byte[] mBlock;
		private static List<byte> mStringByteArray;
		private static byte[] mStringBuffer;
		private static byte[] mIntBuffer;

		/// <summary>
        /// 精确读取（如果读取的总字节数不是count要求的数量，也表示读取失败）
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="numBytesRead"></param>
        /// <returns></returns>
		public static bool ReadBytes(Stream stream, byte[] bytes, int offset, int count, out int numBytesRead) {
			numBytesRead = 0;
			if (stream == null || bytes == null || offset < 0 || count <= 0) {
				return false;
			}
			if (bytes.Length < offset + count) {
				return false;
			}
			if (!stream.CanRead) {
				return false;
			}
			var numBytesToRead = count;
            numBytesRead = 0;
			while (numBytesToRead > 0) {
				try {
					var n = stream.Read(bytes, numBytesRead, numBytesToRead);
					if (n == 0) {
						break;
					}
					numBytesRead += n;
					numBytesToRead -= n;
				} catch (Exception) {
					return false;
				}
			}
			if (numBytesRead != count) {
				return false;
			}
			return true;
		}

		public static bool ReadBytes(Stream stream, byte[] bytes, out int numBytesRead) {
			numBytesRead = 0;
			if (bytes == null) {
				return false;
			}
			return ReadBytes(stream, bytes, 0, bytes.Length, out numBytesRead);
		}

		public static bool ReadString(Stream stream, Encoding encoding, int size, out string str, out int numBytesRead) {
			str = null;
			numBytesRead = 0;
			if (stream == null) {
				return false;
			}
			if (!stream.CanRead) {
				return false;
			}
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			if (size <= 0) {
				if (mBlock == null) {
					mBlock = new byte[16];
				}
				if (mStringByteArray == null) {
					mStringByteArray = new List<byte>();
				}
				mStringByteArray.Clear();
				var oldPosition = stream.Position;
				while (true) {
					try {
						var n = stream.Read(mBlock, 0, mBlock.Length);
						if (n == 0) {
							str = encoding.GetString(mStringByteArray.ToArray());
							return true;
						}
						for (var i = 0; i < n; i++) {
							var b = mBlock[i];
							if (b == 0x00) {
								numBytesRead++;
								stream.Seek(oldPosition + numBytesRead, SeekOrigin.Begin);
								str = encoding.GetString(mStringByteArray.ToArray());
								return true;
							}
							mStringByteArray.Add(b);
							numBytesRead++;
						}
					} catch (Exception) {
						return false;
					}
				}
			} else {
				if (mStringBuffer == null || mStringBuffer.Length < size) {
					mStringBuffer = new byte[size];
				}
				if (!ReadBytes(stream, mStringBuffer, 0, size, out numBytesRead)) {
					return false;
				}
				str = encoding.GetString(mStringBuffer, 0, size);
				return true;
			}
		}

		public static bool ReadInt32(Stream stream, Endian endian, out int result, out int numBytesRead) {
			result = 0;
			numBytesRead = 0;
			if (stream == null) {
				return false;
			}
			if (!stream.CanRead) {
				return false;
			}
			if (mIntBuffer == null) {
				mIntBuffer = new byte[sizeof(int)];
			}
			try {
				numBytesRead = stream.Read(mIntBuffer, 0, mIntBuffer.Length);
				if (numBytesRead == 0) {
					return false;
				}
				result = BitUtils.ToInt(mIntBuffer, 0, endian);
			} catch (Exception) {
				return false;
			}
			return true;
		}

		public bool WriteBytes(Stream stream, byte[] bytes) {
			if (stream == null) {
				return false;
			}
			if (bytes == null) {
				return true;
			}
			if (!stream.CanWrite) {
				return false;
			}
			try {
				stream.Write(bytes, 0, bytes.Length);
			} catch (Exception) {
				return false;
			}
			return true;
		}

	}

}
