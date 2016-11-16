﻿// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class offers services to perform deployments for ASP.NET 4.x applications to a GCE VM.
    /// </summary>
    public static class AspnetDeployment
    {
        private static readonly Lazy<string> s_msbuildPath = new Lazy<string>(GetMsbuildPath);
        private static readonly Lazy<string> s_msdeployPath = new Lazy<string>(GetMsdeployPath);

        /// <summary>
        /// Publishes an ASP.NET 4.x project to the given GCE <seealso cref="Instance"/>.
        /// </summary>
        /// <param name="projectPath">The full path to the project file.</param>
        /// <param name="targetInstance">The instance to deploy.</param>
        /// <param name="credentials">The Windows credentials to use to deploy to the <paramref name="targetInstance"/>.</param>
        /// <param name="progress">The progress indicator.</param>
        /// <param name="outputAction">The action to call with lines of output.</param>
        /// <returns></returns>
        public static async Task<bool> PublishProjectAsync(
            string projectPath,
            Instance targetInstance,
            WindowsInstanceCredentials credentials,
            IProgress<double> progress,
            Action<string> outputAction)
        {
            var stagingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stagingDirectory);
            progress.Report(0.1);

            var publishSettingsPath = Path.GetTempFileName();
            var publishSettingsContent = targetInstance.GeneratePublishSettings(credentials.User, credentials.Password);
            File.WriteAllText(publishSettingsPath, publishSettingsContent);

            using (var cleanup = new Disposable(() => Cleanup(publishSettingsPath, stagingDirectory)))
            {
                // Wait for the bundle operation to finish and update the progress in the mean time to show progress.
                if (!await ProgressHelper.UpdateProgress(
                        CreateAppBundleAsync(projectPath, stagingDirectory, outputAction),
                        progress,
                        from: 0.1, to: 0.5))
                {
                    return false;
                }
                progress.Report(0.6);

                // Update for the deploy operation to finish and update the progress as it goes.
                if (!await ProgressHelper.UpdateProgress(
                        DeployAppAsync(stagingDirectory, publishSettingsPath, outputAction),
                        progress,
                        from: 0.6, to: 0.9))
                {
                    return false;
                }
                progress.Report(1);
            }

            return true;
        }

        private static void Cleanup(string publishSettings, string stagingDirectory)
        {
            try
            {
                if (File.Exists(publishSettings))
                {
                    File.Delete(publishSettings);
                }

                if (Directory.Exists(stagingDirectory))
                {
                    Directory.Delete(stagingDirectory, recursive: true);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// This method publishes the app to the VM using the <paramref name="publishSettingsPath"/> to find the publish
        /// settings to use to do so.
        /// </summary>
        private static Task<bool> DeployAppAsync(string stageDirectory, string publishSettingsPath, Action<string> outputAction)
        {
            var arguments = "-verb:sync " +
                $@"-source:contentPath=""{stageDirectory}"" " +
                $@"-dest:contentPath=""Default Web Site"",publishSettings=""{publishSettingsPath}"" " +
                "-allowUntrusted";

            outputAction($"msdeploy.exe {arguments}");
            return ProcessUtils.RunCommandAsync(s_msdeployPath.Value, arguments, (o, e) => outputAction(e.Line));
        }

        /// <summary>
        /// This method stages the application into the <paramref name="stageDirectory"/> by invoking the WebPublish target
        /// present in all Web projects. It publishes to the staging directory by using the FileSystem method.
        /// </summary>
        private static Task<bool> CreateAppBundleAsync(string projectPath, string stageDirectory, Action<string> outputAction)
        {
            var arguments = $@"""{projectPath}""" + " " +
                "/p:Configuration=Release " +
                "/p:Platform=AnyCPU " +
                "/t:WebPublish " +
                "/p:WebPublishMethod=FileSystem " +
                "/p:DeleteExistingFiles=True " +
                $@"/p:publishUrl=""{stageDirectory}""";

            outputAction($"msbuild.exe {arguments}");
            return ProcessUtils.RunCommandAsync(s_msbuildPath.Value, arguments, (o, e) => outputAction(e.Line));
        }

        private static string GetMsbuildPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            return Path.Combine(programFilesPath, @"MSBuild\14.0\Bin\MSBuild.exe");
        }

        private static string GetMsdeployPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(programFilesPath, @"IIS\Microsoft Web Deploy V3\msdeploy.exe");
        }
    }
}
