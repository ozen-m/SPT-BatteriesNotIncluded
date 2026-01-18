using System.Collections.Generic;
using System.Reflection;
using BatteriesNotIncluded.Managers;
using BatteriesNotIncluded.Utils;
using Comfort.Common;
using EFT.InventoryLogic;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Generic.SubPackets;
using SPT.Reflection.Patching;

namespace BatteriesNotIncluded.FikaSync.Patches;

/// <summary>
/// Remove player items from the manager when a player extracts in a coop game
/// </summary>
public static class ExtractPatches
{
    private static readonly List<Item> _playerItemsScratch = [];

    private static void HandlePlayerExtract(FikaPlayer player)
    {
        var manager = Singleton<DeviceManager>.Instance;
        if (manager == null) return;

        player.Inventory.Equipment.GetAllItemsNonAlloc(_playerItemsScratch, false, false);
        foreach (var item in _playerItemsScratch)
        {
            manager.Remove(item);
        }

        _playerItemsScratch.Clear();
    }

    public class HostExtractPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(HostGameController).GetMethod(nameof(HostGameController.Extract));
        }

        [PatchPrefix]
        public static void Prefix(FikaPlayer player)
        {
            HandlePlayerExtract(player);
        }
    }

    public class ClientExtractPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ClientGameController).GetMethod(nameof(ClientGameController.Extract));
        }

        [PatchPrefix]
        public static void Prefix(FikaPlayer player)
        {
            HandlePlayerExtract(player);
        }
    }

    public class PacketExtractPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ClientExtract).GetMethod(nameof(ClientExtract.Execute));
        }

        [PatchPrefix]
        public static void Prefix(ClientExtract __instance)
        {
            var coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
            if (coopHandler == null)
            {
                LoggerUtil.Info($"CoopHandler is null, cannot remove items for extracted player with netId: {__instance.NetId}");
                return;
            }
            if (!coopHandler.Players.TryGetValue(__instance.NetId, out var extractedPlayer))
            {
                LoggerUtil.Info($"Player not found in CoopHandler, cannot remove items for extracted player with netId: {__instance.NetId}");
                return;
            }

            HandlePlayerExtract(extractedPlayer);
        }
    }
}
