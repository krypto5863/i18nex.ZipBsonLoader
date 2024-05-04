using System.IO;

namespace i18nex.ZipBsonLoader
{
	public interface ITranslationAsset
	{
		Stream GetContentStream();
	}
	public class LooseFileAsset : ITranslationAsset
	{
		private string FilePath { get; }

		public LooseFileAsset(string filePath)
		{
			FilePath = filePath;
		}
		public Stream GetContentStream()
		{
			return new FileStream(FilePath, FileMode.Open);
		}
	}
	public class PackagedAsset : ITranslationAsset
	{
		private readonly byte[] _data;
		public PackagedAsset(byte[] data)
		{
			_data = data;
		}
		public Stream GetContentStream()
		{
			return new MemoryStream(_data);
		}
	}
}