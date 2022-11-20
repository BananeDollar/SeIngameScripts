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
        // --- Settings ---
        string blocksTags = "[GalNetServer]";

        // --- Settings ---

        IMyTextSurface[] websiteScreens;
        IMyLaserAntenna laserAntenna;

        List<IMyBroadcastListener> broadcastListeners;

        public Program()
        {
            laserAntenna = FindBlockOfType<IMyLaserAntenna>(blocksTags, "Laserantenna");

            List<IMyTerminalBlock> terminalBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(terminalBlocks, s => s.CustomName.Contains(blocksTags));

            websiteScreens = new IMyTextSurface[terminalBlocks.Count];

            for (int i = 0; i < terminalBlocks.Count; i++)
            {
                websiteScreens[i] = terminalBlocks[i] as IMyTextSurface;
                websiteScreens[i].ContentType = ContentType.TEXT_AND_IMAGE;
                websiteScreens[i].Font = "Monospace";
                websiteScreens[i].FontSize = 0.71F;
            }
            Echo("Loaded "+ terminalBlocks.Count + " websites");

            if (laserAntenna != null)
            {
                IGC.RegisterBroadcastListener("WebsiteRequest");
                broadcastListeners = new List<IMyBroadcastListener>();
                IGC.GetBroadcastListeners(broadcastListeners);

                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                Echo("Started!");
            }
            else
            {
                Echo("No Laserantenna Found!");
            }
        }

        void CheckForWebsiteRequests()
        {
            if (broadcastListeners[0].HasPendingMessage)
            {
                MyIGCMessage message = new MyIGCMessage();
                message = broadcastListeners[0].AcceptMessage();
                string requestedDomain = message.Data.ToString();
                long sender = message.Source;

                Echo(sender+ " Requested:"+requestedDomain);

                FindWebsite(requestedDomain, sender);
            }
        }

        void FindWebsite(string domain, long sender)
        {
            for (int i = 0; i < websiteScreens.Length; i++)
            {
                if (((IMyTerminalBlock)websiteScreens[i]).CustomName.StartsWith(domain))
                {
                    SendWebsite(websiteScreens[i].GetText(), sender);
                    return;
                }
            }
            SendWebsite("-404-\nDie angegebene Website\nwurde nicht gefunden",sender);
        }

        void SendWebsite(string website, long sender)
        {
            IGC.SendUnicastMessage(sender, "Website", website);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            CheckForWebsiteRequests();
        }

        #region Util
        public T FindBlockOfType<T>(string tag, string objectName) where T : class
        {
            List<IMyTerminalBlock> terminalBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(terminalBlocks, s => s.CustomName.Contains(tag) && s.IsSameConstructAs(Me));

            if (terminalBlocks.Count > 0)
            {
                return terminalBlocks[0] as T;
            }
            else
            {
                Echo("Cant find " + objectName + "!");
                return null;
            }
        }

        #endregion
    }
}
