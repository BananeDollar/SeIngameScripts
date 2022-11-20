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
using System.Collections.Immutable;

namespace IngameScript
{
    partial class Program
    {
        class Arm
        {
            public bool init = false;
            public float moveSpeed = 0.5F;

            public IMyTextSurface debugDisplay;

            public IMyPistonBase piston;
            public IMyMotorStator hingeHBack, hingeVBack, hingeHFront, hingeVFront;

            public Arm(IMyGridTerminalSystem gts, string BaseName, string displayName = "")
            {
                init = false;

                if (displayName != "")
                {
                    debugDisplay = (IMyTextSurface)gts.GetBlockWithName(displayName);
                    debugDisplay.WriteText("Arm Started...");
                }

                hingeVBack = (IMyMotorStator)gts.GetBlockWithName(BaseName);
                hingeHBack = (IMyMotorStator)GetChildBlock(gts, hingeVBack);
                piston = (IMyPistonBase)GetChildBlock(gts, hingeHBack);
                hingeHFront = (IMyMotorStator)GetChildBlock(gts, piston);
                hingeVFront = (IMyMotorStator)GetChildBlock(gts, hingeHFront);

                init = true;

                MoveToPosition(Vector3.Zero);
            }

            private IMyMechanicalConnectionBlock GetChildBlock(IMyGridTerminalSystem gts, IMyMechanicalConnectionBlock baseBlock)
            {
                List<IMyMechanicalConnectionBlock> newBlocks = new List<IMyMechanicalConnectionBlock>();

                gts.GetBlocksOfType(newBlocks, child => child.CubeGrid == baseBlock.TopGrid);
                if (newBlocks.Count != 0)
                {
                    debugDisplay.WriteText("\n"+"Found:" +newBlocks[0].CustomName,true);
                    return newBlocks[0];
                }
                debugDisplay.WriteText("\n"+baseBlock.CustomName + " has no Child"+"\n", true);
                return null;
            }

            public void MoveToPosition(Vector3 pos)
            {
                double hAngle = 0;
                double vAngle = 0;

                //pos.Z += 15;

                double planeDistance = Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y);
                double armLength = Math.Sqrt(planeDistance * planeDistance + pos.Z * pos.Z);

                if (armLength != 0)
                {
                    hAngle = Math.Atan(pos.X / armLength) * (180 / Math.PI);
                    vAngle = Math.Atan(pos.Y / armLength) * (180 / Math.PI);
                }

                MovePiston(armLength, moveSpeed);

                MoveHinges(hingeHBack, hAngle, moveSpeed);
                MoveHinges(hingeHFront, hAngle, moveSpeed);

                MoveHinges(hingeVFront, vAngle, moveSpeed);
                MoveHinges(hingeVBack, vAngle, moveSpeed);
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
            }

            private int lineCount = 0;
            private string debugLog = "";
            private void Print(string text)
            {
                debugLog += "\n" + text;
                if (lineCount < 10)
                {
                    lineCount++;
                }
                else
                {
                    debugLog = debugLog.Substring(debugLog.IndexOf('\n') + 1);
                }

                debugDisplay.WriteText(debugLog);
            }
        }
    }
}
