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
        IMyRemoteControl remote;

        IMyMotorSuspension[] wheels = new IMyMotorSuspension[4];

        bool aligning = false;
        bool centered = false;

        int resolution = 100; // Kommaverschiebung für Gravitationsabfrage
        float heightChange = 0.01F; // Höhenverschub pro Tick in Metern bei angleichung

        float suspensionSpeed = 0.1F; // Höhenverschub pro Tick in Metern für generelle bewegung

        float[] targetHeights = new float[4];

        public Program()
        {
            remote = GridTerminalSystem.GetBlockWithName("Remote") as IMyRemoteControl;

            wheels[0] = GridTerminalSystem.GetBlockWithName("Rad Vorne Links") as IMyMotorSuspension;
            wheels[1] = GridTerminalSystem.GetBlockWithName("Rad Vorne Rechts") as IMyMotorSuspension;
            wheels[2] = GridTerminalSystem.GetBlockWithName("Rad Hinten Links") as IMyMotorSuspension;
            wheels[3] = GridTerminalSystem.GetBlockWithName("Rad Hinten Rechts") as IMyMotorSuspension;

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument == "toggle")
            {
                aligning = !aligning;
                centered = false;
            }

            if (aligning)
            {
                if (!centered)
                {
                    targetHeights[0] = 0;
                    targetHeights[1] = 0;
                    targetHeights[2] = 0;
                    targetHeights[3] = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        centered = false;
                        if (wheels[i].Height != 0)
                        {
                            centered = true;
                        }
                    }
                }
                else
                {

                    Vector3D current = remote.GetNaturalGravity();

                    Vector3D gravDir = Vector3D.Transform(current, MatrixD.Transpose(remote.WorldMatrix.GetOrientation()));

                    int x = (int)(gravDir.X * resolution);
                    int z = (int)(gravDir.Z * resolution);

                    float leftRight = 0;
                    float frontBack = 0;

                    if (x > 0)
                        leftRight += heightChange;

                    if (x < 0)
                        leftRight -= heightChange;

                    if (z > 0)
                        frontBack += heightChange;

                    if (z < 0)
                        frontBack -= heightChange;

                    targetHeights[0] += frontBack + leftRight;
                    targetHeights[1] += frontBack - leftRight;
                    targetHeights[2] += -frontBack + leftRight;
                    targetHeights[3] += -frontBack - leftRight;
                }
            }
            else
            {
                targetHeights[0] = -1.5F;
                targetHeights[1] = -1.5F;
                targetHeights[2] = -1.5F;
                targetHeights[3] = -1.5F;
            }

            for (int i = 0; i < 4; i++)
            {
                if (wheels[i].Height < targetHeights[i])
                {
                    wheels[i].Height += Math.Min(suspensionSpeed,targetHeights[i] - wheels[i].Height);
                }

                if (wheels[i].Height > targetHeights[i])
                {
                    wheels[i].Height -= Math.Min(suspensionSpeed, wheels[i].Height - targetHeights[i]);
                }
            }
        }
    }
}
