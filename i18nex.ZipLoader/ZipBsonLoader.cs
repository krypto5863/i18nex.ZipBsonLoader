using BepInEx.Logging;
using COM3D2.i18nEx.Core.Loaders;
using ExIni;
using HarmonyLib;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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

		internal static SortedDictionary<string, IEnumerable<string>> UiDirectoryTree = new SortedDictionary<string, IEnumerable<string>>(StringComparer.InvariantCultureIgnoreCase);

		public void SelectLanguage(string name, string path, IniFile config)
		{
			Logger.LogInfo($"Loading language \"{name}\"");

			CurrentLanguage = name;
			_langPath = path;

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Textures = TexturesLoad();
			UIs = UiLoad(out var directoryTree);
			Scripts = ScriptLoad();
			UiDirectoryTree = directoryTree;

			Logger.LogInfo($"Done loading everything @ {stopwatch.Elapsed}");
		}

		public void UnloadCurrentTranslation()
		{
			Logger.LogInfo($"Unloading language \"{CurrentLanguage}\"");
			CurrentLanguage = null;
			_langPath = null;
		}

		public Dictionary<string, ITranslationAsset> GetLooseTranslationFiles(string type, string searchPattern)
		{
			var path = Path.Combine(_langPath, type);

			if (!Directory.Exists(path))
			{
				return null;
			}

			var result = new Dictionary<string, ITranslationAsset>();

			foreach (var file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
			{
				result[file] = new LooseFileAsset(file);
			}

			return result;
		}

		public Dictionary<string, byte[]> LoadZipFiles(string type)
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;

			var path = Path.Combine(_langPath, type);

			if (!Directory.Exists(path))
			{
				Logger.LogInfo($"{path} not found. Nothing will be loaded...");
				return null;
			}

			var completeDictionary = new Dictionary<string, byte[]>();

			foreach (var zipPath in Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories))
			{
				using (var zip = new ZipFile(zipPath))
				{
					foreach (ZipEntry zFile in zip)
					{
						if (!zFile.IsFile)
						{
							continue;
						}

						if (!zFile.CanDecompress)
						{
							Logger.LogInfo($"Can't Decompress {zFile.Name}");
							continue;
						}

						using (var reader = new BsonReader(zip.GetInputStream(zFile)))
						{
							var serializer = new JsonSerializer();
							var dictionary = serializer.Deserialize<Dictionary<string, byte[]>>(reader);

							Logger.LogInfo($"{Path.GetFileName(zFile.Name)} has {dictionary.Count} files in it!");

							foreach (var file in dictionary)
							{
								if (completeDictionary.ContainsKey(file.Key) == false)
								{
									completeDictionary[file.Key] = file.Value;
								}
							}
						}
					}
				}
			}

			return completeDictionary;
		}

		public Dictionary<string, ITranslationAsset> TexturesLoad()
		{
			const string searchPattern = "*.png";
			return GetLooseTranslationFiles("Textures", searchPattern);
		}

		public Dictionary<string, ITranslationAsset> ScriptLoad()
		{
			const string searchPattern = "*.txt";
			var scriptFiles = GetLooseTranslationFiles("Script", searchPattern);
			var packedScriptFiles = LoadZipFiles("Script");

			var resultDictionary = new Dictionary<string, ITranslationAsset>();

			foreach (var scriptFile in scriptFiles)
			{
				var fileName = Path.GetFileName(scriptFile.Key);
				if (resultDictionary.ContainsKey(fileName))
				{
					continue;
				}

				resultDictionary[fileName] = scriptFile.Value;
			}

			foreach (var scriptFile in packedScriptFiles)
			{
				var fileName = Path.GetFileName(scriptFile.Key);
				if (resultDictionary.ContainsKey(fileName))
				{
					continue;
				}
				resultDictionary[fileName] = new PackagedAsset(scriptFile.Value);
			}

			return resultDictionary;
		}

		public Dictionary<string, ITranslationAsset> UiLoad(out SortedDictionary<string, IEnumerable<string>> directoryTree)
		{
			const string searchPattern = "*.csv";
			var looseCsvFiles = GetLooseTranslationFiles("UI", searchPattern);
			var uiFiles = LoadZipFiles("UI");

			var resultDictionary = new Dictionary<string, ITranslationAsset>();
			directoryTree = new SortedDictionary<string, IEnumerable<string>>();

			var path = Path.Combine(_langPath, "UI");

			foreach (var csvFile in looseCsvFiles)
			{
				var parentPath = Path.GetDirectoryName(csvFile.Key) ?? string.Empty;
				var fileName = Path.GetFileName(csvFile.Key);

				if (directoryTree.TryGetValue(parentPath, out var ieEnumerable) == false)
				{
					directoryTree[parentPath] = new List<string>();
					ieEnumerable = directoryTree[parentPath];
				}

				ieEnumerable.AddItem(fileName);
				resultDictionary[csvFile.Key] = csvFile.Value;
			}

			foreach (var csvFile in uiFiles)
			{
				const string packageGhostFolder = "zzzzPackagedCsvFiles";
				var relativeParentPath = Path.Combine(Path.GetDirectoryName(csvFile.Key) ?? string.Empty, packageGhostFolder);
				var fullParentPath = Path.Combine(path, relativeParentPath);
				var fileName = Path.GetFileName(csvFile.Key);

				if (directoryTree.TryGetValue(fullParentPath, out var ieEnumerable) == false)
				{
					directoryTree[fullParentPath] = new List<string>();
					ieEnumerable = directoryTree[fullParentPath];
				}

				ieEnumerable.AddItem(fileName);
				var newFullPath = Path.Combine(path, Path.GetFileName(csvFile.Key));
				resultDictionary[newFullPath] = new PackagedAsset(csvFile.Value);
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

		public SortedDictionary<string, IEnumerable<string>> GetUITranslationFileNames()
		{
			return UiDirectoryTree;
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
			return GetStream(path, UIs);
		}
	}
}