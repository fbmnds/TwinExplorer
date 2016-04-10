//------------------------------------------------------------------------------
// <copyright file="TwinExplorerCommand.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace TwinExplorer
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TwinExplorerCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f4da10d4-695e-44e1-a82e-fc0ed6cac797");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwinExplorerCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TwinExplorerCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TwinExplorerCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new TwinExplorerCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            string file1, file2;

            var items = GetSelectedFiles(dte);

            file1 = items.ElementAtOrDefault(0);
            file2 = items.ElementAtOrDefault(1);

            if (!string.IsNullOrEmpty(file1) && !string.IsNullOrEmpty(file2))
            {
                StartExplorer(file1);
                System.Threading.Thread.Sleep(1000);
                StartExplorer(file2);
                System.Threading.Thread.Sleep(1000);
            }
            else if (!string.IsNullOrEmpty(file1))
            {
                StartExplorer(file1);
                System.Threading.Thread.Sleep(1000);
                StartExplorer(file1);
                System.Threading.Thread.Sleep(1000);
            }

            List<Process> explorers = new List<Process>();
            foreach (var p in System.Diagnostics.Process.GetProcesses())
            {
                try
                {
                    if (!p.HasExited && p.ProcessName == "explorer")
                    {
                        explorers.Add(new Process(p));
                    }
                }
                catch { }
            }
            explorers.Sort();

            RECT desktop = new RECT();
            GetWindowRect(new HandleRef(null, GetDesktopWindow()), out desktop);
            int height = desktop.Bottom - desktop.Top;
            int width = desktop.Right - desktop.Left;

            if (explorers.Count > 1)
            {
                MoveWindow(explorers.ElementAt(1).GetProcess().MainWindowHandle, 0, 0, (width / 2), (height - 40), true);
                ShowWindow(explorers.ElementAt(1).GetProcess().MainWindowHandle, SW_SHOWNOACTIVATE);

                MoveWindow(explorers.ElementAt(0).GetProcess().MainWindowHandle, (width / 2), 0, (width / 2), (height - 40), true);
                ShowWindow(explorers.ElementAt(0).GetProcess().MainWindowHandle, SW_SHOWNOACTIVATE);

                for (var i = 5; i < explorers.Count; i++)
                {
                    try
                    {
                        explorers.ElementAt(i).GetProcess().Kill();
                    }
                    catch { }
                }

            }
            return;
        }

        private class Process : IComparable<Process>
        {
            private System.Diagnostics.Process _p = null;

            private Process() { }

            public Process(System.Diagnostics.Process p) { _p = p; }

            public System.Diagnostics.Process GetProcess()
            {
                return _p;
            }

            public int CompareTo (Process p)
            {
                if (_p.StartTime.Ticks < p.GetProcess().StartTime.Ticks) { return 1; }
                else if (_p.StartTime.Ticks > p.GetProcess().StartTime.Ticks) { return -1; }
                else return 0;
            }
        }


        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr child, IntPtr parent);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmd);
        private const int SW_SHOWNOACTIVATE = 0x004;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndAfter, int x, int y, int cx, int cy, int uFlags);

        private const int HWND_TOPMOST = -1;
        private const int SWP_ASYNCWINDOWPOS = 0x4000;
        private const int SWP_SHOWWINDOW = 0x0040;
        private const int  SWP_FRAMECHANGED = 0x0020;


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        private void StartExplorer (string fileName)
        {
            try
            {
                FileAttributes attr = File.GetAttributes(fileName);
                if (attr.HasFlag(FileAttributes.Directory))
                    StartProcess("explorer.exe", "/n /e," + fileName);
                else
                    StartProcess("explorer.exe", "/n /e," + Path.GetDirectoryName(fileName));
            }
            catch { StartProcess("explorer.exe", "/n /e," + Directory.GetCurrentDirectory()); }
        }

        private System.Diagnostics.Process StartProcess (string fileName, string arguments)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = fileName;    //"explorer.exe"
            p.StartInfo.Arguments = arguments;  //(sprintf @"/n /e,%s" home)
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            return p;
        }

        private static IEnumerable<string> GetSelectedFiles(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;
            return from item in items.Cast<EnvDTE.UIHierarchyItem>()
                   let pi = item.Object as ProjectItem
                   select pi.FileNames[1];
        }
    }
}
