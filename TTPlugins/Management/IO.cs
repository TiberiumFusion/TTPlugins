﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using com.tiberiumfusion.ttplugins.Management.SecurityCompliance;

namespace com.tiberiumfusion.ttplugins.Management
{
    /// <summary>
    /// IO-related aspects of plugin management.
    /// </summary>
    public static class IO
    {
        #region Properties

        /// <summary>
        /// Absolute path to the folder containing the user's plugins, whether in .cs or .dll form.
        /// </summary>
        public static string PluginsUserFilesFolder { get; private set; } // Should be %APPDATA%/Terraria Tweaker 2/Plugins

        /// <summary>
        /// Absolute path to the top-level working folder for plugin data, where things like temporary savedata are stored.
        /// </summary>
        public static string PluginsDataFolder { get; private set; } // Should be %APPDATA%/Terraria Tweaker 2/ttplugins

        /// <summary>
        /// Absolute path to the root folder where plugin data is temporarily written, such as for a tweak launch or to edit configuration.
        /// </summary>
        public static string PluginsTempFilesFolder { get { return Path.Combine(PluginsDataFolder, "TempPluginFiles"); } }

        /// <summary>
        /// List of all PluginFiles that were found in the PluginsUserFilesFolder.
        /// </summary>
        public static List<PluginFile> FoundUserPluginFiles { get; private set; } = new List<PluginFile>();

        #endregion


        #region Fields

        /// <summary>
        /// The FileSystemWatcher that watches the PluginsUserFilesFolder.
        /// </summary>
        private static FileSystemWatcher FSWatcherUserFiles;

        #endregion


        #region Events
        
        /// <summary>
        /// Generic event args class for the first three UserPluginFile events.
        /// </summary>
        public class UserPluginFileEventArgs : EventArgs
        {
            public string FilePath { get; private set; }
            public PluginFile AffectedPluginFile { get; private set; }
            public UserPluginFileEventArgs(string path, PluginFile pluginFile)
            {
                FilePath = path;
                AffectedPluginFile = pluginFile;
            }
        }

        /// <summary>
        /// Event raised immediately after a new user plugin file has been added to the FoundUserPluginFiles list.
        /// </summary>
        public static event EventHandler<UserPluginFileEventArgs> UserPluginFileAdded;
        internal static void OnUserPluginFileAdded(UserPluginFileEventArgs e)
        {
            UserPluginFileAdded?.Invoke(null, e);
        }

        /// <summary>
        /// Event raised immediately after an existing user plugin file has been removed from the FoundUserPluginFiles list.
        /// </summary>
        public static event EventHandler<UserPluginFileEventArgs> UserPluginFileRemoved;
        internal static void OnUserPluginFileRemoved(UserPluginFileEventArgs e)
        {
            UserPluginFileRemoved?.Invoke(null, e);
        }

        /// <summary>
        /// Event raised immediately after an existing user plugin file has experienced a change, such as being saved.
        /// </summary>
        public static event EventHandler<UserPluginFileEventArgs> UserPluginFileChanged;
        internal static void OnUserPluginFileChanged(UserPluginFileEventArgs e)
        {
            UserPluginFileChanged?.Invoke(null, e);
        }

        /// <summary>
        /// Event args class for the renamed UserPluginFile event.
        /// </summary>
        public class UserPluginFileRenamedEventArgs : EventArgs
        {
            public string OldFilePath { get; private set; }
            public string NewFilePath { get; private set; }
            public PluginFile AffectedPluginFile { get; private set; }
            public UserPluginFileRenamedEventArgs(string oldPath, string newPath, PluginFile pluginFile)
            {
                OldFilePath = oldPath;
                NewFilePath = newPath;
                AffectedPluginFile = pluginFile;
            }
        }

        /// <summary>
        /// Event raised immediately after an existing user plugin file has been renamed.
        /// </summary>
        public static event EventHandler<UserPluginFileRenamedEventArgs> UserPluginFileRenamed;
        internal static void OnUserPluginFileRenamed(UserPluginFileRenamedEventArgs e)
        {
            UserPluginFileRenamed?.Invoke(null, e);
        }

        #endregion


        /// <summary>
        /// Sets up the bulk of the plugin management system and ensures all the necessary paths exist.
        /// </summary>
        /// <param name="tt2SavedataDirectory">Absolute path where Terraria Tweaker 2 stores its savedata.</param>
        public static void Initialize(string tt2SavedataDirectory)
        {
            // Find root folder
            PluginsUserFilesFolder = Path.Combine(tt2SavedataDirectory, "Plugins"); // tt2SavedataPath should be %APPDATA%/Terraria Tweaker 2
            PluginsDataFolder = Path.Combine(tt2SavedataDirectory, "ttplugins");

            // Create folders
            Directory.CreateDirectory(PluginsUserFilesFolder);
            Directory.CreateDirectory(PluginsDataFolder);
            Directory.CreateDirectory(PluginsTempFilesFolder);

            // Initial user file scan
            RescanAll();

            // File system watcher(s) for the user files
            CreateFileSystemWatchers();
        }


        /// <summary>
        /// Returns the PluginFile in FoundUserPluginFiles that has the provided relpath.
        /// </summary>
        /// <param name="relpath">The relpath identifier to use.</param>
        /// <returns>The matched PluginFile, no null if none was found.</returns>
        public static PluginFile GetPluginFileByRelPath(string relpath)
        {
            foreach (PluginFile pluginFile in FoundUserPluginFiles)
            {
                if (pluginFile.GetRelativePath().ToLowerInvariant() == relpath.ToLowerInvariant())
                {
                    return pluginFile;
                }
            }
            return null;
        }


        #region File System Watchers

        /// <summary>
        /// Creates the FileSystemWatcher(s) that monitor the plugin user files and plugin data folder.
        /// </summary>
        private static void CreateFileSystemWatchers(bool recreateUserFilesWatcher = true)
        {
            if (recreateUserFilesWatcher)
            {
                FSWatcherUserFiles = new FileSystemWatcher(PluginsUserFilesFolder);
                FSWatcherUserFiles.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite;
                FSWatcherUserFiles.Changed += FSWatcherUserFiles_Changed;
                FSWatcherUserFiles.Created += FSWatcherUserFiles_Created;
                FSWatcherUserFiles.Deleted += FSWatcherUserFiles_Deleted;
                FSWatcherUserFiles.Renamed += FSWatcherUserFiles_Renamed;
                FSWatcherUserFiles.Error += FSWatcherUserFiles_Error;
                FSWatcherUserFiles.IncludeSubdirectories = true;
                FSWatcherUserFiles.EnableRaisingEvents = true;
            }
        }
        
        ///// FileSystemWatcher events for the plugin user files folder
        private static void FSWatcherUserFiles_Created(object sender, FileSystemEventArgs e)
        {
            if (Path.GetExtension(e.FullPath).ToLowerInvariant() == ".cs")
                TryAddUserFileCS(e.FullPath);

            if (Path.GetExtension(e.FullPath).ToLowerInvariant() == ".dll")
                TryAddUserFileDLL(e.FullPath);
        }
        private static void FSWatcherUserFiles_Deleted(object sender, FileSystemEventArgs e)
        {
            if (Path.GetExtension(e.FullPath).ToLowerInvariant() == ".cs" || Path.GetExtension(e.FullPath).ToLowerInvariant() == ".dll")
                TryRemoveUserFile(e.FullPath);
            else if (!Directory.Exists(e.FullPath)) // FileSystemWatcher is pretty crap and doesn't tell the difference between a file or folder event. So any time a 'thing' that isnt a .cs or .dll is deleted, we have to treat it like a directory in case there are files inside its path.
                TryRemoveUserFilesInDirectory(e.FullPath);
        }
        private static void FSWatcherUserFiles_Changed(object sender, FileSystemEventArgs e)
        {
            if (Path.GetExtension(e.FullPath).ToLowerInvariant() == ".cs" || Path.GetExtension(e.FullPath).ToLowerInvariant() == ".dll")
                TryUpdateUserFile(e.FullPath);
        }
        private static void FSWatcherUserFiles_Renamed(object sender, RenamedEventArgs e)
        {
            if (Path.GetExtension(e.OldFullPath).ToLowerInvariant() == ".cs" || Path.GetExtension(e.OldFullPath).ToLowerInvariant() == ".dll")
                TryRenameUserFile(e.OldFullPath, e.FullPath);
            else if (!Directory.Exists(e.OldFullPath) && Directory.Exists(e.FullPath)) // See comment in FSWatcherUserFiles_Deleted
                TryRenameUserFilesInDirectory(e.OldFullPath, e.FullPath);
        }
        private static void FSWatcherUserFiles_Error(object sender, ErrorEventArgs e)
        {
            // Try to recreate the file system watcher if it gets borked somehow
            CreateFileSystemWatchers(true);
        }

        #endregion


        #region File Processing

        /// <summary>
        /// Rescans all files in the PluginsUserFilesFolder folder for .cs source files and .dll compiled assemblies.
        /// </summary>
        public static void RescanAll()
        {
            FoundUserPluginFiles.Clear();

            // Find CS source plugin files
            string[] filesA = Directory.GetFiles(PluginsUserFilesFolder, "*.cs", SearchOption.AllDirectories);
            foreach (string file in filesA)
                TryAddUserFileCS(file);

            // Find compiled assembly plugin files
            string[] filesB = Directory.GetFiles(PluginsUserFilesFolder, "*.dll", SearchOption.AllDirectories);
            foreach (string file in filesB)
                TryAddUserFileDLL(file);
        }

        
        /// <summary>
        /// Adds the specified .cs file to the FoundUserPluginFiles list if it is valid. If a plugin file already exists at the specified path, it will be replaced.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>True if the plugin file was added, false if otherwise.</returns>
        private static bool TryAddUserFileCS(string path)
        {
            List<PluginFile> existing = FoundUserPluginFiles.Where(x => PathsAreEqual(path, x.PathToFile)).ToList();
            foreach (PluginFile pluginFile in existing)
                FoundUserPluginFiles.Remove(pluginFile);

            // In this case, there is no file contents checking (for now)
            PluginFile newPluginFile = new PluginFile(path, PluginFileType.CSSourceFile);
            FoundUserPluginFiles.Add(newPluginFile);
            OnUserPluginFileAdded(new UserPluginFileEventArgs(path, newPluginFile));
            return true;
        }

        /// <summary>
        /// Adds the specified .dll file to the FoundUserPluginFiles list if it is a valid .net assembly. If a plugin file already exists at the specified path, it will be replaced.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>True if the plugin file was added, false if otherwise.</returns>
        private static bool TryAddUserFileDLL(string path)
        {
            List<PluginFile> existing = FoundUserPluginFiles.Where(x => PathsAreEqual(path, x.PathToFile)).ToList();
            foreach (PluginFile pluginFile in existing)
                FoundUserPluginFiles.Remove(pluginFile);

            // Check if the DLL is a valid .NET assembly
            bool valid = false;
            try
            {
                AssemblyName asmName = AssemblyName.GetAssemblyName(path);
                valid = true;
            }
            catch (Exception e) { }

            if (valid)
            {
                PluginFile newPluginFile = new PluginFile(path, PluginFileType.CompiledAssemblyFile);
                FoundUserPluginFiles.Add(newPluginFile);
                OnUserPluginFileAdded(new UserPluginFileEventArgs(path, newPluginFile));
                return true;
            }
            else
                return false;
        }
        
        /// <summary>
        /// Removes the specified file from the FoundUserPluginFiles list if it is valid.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>True if the plugin file was removed, false if otherwise.</returns>
        private static bool TryRemoveUserFile(string path)
        {
            PluginFile toRemove = null;
            foreach (PluginFile pluginFile in FoundUserPluginFiles)
            {
                if (PathsAreEqual(path, pluginFile.PathToFile))
                {
                    toRemove = pluginFile;
                    break;
                }
            }
            if (toRemove != null)
            {
                FoundUserPluginFiles.Remove(toRemove);
                OnUserPluginFileRemoved(new UserPluginFileEventArgs(toRemove.PathToFile, toRemove));
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Removes all files in the provided directory from the FoundUserPluginFiles list if they are valid. Necessary because FileSystemWatcher does not fire for the individual files that are deleted when their parent folder is deleted.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>True if any plugin files were removed, false if otherwise.</returns>
        private static bool TryRemoveUserFilesInDirectory(string path)
        {
            List<PluginFile> toRemove = new List<PluginFile>();
            foreach (PluginFile pluginFile in FoundUserPluginFiles)
            {
                if (IsPathInPath(pluginFile.PathToFile, path))
                {
                    toRemove.Add(pluginFile);
                }
            }

            bool removedAny = false;
            foreach (PluginFile pluginFile in toRemove)
            {
                FoundUserPluginFiles.Remove(pluginFile);
                removedAny = true;
                OnUserPluginFileRemoved(new UserPluginFileEventArgs(pluginFile.PathToFile, pluginFile));
            }
            return removedAny;
        }

        /// <summary>
        /// Updates the PluginFile specified by the provided path if it exists in the FoundUserPluginFiles list.
        /// </summary>
        /// <param name="path">The full path to the file.</param>
        /// <returns>True if the plugin existed and was updated, false if otherwise.</returns>
        private static bool TryUpdateUserFile(string path)
        {
            // Try to update existing user file first
            foreach (PluginFile pluginFile in FoundUserPluginFiles)
            {
                if (PathsAreEqual(path, pluginFile.PathToFile))
                {
                    pluginFile.UpdateFromFileChange();
                    OnUserPluginFileChanged(new UserPluginFileEventArgs(pluginFile.PathToFile, pluginFile));
                    return true;
                }
            }

            // If there is no existing user file, then we need to create a new PluginFile for this
            // (This is the result of the situation where a 0 byte file is created, e.g. "new.txt", which is not deteced by OnCreated; then it is changede to "new.cs" (also not deteced b/c still 0 bytes) and finally edited to be >0 bytes)
            string ext = Path.GetExtension(Path.GetFullPath(path)).ToLowerInvariant(); ;
            if (ext == ".cs")
                TryAddUserFileCS(path);
            else if (ext == ".dll")
                TryAddUserFileDLL(path);

            return false;
        }


        /// <summary>
        /// Updates the PluginFile specified by the provided path if it exists in the FoundUserPluginFiles list.
        /// </summary>
        /// <param name="oldPath">The old file path.</param>
        /// <param name="newPath">The new file path.</param>
        /// <returns>True if a plugin was existing and updated/removed or if a new plugin was added; false if otherwise.</returns>
        private static bool TryRenameUserFile(string oldPath, string newPath)
        {
            string oldExtention = Path.GetExtension(Path.GetFullPath(oldPath)).ToLowerInvariant();
            string newExtension = Path.GetExtension(Path.GetFullPath(newPath)).ToLowerInvariant();

            // Check for an existing plugin being updated
            foreach (PluginFile pluginFile in FoundUserPluginFiles)
            {
                if (PathsAreEqual(oldPath, pluginFile.PathToFile))
                {
                    // If the extension did not change, then the plugin file is still valid
                    if (oldExtention == newExtension)
                    {
                        pluginFile.UpdateFilePath(newPath);
                        OnUserPluginFileRenamed(new UserPluginFileRenamedEventArgs(oldPath, newPath, pluginFile));
                        return true;
                    }
                    // If the extension switched from cs to dll or vice versa, then the plugin is still valid but needs updating too
                    else if (newExtension == ".cs" || newExtension == ".dll")
                    {
                        // Validate dlls
                        bool valid = false;
                        if (newExtension == ".cs")
                            valid = true;
                        else if (newExtension == ".dll")
                        {
                            try
                            {
                                AssemblyName asmName = AssemblyName.GetAssemblyName(newPath);
                                valid = true;
                            }
                            catch (Exception e) { }
                        }

                        if (valid)
                        {
                            pluginFile.UpdateFilePath(newPath);
                            OnUserPluginFileRenamed(new UserPluginFileRenamedEventArgs(oldPath, newPath, pluginFile));
                            pluginFile.UpdateFromFileChange();
                            OnUserPluginFileChanged(new UserPluginFileEventArgs(pluginFile.PathToFile, pluginFile));
                            return true;
                        }
                    }
                    
                    // Otherwise, the extension is no longer .cs or .dll and so the plugin is no longer valid
                    return TryRemoveUserFile(oldPath);
                }
            }

            // If not an existing plugin file, then check if this is a file that was just renamed to be a .cs or .dll and is thus now a plugin file
            if (newExtension == ".cs")
                return TryAddUserFileCS(newPath);
            else if (newExtension == ".dll")
                return TryAddUserFileDLL(newPath);

            return false;
        }

        /// <summary>
        /// Similar to TryRemoveUserFilesInDirectory, this renames all PluginFiles in the provided directory which was just renamed itself (thus changing the full path of its contents).
        /// </summary>
        /// <param name="oldPath">The old path to directory.</param>
        /// <param name="newPath">The new path to directory.</param>
        /// <returns>True if any plugin files were renamed, false if otherwise.</returns>
        private static bool TryRenameUserFilesInDirectory(string oldPath, string newPath)
        {
            List<PluginFile> toRename = new List<PluginFile>();
            foreach (PluginFile pluginFile in FoundUserPluginFiles)
            {
                if (IsPathInPath(pluginFile.PathToFile, oldPath))
                {
                    toRename.Add(pluginFile);
                }
            }

            bool renamedAny = false;
            foreach (PluginFile pluginFile in toRename)
            {
                // Derive the new path simply by replacing part of the path string
                string simulatedNewPath = pluginFile.PathToFile;
                int spot = simulatedNewPath.IndexOf(oldPath);
                if (spot >= 0)
                    simulatedNewPath = simulatedNewPath.Substring(0, spot) + newPath + simulatedNewPath.Substring(spot + oldPath.Length);

                TryRenameUserFile(pluginFile.PathToFile, simulatedNewPath);
                renamedAny = true;
            }
            return renamedAny;
        }

        #endregion


        /// <summary>
        /// Tests all found PluginFiles against all security levels so as to determine the maximum security level that will allow each plugin to function.
        /// </summary>
        /// <remarks>
        /// This method should only be used by management tools (i.e. TT2 and TTApp).
        /// </remarks>
        /// <param name="terrariaPath">Path to Terraria.exe, which will be referenced by CodeDom during compilation.</param>
        /// <param name="terrariaDependencyAssemblies">List of Terraria.exe's embedded dependency assemblies, which will be temporarily written to disk and reference by CodeDom during compilation.</param>
        /// <param name="additionalCompileDependencies">Optional list of on-disk paths to additional assemblies that may be required for plugin compilation.</param>
        /// <returns>A SecurityLevelComplianceTestResult object containing the test results.</returns>
        public static MultipleTestsResults TestAllSecurityLevelComplianceForAllPlugins(string terrariaPath, List<byte[]> terrariaDependencyAssemblies,  List<string> additionalCompileDependencies = null)
        {
            PluginTestConfiguration config = new PluginTestConfiguration();
            config.PluginFilesToTest = new List<PluginFile>();
            config.PluginFilesToTest.AddRange(FoundUserPluginFiles);
            config.TerrariaEnvironment = TerrariaEnvironment.Offline;
            config.TerrariaPath = terrariaPath;
            config.TerrariaDependencyAssemblies = terrariaDependencyAssemblies;
            config.AdditionalCompileDependencies = additionalCompileDependencies;
            return CecilTests.TestPluginCompliance(config);
        }


        /// <summary>
        /// Returns the temporary folder path to use for reading and writing temporary, on-disk files relating to the specific PluginFile.
        /// </summary>
        /// <param name="pluginFile">The PluginFile whose PathToFile will be used.</param>
        /// <returns>The path to the directory to use. The directory will NOT be created if it does not exist.</returns>
        public static string GetTemporaryFilePathFor(PluginFile pluginFile)
        {
            return Path.Combine(PluginsTempFilesFolder, pluginFile.GetRelativePath());
        }

        /// <summary>
        /// Returns the relative path of provided file on the disk (relative to IO.PluginsUserFilesFolder).
        /// </summary>
        /// <param name="fullPath">The full path to be transformed into a relative path.</param>
        /// <returns>The relative path.</returns>
        public static string GetRelativeUserFilesPathFor(string fullPath)
        {
            string standardizedFullPath = Path.GetFullPath(fullPath).ToLowerInvariant();
            string standardizedRootDir = Path.GetFullPath(PluginsUserFilesFolder).ToLowerInvariant();
            int spot = standardizedFullPath.IndexOf(standardizedRootDir);
            if (spot >= 0)
                return (standardizedFullPath.Substring(0, spot) + standardizedFullPath.Substring(spot + standardizedRootDir.Length)).TrimStart('\\', '/');
            else
                return null; // Shouldn't happen
        }


        #region Helpers

        /// <summary>
        /// Standardizes paths into absolute paths with identical format before comparing them.
        /// </summary>
        /// <param name="pathA">The first path to compare.</param>
        /// <param name="pathB">The second path to compare.</param>
        /// <returns>True if the paths are considered equivalent, false if otherwise.</returns>
        private static bool PathsAreEqual(string pathA, string pathB)
        {
            return (Path.GetFullPath(pathA).ToLowerInvariant() == Path.GetFullPath(pathB).ToLowerInvariant()); // This isn't 100% correct on Linux, but I'm only targeting Windows
        }

        /// <summary>
        /// Checks if a deeper path is part of a shallower path, using standardized, absolute paths for comparison.
        /// </summary>
        /// <param name="deeperPath">The deeper path, i.e. C:\Users\MyUsername\SomeFile.txt</param>
        /// <param name="shallowerPath">The shallower path, i.e. C:\Users\</param>
        /// <returns>True if the the deeper path path is contained by the shallower path, false if otherwise</returns>
        private static bool IsPathInPath(string deeperPath, string shallowerPath)
        {
            string deeperPathStandardized = Path.GetFullPath(deeperPath).ToLowerInvariant();
            string shallowerPathStandardized = Path.GetFullPath(shallowerPath).ToLowerInvariant();
            return (deeperPath.IndexOf(shallowerPath) == 0);
        }

        #endregion
    }
}
