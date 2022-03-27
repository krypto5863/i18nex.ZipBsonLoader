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

namespace i18nex.ZipLoader
{
    class MyAttribute
    {
        public const string PLAGIN_NAME = "ZipLoader";
        public const string PLAGIN_VERSION = "22.3.27";
        public const string PLAGIN_FULL_NAME = "i18nEx.ZipLoader";
    }

    //[BepInPlugin(MyAttribute.PLAGIN_FULL_NAME, MyAttribute.PLAGIN_NAME, MyAttribute.PLAGIN_VERSION)]// 버전 규칙 잇음. 반드시 2~4개의 숫자구성으로 해야함. 미준수시 못읽어들임    
    //[BepInProcess("COM3D2x64.exe")]
    public class ZipLoader : ITranslationLoader// BaseUnityPlugin,
    {
        //public static ManualLogSource log;

        public string CurrentLanguage { get; private set; }
        private static string langPath;

        internal readonly static Dictionary<string, ZipFile> ScriptZips = new Dictionary<string, ZipFile>();
        internal readonly static Dictionary<string, ZipEntry> ScriptEntrys = new Dictionary<string, ZipEntry>();
        //internal readonly static Dictionary<string, ZipFile> TextureZips = new Dictionary<string, ZipFile>();
        //internal readonly static Dictionary<string, ZipEntry> TextureEntrys = new Dictionary<string, ZipEntry>();
        
        internal readonly static List<ZipFile> zipFiles = new List<ZipFile>();

        /*
        public ZipLoader()
        {
            log = Logger;
        }

        private void Awake()
        {
            Logger.LogMessage($"Awake");
        }
        */

        public void SelectLanguage(string name, string path, global::ExIni.IniFile config)
        {
            CurrentLanguage = name;
            langPath = path;
            Core.Logger.LogInfo($"Loading language \"{CurrentLanguage}\"");

            ZipLoad("Script", ScriptZips,ScriptEntrys);
            //ZipLoad("Textures", TextureZips, TextureEntrys);
        }

        public void UnloadCurrentTranslation()
        {
            Core.Logger.LogInfo($"Unloading language \"{CurrentLanguage}\"");

            foreach (var zip in zipFiles)
            {
                zip.Close();
            }
            zipFiles.Clear();

            CurrentLanguage = null;
            langPath = null;
        }

        public void ZipLoad(string type, Dictionary<string, ZipFile> zips, Dictionary<string, ZipEntry> entrys)
        {
            ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;

            Core.Logger.LogInfo($"ZipLoad : {type} {ZipConstants.DefaultCodePage}");

            //string path = langPath;// Path.Combine(langPath, "Script");
            string path = Path.Combine(langPath, type);
            if (!Directory.Exists(path))
                return;

            zips.Clear();
            entrys.Clear();

            foreach (string zipPath in Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories))
            {
                ZipFile zip = new ZipFile(zipPath);
                
                zipFiles.Add(zip);
                Core.Logger.LogInfo($"zip : {zipPath} , {zip.Count} , {zip.ZipFileComment}");
#if Debug
#endif
                foreach (ZipEntry zfile in zip)
                {
                    if (!zfile.IsFile) continue;
                    if (!zfile.CanDecompress)
                    {
                        Core.Logger.LogInfo($"Can't Decompress {zfile.Name}");//  , {zip.FindEntry(name, true)}
                        continue;
                    }


                    //var fileName = Path.GetFileNameWithoutExtension(zfile.Name);
                    var fileName = Path.GetFileName(zfile.Name);
#if Debug
                    Core.Logger.LogInfo($"{fileName} , {zfile.Name} , {zfile.IsDirectory} , {zfile.IsFile}");//  , {zip.FindEntry(name, true)}
#endif
                    zips[fileName] = zip;
                    entrys[fileName] = zfile;
                }


            }

            Core.Logger.LogInfo($"ZipLoad : {type} , {zips.Count} , {entrys.Count}");
        }

        public IEnumerable<string> GetScriptTranslationFileNames()
        {
            return ScriptEntrys.Keys;//.Where(x => x.EndsWith(".txt"));
        }

        public IEnumerable<string> GetTextureTranslationFileNames()
        {
            var texPath = Path.Combine(langPath, "Textures");
            if (!Directory.Exists(texPath))
                return null;
            return Directory.GetFiles(texPath, "*.png", SearchOption.AllDirectories);
            //return TextureEntrys.Keys;//.Where(x => x.EndsWith(".png"));
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

        static byte[] buffer = new byte[4096];

        private static Stream GetStream(string path, Dictionary<string, ZipFile> zips, Dictionary<string, ZipEntry> entrys)
        {
            Stream stream = null;
            ZipEntry zfile = null;
            try
            {
                Core.Logger.LogInfo($"GetStream , {path} , {entrys.ContainsKey(path)}");//  , {zip.FindEntry(name, true)}
                if (!entrys.ContainsKey(path))
                {
                    return stream;
                }
                
                zfile = entrys[path];

                stream = zips[path].GetInputStream(zfile);              
                //stream = new MemoryStream();
                //StreamUtils.Copy(zips[path].GetInputStream(zfile), stream, buffer);                                            

                Core.Logger.LogInfo($"GetStream , {zfile?.Name} , {zfile?.Offset} , {zfile?.Size} , {zfile?.CompressedSize} , {zfile?.Crc} , {zfile?.ZipFileIndex} , {stream?.Length} ");//  , {zip.FindEntry(name, true)}
            }
            catch (Exception e)
            {
                Core.Logger.LogError($"GetStream , {path} , {e}");
            }
            return stream;
        }

        public Stream OpenScriptTranslation(string path)
        {
            Core.Logger.LogInfo($"OpenScriptTranslation , {path} ");

            Stream stream = GetStream(path, ScriptZips, ScriptEntrys);
            //using (FileStream fileStream = File.Create(Path.Combine(langPath, "Script\\" + path)))
            //{
            //    StreamUtils.Copy(stream, fileStream, buffer);
            //}
            //using (FileStream fileStream = File.Create(Path.Combine(langPath, "Script\\t_" + path)))
            //{
            //    StreamUtils.Copy(stream, fileStream, buffer);
            //}
            return stream;
        }

        public Stream OpenTextureTranslation(string path)
        {
            Core.Logger.LogInfo($"OpenTextureTranslation , {path} ");
            return !File.Exists(path) ? null : File.OpenRead(path);

            //Stream stream = GetStream(path, TextureZips, TextureEntrys);
            //using (FileStream fileStream = File.Create(Path.Combine(langPath, "Textures\\"+path)))
            //{
            //    StreamUtils.Copy(stream, fileStream, buffer);
            //}
            //using (FileStream fileStream = File.Create(Path.Combine(langPath, "Textures\\t_"+path)))
            //{
            //    StreamUtils.Copy(stream, fileStream, buffer);
            //}
            //return stream;
            //return null;
        }

        public Stream OpenUiTranslation(string path)
        {
            path = Utility.CombinePaths(langPath, "UI", path);
            return !File.Exists(path) ? null : File.OpenRead(path);
        }

    }
}
