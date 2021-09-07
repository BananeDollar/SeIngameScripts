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
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        IMyRemoteControl remote;
        string baseComputerName = "";
        IMyTextSurface screen;
        int trainID = 0;
        bool reachedGoal = false;

        TrainStatus status = TrainStatus.parkedAtA;

        public Program()
        {
            List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(allBlocks);

            for (int i = 0; i < allBlocks.Count; i++)
            {
                if (allBlocks[i].IsSameConstructAs(Me))
                {
                    connectors.Add(allBlocks[i] as IMyShipConnector);
                }
            }

            allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(allBlocks);

            for (int i = 0; i < allBlocks.Count; i++)
            {
                if (allBlocks[i].IsSameConstructAs(Me))
                {
                    remote = allBlocks[i] as IMyRemoteControl;
                    break;
                }
            }

            allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(allBlocks);

            for (int i = 0; i < allBlocks.Count; i++)
            {
                if (allBlocks[i].IsSameConstructAs(Me))
                {
                    batteries.Add(allBlocks[i] as IMyBatteryBlock);
                }
            }

            screen = Me.GetSurface(0);
            screen.ContentType = ContentType.TEXT_AND_IMAGE;

            screen.WriteText("Warte auf Base Computer...\n");
            screen.WriteText(connectors.Count.ToString() + "Verbinder\n", true);
            screen.WriteText(batteries.Count.ToString() + "Batterien\n", true);
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            string[] args = argument.Split('-');
            if (args[0] == "init")
            {
                baseComputerName = args[1];
                trainID = int.Parse(args[2]);
                screen.WriteText("Als Zug " + trainID + " registriert");

                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }

            if (argument == "Go")
            {
                Go();
            }

            if (argument == "Stop")
            {
                ReachedGoal();
            }

            screen.WriteText(status.ToString()+" "+reachedGoal.ToString());

            if (reachedGoal)
            {
                bool connectable = connectors[0].Status == MyShipConnectorStatus.Connectable || connectors[1].Status == MyShipConnectorStatus.Connectable;

                if (connectable && baseComputerName != "")
                {
                    foreach (IMyShipConnector con in connectors)
                    {
                        con.Connect();
                    }

                    IMyProgrammableBlock baseComputer = GridTerminalSystem.GetBlockWithName(baseComputerName) as IMyProgrammableBlock;
                    if (status == TrainStatus.movingToA)
                    {
                        baseComputer.TryRun(0 + "-reachedStationA");
                        status = TrainStatus.parkedAtA;
                    }
                    if (status == TrainStatus.movingToB)
                    {
                        baseComputer.TryRun(0 + "-reachedStationB");
                        status = TrainStatus.parkedAtB;
                    }

                    foreach (IMyBatteryBlock bat in batteries)
                    {
                        bat.ChargeMode = ChargeMode.Recharge;
                    }

                    remote.SetAutoPilotEnabled(false);
                }
            }
            else
            {
                Vector3D remotePos = remote.CubeGrid.GridIntegerToWorld(remote.Position);
                Vector3D targetPos = remote.CurrentWaypoint.Coords;

                Echo(Vector3D.Distance(remotePos, targetPos).ToString());
            }
        }

        void ReachedGoal()
        {
            reachedGoal = true;
        }

        void Go()
        {
            reachedGoal = false;
            foreach (IMyBatteryBlock bat in batteries)
            {
                bat.ChargeMode = ChargeMode.Auto;
            }

            foreach (IMyShipConnector con in connectors)
            {
                con.Disconnect();
            }

            if (status == TrainStatus.parkedAtA)
            {
                remote.Direction = Base6Directions.Direction.Backward;
                status = TrainStatus.movingToB;
            }
            if (status == TrainStatus.parkedAtB)
            {
                remote.Direction = Base6Directions.Direction.Forward;
                status = TrainStatus.movingToA;
            }

            remote.SetAutoPilotEnabled(true);
        }

        public enum TrainStatus
        {
            parkedAtA,
            movingToB,
            parkedAtB,
            movingToA
        }
    }
}
