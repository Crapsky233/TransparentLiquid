using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TransparentLiquid
{
    [Label("$Mods.TransparentLiquid.Config.Title")]
    public class Configuration : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        public override void OnLoaded() => TransparentLiquid.Config = this;

        [Header("$Mods.TransparentLiquid.Config.Header")]

        [DefaultValue(.45f)]
        [Label("$Mods.TransparentLiquid.Config.WaterOpacity")]
        public float WaterOpacity { get; set; }

        [DefaultValue(.6f)]
        [Label("$Mods.TransparentLiquid.Config.LavaOpacity")]
        public float LavaOpacity { get; set; }

        [DefaultValue(.4f)]
        [Label("$Mods.TransparentLiquid.Config.HoneyOpacity")]
        public float HoneyOpacity { get; set; }

        [DefaultValue(true)]
        [Label("$Mods.TransparentLiquid.Config.LightingFix.Label")]
        [Tooltip("$Mods.TransparentLiquid.Config.LightingFix.Tooltip")]
        public bool LightingFix { get; set; }
    }
}
