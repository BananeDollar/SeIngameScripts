using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        Column[] columns;

        List<IMyMotorStator> baseMotors = new List<IMyMotorStator>();
        List<IMyMotorStator> hinges = new List<IMyMotorStator>();


        int setupStep = 0;
        bool setupStarted = false;
        bool setupDone = false;

        static string lastAction = "";

        // Running Display Indicator
        int rdiIndex = 0;
        string[] rdi = new string[] { "-....", ".-...", "..-..", "...-.", "....-", "...-.", "..-..", ".-..." };

        public void Main(string argument, UpdateType updateSource)
        {
            string[] args = argument.Split(' ');
            if (updateSource == UpdateType.Update10)
            {
                if (!setupDone)
                {
                    if (!setupStarted)
                        SetupStart();
                    else
                        SetupLoop();
                }
                else
                {
                    Echo("--- Info ---");
                    Echo("Status: Stil Alive |" + rdi[rdiIndex] + "|");
                    Echo("Last Action:");
                    Echo("'" + lastAction + "'");

                    Echo("");
                    Echo("--- Commands ---");
                    Echo("debug [ID]");

                    if (rdiIndex < rdi.Length - 1)
                        rdiIndex++;
                    else
                        rdiIndex = 0;
                }
            }
            else
            {
                if (argument.StartsWith("debug"))
                {
                    if (args.Length == 2)
                    {
                        int id;
                        if (int.TryParse(args[1], out id))
                        {
                            ToggleDebugHud(id);
                        }
                    }
                }

                if (argument.StartsWith("idle"))
                {
                    PlatformPosition[] pos = new PlatformPosition[]
                    {
                        new PlatformPosition(0, 0, 0.2F, 0, 25),
                        new PlatformPosition(0, 0, 0, 0, 15),
                        new PlatformPosition(0, 0, 0, 0, 10),
                        new PlatformPosition(0, 0, 0, 0, 0),
                        new PlatformPosition(0, 0, 0 ,0, 0)
                    };
                    SetPlatformPositions(pos);
                }

                if (argument.StartsWith("look"))
                {
                    PlatformPosition[] pos = new PlatformPosition[]
                    {
                        new PlatformPosition(0, -1, 2, 0, -20),
                        new PlatformPosition(0, -2, 0, 0, -40),
                        new PlatformPosition(0, -2, 0, 0, -50),
                        new PlatformPosition(0, -2, -3, 0, -70),
                        new PlatformPosition(0, -2, -1 ,0, -90)
                    };
                    SetPlatformPositions(pos);
                }
            }
        }

        private void SetupStart()
        {
            GridTerminalSystem.GetBlocksOfType(baseMotors, motor => motor.BlockDefinition.SubtypeName == "LargeStator");

            columns = new Column[baseMotors.Count];

            baseMotors.Sort(
                delegate (IMyMotorStator a, IMyMotorStator b)
                {
                    int x = int.Parse(a.CustomName.Split(' ')[1]);
                    int y = int.Parse(b.CustomName.Split(' ')[1]);

                    if (x < y)
                        return 1;
                    if (x > y)
                        return -1;
                    else
                        return 0;
                }
                );

            setupStarted = true;
        }

        public void SetupLoop()
        {
            columns[setupStep] = new Column();


            for (int a = 0; a < 5; a++)
            {
                Platform p = new Platform();

                GridTerminalSystem.GetBlocksOfType(hinges, hinge => hinge.CubeGrid == baseMotors[setupStep].TopGrid);

                hinges.Sort(
                delegate (IMyMotorStator x, IMyMotorStator y)
                {
                    int dista = Vector3I.DistanceManhattan(x.Position, baseMotors[setupStep].Top.Position);
                    int distb = Vector3I.DistanceManhattan(y.Position, baseMotors[setupStep].Top.Position);
                    if (dista > distb)
                        return 1;
                    if (dista < distb)
                        return -1;
                    else
                        return 0;
                }
            );

                if (hinges.Count == 0)
                    Echo("no horizontal back hinge found");
                p.horizontalBack = hinges[a];

                GridTerminalSystem.GetBlocksOfType(hinges, hinge => hinge.CubeGrid == p.horizontalBack.TopGrid);
                if (hinges.Count == 0)
                    Echo("no vertical back hinge found");
                p.verticalBack = hinges[0];

                List<IMyPistonBase> pistons = new List<IMyPistonBase>();
                GridTerminalSystem.GetBlocksOfType(pistons, piston => piston.CubeGrid == p.verticalBack.TopGrid);
                if (pistons.Count == 0)
                    Echo("no piston found");
                p.piston = pistons[0];

                GridTerminalSystem.GetBlocksOfType(hinges, hinge => hinge.CubeGrid == p.piston.TopGrid);
                if (hinges.Count == 0)
                    Echo("no horizontal front hinge found");
                p.horizontalFront = hinges[0];

                GridTerminalSystem.GetBlocksOfType(hinges, hinge => hinge.CubeGrid == p.horizontalFront.TopGrid);
                if (hinges.Count == 0)
                    Echo("no vertical front hinge found");
                p.verticalFront = hinges[0];

                p.horizontalBack.CustomName = "HingeHB" + setupStep + "-" + a;
                p.verticalBack.CustomName = "HingeVB" + setupStep + "-" + a;
                p.piston.CustomName = "Piston" + setupStep + "-" + a;
                p.horizontalFront.CustomName = "HingeHF" + setupStep + "-" + a;
                p.verticalFront.CustomName = "HingeVF" + setupStep + "-" + a;

                columns[setupStep].platforms[a] = p;
            }

            if (setupStep < columns.Length - 1)
            {
                setupStep++;
                Echo("Setup:" + ((float)setupStep / (float)columns.Length).ToString("P0") + "  ID:" + setupStep + "  of:" + columns.Length);
            }
            else
            {
                setupDone = true;
            }

        }

        public void ToggleDebugHud(int id)
        {
            bool enable = !columns[id].platforms[0].horizontalBack.ShowOnHUD;
            lastAction = "Setting Debug " + id + " to " + enable;
            for (int i = 0; i < 5; i++)
            {
                columns[id].platforms[i].horizontalBack.ShowOnHUD = enable;
                columns[id].platforms[i].verticalBack.ShowOnHUD = enable;
                columns[id].platforms[i].piston.ShowOnHUD = enable;
                columns[id].platforms[i].horizontalFront.ShowOnHUD = enable;
                columns[id].platforms[i].verticalFront.ShowOnHUD = enable;
            }
        }

        public void SetPlatformPositions(PlatformPosition[] columnPosition)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i].MovePlatforms(columnPosition);
            }
        }

        public struct PlatformPosition
        {
            public Vector3 position;
            public Vector2 rotation;

            public PlatformPosition(float x, float y, float z, float v, float h)
            {
                position = new Vector3(x,y,z);
                rotation = new Vector2(v,h);
            }
        }

        class Column
        {
            public Platform[] platforms;
            public Column()
            {
                platforms = new Platform[5];
            }

            public void MovePlatforms(PlatformPosition[] pos)
            {
                if (pos.Length == 5)
                {
                    for (int i = 4; i >= 0; i--)
                    {
                        MovePlatform(i, pos[i]);
                    }
                }
            }

            public void MovePlatform(int id, PlatformPosition pos)
            {
                platforms[id].MoveToPosition(pos);
            }
        }

        class Platform
        {
            public IMyPistonBase piston;
            public IMyMotorStator horizontalBack, verticalBack, horizontalFront, verticalFront;

            public double rotationVB;
            public double rotationVF;
            public double rotationHB;
            public double rotationHF;

            public void MoveToPosition(PlatformPosition posRot)
            {
                Vector3 pos = posRot.position;
                Vector2 rot = posRot.rotation;

                pos.Z += 3;
                double pistonLength; // Länge des Arms

                pistonLength = Math.Sqrt(pos.Y * pos.Y + pos.Z * pos.Z + pos.X * pos.X);

                rotationVB = Math.Asin(pos.Y / pistonLength) * (180 / Math.PI);
                rotationVF = -rotationVB + rot.Y;

                rotationHB = Math.Asin(pos.X / pistonLength) * (180 / Math.PI);
                rotationHF = -rotationHB + rot.X;

                MoveHinges(verticalBack, rotationVB);
                MoveHinges(verticalFront, rotationVF);
                MovePiston(pistonLength-3);

                MoveHinges(horizontalBack, rotationHB);
                MoveHinges(horizontalFront, rotationHF);
            }

            private void MovePiston(double targetLength, float speed = 1)
            {
                float diff = Math.Abs(piston.CurrentPosition - (float)targetLength);

                if (piston.CurrentPosition < targetLength)
                {
                    piston.MaxLimit = (float)targetLength;
                    piston.Velocity = speed * diff;
                }
                else if (piston.CurrentPosition > targetLength)
                {
                    piston.MinLimit = (float)targetLength;
                    piston.Velocity = -speed * diff;
                }
                else
                {
                    // Dont Move
                }
            }

            private void MoveHinges(IMyMotorStator hinge, double targetAngle, float speed = 1)
            {
                double currentAngle = hinge.Angle * (180 / Math.PI);
                float diff = Math.Abs((float)currentAngle - (float)targetAngle);

                if (currentAngle < targetAngle)
                {
                    hinge.UpperLimitDeg = (float)targetAngle;
                    hinge.TargetVelocityRPM = speed / 6F * diff;
                }
                else if (currentAngle > targetAngle)
                {
                    hinge.LowerLimitDeg = (float)targetAngle;
                    hinge.TargetVelocityRPM = -speed / 6F * diff;
                }
                else
                { 
                    // Dont Move
                }

            }
        }
    }
}
