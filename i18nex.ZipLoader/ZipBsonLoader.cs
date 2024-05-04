using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using COM3D2.i18nEx.Core.Loaders;
using ExIni;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace i18nex.ZipBsonLoader
{
	public class ZipBsonLoader : ITranslationLoader
	{
		internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ZipBsonLoader");

		public string CurrentLanguage { get; private set; }

		private static string _langPath;

		internal static readonly Dictionary<string, ITranslationAsset> Scripts = new Dictionary<string, ITranslationAsset>();
		internal static readonly Dictionary<string, ITranslationAsset> Textures = new Dictionary<string, ITranslationAsset>();
		internal static readonly Dictionary<string, ITranslationAsset> UIs = new Dictionary<string, ITranslationAsset>();

		internal static readonly SortedDictionary<string, HashSet<string>> TemporaryUiDirectoryTree = new SortedDictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);
		internal static readonly SortedDictionary<string, IEnumerable<string>> UiDirectoryTree = new SortedDictionary<string, IEnumerable<string>>(StringComparer.InvariantCultureIgnoreCase);

		public void SelectLanguage(string name, string path, IniFile config)
		{
			Logger.LogInfo($"Loading language \"{name}\"");

			CurrentLanguage = name;
			_langPath = path;

			Scripts.Clear();
			Textures.Clear();
			UIs.Clear();
			UiDirectoryTree.Clear();
			TemporaryUiDirectoryTree.Clear();

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			ZipLoad("Script", Scripts, ".txt");
			Logger.LogInfo($"Scripts ZIP done loading @ {stopwatch.Elapsed}");

			ZipLoad("UI", UIs, ".csv");
			Logger.LogInfo($"UI ZIP done loading @ {stopwatch.Elapsed}");

			Logger.LogInfo($"Done loading all ZIP files @ {stopwatch.Elapsed}");

			TemporaryUiDirectoryTree[string.Empty] = new HashSet<string>(UIs.Keys.Select(r => r.Replace("\\", string.Empty)));

			FileLoad("Script", Scripts, "*.txt");
			FileLoad("Textures", Textures, "*.png");
			UiLoad();

			Logger.LogInfo($"Done loading everything @ {stopwatch.Elapsed}");

			CommitTemporaryDirectoryDictionary();
		}

		public void UnloadCurrentTranslation()
		{
			Logger.LogInfo($"Unloading language \"{CurrentLanguage}\"");
			CurrentLanguage = null;
			_langPath = null;
		}

		[Obsolete("Obsolete")]
		public void ZipLoad(string type, Dictionary<string, ITranslationAsset> targetDictionary, string searchPattern)
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;

			var path = Path.Combine(_langPath, type);
			if (!Directory.Exists(path))
			{
				Logger.LogInfo("Directory not found. Nothing will be loaded...");
				return;
			}

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
								if (!file.Key.EndsWith(searchPattern))
								{
									continue;
								}

								AddToMainDictionary(targetDictionary, new PackagedAsset(file.Value), "\\"+file.Key);
							}
						}
					}
				}
			}
		}

		public void FileLoad(string type, Dictionary<string, ITranslationAsset> targetDictionary, string searchPattern)
		{
			var path = Path.Combine(_langPath, type);

			if (!Directory.Exists(path))
			{
				return;
			}

			foreach (var file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
			{
				AddToMainDictionary(targetDictionary, new LooseFileAsset(file), Path.GetFileName(file));
			}
		}

		/*
		public void UiZipLoad(string type, Dictionary<string, ITranslationAsset> dic)
		{
			ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
			Logger.LogInfo($"UIZipLoad : {type} {ZipConstants.DefaultCodePage}");

			var path = Path.Combine(_langPath, type);

			if (!Directory.Exists(path))
			{
				return;
			}

			foreach (var zipPath in Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories))
			{
				using (var zip = new ZipFile(zipPath))
				{
					Logger.LogInfo($"zip : {zip.Name} , {zipPath} , {zip.Count} , {zip.ZipFileComment}");

					var dirName = Path.GetFileNameWithoutExtension(zip.Name);
					AddDirectoryToTemporaryDictionary(dirName);

					foreach (ZipEntry zipFile in zip)
					{
						if (zipFile.IsDirectory)
						{
							dirName = zipFile.Name.Split('/')[0];
							AddDirectoryToTemporaryDictionary(dirName);
						}

						if (!zipFile.IsFile)
						{
							continue;
						}

						var name = Path.GetFileName(zipFile.Name);
						if (!zipFile.CanDecompress)
						{
							Logger.LogInfo($"Can't Decompress {zipFile.Name}");
							continue;
						}

						if (!name.EndsWith(".csv"))
						{
							continue;
						}

						AddToMainDictionary(UIs, zip.GetInputStream(zipFile), dirName + "\\" + name);
						TemporaryUiDirectoryTree[dirName].Add(name);
					}
				}
			}

			Logger.LogInfo($"UIZipLoad : {type} , {dic.Count}");
		}
		*/

		public void UiLoad()
		{
			var uiPath = Path.Combine(_langPath, "UI");
			if (!Directory.Exists(uiPath))
			{
				return;
			}

			foreach (var directory in Directory.GetDirectories(uiPath, "*", SearchOption.TopDirectoryOnly))
			{
				var dirName = Path.GetFileName(directory);

				var files = Directory.GetFiles(directory, "*.csv", SearchOption.AllDirectories);

				AddDirectoryToTemporaryDictionary(dirName);

				foreach (var file in files)
				{
					var name = Path.GetFileName(file);
					AddToMainDictionary(UIs, new LooseFileAsset(file), dirName + "\\" + name);
					TemporaryUiDirectoryTree[dirName].Add(name);
				}
			}
		}

		public void CommitTemporaryDirectoryDictionary()
		{
			foreach (var item in TemporaryUiDirectoryTree)
			{
				if (item.Value.Count == 0)
				{
					continue;
				}

				UiDirectoryTree[item.Key] = item.Value;
			}

			TemporaryUiDirectoryTree.Clear();
		}

		private static void AddDirectoryToTemporaryDictionary(string dirName)
		{
			if (!TemporaryUiDirectoryTree.ContainsKey(dirName))
			{
				TemporaryUiDirectoryTree[dirName] = new HashSet<string>();
			}
		}

		private static void AddToMainDictionary(IDictionary<string, ITranslationAsset> targetDictionary, ITranslationAsset translationAsset, string fileName)
		{
			targetDictionary[fileName] = translationAsset;
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