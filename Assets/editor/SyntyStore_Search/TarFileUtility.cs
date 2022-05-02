using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace PixelShenanigans.FileUtilities
{
    public class TarFileUtility
	{
		public static MemoryStream DecompressGzFile(Stream stream)
		{
			const int BytesToRead = 4096;

			using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
			{
				using (var memStream = new MemoryStream())
				{
					var buffer = new byte[BytesToRead];
					int bytesRead;

					do
					{
						bytesRead = gzipStream.Read(buffer, 0, BytesToRead);
						memStream.Write(buffer, 0, bytesRead);
					}
					while (bytesRead == BytesToRead);

					memStream.Seek(0, SeekOrigin.Begin);

					return memStream;
				}
			}
		}

		public static IEnumerable<Tuple<string,byte[]>> ReadFiles(Stream stream)
		{
			// https://en.wikipedia.org/wiki/Tar_(computing)
			const int FileNameLength = 100;
			const int FileModeLength = 8;
			const int FileOwnerUserIDLength = 8;
			const int FileGroupUserIDLength = 8;
			const int FileSizeLength = 12;
			const int LastModifiedTimeLength = 12;
			const int ChecksumLength = 18;
			const int LinkIndicatorLength = 1;
			const int LinkedFileNameLength = 100;
			const int MaxFieldLength = FileNameLength;
			const int RemainingHeader = LastModifiedTimeLength + ChecksumLength + LinkIndicatorLength + LinkedFileNameLength;
			const int HeaderLength = FileNameLength + FileModeLength + FileOwnerUserIDLength + FileGroupUserIDLength + FileSizeLength + RemainingHeader;
			const int BlockSize = 512;
			const int RemainingBytes = BlockSize - HeaderLength + RemainingHeader;

			var buffer = new byte[MaxFieldLength];

			while (true)
			{
				stream.Read(buffer, 0, FileNameLength);
				string fileName = Encoding.ASCII.GetString(buffer).Trim('\0', ' ');

				if (string.IsNullOrWhiteSpace(fileName)) break;

				stream.Seek(FileModeLength + FileOwnerUserIDLength + FileGroupUserIDLength, SeekOrigin.Current);
				stream.Read(buffer, 0, FileSizeLength);

				long fileSize = Convert.ToInt64(
									Encoding.ASCII.GetString(buffer, 0, FileSizeLength).Trim('\0', ' '),
									8); // Octal

				stream.Seek(RemainingBytes, SeekOrigin.Current);

				var fileData = new byte[fileSize];
				stream.Read(fileData, 0, fileData.Length);

				yield return new Tuple<string, byte[]>(fileName, fileData);

				long position = stream.Position;
				long offset = BlockSize - (position % BlockSize);
				if (offset == BlockSize)
				{
					offset = 0;
				}

				stream.Seek(offset, SeekOrigin.Current);
			}
		}
	}
}