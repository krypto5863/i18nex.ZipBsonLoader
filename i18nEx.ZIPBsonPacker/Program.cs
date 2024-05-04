using System.Text;
using Newtonsoft.Json.Bson;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace i18nEx.ZipBsonPacker
{
	
	internal class Program
	{

		private static readonly FolderBrowserDialog FolderDialog = new();

		[STAThread]
		static void Main(string[] args)
		{
			var folderDialogResult = FolderDialog.ShowDialog();

			while (folderDialogResult != DialogResult.OK)
			{
				folderDialogResult = FolderDialog.ShowDialog();
			}

			var filesDictionary = new Dictionary<string, byte[]>();
			var targetDir = FolderDialog.SelectedPath;

			foreach (var file in Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories))
			{
				var fileName = Path.GetFileName(file);
				var textContent = File.ReadAllText(file).Trim();

				Console.WriteLine($"Reading {fileName}...");

				if (filesDictionary.ContainsKey(fileName))
				{
					if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
					{
						Console.WriteLine($"{fileName} was already declared!! Merging...");
						filesDictionary[fileName] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(filesDictionary[fileName]) + textContent);
						continue;
					}

					Console.WriteLine($"{fileName} was already declared!! Skipping...");
					continue;
				}

				filesDictionary.Add(fileName, Encoding.UTF8.GetBytes(textContent));
			}

			var bsonFile = Path.GetFileName(targetDir) + ".bson";
			var outputPath = Path.Combine(Path.GetDirectoryName(targetDir), bsonFile);

			using (var bsonFileStream = new FileStream(outputPath, FileMode.CreateNew))
			using (var writer = new BsonDataWriter(bsonFileStream))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(writer, filesDictionary);
			}


			Console.WriteLine($"Done, saved file to {outputPath}");
		}
	}
}
