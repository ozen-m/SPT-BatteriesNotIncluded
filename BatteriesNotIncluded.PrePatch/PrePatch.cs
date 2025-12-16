using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace BatteriesNotIncluded.PrePatch;

public static class BatteriesNotIncludedPrePatch
{
    public static IEnumerable<string> TargetDLLs => ["Assembly-CSharp.dll"];

    public static void Patch(AssemblyDefinition assembly)
    {
        // TODO: Find a way to not break NVG/Thermal icons

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

        // Sights
        var sightsItemType = assembly.MainModule.GetType("SightsItemClass");
        var sightsTogglableField = new FieldDefinition(
            "Togglable",
            FieldAttributes.Public,
            togglableComponentType
        );
        var sightsAttribute = new CustomAttribute(componentAttributeCtor);
        sightsItemType.Fields.Add(sightsTogglableField);
        sightsTogglableField.CustomAttributes.Add(sightsAttribute);

        logSource.LogInfo($"PrePatch complete.");
    }
}
