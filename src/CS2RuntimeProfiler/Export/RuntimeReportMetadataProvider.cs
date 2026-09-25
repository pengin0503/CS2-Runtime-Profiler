using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Modding;
using Game.SceneFlow;
using UnityEngine;

namespace CS2RuntimeProfiler.Export
{
    internal static class RuntimeReportMetadataProvider
    {
        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static RuntimeReportMetadata Capture()
        {
            return new RuntimeReportMetadata
            {
                HardwareSummary = BuildHardwareSummary(),
                EnabledMods = GetEnabledMods()
            };
        }

        private static string BuildHardwareSummary()
        {
            try
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(SystemInfo.processorType))
                    parts.Add($"CPU={SystemInfo.processorType} ({SystemInfo.processorCount} logical)");
                if (SystemInfo.systemMemorySize > 0)
                    parts.Add($"RAM={SystemInfo.systemMemorySize} MB");
                if (!string.IsNullOrWhiteSpace(SystemInfo.graphicsDeviceName))
                    parts.Add($"GPU={SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB, {SystemInfo.graphicsDeviceType})");
                if (!string.IsNullOrWhiteSpace(SystemInfo.operatingSystem))
                    parts.Add($"OS={SystemInfo.operatingSystem}");
                return string.Join("; ", parts);
            }
            catch
            {
                return null;
            }
        }

        private static IReadOnlyList<string> GetEnabledMods()
        {
            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var gameManager = GameManager.instance;
                if (gameManager == null)
                    return names.ToArray();

                var modManager = ReadMember(gameManager, "modManager") ?? ReadMember(gameManager, "m_ModManager");
                if (modManager == null)
                    return names.ToArray();

                var method = modManager.GetType().GetMethod(
                    "GetActiveMods",
                    InstanceFlags,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                var activeMods = method?.Invoke(modManager, null);
                foreach (var item in Enumerate(activeMods))
                    CollectModNames(item, names, depth: 0);
            }
            catch
            {
                // Metadata is best-effort and must never make report export fail.
            }

            return names.ToArray();
        }

        private static IEnumerable<object> Enumerate(object value)
        {
            if (value == null || value is string)
                yield break;

            if (value is IEnumerable sequence)
            {
                foreach (var item in sequence)
                {
                    if (item != null)
                        yield return item;
                }
                yield break;
            }

            yield return value;
        }

        private static void CollectModNames(object value, ISet<string> names, int depth)
        {
            if (value == null || depth > 2)
                return;

            if (value is IMod mod)
            {
                var assemblyName = mod.GetType().Assembly.GetName().Name;
                if (IsSafeLabel(assemblyName))
                    names.Add(assemblyName);
                return;
            }

            if (value is string text)
            {
                if (IsSafeLabel(text))
                    names.Add(text.Trim());
                return;
            }

            foreach (var memberName in new[] { "displayName", "DisplayName", "modName", "ModName", "name", "Name", "modId", "ModId" })
            {
                var memberValue = ReadMember(value, memberName) as string;
                if (IsSafeLabel(memberValue))
                {
                    names.Add(memberValue.Trim());
                    return;
                }
            }

            foreach (var nestedName in new[] { "mod", "Mod", "instance", "Instance", "value", "Value", "key", "Key" })
            {
                var nested = ReadMember(value, nestedName);
                if (nested != null && !ReferenceEquals(nested, value))
                    CollectModNames(nested, names, depth + 1);
            }
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null || string.IsNullOrWhiteSpace(name))
                return null;

            var type = target.GetType();
            try
            {
                var property = type.GetProperty(name, InstanceFlags);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target, null);
            }
            catch
            {
            }

            try
            {
                var field = type.GetField(name, InstanceFlags);
                return field?.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsSafeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            if (trimmed.Length > 160)
                return false;

            // Avoid exporting values that look like local file paths. The serializer also sanitizes output,
            // but rejecting path-shaped labels here keeps enabledMods semantically clean.
            return trimmed.IndexOf('\\') < 0
                && trimmed.IndexOf('/') < 0
                && trimmed.IndexOf(":\\", StringComparison.Ordinal) < 0;
        }
    }
}
