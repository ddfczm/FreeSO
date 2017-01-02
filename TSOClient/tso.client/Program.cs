/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.IO;
using System.Threading;
using FSO.Client.Utils.GameLocator;
using FSO.Client.Utils;
using System.Reflection;
using FSO.Common;
using FSO.Client.Debug;
using System.Windows.Forms;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client
{

    public static class Program
    {

        public static bool UseDX = true;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static void Main(string[] args)
        {
            if (InitWithArguments(args))
                (new GameStartProxy()).Start(UseDX);
        }

        public static bool InitWithArguments(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            //Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            ClipboardHandler.Default = new WinFormsClipboard();

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;

            ILocator gameLocator;
            bool linux = pid == PlatformID.MacOSX || pid == PlatformID.Unix;
            if (linux) gameLocator = new LinuxLocator();
            else gameLocator = new WindowsLocator();

            bool useDX = false;

            #region User resolution parmeters

            foreach (var arg in args)
            {
                if (char.IsDigit(arg[0]))
                {
                    //attempt parsing resoulution
                    try
                    {
                        var split = arg.Split("x".ToCharArray());
                        int ScreenWidth = int.Parse(split[0]);
                        int ScreenHeight = int.Parse(split[1]);

                        GlobalSettings.Default.GraphicsWidth = ScreenWidth;
                        GlobalSettings.Default.GraphicsHeight = ScreenHeight;
                    }
                    catch (Exception) { }
                }
                else if (arg[0] == '-')
                {
                    var cmd = arg.Substring(1);
                    if (cmd.StartsWith("lang"))
                    {
                        GlobalSettings.Default.LanguageCode = byte.Parse(cmd.Substring(4));
                    }
                    else if (cmd.StartsWith("hz")) GlobalSettings.Default.TargetRefreshRate = int.Parse(cmd.Substring(2));
                    else
                    {
                        //normal style param
                        switch (cmd)
                        {
                            case "dx11":
                            case "dx":
                                useDX = true;
                                break;
                            case "gl":
                            case "ogl":
                                useDX = false;
                                break;
                        }
                    }
                }
                else
                {
                    if (arg.Equals("w", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = true;
                    else if (arg.Equals("f", StringComparison.InvariantCultureIgnoreCase))
                        GlobalSettings.Default.Windowed = false;
                }
            }

            #endregion

            UseDX = MonogameLinker.Link(useDX);

            /*if (GlobalSettings.Default.Windowed == false && !UseDX)
            {
                //temporary while SDL issues are fixed
                MessageBox.Show("Fullscreen is currently disabled on OpenGL. Please switch to DirectX (-dx flag) if you really need to use fullscreen.");
            }*/

            var path = gameLocator.FindTheSimsOnline();

            if (UseDX) GlobalSettings.Default.AntiAlias = false;

            if (path != null)
            {
                FSOEnvironment.ContentDir = "Content/";
                FSOEnvironment.GFXContentDir = "Content/" + (UseDX ? "DX/" : "OGL/");
                FSOEnvironment.Linux = linux;
                FSOEnvironment.DirectX = UseDX;
                FSOEnvironment.GameThread = Thread.CurrentThread;
                if (GlobalSettings.Default.LanguageCode == 0) GlobalSettings.Default.LanguageCode = 1;
                Files.Formats.IFF.Chunks.STR.DefaultLangCode = (Files.Formats.IFF.Chunks.STRLangCode)GlobalSettings.Default.LanguageCode;

                GlobalSettings.Default.StartupPath = path;
                GlobalSettings.Default.ClientVersion = GetClientVersion();
                return true;
            }
            else
            {
                //MessageBox.Show("The Sims Online was not found on your system. FreeSO will not be able to run without access to the original game files.");
                return false;
            }
        }

        private static System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var assemblyPath = Path.Combine(MonogameLinker.AssemblyDir, args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll");
                var assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Exception: \r\n" + e.ExceptionObject.ToString());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogThis.Log.LogThis("Exception: " + e.Exception.ToString(), LogThis.eloglevel.error);
            MessageBox.Show("Exception: \r\n" + e.Exception.ToString());
        }

        private static string GetClientVersion()
        {
            string ExeDir = GlobalSettings.Default.StartupPath;

            if (File.Exists("version.txt"))
            {
                using (StreamReader Reader = new StreamReader(File.Open("version.txt", FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    return Reader.ReadLine();
                }
            }
            else
            {
                return "(?)";
            }
        }
    }
}
