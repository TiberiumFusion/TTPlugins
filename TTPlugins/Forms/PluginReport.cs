using com.tiberiumfusion.ttplugins.HarmonyPlugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace com.tiberiumfusion.ttplugins.Forms
{
    /// <summary>
    /// Form which can be shown while ingame to provide information on the state of all processed plugins.
    /// </summary>
    internal partial class PluginReport : Form
    {
        internal PluginReport()
        {
            InitializeComponent();

            MinimizeBox = false;
            MaximizeBox = false;
        }

        internal void CreateReport(HPluginApplicatorResult applicatorResult)
        {
            richText.Clear();
            Line("===== Plugin Status Report =====");
            
            Line(">>> Plugin Launch Configuration:");
            Line("Debug Mode: " + HPluginApplicator.PluginDebugMode);
            Line("Security Level: " + HPluginApplicator.SecurityLevel);

            Line("\n>>> Loaded plugin assemblies: (" + HPluginApplicator.LoadedPluginAssemblies.Count + ")");
            if (HPluginApplicator.LoadedPluginAssemblies.Count == 0)
                Line("(none)");
            else
            {
                foreach (Assembly pluginAsm in HPluginApplicator.LoadedPluginAssemblies)
                {
                    Line("- " + pluginAsm.FullName);
                    foreach (Type t in pluginAsm.DefinedTypes)
                        Line("    - " + t.FullName);
                }
            }

            Line("\n>>> Applied plugins (plugins with at least 1 successful patch operation): (" + HPluginApplicator.AppliedHPlugins.Count + ")");
            if (HPluginApplicator.AppliedHPlugins.Count == 0)
                Line("(none)");
            else
            {
                foreach (HSupervisedPlugin supervisedPlugin in HPluginApplicator.AppliedHPlugins)
                {
                    Line("- " + supervisedPlugin.Plugin.GetType().FullName);
                    Line("    - Source: " + supervisedPlugin.SourceFileRelativePath);
                    Line("    - Identity:");
                    try
                    {
                        Line("        - PluginName: " + supervisedPlugin.Plugin.Identity.PluginName);
                        Line("        - PluginDescription: " + supervisedPlugin.Plugin.Identity.PluginDescription);
                        Line("        - PluginAuthor: " + supervisedPlugin.Plugin.Identity.PluginAuthor);
                        Line("        - PluginVersion: " + supervisedPlugin.Plugin.Identity.PluginVersion.ToString());
                    }
                    catch (Exception e)
                    {
                        Line("    - [!] Plugin has invalid Identity object. Details: " + e);
                    }
                }
            }
            
            Line("\n>>> HPluginApplicator Result:");
            Line("Result code: " + (int)applicatorResult.ResultCode + " (" + applicatorResult.ResultCode.ToString() + ")");
            Line("Error message: " + (applicatorResult.ErrorMessage ?? "(none)"));
            Line("Exception: " + (applicatorResult.ThrownException != null ? applicatorResult.ThrownException.ToString() : "(none)"));
            
            Line("\n>>> Plugins that failed to construct: (" + applicatorResult.HPluginsThatFailedConstruction.Count + ")");
            if (applicatorResult.HPluginsThatFailedConstruction.Count == 0)
                Line("(none)");
            else
            {
                foreach (string entry in applicatorResult.HPluginsThatFailedConstruction)
                    Line("- " + entry);
            }

            Line("\n>>> Plugins that threw exceptions in their Initialize(): (" + applicatorResult.HPluginsThatFailedInitialize.Count + ")");
            if (applicatorResult.HPluginsThatFailedInitialize.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsThatFailedInitialize.Keys)
                {
                    Line("- " + relpath);
                    Line("    Exception: " + applicatorResult.HPluginsThatFailedInitialize[relpath].ToString());
                }
            }

            Line("\n>>> Plugins whose configuration failed to load: (" + applicatorResult.HPluginsWithFailedConfigurationLoads.Count + ")");
            if (applicatorResult.HPluginsWithFailedConfigurationLoads.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsWithFailedConfigurationLoads.Keys)
                {
                    Line("- " + relpath);
                    Line("    Exception: " + applicatorResult.HPluginsWithFailedConfigurationLoads[relpath].ToString());
                }
            }

            Line("\n>>> Plugins that threw exceptions in their ConfigurationLoaded(): (" + applicatorResult.HPluginsThatFailedConfigurationLoaded.Count + ")");
            if (applicatorResult.HPluginsThatFailedConfigurationLoaded.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsThatFailedConfigurationLoaded.Keys)
                {
                    Line("- " + relpath);
                    Line("    Exception: " + applicatorResult.HPluginsThatFailedConfigurationLoaded[relpath].ToString());
                }
            }

            Line("\n>>> Plugins that threw exceptions in their PrePatch(): (" + applicatorResult.HPluginsThatFailedPrePatch.Count + ")");
            if (applicatorResult.HPluginsThatFailedPrePatch.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsThatFailedPrePatch.Keys)
                {
                    Line("- " + relpath);
                    Line("    Exception: " + applicatorResult.HPluginsThatFailedPrePatch[relpath].ToString());
                }
            }

            Line("\n>>> Plugins that tried to patch a method on a restricted type: (" + applicatorResult.HPluginsThatBrokeRules.Count + ")");
            if (applicatorResult.HPluginsThatBrokeRules.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsThatBrokeRules.Keys)
                {
                    Line("- " + relpath);
                    Line("    Details: " + applicatorResult.HPluginsThatBrokeRules[relpath].ToString());
                }
            }
            
            Line("\n>>> Plugins that tried to patch null MethodInfos: (" + applicatorResult.HPluginsThatTriedToPatchNullMethodInfos.Count + ")");
            if (applicatorResult.HPluginsThatTriedToPatchNullMethodInfos.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsThatTriedToPatchNullMethodInfos)
                    Line("- " + relpath);
            }

            Line("\n>>> Plugins with patch operations that failed: (" + applicatorResult.HPluginsThatDidntPatch.Count + ")");
            if (applicatorResult.HPluginsThatDidntPatch.Count == 0)
                Line("(none)");
            else
            {
                foreach (string relpath in applicatorResult.HPluginsThatDidntPatch.Keys)
                {
                    Line("- " + relpath);
                    Line("    Exception: " + applicatorResult.HPluginsThatDidntPatch[relpath].ToString());
                }
            }

            richText.SelectionStart = 0;
            richText.ScrollToCaret();
        }

        [DebuggerStepThrough]
        private void Line(string text)
        {
            richText.AppendText(text + "\n");
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
