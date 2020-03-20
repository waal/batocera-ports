﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace emulatorLauncher.libRetro
{
    class LibRetroGenerator : Generator
    {
        public string RetroarchPath { get; set; }
        public string RetroarchCorePath { get; set; }
        
        public LibRetroGenerator()
        {
            RetroarchPath = AppConfig.GetFullPath("retroarch");

            RetroarchCorePath = AppConfig.GetFullPath("retroarch.cores");
            if (string.IsNullOrEmpty(RetroarchCorePath))
                RetroarchCorePath = Path.Combine(RetroarchPath, "cores");
        }

        private void ConfigureCoreOptions(string system, string core)
        {
            var coreSettings = ConfigFile.FromFile(Path.Combine(RetroarchPath, "retroarch-core-options.cfg"));

            if (core == "atari800")
            {
                if (system == "atari800")
                {
                    coreSettings["atari800_system"] = "800XL (64K)";
                    coreSettings["RAM_SIZE"] = "64";
                    coreSettings["STEREO_POKEY"] = "1";
                    coreSettings["BUILTIN_BASIC"] = "1";
                }
                else
                {
                    coreSettings["atari800_system"] = "5200";
                    coreSettings["RAM_SIZE"] = "16";
                    coreSettings["STEREO_POKEY"] = "0";
                    coreSettings["BUILTIN_BASIC"] = "0";
                }
            }

            if (core == "bluemsx")
            {
                coreSettings["bluemsx_overscan"] = "enabled";

                if (system == "colecovision")
                    coreSettings["bluemsx_msxtype"] = "ColecoVision";
                else if (system == "msx1")
                    coreSettings["bluemsx_msxtype"] = "MSX";
                else if (system == "msx2")
                    coreSettings["bluemsx_msxtype"] = "MSX2";
                else if (system == "msx2+")
                    coreSettings["bluemsx_msxtype"] = "MSX2+";
                else if (system == "msxturbor")
                    coreSettings["bluemsx_msxtype"] = "MSXturboR";
                else 
                    coreSettings["bluemsx_msxtypec"] = "Auto";
            }

            if (core == "mame078" || core == "mame2003")
            {
                coreSettings["mame2003_skip_disclaimer"] = "enabled";
                coreSettings["mame2003_skip_warnings"] = "enabled";
            }

            if (core == "mame078plus" || core == "mame2003_plus")
            {
                coreSettings["mame2003-plus_skip_disclaimer"] = "enabled";
                coreSettings["mame2003-plus_skip_warnings"] = "enabled";
                //coreSettings["mame2003-plus_analog"] = "digital";
            }

            if (core == "virtualjaguar")
                coreSettings["virtualjaguar_usefastblitter"] = "enabled";

            if (core == "flycast")
                coreSettings["reicast_threaded_rendering"] = "enabled";

            if (coreSettings.IsDirty)
                coreSettings.Save(Path.Combine(RetroarchPath, "retroarch-core-options.cfg"), true);
        }

        private void Configure(string system)
        {
            var retroarchConfig = ConfigFile.FromFile(Path.Combine(RetroarchPath, "retroarch.cfg"));

            retroarchConfig["quit_press_twice"] = "false";
            retroarchConfig["pause_nonactive"] = "false";
            retroarchConfig["video_fullscreen"] = "true";
            // retroarchConfig["menu_driver"] = "ozone";

            if (!string.IsNullOrEmpty(AppConfig["bios"]) && Directory.Exists(AppConfig["bios"]))
                retroarchConfig["system_directory"] = AppConfig["bios"];
            else 
                retroarchConfig["system_directory"] = @":\system";

            if (!string.IsNullOrEmpty(AppConfig["thumbnails"]) && Directory.Exists(AppConfig["thumbnails"]))
                retroarchConfig["thumbnails_directory"] = AppConfig["thumbnails"];
            else 
                retroarchConfig["system_directory"] = @":\thumbnails";

            if (!string.IsNullOrEmpty(AppConfig["saves"]) && Directory.Exists(AppConfig["saves"]))
            {
                retroarchConfig["savestate_directory"] = Path.Combine(AppConfig["saves"], system);
                retroarchConfig["savefile_directory"] = Path.Combine(AppConfig["saves"], system);
            }

            if (SystemConfig.isOptSet("smooth") && SystemConfig.getOptBoolean("smooth"))
                retroarchConfig["video_smooth"] = "true";
            else
                retroarchConfig["video_smooth"] = "false";



            if (AppConfig.isOptSet("shaders") && SystemConfig.isOptSet("shader") && SystemConfig["shader"] != "None")
            {
                retroarchConfig["video_shader_enable"] = "true";
                retroarchConfig["video_smooth"]        = "false";     // seems to be necessary for weaker SBCs
                retroarchConfig["video_shader_dir"] = AppConfig.GetFullPath("shaders");
            }
            else
                retroarchConfig["video_shader_enable"] = "false";

            if (SystemConfig.isOptSet("ratio"))
            {
                if (SystemConfig["ratio"] == "custom")
                    retroarchConfig["video_aspect_ratio_auto"] = "false";
                else
                {
                    int idx = ratioIndexes.IndexOf(SystemConfig["ratio"]);
                    if (idx >= 0)
                    {
                        retroarchConfig["aspect_ratio_index"] = idx.ToString();
                        retroarchConfig["video_aspect_ratio_auto"] = "false";
                    }
                    else
                    {
                        retroarchConfig["video_aspect_ratio_auto"] = "true";
                        retroarchConfig["aspect_ratio_index"] = "";
                    }
                }
            }
            else
                retroarchConfig["aspect_ratio_index"] = "";

            if (SystemConfig.isOptSet("rewind") && SystemConfig.getOptBoolean("rewind"))
                retroarchConfig["rewind_enable"] = "true";
            else
                retroarchConfig["rewind_enable"] = "false";

            if (SystemConfig.isOptSet("integerscale") && SystemConfig.getOptBoolean("integerscale"))
                retroarchConfig["video_scale_integer"] = "true";
            else
                retroarchConfig["video_scale_integer"] = "false";

            if (SystemConfig.isOptSet("video_threaded") && SystemConfig.getOptBoolean("video_threaded"))
                retroarchConfig["video_threaded"] = "true";               
            else
                retroarchConfig["video_threaded"] = "false";

            if (SystemConfig.isOptSet("showFPS") && SystemConfig.getOptBoolean("showFPS"))
                retroarchConfig["fps_show"] = "true";
            else
                retroarchConfig["fps_show"] = "false";

            if (SystemConfig.isOptSet("autosave") && SystemConfig.getOptBoolean("autosave"))
            {
                retroarchConfig["savestate_auto_save"] = "true";
                retroarchConfig["savestate_auto_load"] = "true";
            }
            else
            {
                retroarchConfig["savestate_auto_save"] = "false";
                retroarchConfig["savestate_auto_load"] = "false";
            }

            if (SystemConfig["retroachievements"] == "true" && systemToRetroachievements.Contains(system))
            {
                retroarchConfig["cheevos_enable"] = "true";
                retroarchConfig["cheevos_username"] = SystemConfig["retroachievements.username"];
                retroarchConfig["cheevos_password"] = SystemConfig["retroachievements.password"];
                retroarchConfig["cheevos_hardcore_mode_enable"] = SystemConfig["retroachievements.hardcore"] == "true" ? "true" : "false";
                retroarchConfig["cheevos_leaderboards_enable"] = SystemConfig["retroachievements.leaderboards"] == "true" ? "true" : "false";
                retroarchConfig["cheevos_verbose_enable"] = SystemConfig["retroachievements.verbose"] == "true" ? "true" : "false";
                retroarchConfig["cheevos_auto_screenshot"] = SystemConfig["retroachievements.screenshot"] == "true" ? "true" : "false";
            }
            else
                retroarchConfig["cheevos_enable"] = "false";

            // Netplay management : netplaymode client -netplayport " + std::to_string(options.port) + " -netplayip
            if (SystemConfig["netplay"] == "true" && !string.IsNullOrEmpty(SystemConfig["netplaymode"]))
            {
                // Security : hardcore mode disables save states, which would kill netplay
                retroarchConfig["cheevos_hardcore_mode_enable"] = "false";

                retroarchConfig["netplay_mode"] = "false";
                retroarchConfig["netplay_ip_port"] = SystemConfig["netplay.port"]; // netplayport
                retroarchConfig["netplay_nickname"] = SystemConfig["netplay.nickname"];
                retroarchConfig["netplay_mitm_server"] = SystemConfig["netplay.relay"];
                retroarchConfig["netplay_use_mitm_server"] = string.IsNullOrEmpty(SystemConfig["netplay.relay"]) ? "true" : "false";

                retroarchConfig["netplay_spectator_mode_enable"] = SystemConfig.getOptBoolean("netplay.spectator") ? "true" : "false";
                retroarchConfig["netplay_client_swap_input"] = "false";

                if (SystemConfig["netplaymode"] == "client")
                {
                    retroarchConfig["netplay_mode"] = "true";
                    retroarchConfig["netplay_ip_address"] = SystemConfig["netplayip"];
                    retroarchConfig["netplay_ip_port"] = SystemConfig["netplayport"];
                    retroarchConfig["netplay_client_swap_input"] = "true";
                }
            }

            // AI service for game translations
            if (SystemConfig.isOptSet("ai_service_enabled") && SystemConfig.getOptBoolean("ai_service_enabled"))
            {
                retroarchConfig["ai_service_enable"] = "true";
                retroarchConfig["ai_service_mode"] = "0";
                retroarchConfig["ai_service_source_lang"] = "0";

                if (!string.IsNullOrEmpty(SystemConfig["ai_service_url"]))
                    retroarchConfig["ai_service_url"] = SystemConfig["ai_service_url"] + "&mode=Fast&output=png&target_lang=" + SystemConfig["ai_target_lang"];
                else
                    retroarchConfig["ai_service_url"] = "http://" + "ztranslate.net/service?api_key=BATOCERA&mode=Fast&output=png&target_lang=" + SystemConfig["ai_target_lang"];

                if (SystemConfig.isOptSet("ai_service_pause") && SystemConfig.getOptBoolean("ai_service_pause"))
                    retroarchConfig["ai_service_pause"] = "true";
                else
                    retroarchConfig["ai_service_pause"] = "false";
            }
            else
                retroarchConfig["ai_service_enable"] = "false";
            
            // custom : allow the user to configure directly retroarch.cfg via batocera.conf via lines like : snes.retroarch.menu_driver=rgui
            foreach (var user_config in SystemConfig)
                if (user_config.Name.StartsWith("retroarch."))
                    retroarchConfig[user_config.Name.Substring("retroarch.".Length)] = user_config.Value;

            if (retroarchConfig.IsDirty)
                retroarchConfig.Save(Path.Combine(RetroarchPath, "retroarch.cfg"), true);
        }

        public override ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, string gameResolution)
        {
            Configure(system);
            ConfigureCoreOptions(system, core);

            List<string> commandArray = new List<string>();

            if (!string.IsNullOrEmpty(SystemConfig["netplaymode"]))
            {
                // Netplay mode
                if (SystemConfig["netplaymode"] == "host")
                    commandArray.Add("--host");
                else if (SystemConfig["netplaymode"] == "client")
                {
                    commandArray.Add("--connect");
                    commandArray.Add(SystemConfig["netplay.server.address"]);
                }
            }

            return new ProcessStartInfo()
            {
                FileName = Path.Combine(RetroarchPath, "retroarch.exe"),
                Arguments = "-L \"" + Path.Combine(RetroarchCorePath, core + "_libretro.dll") + "\" \"" + rom + "\" " + string.Join(" ", commandArray)
            };
        }

        List<string> ratioIndexes = new List<string> { "4/3", "16/9", "16/10", "16/15", "21/9", "1/1", "2/1", "3/2", "3/4", "4/1", "4/4", "5/4", "6/5", "7/9", "8/3",
                "8/7", "19/12", "19/14", "30/17", "32/9", "config", "squarepixel", "core", "custom" };

        List<string> systemToRetroachievements = new List<string> { 
            "atari2600", "atari7800", "atarijaguar", "colecovision", "nes", "snes", "virtualboy", "n64", "sg1000", "mastersystem", "megadrive", 
            "segacd", "sega32x", "saturn", "pcengine", "pcenginecd", "supergrafx", "psx", "mame", "fbneo", "neogeo", "lightgun", "apple2", 
            "lynx", "wswan", "wswanc", "gb", "gbc", "gba", "nds", "pokemini", "gamegear", "ngp", "ngpc"}; 

    }
}