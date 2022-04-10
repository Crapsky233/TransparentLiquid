using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TransparentLiquid
{
    public partial class TransparentLiquid : Mod
    {
        internal static Configuration Config;

        public override void Load() {
            On.Terraria.Main.DrawWater += Main_DrawWater;
            IL.Terraria.Main.DrawBlack += Main_DrawBlack;
            IL.Terraria.Main.DrawWalls += WallDrawing_DrawWalls;
            IL.Terraria.WaterfallManager.DrawWaterfall += WaterfallManager_DrawWaterfall;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += LiquidRenderer_InternalDraw;
        }

        private void Main_DrawWater(On.Terraria.Main.orig_DrawWater orig, Main self, bool bg, int Style, float Alpha) {
            if (Style == 1 || Style == 11) {
                Alpha = Main.liquidAlpha[Style];
            }
            orig.Invoke(self, bg, Style, Alpha);
        }

        //Get the liquid amount.
        //IL_026B: div
        //IL_026C: stloc.s V_14
        //IL_026E: ldloc.s V_13
        //IL_0270: ldfld uint8 Terraria.Tile::liquid
        //Modify the value to execute break to exit the loop.
        //IL_0275: stloc.s V_15
        //IL_0277: ldloc.s V_14
        //IL_0279: ldloc.3
        //IL_027A: bgt.un IL_031C
        private void Main_DrawBlack(ILContext il) {
            var c = new ILCursor(il);
            int liquidAmount = 0;

            c.GotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Div),
                i => i.Match(OpCodes.Stloc_S),
                i => i.Match(OpCodes.Ldloc_S),
                i => i.MatchLdfld(typeof(Tile), nameof(Tile.liquid))
            );

            c.EmitDelegate<Func<int, int>>((returnValue) => {
                liquidAmount = returnValue;
                return returnValue;
            });

            c.GotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Stloc_S),
                i => i.Match(OpCodes.Ldloc_S),
                i => i.Match(OpCodes.Ldloc_3)
            );

            c.EmitDelegate<Func<float, float>>((returnValue) => {
                var player = Main.LocalPlayer;
                if (Config.LightingFix && liquidAmount >= 250) {
                    return -0.1f;
                }
                return returnValue;
            });
        }

        // Set lavafalls and honeyfalls opacity.
        //IL_0A18: ldloc.s num12
        //IL_0A1A: ldc.i4.s  14
        //IL_0A1C: beq.s IL_0A29
        //IL_0A1E: br.s IL_0A32
        //IL_0A20: ldc.r4    1 (This is lavafall opacity)
        //IL_0A25: stloc.s num37
        //IL_0A27: br.s IL_0A52
        //IL_0A29: ldc.r4    0.8 (This is honeyfall opacity)
        //IL_0A2E: stloc.s num37
        private void WaterfallManager_DrawWaterfall(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Ldloc_S),
                i => i.Match(OpCodes.Ldc_I4_S),
                i => i.Match(OpCodes.Beq_S),
                i => i.Match(OpCodes.Br_S),
                i => i.Match(OpCodes.Ldc_R4) // lavafall here
            );
            c.EmitDelegate<Func<float, float>>((opacity) => {
                return Config.LavaOpacity * opacity;
            });

            c.GotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Stloc_S),
                i => i.Match(OpCodes.Br_S),
                i => i.Match(OpCodes.Ldc_R4) // honeyfall here
            );
            c.EmitDelegate<Func<float, float>>((opacity) => {
                return Config.HoneyOpacity * opacity;
            });
        }

        // Get the liquid type.
        //IL_009C: mul
        //IL_009D: stloc.s num
        //IL_009F: ldloc.2
        //IL_00A0: ldfld uint8 Terraria.GameContent.Liquid.LiquidRenderer/LiquidDrawCache::Type
        //IL_00A5: stloc.s num2

        // Modify liquid opacity.
        //IL_00BE: ldc.i4.s  11
        //IL_00C0: stloc.s num2
        //IL_00C2: ldc.r4    1
        //IL_00C7: ldloc.s num
        //IL_00C9: call float32[System.Runtime]System.Math::Min(float32, float32)
        //IL_00CE: stloc.s num
        private void LiquidRenderer_InternalDraw(ILContext il) {
            try {
                var c = new ILCursor(il);
                int type = 0;

                c.GotoNext(
                    MoveType.After,
                    i => i.Match(OpCodes.Mul),
                    i => i.Match(OpCodes.Stloc_S),
                    i => i.Match(OpCodes.Ldloc_2),
                    i => i.Match(OpCodes.Ldfld)
                );

                c.EmitDelegate<Func<byte, byte>>((returnValue) => {
                    type = returnValue;
                    return returnValue;
                });

                c.GotoNext(
                    MoveType.After,
                    i => i.MatchLdcI4(11),
                    i => i.Match(OpCodes.Stloc_S),
                    i => i.MatchLdcR4(1)
                );

                c.EmitDelegate<Func<float, float>>((returnValue) => {
                    // liquidAlpha is used when current biome changes and water style turning into another one.
                    // We have to set a maximum value for it, instead of direct changing its value.
                    Main.liquidAlpha[0] = Math.Min(Main.liquidAlpha[0], Config.WaterOpacity);
                    for (int i = 2; i <= 10; i++) {
                        Main.liquidAlpha[i] = Math.Min(Main.liquidAlpha[i], Config.WaterOpacity);
                    }
                    // For lava and honey, just set the value directly.
                    Main.liquidAlpha[1] = Config.LavaOpacity;
                    Main.liquidAlpha[11] = Config.HoneyOpacity;

                    // And here is main liquid opacity modifying
                    switch (type) {
                        case Tile.Liquid_Water:
                            return Config.WaterOpacity * returnValue;
                        case Tile.Liquid_Lava:
                            return Config.LavaOpacity * returnValue;
                        case Tile.Liquid_Honey:
                            return Config.HoneyOpacity * returnValue;
                    }
                    return returnValue;
                });

            }
            catch {
                throw new Exception("Error happened with TransparentLiquid mod LiquidRenderer_InternalDraw IL editing.");
            }
        }

        // This is what 1.3 used to determine if the wall is in underworld layer
        // C# code be like: int num8 = Main.maxTilesY - 200
        // So just set 200 to Main.maxTilesY
        //IL_0132: ldsfld    int32 Terraria.Main::maxTilesY
        //IL_0137: ldc.i4    200
        private void WallDrawing_DrawWalls(ILContext il) {
            var c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld(typeof(Main), nameof(Main.maxTilesY)),
                i => i.MatchLdcI4(200))) {
                c.EmitDelegate<Func<int, int>>((returnValue) => {
                    return Config.LightingFix ? Main.maxTilesY : returnValue;
                });
            }
        }
    }
}