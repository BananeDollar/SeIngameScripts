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
        string blocksTags = "[GalNet]";
        string[] idleAnimation = new string[] { "-", "/", "|", "\\" };
        bool closeConnectionAfterLoadingWebsites = true;

        // --- Settings ---

        IMyTextSurface screen;
        IMyLaserAntenna laserAntenna;

        bool webSiteLoaded = false;
        string websiteText = "";
        string currentWebsite = "";
        string currentRequest = "";
        bool resendRequestAfterConnection = false; // wait for laser antenna
        int cursorPosition;
        int maxCursorPosition;
        List<Action> currentButtonActions;
        List<string> currentButtonTexts;

        List<string> favorites = new List<string>();

        #region Init
        public Program()
        {
            laserAntenna = FindBlockOfType<IMyLaserAntenna>(blocksTags, "Laserantenna");
            screen = FindBlockOfType<IMyTextSurface>(blocksTags, "Screen");

            if (screen != null && laserAntenna != null)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                InitScreen();
                loadfavorites();
                LoadMainMenu();
            }
        }

        public void InitScreen()
        {
            screen.ContentType = ContentType.TEXT_AND_IMAGE;
            screen.Alignment = TextAlignment.LEFT;
            screen.FontSize = 0.71F;
            screen.Font = "Monospace";
        }
        #endregion

        #region favorites
        void loadfavorites()
        {
            favorites = Me.CustomData.Split(',').ToList();
            favorites.Remove("");
        }

        void SaveFavorites()
        {
            string text = "";
            for (int i = 0; i < favorites.Count; i++)
            {
                text += favorites[i];
                if (i < favorites.Count-1)
                {
                    text += ",";
                }
            }

            Me.CustomData = text;
        }

        void AddFavorites(string website)
        {
            if (!favorites.Contains(website))
            {
                favorites.Add(website);
                SaveFavorites();
            }
        }

        #endregion

        #region Display
        int animationIndex = 0;
        bool enterPressAnimation;
        void UpdateMainScreen()
        {
            string selectedCursor = enterPressAnimation ? "->" : "> ";

            string headerInfo = " |" + (laserAntenna.Status == MyLaserAntennaStatus.Connected ? "Con" : "---") + "| |"+ (webSiteLoaded?"Lod":"---") +"|             "+ idleAnimation[animationIndex];

            string text =
                "+--------+--------------------------+\n" +
                "| GalNet |"+headerInfo+"|\n" +
                "+--------+--------------------------+\n";

            if (!webSiteLoaded)
            {
                if (currentRequest == "")
                {
                    text +=
                        "-Status: " + laserAntenna.Status.ToString() + "\n" +
                        "-Usage:" + getLaserAntennaPowerUsage() + "\n";
                }
                else
                {
                    text += "Lade:'"+currentRequest+"'"+ (resendRequestAfterConnection?"  -Retry-":"")+"\n";
                }
            }
            else
            {
                text += websiteText+"\n";
            }

            text += "+-----------------------------------+\n";
            for (int i = 0; i < currentButtonTexts.Count; i++)
            {
                text += (i == cursorPosition ? selectedCursor : "  ") + currentButtonTexts[i] + "\n";
            }
            enterPressAnimation = false;
            screen.WriteText(text);
        }

        void LoadMainMenu()
        {
            currentRequest = "";
            webSiteLoaded = false;
            cursorPosition = 0;

            currentButtonActions = new List<Action>();
            currentButtonTexts = new List<string>();

            for (int i = 0; i < favorites.Count; i++)
            {
                string value = favorites[i];
                currentButtonTexts.Add("*"+value);
                currentButtonActions.Add(() => ConnectAndRequestWebsite(value));
            }

            currentButtonTexts.Add("Lade Startseite");
            currentButtonActions.Add(() => ConnectAndRequestWebsite("Homepage"));

            currentButtonTexts.Add("Trennen");
            currentButtonActions.Add(() => SetLaserAntenna(false));

            currentButtonTexts.Add("Deaktivieren");
            currentButtonActions.Add(() => SetLaserAntenna(false, true));


            maxCursorPosition = currentButtonActions.Count - 1;
        }
        void LoadWebsite(string rawWebsiteText)
        {
            currentWebsite = currentRequest;
            currentRequest = "";
            resendRequestAfterConnection = false;
            webSiteLoaded = true;
            cursorPosition = 0;
            bool closeConnection = closeConnectionAfterLoadingWebsites;

            currentButtonTexts = new List<string>();
            currentButtonActions = new List<Action>();

            string[] website = rawWebsiteText.Split('^'); //TODO: remove buttons from websiteText and apply here
            websiteText = website[0];

            if (website.Length == 2)
            {
                string[] content = website[1].Split('\n');
                for (int i = 0; i < content.Length; i++)
                {
                    switch (content[i])
                    {
                        case"keepConnection":
                            closeConnection = false;
                            break;
                        default:
                            string[] button = content[i].Split(':');
                            if (button.Length == 2)
                            {
                                if (button[0] == "redirect")
                                {
                                    currentButtonTexts.Add(button[1]);
                                    currentButtonActions.Add(() => ConnectAndRequestWebsite(button[1]));
                                }
                            }
                            break;
                    }
                }
            }

            currentButtonTexts.Add("*+"+currentWebsite);
            currentButtonActions.Add(() => AddFavorites(currentWebsite));

            currentButtonTexts.Add("Beenden");
            currentButtonActions.Add(() => LoadMainMenu());

            maxCursorPosition = currentButtonActions.Count - 1;

            if (closeConnection)
            {
                SetLaserAntenna(false);
            }
        }
        #endregion


        #region Network Communication
        void ConnectAndRequestWebsite(string domain)
        {
            currentRequest = domain;
            webSiteLoaded = false;
            if (laserAntenna.Status != MyLaserAntennaStatus.Connected)
            {
                SetLaserAntenna(true);
                resendRequestAfterConnection = true;
            }
            else
            {
                RequestWebsite(domain);
            }
        }

        void RequestWebsite(string domain)
        {
            IGC.SendBroadcastMessage("WebsiteRequest", domain);
            Echo("Request Send! for:"+domain);
        }


        void CheckForNewMessage()
        {
            if (IGC.UnicastListener.HasPendingMessage)
            {
                MyIGCMessage message = IGC.UnicastListener.AcceptMessage();
                LoadWebsite(message.Data.ToString());
                Echo("Recieved website!");
            }
        }

        #endregion

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update100)
            {
                CheckForNewMessage();

                if (animationIndex < 3)
                    animationIndex++;
                else
                    animationIndex = 0;

                if (resendRequestAfterConnection && currentRequest != "")
                {
                    if (laserAntenna.Status == MyLaserAntennaStatus.Connected)
                    {
                        RequestWebsite(currentRequest);
                        //resendRequestAfterConnection = false;
                    }
                }

                UpdateMainScreen();
            }

            if (updateSource == UpdateType.Trigger ||updateSource == UpdateType.Terminal)
            {
                switch (argument)
                {
                    case "up":
                        if (cursorPosition > 0)
                            cursorPosition--;
                        break;
                    case "down":
                        if (cursorPosition < maxCursorPosition)
                            cursorPosition++;
                        break;
                    case "enter":
                        enterPressAnimation = true;
                        currentButtonActions[cursorPosition].Invoke();
                        break;
                }
                UpdateMainScreen();
            }
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
        void SetLaserAntenna(bool connect, bool turnOff = false)
        {
            if (connect)
            {
                laserAntenna.Enabled = true;
                laserAntenna.Connect();
            }
            else
            {
                if (turnOff)
                    laserAntenna.Enabled = false;

                laserAntenna.ApplyAction("Idle");
            }
        }

        string getLaserAntennaPowerUsage()
        {
            return laserAntenna.DetailedInfo.Split('\n')[1].Split(':')[1];
        }
        #endregion

    }
}
