using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //Settings
        int batH = 200;
        int batW = 200;

        //Settings End
        IMyTextSurface statusSurface;
        List<IMyBatteryBlock> batterys;

        bool tick = false;

        double maxCharge, maxOutput, maxInput;
        double curCharge, curoutput, curInput;
        Color chargeColor, inputColor, outputColor;

        public Program()
        {
            batterys = new List<IMyBatteryBlock>();

            List<IMyTextPanel> texts = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(texts);
            foreach (IMyTextPanel ts in texts)
            {
                if (ts.IsSameConstructAs(Me))
                {
                    statusSurface = ts;
                    Echo("Found Status Surface");
                }
            }

            List<IMyBatteryBlock> tmp = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(tmp);

            foreach (IMyBatteryBlock b in tmp)
            {
                if (b.IsSameConstructAs(Me))
                {
                    batterys.Add(b);
                }
            }
            Echo("found " + batterys.Count + " batterys");

            GetBatteryStaticInfo();
            if (statusSurface != null && batterys.Count > 0)
            {
                Echo("Startup complete!");
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            GetBatteryDynamicInfo();

            var frame = statusSurface.DrawFrame();

            DrawFrame(ref frame);

            frame.Dispose();
        }

        public void GetBatteryDynamicInfo()
        {
            curCharge = 0;
            curoutput = 0;
            curInput = 0;

            foreach (var bat in batterys)
            {
                curCharge += bat.CurrentStoredPower;
                curoutput += bat.CurrentOutput;
                curInput += bat.CurrentInput;
            }

            curCharge = Math.Round(curCharge, 2);
            curoutput = Math.Round(curoutput, 2);
            curInput = Math.Round(curInput, 2);

            chargeColor = Color.Lerp(Color.Red, Color.Green, (float)(curCharge / maxCharge));
            inputColor = Color.Lerp(Color.White, Color.Aqua, (float)(curInput / maxInput));
            outputColor = Color.Lerp(Color.White, Color.Aqua, (float)(curoutput / maxOutput));
        }

        public void GetBatteryStaticInfo()
        {
            maxCharge = 0;
            maxOutput = 0;
            maxInput = 0;

            foreach (var bat in batterys)
            {
                maxCharge += bat.MaxStoredPower;
                maxOutput += bat.MaxOutput;
                maxInput += bat.MaxInput;
            }

            maxCharge = Math.Round(maxCharge, 2);
            maxOutput = Math.Round(maxCharge, 2);
            maxInput = Math.Round(maxInput, 2);
        }

        public void DrawFrame(ref MySpriteDrawFrame frame)
        {
            int sizex = (int)statusSurface.SurfaceSize.X;
            int sizey = (int)statusSurface.SurfaceSize.Y;
            
            MySprite Sprite = new MySprite();

            if (tick)
            {
                frame.Add(Sprite);
            }
            tick = !tick;

            DrawCube(ref frame, sizex / 2 - batW / 2, sizey / 2 - batH / 2, batW, batH, new Color(10,10,10));

            DrawTriangle(ref frame, sizex / 2, sizey / 2 - batH / 2 - 80, 20, -45, inputColor);
            DrawTriangle(ref frame, sizex / 2, sizey / 2 - batH / 2 - 20, 20, -45, inputColor);
            DrawTriangle(ref frame, sizex / 2, sizey / 2 + batH / 2 + 10, 20, -45, outputColor);
            DrawTriangle(ref frame, sizex / 2, sizey / 2 + batH / 2 + 70, 20, -45, outputColor);

            if (curInput > curoutput)
            {
                DrawTriangle(ref frame, sizex / 2 + batW - 60, sizey / 2, 20, -45+180, Color.Green);
                DrawTriangle(ref frame, sizex / 2 - batW + 60, sizey / 2, 20, -45+180, Color.Green);
            }
            else if (curInput < curoutput)
            {
                DrawTriangle(ref frame, sizex / 2 + batW - 60, sizey / 2, 20, -45, Color.Red);
                DrawTriangle(ref frame, sizex / 2 - batW + 60, sizey / 2, 20, -45, Color.Red);
            }

            int bdown = sizey / 2 + batH/2;
            int chargeHeight = (int)((batH - 20) * (curCharge / maxCharge));

            DrawCube(ref frame, sizex / 2 - batW / 2 + 10, bdown-10 - chargeHeight, batW - 20, chargeHeight, chargeColor);

            // Text

            Sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = curInput + " MW / " + maxInput + " MW",
                Position = new Vector2(sizex/2, sizey/2-batH/2-60),
                RotationOrScale = 1,
                Color = Color.White,
                Alignment = TextAlignment.CENTER,
                FontId = "White",
            };
            frame.Add(Sprite);

            Sprite.Data = curCharge + " MWh";
            Sprite.Position = new Vector2(sizex / 2, Math.Min(bdown-chargeHeight,bdown-40));
            frame.Add(Sprite);

            Sprite.Data = curoutput + " MW / " + maxOutput + " MW";
            Sprite.Position = new Vector2(sizex / 2, sizey / 2 + batH/2 + 30);
            frame.Add(Sprite);

            Sprite.Data = batterys[0].DetailedInfo.Split('\n')[7];
            Sprite.Position = new Vector2(sizex / 2, 10);
            Sprite.RotationOrScale = 1.25F;
            frame.Add(Sprite);
        }

        public void DrawCube(ref MySpriteDrawFrame frame, int x, int y, int width, int height, Color c)
        {
            using (frame.Clip(x, y, width, height))
            {
                MySprite Sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Color = c,
                    Size = statusSurface.SurfaceSize,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(Sprite);
            }
        }
        public void DrawTriangle(ref MySpriteDrawFrame frame, int x, int y, int size, int angle, Color c)
        {
            MySprite Sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "RightTriangle",
                Color = c,
                RotationOrScale = (float)(angle * Math.PI / 180F),
                Size = new Vector2(size,size),
                Position = new Vector2(x, y),
                Alignment = TextAlignment.CENTER
            };
            frame.Add(Sprite);
        }
    }
}
