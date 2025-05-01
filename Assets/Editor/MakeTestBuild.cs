using System;
using System.Linq;
using UnityEditor;

namespace Editor
{
    public static class MakeTestBuild
    {
        private const string YARG_TEST_BUILD = "YARG_TEST_BUILD";
        private const string YARG_NIGHTLY_BUILD = "YARG_NIGHTLY_BUILD";
        private const string YARG_PREVIEW_BUILD = "YARG_PREVIEW_BUILD";

        [MenuItem("File/Make Test Build", false, 220)]
        public static void MakeTestBuildClicked()
        {
            MakeBuild(YARG_TEST_BUILD);
        }

        [MenuItem("File/Make Nightly Build", false, 220)]
        public static void MakeNightlyBuildClicked()
        {
            MakeBuild(YARG_NIGHTLY_BUILD);
        }

        // This exists to make a nightly-like build that uses a separate user data folder
        [MenuItem("File/Make Preview Build", false, 220)]
        public static void MakePreviewBuildClicked()
        {
            MakeBuild(YARG_PREVIEW_BUILD);
        }

        public static void MakeBuild(string defineSymbol)
        {
            // Get build settings
            var buildSettings = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(default);

            // Get current defines
            // TODO: BuildTargetGroup is slated for deprecation, figure out how to do this with NamedBuildTarget instead
            var buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(buildGroup, out var originalDefines);
            originalDefines ??= Array.Empty<string>();

            // Set test build define
            var buildDefines = buildSettings.extraScriptingDefines ?? Array.Empty<string>();
            if (!originalDefines.Contains(defineSymbol) && !buildDefines.Contains(defineSymbol))
            {
                ArrayUtility.Add(ref buildDefines, defineSymbol);
            }

            buildSettings.extraScriptingDefines = buildDefines;

            // Build the player
            BuildPipeline.BuildPlayer(buildSettings);
        }
    }
}