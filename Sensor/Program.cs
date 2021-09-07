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
        public List<OSTerminal> terminals = new List<OSTerminal>();

        public Program()
        {
            Me.GetSurface(0).WriteText("Initializing...\n");
            FindTerminals();
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Trigger)
            {
                if (argument.Contains("-"))
                {
                    string[] split = argument.Split('-');
                    int terminalId = int.Parse(split[0]);
                    int buttonId = int.Parse(split[1]);
                    Print("Terminal:"+terminalId+" Button:"+buttonId);
                    terminals[terminalId].PressButton(buttonId);
                }
            }
        }
        public void FindTerminals()
        {
            List<IMyTerminalBlock> screens = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurface>(screens);

            List<IMyTerminalBlock> buttonPannels = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyButtonPanel>(buttonPannels);

            for (int s = 0; s < screens.Count; s++)
            {
                string screenName = screens[s].CustomName.Split('[', ']')[1];
                if (screenName.Contains("Terminal"))
                {
                    int screenID = int.Parse(screenName.Remove(0, 8));
                    bool found = false;

                    for (int b = 0; b < buttonPannels.Count; b++)
                    {
                        string buttonname = buttonPannels[b].CustomName.Split('[', ']')[1];
                        if (buttonname.Contains("Terminal"))
                        {
                            int buttonPannelID = int.Parse(buttonname.Remove(0, 8));
                            if (screenID == buttonPannelID)
                            {
                                Print("Found Terminal " + screenID);
                                terminals.Add(new OSTerminal(screens[s],buttonPannels[b], terminals.Count));

                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        Print("Could not find Button for Screen " + screenID);
                    }
                }
            }
        }

        public void Print(string text)
        {
            Me.GetSurface(0).WriteText(text+"\n", true);
        }
    }

    public class OSTerminal
    {
        public IMyTextSurface display;
        public IMyButtonPanel buttons;
        public IMyTextSurfaceProvider buttonLcdHolder;
        public int terminalIndex = 0;
        public bool buttonsSetUp;
        public int menuLevel = 0;

        public delegate void ButonAction();

        public ButonAction[] buttonActions = new ButonAction[4];

        public OSTerminal(IMyTerminalBlock textSurface, IMyTerminalBlock buttonPanel, int index)
        {
            terminalIndex = index;
            display = (IMyTextSurface)textSurface;
            buttons = (IMyButtonPanel)buttonPanel;
            buttonLcdHolder = (IMyTextSurfaceProvider)buttonPanel;

            CheckForButtonSetings();
        }

        public void PressButton(int id)
        {
            if (!buttonsSetUp)
            {
                CheckForButtonSetings();
            }
            else
            {
                UpdateMenu();
            }
        }

        public void CheckForButtonSetings()
        {
            for (int i = 0; i < 4; i++)
            {
                buttonsSetUp = true;
                if (!buttons.IsButtonAssigned(i))
                {
                    buttonsSetUp = false;
                }
                else
                {
                    IMyTextSurface lcd = buttonLcdHolder.GetSurface(i);
                    lcd.WriteText("");
                }
            }
            if (!buttonsSetUp)
            {
                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.FontSize = 2;
                display.TextPadding = 0;
                display.Alignment = TextAlignment.CENTER;
                display.WriteText("\n\n\nPlease configure\nall buttons");
            }
            else
            {
                display.WriteText("");
            }
        }

        public void UpdateMenu()
        {
            switch (menuLevel)
            {
                case 0:
                    buttonActions[0] = delegate { CheckForButtonSetings(); };
                break;
            }
        }
    }
}
