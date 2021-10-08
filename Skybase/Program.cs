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
        float maxThrustNewton;
        IMyTextSurface display;
        IMyRemoteControl remote;
        
        public Program()
        {
            IMyBlockGroup flyingThrusters = GridTerminalSystem.GetBlockGroupWithName("Thrusters");
            remote = GridTerminalSystem.GetBlockWithName("Remote") as IMyRemoteControl;

            List<IMyThrust> thrusters = new List<IMyThrust>();
            flyingThrusters.GetBlocksOfType(thrusters);

            for (int i = 0; i < thrusters.Count; i++)
            {
                maxThrustNewton += thrusters[i].MaxEffectiveThrust;
            }

            maxThrustNewton = maxThrustNewton / 10000;

            display = Me.GetSurface(0);

            display.ContentType = ContentType.TEXT_AND_IMAGE;
            display.ScriptForegroundColor = Color.White;
            display.ScriptBackgroundColor = Color.Black;

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            MyShipMass mass = remote.CalculateShipMass();
            Vector3 gravityVector = remote.GetTotalGravity();
            float grav = gravityVector.Length();

            float neededNewton = ((mass.TotalMass * 0.981F) / grav);
            float percent = neededNewton / maxThrustNewton;
            display.WriteText("M:"+neededNewton+"  "+ percent + "%");
        }
    }
}
