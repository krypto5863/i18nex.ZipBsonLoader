//using BepInEx;
//using BepInEx.Logging;
using COM3D2.i18nEx.Core;
using COM3D2.i18nEx.Core.Loaders;
using COM3D2.i18nEx.Core.Util;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i18nex.ZipLoader
{
    public class ZipLoader : ITranslationLoader
    {

        public string CurrentLanguage { get; private set; }
        private static string langPath;

        static byte[] buffer = new byte[4096];

        internal readonly static Dictionary<string, byte[]> Scripts = new Dictionary<string, byte[]>();
        internal readonly static Dictionary<string, byte[]> Textures = new Dictionary<string, byte[]>();



        public void SelectLanguage(string name, string path, global::ExIni.IniFile config)
        {
            CurrentLanguage = name;
            langPath = path;
            Core.Logger.LogInfo($"Loading language \"{CurrentLanguage}\"");

            Scripts.Clear();
            Textures.Clear();
            ZipLoad("Script", Scripts);
            ZipLoad("Textures", Textures);
            FileLoad("Script", Scripts, "*.txt");
            FileLoad("Textures", Textures, "*.png");
        }

        public void UnloadCurrentTranslation()
        {
            Core.Logger.LogInfo($"Unloading language \"{CurrentLanguage}\"");

            CurrentLanguage = null;
            langPath = null;
        }

        public void ZipLoad(string type, Dictionary<string, byte[]> dic)
        {
            ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;
            Core.Logger.LogInfo($"ZipLoad : {type} {ZipConstants.DefaultCodePage}");

            string path = Path.Combine(langPath, type);
            if (!Directory.Exists(path))
                return;           

            foreach (string zipPath in Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories))
            {
                using (ZipFile zip = new ZipFile(zipPath))
                {
                    Core.Logger.LogInfo($"zip : {zipPath} , {zip.Count} , {zip.ZipFileComment}");

                    foreach (ZipEntry zfile in zip)
                    {
                        if (!zfile.IsFile) continue;
                        if (!zfile.CanDecompress)
                        {
                            Core.Logger.LogInfo($"Can't Decompress {zfile.Name}");
                            continue;
                        }
                        using (Stream stream =zip.GetInputStream(zfile))
                        using (MemoryStream mstream =new MemoryStream())
                        {
                            StreamUtils.Copy(stream, mstream, buffer);
                            var fileName = Path.GetFileName(zfile.Name);
                            dic[fileName] =mstream.ToArray();
                            //Core.Logger.LogInfo($"ZipLoad : {fileName} , {dic[fileName].Length}");
                        }                        
                    }
                }
            }

            Core.Logger.LogInfo($"ZipLoad : {type} , {dic.Count}");
        }

        public void FileLoad(string type, Dictionary<string, byte[]> dic,string searchPattern)
        {            
            Core.Logger.LogInfo($"FileLoad : {type}");

            string path = Path.Combine(langPath, type);
            if (!Directory.Exists(path))
                return;
                       
            foreach (string file in Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories))
            {
                using (Stream stream = File.OpenRead(file))
                using (MemoryStream mstream = new MemoryStream())
                {
                    StreamUtils.Copy(stream, mstream, buffer);
                    var fileName = Path.GetFileName(file);
                    dic[fileName] = mstream.ToArray();
                    //Core.Logger.LogInfo($"FileLoad : {fileName} , {dic[fileName].Length}");
                }
            }
            Core.Logger.LogInfo($"FileLoad : {type} , {dic.Count}");
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
            var uiPath = Path.Combine(langPath, "UI");
            if (!Directory.Exists(uiPath))
                return null;

            var dict = new SortedDictionary<string, IEnumerable<string>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var directory in Directory.GetDirectories(uiPath, "*", SearchOption.TopDirectoryOnly))
            {
                var dirName = directory.Splice(uiPath.Length, -1).Trim('\\', '/');
                dict.Add(dirName,
                         Directory.GetFiles(directory, "*.csv", SearchOption.AllDirectories)
                                  .Select(s => s.Splice(directory.Length + 1, -1)));
            }

            return dict;
        }


        public Stream GetStream(string path, Dictionary<string, byte[]> dic)
        {            
            return new MemoryStream(dic[path]);
        }

        public Stream OpenScriptTranslation(string path)
        {
            Core.Logger.LogInfo($"OpenScriptTranslation , {path} ");
                        
            return GetStream(path, Scripts);
        }

        public Stream OpenTextureTranslation(string path)
        {
            Core.Logger.LogInfo($"OpenTextureTranslation , {path} ");

            return GetStream(path, Textures);
        }

        public Stream OpenUiTranslation(string path)
        {
            path = Utility.CombinePaths(langPath, "UI", path);
            return !File.Exists(path) ? null : File.OpenRead(path);
        }

    }
}
