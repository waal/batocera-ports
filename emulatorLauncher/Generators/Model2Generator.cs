﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace emulatorLauncher
{
    class Model2Generator : Generator
    {
        private string destFile;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, string gameResolution)
        {
            string path = AppConfig.GetFullPath("m2emulator");

            string exe = Path.Combine(path, "emulator.exe");
            if (emulator == "model2")
                exe = Path.Combine(path, "emulator_multicpu.exe");

            if (!File.Exists(exe))
                return null;

            string pakDir = Path.Combine(path, "roms");
            foreach (var file in Directory.GetFiles(pakDir))
            {
                if (Path.GetFileName(file) == Path.GetFileName(rom))
                    continue;

                File.Delete(file);
            }

            destFile = Path.Combine(pakDir, Path.GetFileName(rom));
            if (!File.Exists(destFile))
                File.Copy(rom, destFile);

            string arg = Path.GetFileNameWithoutExtension(destFile);

            return new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = arg,
                WorkingDirectory = path,                
            };            
        }

        public override void Cleanup()
        {
            if (destFile != null && File.Exists(destFile))
                File.Delete(destFile);
        }
    }
}