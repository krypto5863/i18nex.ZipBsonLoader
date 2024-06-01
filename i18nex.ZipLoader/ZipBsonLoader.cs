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
using BepInEx;
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

		/// <summary>
		/// Returns a dictionary of the files of the given folder and with the following search pattern. The key is the full path of the file.
		/// </summary>
		/// <param name="type"></param>
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
		/// Loads all BSON containing ZIP files in the specified directory.
		/// </summary>
		/// <param name="directory">The directory where zip files will be loaded from.</param>
		/// <returns>A dictionary, the keys are the relative paths of the file that was packed, in relation to the directory selected at packing time. And the value is the byte array contents.</returns>
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
                .GetFiles(directory, "*", SearchOption.AllDirectories)
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            //Logger.LogDebug($"Searching for zip files in {Path.GetFileName(directory)}");
            foreach (var bsonFile in filesInFolder
                         .Where(m => m.EndsWith(".bson", StringComparison.OrdinalIgnoreCase)))
            {
                Logger.LogInfo($"Reading loose {Path.GetFileName(bsonFile)}");
                LoadBsonFunc(File.Open(bsonFile, FileMode.Open));
            }

            foreach (var zipPath in filesInFolder
                         .Where(m => m.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
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

						if (!zFile.CanDecompress)
						{
							Logger.LogInfo($"Can't Decompress {zFile.Name}");
							continue;
						}

                        if (!zFile.Name.EndsWith(".bson", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        Logger.LogInfo($"Reading {zFile.Name} in {Path.GetFileName(zipPath)}");
                        LoadBsonFunc(zip.GetInputStream(zFile));
                    }
				}
			}

			return completeDictionary;

            void LoadBsonFunc(Stream bsonFile)
            {
                using (var reader = new BsonReader(bsonFile))
                {
                    var serializer = new JsonSerializer();
                    var dictionary = serializer.Deserialize<Dictionary<string, byte[]>>(reader);

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

		public Dictionary<string, ITranslationAsset> TexturesLoad()
		{
			const string searchPattern = "*.png";
			var folderPath = Path.Combine(_langPath, "Textures");
			return GetLooseTranslationFiles(folderPath, searchPattern);
		}

		public Dictionary<string, ITranslationAsset> ScriptLoad()
		{
			const string searchPattern = "*.txt";
			var folderPath = Path.Combine(_langPath, "Script");
			var scriptFiles = GetLooseTranslationFiles(folderPath, searchPattern);
			var packedScriptFiles = LoadZipFiles(folderPath);

			var resultDictionary = new Dictionary<string, ITranslationAsset>();

			var looseScriptFilesAdded = new HashSet<string>();
			var packedScriptFilesAdded = new HashSet<string>();

			foreach (var scriptFile in scriptFiles)
			{
				var fileName = Path.GetFileName(scriptFile.Key).ToLower();
				var relativePath = PathExt.GetRelativePath(folderPath, scriptFile.Key);
				looseScriptFilesAdded.Add(fileName);
				resultDictionary[relativePath] = scriptFile.Value;
			}

			foreach (var scriptFile in packedScriptFiles)
			{
				var fileName = Path.GetFileName(scriptFile.Key).ToLower();
				
				if (looseScriptFilesAdded.Contains(fileName))
				{
					continue;
				}

				packedScriptFilesAdded.Add(fileName);
				resultDictionary[scriptFile.Key] = new PackagedAsset(scriptFile.Value);
			}

			return resultDictionary;
		}

		public Dictionary<string, ITranslationAsset> UiLoad(out SortedDictionary<string, IEnumerable<string>> directoryTree)
		{
			const string searchPattern = "*.csv";
			var uiFolderPath = Path.Combine(_langPath, "UI");
			var looseCsvFiles = GetLooseTranslationFiles(uiFolderPath, searchPattern);
			var uiFiles = LoadZipFiles(uiFolderPath);

			var resultDictionary = new Dictionary<string, ITranslationAsset>();
			var tempDirectoryTree = new SortedDictionary<string, List<string>>();

			foreach (var csvFile in looseCsvFiles)
			{
				var relativePath = PathExt.GetRelativePath(uiFolderPath, csvFile.Key);
				Logger.LogDebug($"Relative unit path is {relativePath}, root is {PathExt.GetFirstDirectory(relativePath)}");
				if (Path.GetDirectoryName(relativePath).IsNullOrWhiteSpace())
				{
					Logger.LogWarning($"Skipping {relativePath}, it has no unit.");
					continue;
				}

				var unitPath = PathExt.GetFirstDirectory(relativePath);
				var relativeFileName = PathExt.GetRelativePath(unitPath, relativePath);

				if (tempDirectoryTree.TryGetValue(unitPath, out _) == false)
				{
					tempDirectoryTree[unitPath] = new List<string>();
				}

				tempDirectoryTree[unitPath].Add(relativeFileName);
				resultDictionary[Path.Combine(unitPath,relativeFileName)] = csvFile.Value;
				Logger.LogDebug($"Added unit {unitPath} and file {relativeFileName} as {Path.Combine(unitPath,relativeFileName)}");
			}

			foreach (var csvFile in uiFiles)
			{
				/*
				if (Path.GetDirectoryName(csvFile.Key).IsNullOrWhiteSpace())
				{
					Logger.LogWarning($"Skipping {csvFile.Key}, it has no unit.");
					continue;
				}
				var unitName = PathExt.GetFirstDirectory(csvFile.Key);
				var relativeToUnit = PathExt.GetPathAfterDirectory(csvFile.Key, unitName);

				if (tempDirectoryTree.TryGetValue(unitName, out _) == false)
				{
					tempDirectoryTree[unitName] = new List<string>();
				}

				tempDirectoryTree[unitName].Add(relativeToUnit);
				resultDictionary[csvFile.Key] = new PackagedAsset(csvFile.Value);
				
				Logger.LogInfo($"Added unit {unitName} and file {relativeToUnit} as {csvFile.Key}");
				*/
				const string packageGhostFolder = "zzzzPackagedCsvFiles";
				var fakeRelativePath = Path.Combine(packageGhostFolder, csvFile.Key ?? string.Empty);

				if (tempDirectoryTree.TryGetValue(packageGhostFolder, out _) == false)
				{
					tempDirectoryTree[packageGhostFolder] = new List<string>();
				}

				tempDirectoryTree[packageGhostFolder].Add(csvFile.Key);
				resultDictionary[fakeRelativePath] = new PackagedAsset(csvFile.Value);
				Logger.LogDebug($"Added unit {packageGhostFolder} and file {csvFile.Key} as {fakeRelativePath}");
			}

			directoryTree = new SortedDictionary<string, IEnumerable<string>>(tempDirectoryTree.ToDictionary(m => m.Key, r => r.Value as IEnumerable<string>), StringComparer.OrdinalIgnoreCase);
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
			//Logger.LogDebug($"Returning tree, contains {UiDirectoryTree.Count} and the first collection of {UiDirectoryTree.First().Key} has {UiDirectoryTree.First().Value.Count()} items.");
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
			//Logger.LogDebug($"{path} was requested.");
			return GetStream(path, UIs);
		}
	}
}