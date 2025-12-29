using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil;
using Newtonsoft.Json;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace BatteriesNotIncluded.PrePatch;

public static class BatteriesNotIncludedPrePatch
{
    public static IEnumerable<string> TargetDLLs => ["Assembly-CSharp.dll"];

    public static void Patch(AssemblyDefinition assembly)
    {
        var patcherPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var pluginPath = Path.Combine(patcherPath!, "..", "plugins", "ozen-BatteriesNotIncluded.dll");
        var logSource = Logger.CreateLogSource("Batteries Not Included");
        if (!File.Exists(pluginPath))
        {
            logSource.LogError($"Could not find plugin in path: {pluginPath}. Mod is not installed properly.");
            return;
        }

        // Togglable Component
        var togglableComponentType = assembly.MainModule.GetType("EFT.InventoryLogic.TogglableComponent");

        // Component Attribute
        var componentAttributeType = assembly.MainModule.GetType("GAttribute26");
        var componentAttributeCtor = componentAttributeType.Methods.First(m => m.IsConstructor);

        // Headphones
        var headphonesItemType = assembly.MainModule.GetType("HeadphonesItemClass");
        var headphonesTogglableField = new FieldDefinition(
            "Togglable",
            FieldAttributes.Public,
            togglableComponentType
        );
        var headphonesAttribute = new CustomAttribute(componentAttributeCtor);
        headphonesTogglableField.CustomAttributes.Add(headphonesAttribute);
        headphonesItemType.Fields.Add(headphonesTogglableField);

        logSource.LogInfo($"PrePatch complete.");
    }
}
