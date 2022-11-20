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
        public string baseName = "Hinge Base";
        public string debugDisplayName = "Display RobotArm";

        Vector3 targetPos = new Vector3();

        IMySensorBlock sensor;

        Arm arm;
        public Program()
        {
            Echo("Creating Arm...");
            arm = new Arm(GridTerminalSystem, baseName, debugDisplayName);
            if (arm.init)
                Echo("Done!");
            else
                Echo("Error!");

            sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName("Sensor GRAB");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update100)
            {
                MyDetectedEntityInfo last = sensor.LastDetectedEntity;

                if (last.Type == MyDetectedEntityType.CharacterHuman)
                {
                    Vector3D worldDirection = sensor.GetPosition() - last.Position;
                    targetPos = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(sensor.WorldMatrix)); //note that we transpose to go from world -> body

                    targetPos.Y = -targetPos.Y;

                    targetPos.Z -= 5;
                    
                    targetPos -= new Vector3(4.5F, -0.5F, 15);

                    if (targetPos.Z < 0) {
                        targetPos.Z = 0;
                    }

                    targetPos = new Vector3(targetPos.X / 2, targetPos.Y / 2, targetPos.Z / 2);

                    arm.debugDisplay.WriteText("\n" + targetPos, true);

                    Echo(targetPos.ToString());
                    arm.MoveToPosition(targetPos);
                }
            }
            else
            {
                switch (argument)
                {
                    case "left":
                        targetPos.X--;
                        break;
                    case "right":
                        targetPos.X++;
                        break;
                    case "forward":
                        targetPos.Z++;
                        break;
                    case "back":
                        targetPos.Z--;
                        break;
                    case "up":
                        targetPos.Y--;
                        break;
                    case "down":
                        targetPos.Y++;
                        break;
                    case "reset":
                        targetPos = new Vector3(0, 0, 0);
                        break;
                    default:
                        break;
                }

                Echo(targetPos.ToString());
                arm.MoveToPosition(targetPos);
            }
        }
    }
}
