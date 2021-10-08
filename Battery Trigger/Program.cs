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
        //Settings
        public int highPercentage = 80;
        public string highTimerName = "Timer Batterys High";
        public int lowPercentage = 20;
        public string lowTimerName = "Timer Batterys Low";

        public bool highState = true;
        public bool initialized = false;
        IMyTextSurface debugScreen;

        List<IMyBatteryBlock> trackingBatterys = new List<IMyBatteryBlock>();
        IMyTimerBlock lowTimerBlock, highTimerBlock;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            debugScreen = Me.GetSurface(0);
            debugScreen.ContentType = ContentType.TEXT_AND_IMAGE;

            debugScreen.WriteText("Initialize...");

            GridTerminalSystem.GetBlocksOfType(trackingBatterys,(bat) => { return bat.CubeGrid == Me.CubeGrid; });

            debugScreen.WriteText("\nfound " + trackingBatterys.Count+ " batteries",true);

            lowTimerBlock = GridTerminalSystem.GetBlockWithName(lowTimerName) as IMyTimerBlock;
            highTimerBlock = GridTerminalSystem.GetBlockWithName(highTimerName) as IMyTimerBlock;

            if (lowTimerBlock == null)
                debugScreen.WriteText("\nlow timer block not found", true);
            if (highTimerBlock == null)
                debugScreen.WriteText("\nhigh timer block not found", true);

            if (lowTimerBlock != null && highTimerBlock != null && trackingBatterys.Count > 0)
            {
                lowTimerBlock.Trigger();
                highState = false;
                initialized = true;
                debugScreen.WriteText("\nDone! Starting...", true);
            }
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (initialized)
            {
                float CurrentstoredPower = 0;
                float MaxstoredPower = 0;
                for (int i = 1; i < trackingBatterys.Count; i++)
                {
                    CurrentstoredPower += trackingBatterys[i].CurrentStoredPower;
                    MaxstoredPower += trackingBatterys[i].MaxStoredPower;
                }

                float currentChage = (CurrentstoredPower / MaxstoredPower) * 100F;

                if (highState)
                {
                    if (currentChage <= lowPercentage)
                    {
                        highState = false;
                        lowTimerBlock.Trigger();
                    }
                }
                else {
                    if (currentChage >= highPercentage)
                    {
                        highState = true;
                        highTimerBlock.Trigger();
                    }
                }

                debugScreen.WriteText("tracking " + trackingBatterys.Count + " batteries");
                debugScreen.WriteText("\ncurrent: " + Math.Round(CurrentstoredPower, 0) + "MWH",true);
                debugScreen.WriteText("\nmax: " + Math.Round(MaxstoredPower, 0) + "MWH",true);
                debugScreen.WriteText("\npercentage: " + Math.Round(currentChage, 1) + "%",true);
            }
        }
    }
}
