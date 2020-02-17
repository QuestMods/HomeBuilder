using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

namespace HomeQuest
{
    public class Application
    {
        public static readonly string OVRSCENE_NAME = "_WORLD_MODEL";
        public static readonly string OVRSCENE_EXTENSION = ".gltf.ovrscene";
        public static readonly string OGG_NAME = "_BACKGROUND_LOOP.ogg";
        public static readonly string SCENE_FILE = "scene.zip";
        public static readonly string TEMP = "TEMP";
        public static readonly string ARCHIVE = "archive";
        public static readonly string SILENT_AUDIO_OGG = "originals/Silent_Audio.ogg";
        public static readonly string CLASSIC_HOME = "ClassicHome";
        public static readonly string WINTER_LODGE = "WinterLodge";
        public static readonly string MODEL_FOLDER = "exported_from_blender";

        private string _platform;
        private string _oggFile;
        private string _ovrsceneFile;
        private string _tempApkFile;
        private string _modelName;

        public Application()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _platform = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _platform = "osx";
            else
                _platform = "linux";
        }

        public void Run()
        {
            Reset();

            CheckJava();

            ZipModelFolder();

            _ovrsceneFile = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.ovrscene").FirstOrDefault();
            _oggFile = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.ogg").FirstOrDefault();

            Directory.CreateDirectory(TEMP);

            if (_ovrsceneFile == null)
                throw new Exception(string.Format("Couldn't find a valid '{0}' file", OVRSCENE_EXTENSION));

            _ovrsceneFile = Path.GetFileName(_ovrsceneFile);
            File.Copy(_ovrsceneFile, TEMP + "/" + _ovrsceneFile);

            if (_oggFile != null)
                _oggFile = Path.GetFileName(_oggFile);

            Build(CLASSIC_HOME, SILENT_AUDIO_OGG);
            Build(CLASSIC_HOME, _oggFile);
            Build(WINTER_LODGE, SILENT_AUDIO_OGG);
            Build(WINTER_LODGE, _oggFile);

            PostArchiveEnvironment();
        }

        private void CheckJava()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "java";
                psi.Arguments = "-version";
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception(@"Java must be installed and accessible from the console.");
            }
        }

        private void Build(string originalEnvironment, string customOggFile)
        {
            _tempApkFile = string.Format("{0}_{1}", _modelName, originalEnvironment);
            if (customOggFile == SILENT_AUDIO_OGG)
                _tempApkFile += "_NoAudio";
            _tempApkFile += ".apk";

            Console.WriteLine("Building {0} ...", _tempApkFile);
            if (string.IsNullOrEmpty(customOggFile))
            {
                Console.Error.WriteLine("Couldn't find a custom .ogg file. Skipping!", customOggFile);
                return;
            }

            File.Copy(customOggFile, TEMP + "/" + OGG_NAME, true);
            File.Copy("originals/" + originalEnvironment + ".apk", _tempApkFile, true);

            ReplaceScene();
            ZipAlign();
            SignApk();
            PreArchiveEnvironment();
        }

        private void ZipModelFolder()
        {
            Directory.CreateDirectory(MODEL_FOLDER);

            try
            {
                _modelName = Directory.GetFiles(MODEL_FOLDER, "*.gltf").Single();
                _modelName = Path.GetFileNameWithoutExtension(_modelName);
            }
            catch
            {
                throw new Exception(string.Format(@"ModelFolder '{0}' must contain exactly one '.gltf' file.", MODEL_FOLDER));
            }

            ZipFile.CreateFromDirectory(MODEL_FOLDER, OVRSCENE_NAME + OVRSCENE_EXTENSION);
        }

        private void ReplaceScene()
        {
            ZipFile.CreateFromDirectory(TEMP, SCENE_FILE, CompressionLevel.Fastest, false);
            using (ZipArchive archive = ZipFile.Open(_tempApkFile, ZipArchiveMode.Update))
            {
                var sceneZipEntry = archive.GetEntry("assets/" + SCENE_FILE);
                sceneZipEntry.Delete();
                archive.CreateEntryFromFile(SCENE_FILE, "assets/" + SCENE_FILE, CompressionLevel.Fastest);
            }
            File.Delete(SCENE_FILE);
        }

        private void ZipAlign()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "dependencies/" + _platform + "/zipalign";
            psi.Arguments = string.Format("-c 4 {0}", _tempApkFile);
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            process.WaitForExit();
        }

        private void SignApk()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "java";
            psi.Arguments = "-jar -Duser.language=en -Dfile.encoding=UTF8 \"dependencies/apksigner.jar\" ";
            psi.Arguments += "sign --key \"dependencies/signkey.pk8\" ";
            psi.Arguments += "--cert \"dependencies/signkey.x509.pem\" ";
            psi.Arguments += string.Format("--out \"{0}\" \"{0}\"", _tempApkFile);
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            process.WaitForExit();
        }

        private void PreArchiveEnvironment()
        {
            var tempArchiveName = TEMP + ARCHIVE;

            Directory.CreateDirectory(ARCHIVE);
            Directory.CreateDirectory(tempArchiveName);

            File.Move(_tempApkFile, tempArchiveName + "/" + _tempApkFile, true);
        }

        private void PostArchiveEnvironment()
        {
            var tempArchiveName = TEMP + ARCHIVE;

            File.Move(_ovrsceneFile, tempArchiveName + "/" + _modelName + OVRSCENE_EXTENSION + ".zip", true);

            if(_oggFile != null)
                File.Copy(_oggFile, tempArchiveName + "/" + _oggFile, true);

            ZipFile.CreateFromDirectory(tempArchiveName, tempArchiveName + ".zip");

            var finalArchiveName = string.Format("{0}_{1}.zip", _modelName, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm"));
            File.Move(tempArchiveName + ".zip", ARCHIVE + "/" + finalArchiveName, true);

            Console.WriteLine();
            Console.WriteLine("The builds were saved to {0}/{1}", ARCHIVE, finalArchiveName);
        }

        public void Reset()
        {
            if (Directory.Exists(TEMP))
                Directory.Delete(TEMP, true);

            if (Directory.Exists(TEMP + ARCHIVE))
                Directory.Delete(TEMP + ARCHIVE, true);

            if (Directory.Exists(TEMP + ARCHIVE + ".zip"))
                Directory.Delete(TEMP + ARCHIVE + ".zip", true);

            if (File.Exists(SCENE_FILE))
                File.Delete(SCENE_FILE);

            if (File.Exists(OVRSCENE_NAME + OVRSCENE_EXTENSION))
                File.Delete(OVRSCENE_NAME + OVRSCENE_EXTENSION);

            Directory.GetFiles(Directory.GetCurrentDirectory(), "*.apk").ToList().ForEach(f => File.Delete(f));
        }
    }
}
