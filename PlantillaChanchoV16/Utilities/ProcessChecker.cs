using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PlantillaChanchoV16.Utilities
{
    public static class ProcessChecker
    {
        public static bool IsProhibitedProgramRunning()
        {
            var runningProcesses = Process.GetProcesses();
            foreach (var process in runningProcesses)
            {
                try
                {
                    var description = GetFileDescription(process.MainModule.FileName);
                    var originalName = GetFileOriginalName(process.MainModule.FileName);

                    if (ProcessNames.Any(pn => process.ProcessName.Equals(pn, StringComparison.OrdinalIgnoreCase)) ||
                        ProcessDescriptions.Any(pd => description.Equals(pd, StringComparison.OrdinalIgnoreCase) ||
                            originalName.Equals(pd, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
                catch
                {

                }
            }
            return false;
        }

        private static readonly string[] ProcessNamesArray = new[]
        {
            "Cheat Engine", "Process Hacker 2",
            "taskmgr", "msconfig", "regedit",
            "cheatengine-x86_64-SSE4-AVX2", "ollydbg.exe", "ProcessHacker.exe", "Dump-Fixer.exe",
            "kdstinker.exe", "tcpview.exe", "autoruns.exe", "autorunsc.exe", "filemon.exe",
            "procmon.exe", "regmon.exe", "procexp.exe", "ImmunityDebugger.exe", "Wireshark.exe",
            "dumpcap.exe", "HookExplorer.exe", "ImportREC.exe", "PETools.exe", "LordPE.exe",
            "SysInspector.exe", "proc_analyzer.exe", "sysAnalyzer.exe", "sniff_hit.exe",
            "windbg.exe", "joeboxcontrol.exe", "Fiddler.exe", "joeboxserver.exe", "ida64.exe",
            "ida.exe", "idaq64.exe", "Vmtoolsd.exe", "Vmwaretrat.exe", "Vmwareuser.exe",
            "Vmacthlp.exe", "vboxservice.exe", "vboxtray.exe", "ReClass.NET.exe", "x64dbg.exe",
            "OLLYDBG.exe", "MugenJinFuu-i386.exe", "Mugen JinFuu.exe", "MugenJinFuu-x86_64-SSE4-AVX2.exe",
            "MugenJinFuu-x86_64.exe", "KsDumper.exe", "dnSpy.exe", "cheatengine-i386.exe", "cheatengine-x86_64.exe",
            "Fiddler Everywhere.exe", "HTTPDebuggerSvc.exe", "Fiddler.WebUi.exe", "createdump.exe",
            "ILDASM", "x64dbg", "ollydbg", "ProcessHacker", "Dump-Fixer",
            "kdstinker", "tcpview", "autoruns", "autorunsc", "filemon",
            "procmon", "regmon", "procexp", "ImmunityDebugger", "Wireshark",
            "dumpcap", "HookExplorer", "ImportREC", "PETools", "LordPE",
            "SysInspector", "proc_analyzer", "sysAnalyzer", "sniff_hit",
            "windbg", "joeboxcontrol", "Fiddler", "joeboxserver", "ida64",
            "ida", "idaq64", "Vmtoolsd", "Vmwaretrat", "Vmwareuser",
            "Vmacthlp", "vboxservice", "vboxtray", "ReClass.NET", "JustDecompile"
        };

        private static readonly string[] ProcessDescriptionsArray = new[]
        {
            "Cheat Engine", "Process Hacker 2",
            "Windows Task Manager", "Microsoft System Configuration", "Windows Registry Editor",
            "Cheat Engine", "OllyDbg", "Process Hacker", "Dump-Fixer", "KDStinker", "TCPView",
            "Autoruns", "FileMon", "ProcMon", "RegMon", "Process Explorer", "Immunity Debugger",
            "Wireshark", "Dumpcap", "Hook Explorer", "Import REC", "PE Tools", "LordPE", "SysInspector",
            "Process Analyzer", "SysAnalyzer", "Sniff Hit", "WinDbg", "Joebox Control", "Fiddler",
            "Joebox Server", "IDA Pro", "VM Tools", "VMware Tools", "VMware User", "VMAC Helper",
            "VBoxService", "VBoxTray", "ReClass.NET", "x64dbg", "OllyDbg", "Mugen JinFuu", "KS Dumper",
            "dnSpy", "Cheat Engine", "Fiddler Everywhere", "HTTP Debugger", "Fiddler Web UI", "Create Dump",
            "ILDASM", "x64dbg", "OllyDbg", "Process Hacker", "Dump-Fixer", "KDStinker", "TCPView",
            "Autoruns", "FileMon", "ProcMon", "RegMon", "Process Explorer", "Immunity Debugger",
            "Wireshark", "Dumpcap", "Hook Explorer", "Import REC", "PE Tools", "LordPE", "SysInspector",
            "Process Analyzer", "SysAnalyzer", "Sniff Hit", "WinDbg", "Joebox Control", "Fiddler",
            "Joebox Server", "IDA Pro", "VM Tools", "VMware Tools", "VMware User", "VMAC Helper",
            "VBoxService", "VBoxTray", "ReClass.NET", "JustDecompile"
        };

        private static readonly string[] ProcessNames = ProcessNamesArray.Distinct().ToArray();
        private static readonly string[] ProcessDescriptions = ProcessDescriptionsArray.Distinct().ToArray();

        public static void CheckForProcesses()
        {
            var runningProcesses = Process.GetProcesses();
            var processesToTerminate = new List<Process>();

            foreach (var process in runningProcesses)
            {
                try
                {
                    var description = GetFileDescription(process.MainModule.FileName);
                    var originalName = GetFileOriginalName(process.MainModule.FileName);

                    if (ProcessNames.Any(pn => process.ProcessName.Equals(pn, StringComparison.OrdinalIgnoreCase)) ||
                        ProcessDescriptions.Any(pd => description.Equals(pd, StringComparison.OrdinalIgnoreCase) ||
                            originalName.Equals(pd, StringComparison.OrdinalIgnoreCase)))
                    {
                        processesToTerminate.Add(process);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Acceso denegado al archivo del proceso {process.ProcessName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar el proceso {process.ProcessName}: {ex.Message}");
                }
            }

            foreach (var process in processesToTerminate)
            {
                try
                {
                    TerminateProcess(process.Handle, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cerrar el proceso {process.ProcessName}: {ex.Message}");
                }
            }
        }

        private static string GetFileDescription(string filePath)
        {
            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                return fileVersionInfo.FileDescription ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetFileOriginalName(string filePath)
        {
            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                return fileVersionInfo.OriginalFilename ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static List<string> GetInstalledPrograms()
        {
            List<string> installedPrograms = new List<string>();

            string[] registryKeys = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string registryKey in registryKeys)
            {
                using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (var subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey != null)
                                {
                                    string displayName = subkey.GetValue("DisplayName") as string;
                                    if (!string.IsNullOrEmpty(displayName))
                                    {
                                        foreach (string program in ProcessNames)
                                        {
                                            if (displayName.IndexOf(program, StringComparison.OrdinalIgnoreCase) >= 0)
                                            {
                                                installedPrograms.Add(displayName);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            string dnSpyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "dnSpy");
            if (Directory.Exists(dnSpyFolder))
            {
                installedPrograms.Add("dnSpy (carpeta encontrada en AppData\\Local)");
            }

            return installedPrograms;
        }

        public static List<string> DetectInstalledPrograms()
        {
            List<string> installedPrograms = GetInstalledPrograms();
            List<string> detectedPrograms = new List<string>();

            foreach (string processName in ProcessNames)
            {
                if (installedPrograms.Any(p => p.IndexOf(processName, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    detectedPrograms.Add(processName);
                }
            }

            return detectedPrograms;
        }

        public static void ShowDetectedPrograms()
        {
            CheckForProcesses();

            List<string> detectedPrograms = DetectInstalledPrograms();
            var sb = new StringBuilder();

            if (detectedPrograms.Count == 0)
            {
                sb.AppendLine("No se encontraron los programas especificados instalados.");
            }
            else
            {
                sb.AppendLine("Programas instalados encontrados:");
                foreach (var program in detectedPrograms)
                {
                    sb.AppendLine(program);
                }
            }

            List<string> installedPrograms = GetInstalledPrograms();
            var dnSpyFolderDetected = installedPrograms.Any(p => p.Contains("dnSpy (carpeta encontrada en AppData\\Local)"));
            if (dnSpyFolderDetected)
            {
                sb.AppendLine("dnSpy");
            }

            PlantillaChanchoV16.Template.SakuraMessageBox.Show(sb.ToString(), "Programas Instalados", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Environment.Exit(0);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
    }
}
