﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Patcher
{
    public partial class Patcher : Form
    {
        private int RenameRetry = 0;
        private int RENAME_MAX_ATTEMPTS = 8;
        static HashSet<string> IgnoreFiles = new HashSet<string>()
        {
            "updater.exe",
            "Content/config.ini",
            "Ninject.dll", //til I add dynamic linking for these
            "Ninject.xml",
            "NLog.config"
        };

        public Patcher()
        {
            InitializeComponent();
        }

        private void Patcher_Load(object sender, EventArgs e)
        {
            //attempt to locate the patch files
            StatusLabel.Text = "Starting up...";

            if (!File.Exists("PatchFiles/patch.zip"))
            {
                MessageBox.Show("Could not find FreeSO Patch Files (these must be downloaded by the game!). Starting FreeSO...");
                StartFreeSO();
                return;
            }

            AttemptRename();
        }

        public async void Extract()
        {
            StatusLabel.Text = "Extracting FreeSO Files...";

            var archive = ZipFile.OpenRead("PatchFiles/patch.zip");
            foreach (var file in Directory.GetFiles("Content/Patch/"))
            {
                //delete any stray patch files. Don't delete user or subfolders (eg. translations) because they might be important
                File.Delete(file);
            }
            var entries = archive.Entries;
            foreach (var entry in entries)
            {
                if (IgnoreFiles.Contains(entry.FullName)) continue;
                var targPath = Path.Combine("./", entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(targPath));
                try
                {
                    entry.ExtractToFile(targPath, true);
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not replace " + targPath + "!");
                }
            }
            archive.Dispose();
            StartFreeSO();
        }

        public void AttemptRename()
        {
            try
            {
                File.Delete("FreeSO.exe.old");
                if (File.Exists("FreeSO.exe"))  //shouldn't be in use, unless the user has incorrectly renamed and run the freeso executable
                    File.Move("FreeSO.exe", "FreeSO.exe.old");
            }
            catch (Exception)
            {
                if (RenameRetry++ < RENAME_MAX_ATTEMPTS)
                {
                    StatusLabel.Text = "Waiting for FreeSO to Close...";
                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        AttemptRename();
                    });
                    return;
                }
                else
                {
                    MessageBox.Show("Could not update FreeSO as write access could not be gained to the game files. Try running update.exe as an administrator.");
                    Application.Exit();
                    return;
                }
            }
            Extract();
        }

        public void StartFreeSO()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                System.Diagnostics.Process.Start("mono", "FreeSO.exe");
            }
            else
            {
                System.Diagnostics.Process.Start("FreeSO.exe");
            }
            Application.Exit();
        }

        private void StatusLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
