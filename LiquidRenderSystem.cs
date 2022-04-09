using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace TransparentLiquid
{
    internal class LiquidRenderSystem : ModSystem
    {
        internal static Configuration Config;

        public override void Load() {
            On.Terraria.GameContent.Drawing.TileDrawing.DrawPartialLiquid += TileDrawing_DrawPartialLiquid;
            IL.Terraria.Main.DrawBlack += Main_DrawBlack;
            IL.Terraria.WaterfallManager.DrawWaterfall += WaterfallManager_DrawWaterfall;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += LiquidRenderer_InternalDraw;
            IL.Terraria.GameContent.Drawing.WallDrawing.DrawWalls += WallDrawing_DrawWalls;
        }

        private void TileDrawing_DrawPartialLiquid(On.Terraria.GameContent.Drawing.TileDrawing.orig_DrawPartialLiquid orig, Terraria.GameContent.Drawing.TileDrawing self, Tile tileCache, Vector2 position, Rectangle liquidSize, int liquidType, Color aColor) {
            // liquidAlpha doesn't affect lava and honey. Fixed here.
            if (liquidType == 1) {
                aColor *= Main.liquidAlpha[1];
            }
            if (liquidType == 11) {
                aColor *= Main.liquidAlpha[11];
            }
            orig.Invoke(self, tileCache, position, liquidSize, liquidType, aColor);
        }

        //Get the liquid amount.
        //ldloca.s tile
        //IL_02C4: call instance uint8& Terraria.Tile::get_liquid()
        //IL_02C9: ldind.u1
        //IL_02CA: stloc.s b
        //Modify the value to execute break to exit the loop.
        //IL_02CC: ldloc.s num8
        //IL_02CE: ldloc.3
        //IL_02CF: bgt.un IL_037B
        private void Main_DrawBlack(ILContext il) {
            var c = new ILCursor(il);
            int liquidAmount = 0;

            c.GotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Ldloca_S),
                i => i.MatchCall(typeof(Tile), "get_liquid"),
                i => i.Match(OpCodes.Ldind_U1)
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

            c.Emit(OpCodes.Ldloc, 10); // Load i
            c.Emit(OpCodes.Ldloc, 12); // Load j
            c.EmitDelegate<Func<float, int, int, float>>((returnValue, i, j) => {
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
                    Main.liquidAlpha[12] = Math.Min(Main.liquidAlpha[12], Config.WaterOpacity);
                    for (int i = 2; i <= 10; i++) {
                        Main.liquidAlpha[i] = Math.Min(Main.liquidAlpha[i], Config.WaterOpacity);
                    }
                    // For lava and honey, just set the value directly.
                    Main.liquidAlpha[1] = Config.LavaOpacity;
                    Main.liquidAlpha[11] = Config.HoneyOpacity;

                    // And here is main liquid opacity modifying
                    return type switch {
                        LiquidID.Water => Config.WaterOpacity * returnValue,
                        LiquidID.Lava => Config.LavaOpacity * returnValue,
                        LiquidID.Honey => Config.HoneyOpacity * returnValue,
                        _ => returnValue,
                    };
                });

            }
            catch {
                throw new Exception("Error happened with TransparentLiquid mod LiquidRenderer_InternalDraw IL editing.");
            }
        }

        // Set underworldLayer to 0 and all walls should always draw. Then no transparent blocks will shown.
        //IL_0182: call int32 Terraria.Main::get_UnderworldLayer()
        //IL_0187: stloc.s underworldLayer
        private void WallDrawing_DrawWalls(ILContext il) {
            var c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After, i => i.MatchCall(typeof(Main), "get_UnderworldLayer"))) {
                c.EmitDelegate<Func<int, int>>((returnValue) => {
                    return Config.LightingFix ? 0 : returnValue;
                });
            }
        }
    }
}
