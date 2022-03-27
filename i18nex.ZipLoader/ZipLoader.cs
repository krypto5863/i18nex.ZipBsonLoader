//using BepInEx;
//using BepInEx.Logging;
using COM3D2.i18nEx.Core;
using COM3D2.i18nEx.Core.Loaders;
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

        internal readonly static Dictionary<string, ZipFile> fileZips =new Dictionary<string, ZipFile>();
        internal readonly static Dictionary<string, ZipEntry> fileEntrys =new Dictionary<string, ZipEntry>();

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
        }

        public void UnloadCurrentTranslation()
        {
            Core.Logger.LogInfo($"Unloading language \"{CurrentLanguage}\"");
            CurrentLanguage = null;
            langPath = null;
        }

        public IEnumerable<string> GetScriptTranslationFileNames()
        {
            string path = Path.Combine(langPath, "Script");
            if (!Directory.Exists(path))
                return null;

            foreach (string zipPath in Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories))
            {
                ZipFile zip = new ZipFile(zipPath);
                Core.Logger.LogInfo($"zip : {zipPath} , {zip.Count}");

                foreach (ZipEntry zfile in zip)
                {
                    if (!zfile.IsFile) continue;
                                        
                    var fileName = Path.GetFileNameWithoutExtension(zfile.Name);
                    Core.Logger.LogInfo($"{fileName} , {zfile.Name} , {zfile.IsDirectory} , {zfile.IsFile}");//  , {zip.FindEntry(name, true)}

                    fileZips[fileName] = zip;
                    fileEntrys[fileName] = zfile;

                }
            }

            return fileEntrys.Keys;
               // return Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories);
        }

        public IEnumerable<string> GetTextureTranslationFileNames()
        {
            return null;
        }

        public SortedDictionary<string, IEnumerable<string>> GetUITranslationFileNames()
        {
            return null;
        }

        public Stream OpenScriptTranslation(string path)
        {
            Stream stream=null;
            ZipEntry zfile = null;
            try
            {
                zfile = fileEntrys[path];
                stream = fileZips[path].GetInputStream(fileEntrys[path]);
                Core.Logger.LogInfo($"OpenScriptTranslation , {path} , {zfile?.Name} , {stream?.Length}");//  , {zip.FindEntry(name, true)}
            }
            catch (Exception e)
            {
                Core.Logger.LogError($"OpenScriptTranslation , {path} , {e}");
            }
            return stream;
            // return !File.Exists(path) ? null : File.OpenRead(path);
        }

        public Stream OpenTextureTranslation(string path)
        {
            return null;
        }

        public Stream OpenUiTranslation(string path)
        {
            return null;
        }

    }
}
