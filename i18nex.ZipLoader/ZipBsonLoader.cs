using BepInEx.Logging;
using COM3D2.i18nEx.Core.Loaders;
using ExIni;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using File = System.IO.File;

namespace i18nex.ZipBsonLoader
{
	public class ZipBsonLoader : ITranslationLoader
	{
		internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ZipBsonLoader");

		public string CurrentLanguage { get; private set; }
		private static string _langPath;
		internal static Dictionary<string, ITranslationAsset> Scripts = new Dictionary<string, ITranslationAsset>();
		internal static Dictionary<string, ITranslationAsset> Textures = new Dictionary<string, ITranslationAsset>();
		internal static Dictionary<string, ITranslationAsset> UIs = new Dictionary<string, ITranslationAsset>();

		[Obsolete("Obsolete")]
		public void SelectLanguage(string name, string path, IniFile config)
		{
			Logger.LogInfo($"Loading language \"{name}\"");

			CurrentLanguage = name;
			_langPath = path;

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Textures = TexturesLoad();
			UIs = UiLoad();
			Scripts = ScriptLoad();

			Logger.LogInfo($"Done loading everything @ {stopwatch.Elapsed}");
		}

		public void UnloadCurrentTranslation()
		{
			Logger.LogInfo($"Unloading language \"{CurrentLanguage}\"");
			CurrentLanguage = null;
			_langPath = null;
		}

		public Dictionary<string, ITranslationAsset> TexturesLoad()
		{
			const string searchPattern = "*.png";
			var folderPath = Path.Combine(_langPath, "Textures");
			return GetLooseTranslationFiles(folderPath, searchPattern);
		}

		[Obsolete("Obsolete")]
		public Dictionary<string, ITranslationAsset> ScriptLoad()
		{
			const string searchPattern = "*.txt";
			var folderPath = Path.Combine(_langPath, "Script");
			var scriptFiles = GetLooseTranslationFiles(folderPath, searchPattern);
			var bsonScriptFiles = LoadBsonFiles(folderPath);
			var packedScriptFiles = LoadZipFiles(folderPath);

			var resultDictionary = LoadTranslationAssetsIntoDictionary(folderPath, scriptFiles, bsonScriptFiles, packedScriptFiles);

			return resultDictionary;
		}

		[Obsolete("Obsolete")]
		public Dictionary<string, ITranslationAsset> UiLoad()
		{
			const string searchPattern = "*.csv";
			var folderPath = Path.Combine(_langPath, "UI");
			var csvFiles = GetLooseTranslationFiles(folderPath, searchPattern);
			var bsonCsvFiles = LoadBsonFiles(folderPath);
			var packedCsvFiles = LoadZipFiles(folderPath);

			var resultDictionary = LoadTranslationAssetsIntoDictionary(folderPath, csvFiles, bsonCsvFiles, packedCsvFiles);

			return resultDictionary;
		}

		/// <summary>
		/// Returns a dictionary of the files of the given folder and with the following search pattern. The key is the full path of the file.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="searchPattern"></param>
		/// <returns></returns>
		public Dictionary<string, ITranslationAsset> GetLooseTranslationFiles(string directory, string searchPattern)
		{
			var result = new Dictionary<string, ITranslationAsset>();

			if (!Directory.Exists(directory))
			{
				return result;
			}

			foreach (var file in Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories))
			{
				result[file] = new LooseFileAsset(file);
			}

			Logger.LogDebug($"Found {result.Count} loose files in {Path.GetFileName(directory)}");
			return result;
		}

		/// <summary>
		/// Loads all loose BSONs in the specified directory.
		/// </summary>
		/// <param name="directory">The directory where bson files will be loaded from.</param>
		/// <returns>A dictionary, the keys are the relative paths of the file that was packed, in relation to the directory selected at packing time. And the value is the byte array contents.</returns>
		[Obsolete("Obsolete")]
		public Dictionary<string, byte[]> LoadBsonFiles(string directory)
		{
			var completeDictionary = new Dictionary<string, byte[]>();
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;

			if (!Directory.Exists(directory))
			{
				Logger.LogWarning($"{directory} not found. Nothing will be loaded...");
				return completeDictionary;
			}

			var filesInFolder = Directory
				.GetFiles(directory, "*.bson", SearchOption.AllDirectories)
				.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			//Logger.LogDebug($"Searching for zip files in {Path.GetFileName(directory)}");
			foreach (var bsonFile in filesInFolder)
			{
				Logger.LogInfo($"Reading loose {Path.GetFileName(bsonFile)}");
				LoadBsonFile(File.Open(bsonFile, FileMode.Open), Path.GetFileNameWithoutExtension(bsonFile), ref completeDictionary);
			}

			return completeDictionary;
		}

		/// <summary>
		/// Loads all BSON containing ZIP files in the specified directory.
		/// </summary>
		/// <param name="directory">The directory where zip files will be loaded from.</param>
		/// <returns>A dictionary, the keys are the relative paths of the file that was packed, in relation to the directory selected at packing time. And the value is the byte array contents.</returns>
		[Obsolete("Obsolete")]
		public Dictionary<string, byte[]> LoadZipFiles(string directory)
		{
			var completeDictionary = new Dictionary<string, byte[]>();
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;

			if (!Directory.Exists(directory))
			{
				Logger.LogWarning($"{directory} not found. Nothing will be loaded...");
				return completeDictionary;
			}

			var filesInFolder = Directory
				.GetFiles(directory, "*.zip", SearchOption.AllDirectories)
				.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
				.ToArray();

			foreach (var zipPath in filesInFolder)
			{
				Logger.LogDebug($"Processing {Path.GetFileName(zipPath)}");

				using (var zip = new ZipFile(zipPath))
				{
					Logger.LogDebug($"Loaded {Path.GetFileName(zipPath)}");

					foreach (ZipEntry zFile in zip)

					{
						if (!zFile.IsFile)
						{
							continue;
						}

						if (!zFile.Name.EndsWith(".bson", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						if (!zFile.CanDecompress)
						{
							Logger.LogWarning($"Can't Decompress {zFile.Name}");
							continue;
						}

						Logger.LogInfo($"Reading {zFile.Name} in {Path.GetFileName(zipPath)}");
						LoadBsonFile(zip.GetInputStream(zFile), Path.GetFileNameWithoutExtension(zFile.Name), ref completeDictionary);
					}
				}
			}

			return completeDictionary;
		}

		[Obsolete("Out of date usage of the BSONReader.")]
		private static void LoadBsonFile(Stream bsonFile, string fileName, ref Dictionary<string, byte[]> completeDictionary)
		{
			using (var reader = new BsonReader(bsonFile))
			{
				var serializer = new JsonSerializer();
				var dictionary = serializer.Deserialize<Dictionary<string, byte[]>>(reader);

				foreach (var file in dictionary)
				{
					if (completeDictionary.ContainsKey(file.Key) == false)
					{
						completeDictionary[Path.Combine(fileName, file.Key)] = file.Value;
					}
				}
			}
		}

		private static Dictionary<string, ITranslationAsset> LoadTranslationAssetsIntoDictionary(string folderPath, Dictionary<string, ITranslationAsset> looseFiles,
			Dictionary<string, byte[]> bsonFiles, Dictionary<string, byte[]> zippedFiles)
		{
			const string bsonPrefix = "\udbff\udffd";
			const string zipPrefix = "\udbff\udfff";

			var resultDictionary = new Dictionary<string, ITranslationAsset>();

			foreach (var scriptFile in looseFiles)
			{
				var relativePath = PathExt.GetRelativePath(folderPath, scriptFile.Key);
				resultDictionary[relativePath] = scriptFile.Value;
				//Logger.LogDebug($"Adding loose file as {relativePath}");
			}

			foreach (var scriptFile in bsonFiles)
			{
				resultDictionary[bsonPrefix + scriptFile.Key] = new PackagedAsset(scriptFile.Value);
				//Logger.LogDebug($"Adding BSON packed file as {bsonPrefix + scriptFile.Key}");
			}

			foreach (var scriptFile in zippedFiles)
			{
				resultDictionary[zipPrefix + scriptFile.Key] = new PackagedAsset(scriptFile.Value);
				//Logger.LogDebug($"Adding BSON ZIPPED file as {zipPrefix + scriptFile.Key}");
			}

			return resultDictionary;
		}

		public IEnumerable<string> GetScriptTranslationFileNames()
		{
			return Scripts.Keys;
		}

		public IEnumerable<string> GetTextureTranslationFileNames()
		{
			return Textures.Keys;
		}

		public IEnumerable<string> GetUITranslationFileNames()
		{
			//Logger.LogDebug($"Returning tree, contains {UiDirectoryTree.Count} and the first collection of {UiDirectoryTree.First().Key} has {UiDirectoryTree.First().Value.Count()} items.");
			return UIs.Keys;
		}

		public Stream GetStream(string path, Dictionary<string, ITranslationAsset> dic)
		{
			if (!dic.TryGetValue(path, out var translationAsset))
			{
				Logger.LogError($"Couldn't fetch the asset {path}");
				return null;
			}

			return translationAsset.GetContentStream();
		}

		public Stream OpenScriptTranslation(string path)
		{
			return GetStream(path, Scripts);
		}

		public Stream OpenTextureTranslation(string path)
		{
			return GetStream(path, Textures);
		}

		public Stream OpenUiTranslation(string path)
		{
			//Logger.LogDebug($"{path} was requested.");
			return GetStream(path, UIs);
		}
	}
}