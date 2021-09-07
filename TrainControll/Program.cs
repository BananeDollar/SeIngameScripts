using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        // --- Einstellungen ---
        static Color trainColor = new Color(50, 50, 50);
        static Color trackColor = new Color(255, 255, 255);

        // --- Einstellungen Ende ---

        TrainSystem[] trainSystems;

        public Program()
        {
            trainSystems = new TrainSystem[]
            {
            new TrainSystem("Computer Train A","StationA",20),
            
            };

            for (int i = 0; i < trainSystems.Length; i++)
            {
                trainSystems[i].screens = FindStationScreens(trainSystems[i].stationScreenTag);
                trainSystems[i].trainComputer = GridTerminalSystem.GetBlockWithName(trainSystems[i].computerTrainName) as IMyProgrammableBlock;
                trainSystems[i].trainComputer.TryRun("init-"+Me.CustomName+"-"+i);
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                for (int i = 0; i < trainSystems.Length; i++)
                {
                    trainSystems[i].Tick();
                    Echo("use:"+i+"-COMMAND\n"+ trainSystems[i].ToString());

                    trainSystems[i].UpdateLCDS();
                }
            }
            else
            {
                string[] split = argument.Split('-');
                int stationID = int.Parse(split[0]);
                string arg = split[1];

                switch (arg)
                {
                    case "reachedStationA":
                        trainSystems[stationID].ReachedA();
                        break;
                    case "reachedStationB":
                        trainSystems[stationID].ReachedB();
                        break;

                    case "start":
                        trainSystems[stationID].running = true;
                        break;
                    case "stop":
                        trainSystems[stationID].running = false;
                        break;
                }
            }
        }

        public List<IMyTextSurface> FindStationScreens(string tag)
        {
            List<IMyTerminalBlock> allScreens = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(allScreens);

            List<IMyTextSurface> screens = new List<IMyTextSurface>();

            for (int s = 0; s < allScreens.Count; s++)
            {
                string screenName = allScreens[s].CustomName.Split('[', ']')[1];
                if (screenName == tag)
                {
                    IMyTextSurface screen = (IMyTextSurface)allScreens[s];
                    screens.Add(screen);

                    screen.ContentType = ContentType.SCRIPT;
                    screen.Script = "";
                    screen.ScriptBackgroundColor = new Color(0, 0, 0, 0);
                    screen.ScriptForegroundColor = new Color(255, 255, 255, 255);
                }
            }

            return screens;
        }

        public class TrainSystem
        {
            public string computerTrainName, stationScreenTag;
            public List<IMyTextSurface> screens;
            int stationWaitTicks;
            int travelTicks = 0;
            public IMyProgrammableBlock trainComputer;
            public bool running = false;

            bool screenTick = false;

            public int stationCooldown = 0;
            public int travelTimer = 0;

            // Display
            int trainX = 0;


            public TrainStatus status;
            public enum TrainStatus
            {
                parkedAtA,
                movingToB,
                parkedAtB,
                movingToA
            }

            public TrainSystem(string computerTrainName, string stationScreenTag, int stationWaitTicks)
            {
                this.computerTrainName = computerTrainName;
                this.stationScreenTag = stationScreenTag;
                this.stationWaitTicks = stationWaitTicks;
            }

            public void Tick()
            {
                if (status == TrainStatus.parkedAtA || status == TrainStatus.parkedAtB)
                {
                    if (stationCooldown > 0)
                    {
                        stationCooldown--;
                    }
                    else
                    {
                        if (running || status == TrainStatus.parkedAtB)
                        {
                            trainComputer.TryRun("Go");

                            if (status == TrainStatus.parkedAtA)
                                status = TrainStatus.movingToB;

                            if (status == TrainStatus.parkedAtB)
                                status = TrainStatus.movingToA;
                        }
                    }
                }

                if (status == TrainStatus.movingToA || status == TrainStatus.movingToB)
                {
                    travelTimer++;
                }
            }

            public void ReachedA()
            {
                status = TrainStatus.parkedAtA;
                stationCooldown = stationWaitTicks;
                if (travelTicks == 0)
                {
                    travelTicks = travelTimer;
                }
                travelTimer = 0;
            }

            public void ReachedB()
            {
                status = TrainStatus.parkedAtB;
                stationCooldown = stationWaitTicks;
                if (travelTicks == 0)
                {
                    travelTicks = travelTimer;
                }

                travelTimer = 0;
            }

            public void UpdateLCDS()
            {
                foreach (IMyTextSurface screen in screens)
                {
                    MySpriteDrawFrame frame = screen.DrawFrame();

                    DrawFrame(ref frame, screen);

                    frame.Dispose();
                }
            }

            public void DrawFrame(ref MySpriteDrawFrame frame, IMyTextSurface screen)
            {
                int sizex = (int)screen.SurfaceSize.X;
                int sizey = (int)screen.SurfaceSize.Y;

                int trackLeft = 25;
                int trackRight = sizex - 50;
                int trainOffset = 10;

                MySprite Sprite = new MySprite();

                if (screenTick)
                {
                    frame.Add(Sprite);
                }
                screenTick = !screenTick;

                DrawTrack(ref frame, trackLeft, 100, trackRight);

                switch (status)
                {
                    case TrainStatus.parkedAtA:
                        trainX = trackLeft + trainOffset * 2;
                        break;
                    case TrainStatus.movingToB:
                        trainX = (int)Remap(travelTimer, 0, travelTicks, trackLeft + trainOffset * 2, trackRight - trainOffset);
                        break;
                    case TrainStatus.parkedAtB:
                        trainX = trackRight - trainOffset;
                        break;
                    case TrainStatus.movingToA:
                        trainX = (int)Remap(travelTimer, travelTicks, 0, trackLeft + trainOffset * 2, trackRight - trainOffset);
                        break;
                }

                DrawTrain(ref frame, trainX, 80, 3);

                DrawInfo(ref frame, sizex / 2, 120, 10);
            }

            public float Remap(float value, float from1, float to1, float from2, float to2)
            {
                float result = (value - from1) / (to1 - from1) * (to2 - from2) + from2;
                return Math.Max(Math.Min(result,to2),from2);
            }
            public void DrawTrain(ref MySpriteDrawFrame frame, int x, int y, int scale)
            {
                MySprite Sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Triangle",
                    RotationOrScale = 1.5708F,
                    Color = trainColor,
                    Size = new Vector2(10 * scale, 5 * scale),
                    Position = new Vector2(x+10*scale, y),
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(Sprite);

                Sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Triangle",
                    RotationOrScale = -1.5708F,
                    Color = trainColor,
                    Size = new Vector2(10 * scale, 5 * scale),
                    Position = new Vector2(x - 10 * scale, y),
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(Sprite);

                Sprite = new MySprite()
                {
                    RotationOrScale = 0,
                    Data = "SquareSimple",
                    Size = new Vector2(16 * scale, 10 * scale),
                    Color = trainColor,
                    Position = new Vector2(x, y),
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(Sprite);

                Sprite = new MySprite()
                {
                    Color = new Color(0, 0, 0, 255),
                    RotationOrScale = 0,
                    Data = "SquareSimple",
                    Size = new Vector2(3* scale, 3* scale),
                    Position = new Vector2(x - 5* scale, y+2.5F*scale),
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(Sprite);

                Sprite.Position = new Vector2(x - 5* scale, y - 2.5F* scale);
                frame.Add(Sprite);

                Sprite.Position = new Vector2(x + 5* scale, y + 2.5F* scale);
                frame.Add(Sprite);

                Sprite.Position = new Vector2(x + 5* scale, y - 2.5F* scale);
                frame.Add(Sprite);
            }
            public void DrawTrack(ref MySpriteDrawFrame frame, int x, int y, int width)
            {
                MySprite Sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Color = trackColor,
                    Size = new Vector2(width, 10),
                    Position = new Vector2(x, y)
                };
                frame.Add(Sprite);
            }

            public void DrawInfo(ref MySpriteDrawFrame frame, int x, int y, int size)
            {
                string time = "";

                if (status == TrainStatus.parkedAtA || status == TrainStatus.parkedAtB)
                {
                    time = stationCooldown.ToString();
                }
                else
                {
                    time = travelTimer.ToString();
                }

                MySprite Sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = time,
                    Color = Color.White,
                    RotationOrScale = size,
                    Position = new Vector2(x, y),
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(Sprite);
            }
            public override string ToString()
            {
                return
                    "------\n" +
                    "Settings:\n" +
                    " stationWaitTicks:" + stationWaitTicks + "\n" +
                    " travelTicks:" + travelTicks + "\n" +
                    "Changing:\n" +
                    " stationCooldown:" + stationCooldown + "\n" +
                    " travelTimer:" + travelTimer + "\n" +
                    " status:" + status.ToString() + "\n"+
                    " running:" + running.ToString() + "\n";
            }
        }
    }
}
