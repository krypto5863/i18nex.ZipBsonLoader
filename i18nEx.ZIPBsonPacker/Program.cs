using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.CommandLine;
using System.Diagnostics;
using System.Text;

namespace i18nEx.ZipBsonPacker
{
	internal class Program
	{
		private static async Task<int> Main(string[] args)
		{
			using (var p = Process.GetCurrentProcess())
				p.PriorityClass = ProcessPriorityClass.BelowNormal;

			var rootCommand = new RootCommand("A simple CLI tool to un/pack Bson files for usage with the ZipBsonLoader");

			{
				var packCommand = new Command(
					name: "pack",
					description: "Packs a directory into a single BSON file.");

				rootCommand.AddCommand(packCommand);

				var directoryOption =
					new Option<DirectoryInfo>(aliases: ["--directory", "-d"],
						description: "Directory to be packed.")
					{
						IsRequired = true
					};

				var doCompressionOption =
					new Option<bool>(
						aliases: ["--compress", "-c"],
						description: "Compress the Bson to a zip file. Not suggested.");

				var outputPathOption =
					new Option<string>(aliases: ["--output", "-o"],
						"The full path to output the file, including the file name.")
					{
						IsRequired = true
					};

				packCommand.AddOption(directoryOption);
				packCommand.AddOption(doCompressionOption);
				packCommand.AddOption(outputPathOption);

				packCommand.SetHandler(
					PackBsonFile,
					directoryOption, outputPathOption, doCompressionOption);
			}
			{
				var unpackCommand = new Command(
					name: "unpack",
					description: "Unpacks a BSON file into a directory.");

				rootCommand.AddCommand(unpackCommand);

				var fileOption =
					new Option<FileInfo>(aliases: ["--file", "-f"],
						description: "BSON file to be unpacked.")
					{
						IsRequired = true
					};

				var unpackDirectory =
					new Option<DirectoryInfo>(aliases: ["--output", "-o"],
						description: "Directory to place the unpacked files in.")
					{
						IsRequired = true
					};

				unpackCommand.AddOption(fileOption);
				unpackCommand.AddOption(unpackDirectory);

				unpackCommand.SetHandler(
					UnpackBsonFile,
					fileOption, unpackDirectory);
			}

			return await rootCommand.InvokeAsync(args);
		}

		private static void UnpackBsonFile(FileInfo bsonFile, DirectoryInfo outputDirectory)
		{
			using var bsonFileStream = bsonFile.OpenRead();
			using var bsonReader = new BsonDataReader(bsonFileStream);

			var serializer = new JsonSerializer();
			var deserializeJsonFile = serializer.Deserialize<Dictionary<string, byte[]>>(bsonReader);

			if (deserializeJsonFile == null)
			{
				return;
			}

			var counter = 0;
			foreach (var file in deserializeJsonFile)
			{
				Console.WriteLine($"{++counter}/{deserializeJsonFile.Count} : {file.Key}...");
				var filePath = Path.Combine(outputDirectory.FullName, Path.GetFileNameWithoutExtension(bsonFile.Name), file.Key);
				File.WriteAllBytes(filePath, file.Value);
			}
		}

		private static void PackBsonFile(DirectoryInfo directory, string outputName, bool compression = false)
		{
			var filesDictionary = new Dictionary<string, byte[]>();
			var filesInDir = directory.EnumerateFiles("*", SearchOption.AllDirectories)
				.ToArray();

			var counter = 0;
			foreach (var file in filesInDir)
			{
				var relativeName = Path.GetRelativePath(directory.FullName, file.FullName);
				var textContent = File.ReadAllText(file.FullName).Trim();

				Console.WriteLine($"Reading {++counter}/{filesInDir.Length}: {relativeName}...");

				if (filesDictionary.ContainsKey(relativeName))
				{
					/*
					if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
					{
						Console.WriteLine($"{relativeName} was already declared!! Merging...");
						MergeCsv(textContent, file, ref filesDictionary);
						continue;
					}
					*/
					Console.WriteLine($"{relativeName} was already declared!! Skipping...");
					continue;
				}

				filesDictionary.Add(relativeName, Encoding.UTF8.GetBytes(textContent));
			}

			var bsonFile = directory.Name + ".bson";
			var backupOutput = Path.Combine(directory.Parent?.FullName ?? string.Empty, bsonFile);

			var outputPath = string.IsNullOrEmpty(outputName) ? backupOutput : outputName;

			using (var bsonFileStream = new FileStream(outputPath, FileMode.CreateNew))
			using (var writer = new BsonDataWriter(bsonFileStream))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(writer, filesDictionary);
			}

			Console.WriteLine($"Done, saved file to {outputPath}");
		}

		/*
		private static void MergeCsv(string textContent, FileInfo file, ref Dictionary<string, byte[]> filesDictionary)
		{
			var lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			if (lines.Length <= 1)
			{
				return;
			}

			var previousFile = Encoding.UTF8.GetString(filesDictionary[file.Name])
				.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			var deDuped = previousFile
				.Concat(lines.Skip(1))
				.Distinct();

			var result = string.Join(Environment.NewLine, deDuped, 0, deDuped.Count() - 1);

			filesDictionary[file.Name] = Encoding.UTF8.GetBytes(result);
		}
		*/
	}
}