// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor;
using DiscoveredTargetInfo = UnityEditor.BuildTargetDiscovery.DiscoveredTargetInfo;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class SymbolNameRestrictions
    {
        private const int k_MaxLength = 247;

        public static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.Length > k_MaxLength ||
                name.Contains(" "))
            {
                return false;
            }

            var firstChar = name[0];
            if (!Char.IsLetter(firstChar) && firstChar != '_')
            {
                return false;
            }

            return true;
        }
    }

#pragma warning disable 649
    [Serializable]
    class VersionDefine
    {
        public string name;
        public string expression;
        public string define;
    }

    [System.Serializable]
    class CustomScriptAssemblyData
    {
        public string name;
        public string[] references;
        public string[] optionalUnityReferences;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public VersionDefine[] versionDefines;

        public static CustomScriptAssemblyData FromJson(string json)
        {
            CustomScriptAssemblyData assemblyData = new CustomScriptAssemblyData();
            assemblyData.autoReferenced = true;
            UnityEngine.JsonUtility.FromJsonOverwrite(json, assemblyData);

            if (assemblyData == null)
                throw new System.Exception("Json file does not contain an assembly definition");

            if (string.IsNullOrEmpty(assemblyData.name))
                throw new System.Exception("Required property 'name' not set");

            if ((assemblyData.excludePlatforms != null && assemblyData.excludePlatforms.Length > 0) &&
                (assemblyData.includePlatforms != null && assemblyData.includePlatforms.Length > 0))
                throw new System.Exception("Both 'excludePlatforms' and 'includePlatforms' are set.");

            return assemblyData;
        }

        public static string ToJson(CustomScriptAssemblyData data)
        {
            return UnityEngine.JsonUtility.ToJson(data, true);
        }
    }

    struct CustomScriptOptinalUnityAssembly
    {
        public string DisplayName { get; private set; }
        public string Tooltip { get; private set; }
        public OptionalUnityReferences OptionalUnityReferences { get; private set; }
        public string AdditinalInformationWhenEnabled { get; private set; }

        public CustomScriptOptinalUnityAssembly(string displayName, OptionalUnityReferences optionalUnityReferences, string tooltip, string additinalInformationWhenEnabled = "")
            : this()
        {
            DisplayName = displayName;
            Tooltip = tooltip;
            OptionalUnityReferences = optionalUnityReferences;
            AdditinalInformationWhenEnabled = additinalInformationWhenEnabled;
        }
    }

    struct CustomScriptAssemblyPlatform
    {
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public BuildTarget BuildTarget { get; private set; }

        public CustomScriptAssemblyPlatform(string name, string displayName, BuildTarget buildTarget) : this()
        {
            Name = name;
            DisplayName = displayName;
            BuildTarget = buildTarget;
        }

        public CustomScriptAssemblyPlatform(string name, BuildTarget buildTarget) : this(name, name, buildTarget)
        {
        }
    }

    class CustomScriptAssembly
    {
        public string FilePath { get; set; }
        public string PathPrefix { get; set; }
        public string Name { get; set; }
        public string GUID { get; set; }
        public string[] References { get; set; }
        public string[] AdditionalPrefixes { get; set; }
        public OptionalUnityReferences OptionalUnityReferences { get; set; }
        public CustomScriptAssemblyPlatform[] IncludePlatforms { get; set;  }
        public CustomScriptAssemblyPlatform[] ExcludePlatforms { get; set;  }

        public AssetPathMetaData AssetPathMetaData { get; set; }
        public ScriptCompilerOptions CompilerOptions { get; set; }

        public bool OverrideReferences { get; set; }
        public string[] PrecompiledReferences { get; set; }
        public bool AutoReferenced { get; set; }
        public string[] DefineConstraints { get; set; }
        public VersionDefine[] VersionDefines { get; set; }

        public AssemblyFlags AssemblyFlags
        {
            get
            {
                var assemblyFlags = AssemblyFlags.None;

                if (IncludePlatforms != null && IncludePlatforms.Length == 1 && IncludePlatforms[0].BuildTarget == BuildTarget.NoTarget)
                    assemblyFlags |= AssemblyFlags.EditorOnly;

                if (OverrideReferences)
                {
                    assemblyFlags |= AssemblyFlags.ExplicitReferences;
                }

                if (!AutoReferenced)
                {
                    assemblyFlags |= AssemblyFlags.ExplicitlyReferenced;
                }

                return assemblyFlags;
            }
        }

        public static CustomScriptAssemblyPlatform[] Platforms { get; private set; }
        public static CustomScriptAssemblyPlatform[] DeprecatedPlatforms { get; private set; }

        public static CustomScriptOptinalUnityAssembly[] OptinalUnityAssemblies { get; private set; }

        static CustomScriptAssembly()
        {
            // When removing a platform from Platforms, please add it to DeprecatedPlatforms.
            DiscoveredTargetInfo[] buildTargetList = BuildTargetDiscovery.GetBuildTargetInfoList();
            // Need extra slot for Editor which is not included in the build target list
            Platforms = new CustomScriptAssemblyPlatform[buildTargetList.Length + 1];
            Platforms[0] = new CustomScriptAssemblyPlatform("Editor", BuildTarget.NoTarget);
            for (int i = 1; i < Platforms.Length; i++)
            {
                Platforms[i] = new CustomScriptAssemblyPlatform(
                    BuildTargetDiscovery.GetScriptAssemblyName(buildTargetList[i - 1]),
                    buildTargetList[i - 1].niceName,
                    buildTargetList[i - 1].buildTargetPlatformVal);
            }

#pragma warning disable 0618
            DeprecatedPlatforms = new CustomScriptAssemblyPlatform[]
            {
                new CustomScriptAssemblyPlatform("PSMobile", BuildTarget.PSM),
                new CustomScriptAssemblyPlatform("Tizen", BuildTarget.Tizen),
                new CustomScriptAssemblyPlatform("WiiU", BuildTarget.WiiU),
                new CustomScriptAssemblyPlatform("Nintendo3DS", BuildTarget.N3DS),
                new CustomScriptAssemblyPlatform("PSVita", BuildTarget.PSP2),
            };
#pragma warning restore 0618

            OptinalUnityAssemblies = new[]
            {
                new CustomScriptOptinalUnityAssembly("Test Assemblies", OptionalUnityReferences.TestAssemblies, "When enabled, this assembly is only used in tests, not in in player builds. Predefined assemblies won’t reference this assembly.", "Predefined Assemblies (Assembly-CSharp.dll etc) will not reference this assembly.\nThis assembly will only be used for tests and will not be included in player builds."),
            };
        }

        public bool IsCompatibleWithEditor()
        {
            if (ExcludePlatforms != null)
                return ExcludePlatforms.All(p => p.BuildTarget != BuildTarget.NoTarget);

            if (IncludePlatforms != null)
                return IncludePlatforms.Any(p => p.BuildTarget == BuildTarget.NoTarget);

            return true;
        }

        public bool IsCompatibleWith(BuildTarget buildTarget, EditorScriptCompilationOptions options, string[] defines)
        {
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;

            var isTestAssembly = (OptionalUnityReferences & OptionalUnityReferences.TestAssemblies) == OptionalUnityReferences.TestAssemblies;
            var isBuildingWithTestAssemblies = (options & EditorScriptCompilationOptions.BuildingIncludingTestAssemblies) == EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
            if (!buildingForEditor && isTestAssembly && !isBuildingWithTestAssemblies)
            {
                return false;
            }

            if (defines != null && defines.Length == 0)
                throw new ArgumentException("defines cannot be empty", "defines");

            if (!DefineConstraintsHelper.IsDefineConstraintsCompatible(defines, DefineConstraints))
            {
                return false;
            }

            if (isTestAssembly && AssetPathMetaData != null && !AssetPathMetaData.IsTestable)
            {
                return false;
            }

            // Compatible with editor and all platforms.
            if (IncludePlatforms == null && ExcludePlatforms == null)
                return true;

            if (buildingForEditor)
                return IsCompatibleWithEditor();

            if (ExcludePlatforms != null)
                return ExcludePlatforms.All(p => p.BuildTarget != buildTarget);

            return IncludePlatforms.Any(p => p.BuildTarget == buildTarget);
        }

        public static CustomScriptAssembly Create(string name, string directory)
        {
            var customScriptAssembly = new CustomScriptAssembly();

            var modifiedDirectory = AssetPath.ReplaceSeparators(directory);

            if (modifiedDirectory.Last() != AssetPath.Separator)
                modifiedDirectory += AssetPath.Separator;

            customScriptAssembly.Name = name;
            customScriptAssembly.FilePath = modifiedDirectory;
            customScriptAssembly.PathPrefix = modifiedDirectory;
            customScriptAssembly.References = new string[0];
            customScriptAssembly.PrecompiledReferences = new string[0];
            customScriptAssembly.CompilerOptions = new ScriptCompilerOptions();
            customScriptAssembly.AutoReferenced = true;

            return customScriptAssembly;
        }

        public static CustomScriptAssembly FromCustomScriptAssemblyData(string path, string guid, CustomScriptAssemblyData customScriptAssemblyData)
        {
            if (customScriptAssemblyData == null)
                return null;

            var pathPrefix = path.Substring(0, path.Length - AssetPath.GetFileName(path).Length);

            var customScriptAssembly = new CustomScriptAssembly();

            customScriptAssembly.Name = customScriptAssemblyData.name;
            customScriptAssembly.GUID = guid;
            customScriptAssembly.References = customScriptAssemblyData.references;
            customScriptAssembly.FilePath = path;
            customScriptAssembly.PathPrefix = pathPrefix;
            customScriptAssembly.AutoReferenced = customScriptAssemblyData.autoReferenced;
            customScriptAssembly.OverrideReferences = customScriptAssemblyData.overrideReferences;
            customScriptAssembly.PrecompiledReferences = customScriptAssemblyData.precompiledReferences ?? new string[0];
            customScriptAssembly.DefineConstraints = customScriptAssemblyData.defineConstraints ?? new string[0];
            customScriptAssembly.VersionDefines = (customScriptAssemblyData.versionDefines ?? new VersionDefine[0]);

            customScriptAssemblyData.optionalUnityReferences = customScriptAssemblyData.optionalUnityReferences ?? new string[0];
            foreach (var optionalUnityReferenceString in customScriptAssemblyData.optionalUnityReferences)
            {
                var optionalUnityReference = (OptionalUnityReferences)Enum.Parse(typeof(OptionalUnityReferences), optionalUnityReferenceString);
                customScriptAssembly.OptionalUnityReferences |= optionalUnityReference;
            }

            if (customScriptAssemblyData.includePlatforms != null && customScriptAssemblyData.includePlatforms.Length > 0)
                customScriptAssembly.IncludePlatforms = GetPlatformsFromNames(customScriptAssemblyData.includePlatforms);

            if (customScriptAssemblyData.excludePlatforms != null && customScriptAssemblyData.excludePlatforms.Length > 0)
                customScriptAssembly.ExcludePlatforms = GetPlatformsFromNames(customScriptAssemblyData.excludePlatforms);

            var compilerOptions = new ScriptCompilerOptions();

            compilerOptions.AllowUnsafeCode = customScriptAssemblyData.allowUnsafeCode;

            customScriptAssembly.CompilerOptions = compilerOptions;

            return customScriptAssembly;
        }

        public static CustomScriptAssemblyPlatform[] GetPlatformsFromNames(string[] names)
        {
            var platforms = new List<CustomScriptAssemblyPlatform>();

            foreach (var name in names)
            {
                // Ignore deprecated platforms.
                if (IsDeprecatedPlatformName(name))
                    continue;

                platforms.Add(GetPlatformFromName(name));
            }

            return platforms.ToArray();
        }

        public static bool IsDeprecatedPlatformName(string name)
        {
            foreach (var platform in DeprecatedPlatforms)
                if (string.Equals(platform.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public static CustomScriptAssemblyPlatform GetPlatformFromName(string name)
        {
            foreach (var platform in Platforms)
                if (string.Equals(platform.Name, name, System.StringComparison.OrdinalIgnoreCase))
                    return platform;

            var platformNames = Platforms.Select(p => string.Format("\"{0}\"", p.Name)).ToArray();
            System.Array.Sort(platformNames);

            var platformsString = string.Join(",\n", platformNames);

            throw new System.ArgumentException(string.Format("Platform name '{0}' not supported.\nSupported platform names:\n{1}\n", name, platformsString));
        }

        public static CustomScriptAssemblyPlatform GetPlatformFromBuildTarget(BuildTarget buildTarget)
        {
            foreach (var platform in Platforms)
                if (platform.BuildTarget == buildTarget)
                    return platform;

            throw new System.ArgumentException(string.Format("No CustomScriptAssemblyPlatform setup for BuildTarget '{0}'", buildTarget));
        }
    }
}
