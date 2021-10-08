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
        /* Setup:
         * 
         * Action groups:
         *  - Names:   GAME-ACTION-ID
         *  - example: BJ-1-SA     ( Blackjack, staton 1, Sensor Activate)
         *  - example: BJ-1-B1     ( Blackjack, staton 1, Button1)
         * 
         * 
         * 
         */


        BlackJack blackJack;
        Bank bank;
        SlotMachine slotMachine;
        LuckyWheel luckyWheel;

        public static Dictionary<string, int> userMoney;
        public static Program program;

        public static int[] moneyStepValues = new int[] { 50, 100, 150, 200, 250, 500, 1000, 1500, 2000, 2500, 5000, 10000, 15000, 20000, 25000, 50000, 100000, 150000, 200000, 250000, 500000, 1000000, 1500000, 2000000, 2500000, 5000000, 10000000 };

        public Program()
        {
            program = this;

            IMyCargoContainer vault = GridTerminalSystem.GetBlockWithName("Vault") as IMyCargoContainer;
            IMyTextSurface bankMonitor = GridTerminalSystem.GetBlockWithName("Bank Monitor") as IMyTextSurface;

            bank = new Bank(vault, bankMonitor,
                new Bank.Station(tryGetBlockGroup("BS0")),
                new Bank.Station(tryGetBlockGroup("BS1"))
                );

            bank.LoadUserMoney(Me);

            luckyWheel = new LuckyWheel(tryGetBlockGroup("LW"));

            IMyTextSurface blackJackMonitor = GridTerminalSystem.GetBlockWithName("BlackJack Monitor") as IMyTextSurface;

            blackJack = new BlackJack(
                blackJackMonitor,
                new BlackJack.Player(tryGetBlockGroup("BJ0")),
                new BlackJack.Player(tryGetBlockGroup("BJ1")),
                new BlackJack.Player(tryGetBlockGroup("BJ2"))
            );

            slotMachine = new SlotMachine(
                new SlotMachine.Station(tryGetBlockGroup("SM0"))
                );


            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private IMyBlockGroup tryGetBlockGroup(string name)
        {
            IMyBlockGroup bg = GridTerminalSystem.GetBlockGroupWithName(name);

            if (bg == null) {
                Echo("Group: '"+name+ "' not found");
            }

            return bg;
        }

        public void Save()
        {
            bank.SaveUserMoney(Me);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "save":
                    bank.SaveUserMoney(Me);
                    Echo("Saved!");
                    break;
                case "load":
                    bank.LoadUserMoney(Me);
                    Echo("Loaded!");
                    break;
                default:
                    string[] args = argument.Split('-');
                    if (args.Length == 3)
                    {
                        int id = int.Parse(args[1]);

                        switch (args[0])
                        {
                            case "BJ":
                                blackJack.OnActionCall(args[2], id);
                                break;
                            case "BS":
                                bank.OnActionCall(args[2], id);
                                break;
                            case "SM":
                                slotMachine.OnActionCall(args[2], id);
                                break;
                            case "LW":
                                luckyWheel.OnActionCall(args[2]);
                                break;
                            default:
                                Echo("Wrong argument!");
                                break;
                        }
                    }
                    break;
            }

            if (blackJack.state == BlackJack.GameState.playing)
            {
                blackJack.ExecuteDaelayedAction();
            }

            bank.UpdateTick();
            slotMachine.UpdateTick();
            luckyWheel.UpdateTick();
        }

        class LuckyWheel {

            IMyTextSurface display;
            IMySensorBlock sensor;
            IMyMotorStator motor;
            string currentUser;
            bool spinning = false;

            public LuckyWheel(IMyBlockGroup blockGroup)
            {
                List<IMyTextSurface> screens = new List<IMyTextSurface>();
                blockGroup.GetBlocksOfType(screens);

                if (screens.Count > 0)
                {
                    display = screens[0];

                    display.ContentType = ContentType.SCRIPT;
                    display.ScriptForegroundColor = Color.White;
                    display.ScriptBackgroundColor = Color.Black;
                    display.Script = "None";
                }

                List<IMySensorBlock> sensors = new List<IMySensorBlock>();
                blockGroup.GetBlocksOfType(sensors);
                if (sensors.Count > 0)
                    sensor = sensors[0];

                List<IMyMotorStator> motors = new List<IMyMotorStator>();
                blockGroup.GetBlocksOfType(motors);
                if (motors.Count > 0)
                {
                    motor = motors[0];
                }
                motor.TargetVelocityRPM = 0;
            }

            public void OnActionCall(string arg)
            {
                if (arg == "SA") // Sensor Activate
                {
                    EnterSensor();
                }
                if (arg == "SD") // Sensor Disable
                {
                    ExitSensor();
                }

                if (arg.StartsWith("B")) // Buttons
                {
                    if (currentUser != null && currentUser != "" && !spinning)
                    {
                        Spin();
                        //UpdateDisplay();
                    }
                }
            }

            public void EnterSensor()
            {
                if (!spinning)
                {
                    currentUser = Util.GetUserNameFromSensor(sensor);
                    UpdateDisplay();
                }
            }

            public void ExitSensor()
            {
                currentUser = "";
                UpdateDisplay();
            }

            public void UpdateTick()
            {
                if (spinning)
                {
                    motor.TargetVelocityRPM -= Math.Max(0.1F,motor.TargetVelocityRPM/50F);
                    if (motor.TargetVelocityRPM <= 0)
                    {
                        motor.TargetVelocityRPM = 0;
                        StopedSpinning();
                    }
                }
            }

            private void Spin()
            {
                if (currentUser != "" && !spinning)
                {
                    spinning = true;

                    Random random = new Random();
                    motor.TargetVelocityRPM = random.Next(20, 30);
                }
                UpdateDisplay();
            }

            private void StopedSpinning()
            {
                spinning = false;
                if (currentUser != "")
                {
                    string[] priceTexts = new string[] { "90.000", "ROT", "60.000", "GELB", "70.000", "GELB", "80.000", "GRÜN" };
                    double angle = motor.Angle * (180 / Math.PI);
                    int price = (int)Math.Round((float)angle / 45F);

                    switch (price)
                    {
                        case 0:
                            Bank.singleton.AddMoneyForPlayer(currentUser, 90000);
                            break;
                        case 1:

                            break;
                        case 2:
                            Bank.singleton.AddMoneyForPlayer(currentUser, 60000);
                            break;
                        case 3:

                            break;
                        case 4:
                            Bank.singleton.AddMoneyForPlayer(currentUser, 7000);
                            break;
                        case 5:

                            break;
                        case 6:
                            Bank.singleton.AddMoneyForPlayer(currentUser, 80000);
                            break;
                        case 7:

                            break;
                    }
                    UpdateDisplay(priceTexts[price]);
                }
            }

            public void UpdateDisplay(string priceText = "")
            {
                MySpriteDrawFrame frame = display.DrawFrame();

                if (currentUser != "")
                {
                    Util.DrawText(ref frame, 512 / 2, 100, currentUser.ToString(), Color.White);

                    if (priceText!="")
                    {
                        Util.DrawText(ref frame, 512 / 2, 150, priceText, Color.White);
                    }
                }


                frame.Dispose();
            }
        }

        class SlotMachine {
            public Station[] machines;

            public SlotMachine(params Station[] stations)
            {
                this.machines = stations;
            }
            public void OnActionCall(string arg, int id)
            {
                Station station = machines[id];
                if (arg == "SA") // Sensor Activate
                {
                    station.EnterSensor();
                }
                if (arg == "SD") // Sensor Disable
                {
                    station.ExitSensor();
                }

                if (arg.StartsWith("B") && arg.Length == 2) // Buttons
                {
                    int btnId = int.Parse(arg.Substring(1));
                    station.ButtonPress(btnId);
                }
            }

            public void UpdateTick()
            {
                for (int i = 0; i < machines.Length; i++)
                {
                    machines[i].UpdateTick();
                }
            }

            public class Station
            {
                IMyTextSurface display;
                IMySensorBlock sensor;
                IMyMotorStator[] wheels;
                private IMyTextSurfaceProvider buttonPanel;
                string currentUser;
                int betIndex = 0;
                bool spinning = false;
                int wheelStopIndex = 0;

                public Station(IMyBlockGroup blockGroup)
                {
                    List<IMyTextSurface> screens = new List<IMyTextSurface>();
                    blockGroup.GetBlocksOfType(screens);

                    if (screens.Count > 0)
                    {
                        display = screens[0];

                        display.ContentType = ContentType.SCRIPT;
                        display.ScriptForegroundColor = Color.White;
                        display.ScriptBackgroundColor = Color.Black;
                        display.Script = "None";
                    }

                    List<IMyButtonPanel> buttonPanels = new List<IMyButtonPanel>();
                    blockGroup.GetBlocksOfType(buttonPanels);

                    if (buttonPanels.Count > 0)
                    {
                        buttonPanel = (IMyTextSurfaceProvider)buttonPanels[0];

                        for (int i = 0; i < 4; i++)
                        {
                            IMyTextSurface surface = buttonPanel.GetSurface(i);
                            surface.ContentType = ContentType.TEXT_AND_IMAGE;
                            surface.FontColor = Color.White;
                            surface.BackgroundColor = Color.Black;
                            surface.FontSize = 5;
                            surface.Alignment = TextAlignment.CENTER;
                        }
                    }

                    List<IMySensorBlock> sensors = new List<IMySensorBlock>();
                    blockGroup.GetBlocksOfType(sensors);
                    if (sensors.Count > 0)
                        sensor = sensors[0];

                    List<IMyMotorStator> motors = new List<IMyMotorStator>();
                    blockGroup.GetBlocksOfType(motors);
                    if (motors.Count == 3)
                    {
                        wheels = new IMyMotorStator[3];
                        for (int i = 0; i < motors.Count; i++)
                        {
                            int id = int.Parse(motors[i].CustomName[motors[i].CustomName.Length - 1].ToString());
                            wheels[id] = motors[i];
                            wheels[id].UpperLimitDeg = 0;
                            wheels[id].TargetVelocityRPM = 10;
                        }
                    }
                }

                public void EnterSensor()
                {
                    if (!spinning)
                    {
                        currentUser = Util.GetUserNameFromSensor(sensor);
                        UpdateDisplay();
                    }
                }

                public void ExitSensor()
                {
                    currentUser = "";
                    UpdateDisplay();
                }

                public void ButtonPress(int btnId)
                {
                    if (currentUser != null && currentUser != "" && !spinning)
                    {
                        switch (btnId)
                        {
                            case 0:
                                if (betIndex > 0)
                                    betIndex--;
                                break;
                            case 1:
                                if (betIndex < moneyStepValues.Length - 1)
                                    if (moneyStepValues[betIndex + 1] <= Bank.singleton.GetMoneyForPlayer(currentUser))
                                        betIndex++;
                                break;
                            case 3:
                                if (moneyStepValues[betIndex] <= Bank.singleton.GetMoneyForPlayer(currentUser))
                                    Spin();
                                break;
                            default:
                                break;
                        }
                        UpdateDisplay();
                    }
                }

                int wheelStopMultiply = 20;
                public void UpdateTick()
                {
                    if (spinning)
                    {
                        if (wheelStopIndex < 3 * wheelStopMultiply)
                        {
                            wheelStopIndex++;

                            int currentWheel = (wheelStopIndex / wheelStopMultiply) - 1;

                            if (currentWheel >= 0 && currentWheel < 3 && wheels[currentWheel].UpperLimitDeg == float.MaxValue)
                            {
                                Random random = new Random();
                                wheels[currentWheel].UpperLimitDeg = 45 * random.Next(0, 9);
                            }
                        }
                        else
                        {
                            StopedSpinning();
                        }
                    }
                }

                private void Spin()
                {
                    if (currentUser!="" && !spinning)
                    {
                        spinning = true;
                        wheelStopIndex = 0;
                        for (int i = 0; i < wheels.Length; i++)
                        {
                            wheels[i].TargetVelocityRPM = 50;
                            wheels[i].UpperLimitDeg = float.MaxValue;
                        }
                    }
                    UpdateDisplay();
                }

                private void StopedSpinning()
                {
                    spinning = false;
                    if (currentUser != "")
                    {

                    }

                    UpdateDisplay();
                }

                public void UpdateDisplay()
                {
                    MySpriteDrawFrame frame = display.DrawFrame();

                    if (currentUser != "")
                    {
                        Util.DrawText(ref frame, 512 / 2, 100, currentUser.ToString(), Color.White);
                        Util.DrawText(ref frame, 512 / 2, 250, string.Format("{0,15:N0}$", moneyStepValues[betIndex]), Color.White);

                    }

                    if (currentUser!="" && !spinning)
                    { 
                        buttonPanel.GetSurface(0).WriteText("-");
                        buttonPanel.GetSurface(0).FontSize = 10;
                        buttonPanel.GetSurface(0).FontColor = (betIndex > 0) ? Color.Green : Color.Red;
                        buttonPanel.GetSurface(1).WriteText("+");
                        buttonPanel.GetSurface(1).FontSize = 10;
                        buttonPanel.GetSurface(1).FontColor = (betIndex < moneyStepValues.Length - 1 && moneyStepValues[betIndex + 1] <= Bank.singleton.GetMoneyForPlayer(currentUser)) ? Color.Green : Color.Red;
                        buttonPanel.GetSurface(2).WriteText("");
                        buttonPanel.GetSurface(3).WriteText("Drehen");
                        buttonPanel.GetSurface(3).FontColor = Color.Green;
                    }
                    else
                    {
                        buttonPanel.GetSurface(0).WriteText("");
                        buttonPanel.GetSurface(1).WriteText("");
                        buttonPanel.GetSurface(2).WriteText("");
                        buttonPanel.GetSurface(3).WriteText("");
                    }

                    frame.Dispose();
                }
            }
        }

        class Bank
        {
            public static Bank singleton;
            IMyTextSurface bankMonitor;
            IMyCargoContainer vault;
            public Station[] stations;

            public Bank(IMyCargoContainer vault, IMyTextSurface bankMonitor, params Station[] stations)
            {
                singleton = this;
                this.bankMonitor = bankMonitor;
                this.vault = vault;
                this.stations = stations;
            }

            public void OnActionCall(string arg, int id)
            {
                Station station = stations[id];
                if (arg == "SA") // Sensor Activate
                {
                    station.EnterSensor();
                }
                if (arg == "SD") // Sensor Disable
                {
                    station.ExitSensor();
                }

                if (arg.StartsWith("B") && arg.Length == 2) // Buttons
                {
                    int btnId = int.Parse(arg.Substring(1));
                    station.ButtonPress(btnId);
                }
            }
            public void SaveUserMoney(IMyProgrammableBlock Me)
            {
                string savetext = "";
                foreach (var x in userMoney.Select((Entry, Index) => new { Entry, Index }))
                {
                    savetext += x.Entry.Key + ":" + x.Entry.Value + "\n";
                }
                Me.CustomData = savetext;
            }

            public void LoadUserMoney(IMyProgrammableBlock Me)
            {
                string saveText = Me.CustomData;

                string[] rows = saveText.Split('\n');
                userMoney = new Dictionary<string, int>();

                if (saveText != "")
                {
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (rows[i].Contains(":"))
                        {
                            string[] entry = rows[i].Split(':');

                            userMoney.Add(entry[0], int.Parse(entry[1]));
                        }
                    }
                }
                UpdateBankMonitor();
            }

            public void UpdateBankMonitor()
            {
                bankMonitor.WriteText("User Infos:\n", false);
                foreach (var x in userMoney.Select((Entry, Index) => new { Entry, Index }))
                {
                    bankMonitor.WriteText(x.Entry.Key + ":" + x.Entry.Value+"\n", true);
                }
            }

            public int GetMoneyForPlayer(string username)
            {
                int value;
                if (!userMoney.TryGetValue(username, out value))
                {
                    value = 0;
                    userMoney.Add(username, 0);
                    UpdateBankMonitor();
                }
                return value;
            }

            public void AddMoneyForPlayer(string username, int amount)
            {
                if (userMoney.ContainsKey(username))
                {
                    userMoney[username] += amount;
                }
                else
                {
                    userMoney.Add(username, amount);
                }
                UpdateBankMonitor();
            }

            public void UpdateTick()
            {
                for (int i = 0; i < stations.Length; i++)
                {
                        stations[i].UpdateTick();
                }
            }

            public class Station
            {
                private IMySensorBlock sensor;
                public string currentUser;
                private IMyTextSurface display;
                public IMyCargoContainer cargo;
                private IMyShipConnector connector;
                private IMyTextSurfaceProvider buttonPanel;
                private int retrieveAmountIndex = 0;

                public Station(IMyBlockGroup blockGroup)
                {
                    List<IMyTextSurface> screens = new List<IMyTextSurface>();
                    blockGroup.GetBlocksOfType(screens);

                    if (screens.Count > 0)
                    {
                        display = screens[0];

                        display.ContentType = ContentType.SCRIPT;
                        display.ScriptForegroundColor = Color.White;
                        display.ScriptBackgroundColor = Color.Black;
                        display.Script = "None";
                    }
                    else
                    {
                        program.Echo("Bank: " + blockGroup.Name + " No Screen Found");
                    }


                    List<IMyButtonPanel> buttonPanels = new List<IMyButtonPanel>();
                    blockGroup.GetBlocksOfType(buttonPanels);

                    if (buttonPanels.Count > 0)
                    {
                        buttonPanel = (IMyTextSurfaceProvider)buttonPanels[0];

                        for (int i = 0; i < 4; i++)
                        {
                            IMyTextSurface surface = buttonPanel.GetSurface(i);
                            surface.ContentType = ContentType.TEXT_AND_IMAGE;
                            surface.FontColor = Color.White;
                            surface.BackgroundColor = Color.Black;
                            surface.FontSize = 5;
                            surface.Alignment = TextAlignment.CENTER;
                        }
                    }
                    else
                    {
                        program.Echo("Bank: " + blockGroup.Name + " No ButtonPannel Found");
                    }

                    List<IMySensorBlock> sensors = new List<IMySensorBlock>();
                    blockGroup.GetBlocksOfType(sensors);
                    if (sensors.Count > 0)
                    {
                        sensor = sensors[0];
                    }
                    else
                    {
                        program.Echo("Bank: " + blockGroup.Name + " No Sensor Found");
                    }

                    List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
                    blockGroup.GetBlocksOfType(cargos);
                    if (cargos.Count > 0)
                    {
                        cargo = cargos[0];
                    }
                    else
                    {
                        program.Echo("Bank: " + blockGroup.Name + " No CargoContainer Found");
                    }

                    List<IMyShipConnector> connectors = new List<IMyShipConnector>();
                    blockGroup.GetBlocksOfType(connectors);
                    if (connectors.Count > 0)
                    {
                        connector = connectors[0];
                    }
                    else
                    {
                        program.Echo("Bank: " + blockGroup.Name + " No Connector Found");
                    }
                }

                public void EnterSensor()
                {
                    retrieveAmountIndex = 0;
                    currentUser = Util.GetUserNameFromSensor(sensor);
                    UpdateDisplay();
                }

                public void ExitSensor()
                {
                    currentUser = "";
                    UpdateDisplay();
                }
                bool tick = false;
                public void UpdateDisplay()
                {
                    MySpriteDrawFrame frame = display.DrawFrame();
                    frame.Add(new MySprite());

                    if (tick)
                    {
                        frame.Add(new MySprite());
                        frame.Add(new MySprite());
                        frame.Add(new MySprite());
                        frame.Add(new MySprite());
                    }
                    tick = !tick;

                    if(currentUser != "")
                    {
                        Util.DrawText(ref frame, 512 / 2, 100, currentUser.ToString(), Color.White);

                        Util.DrawText(ref frame, 512 / 2, 150, string.Format("{0,15:N0}$", singleton.GetMoneyForPlayer(currentUser)), Color.White);
                        Util.DrawText(ref frame, 512 / 2, 250, string.Format("{0,15:N0}$", moneyStepValues[retrieveAmountIndex]), Color.White);

                        buttonPanel.GetSurface(0).WriteText("-");
                        buttonPanel.GetSurface(0).FontSize = 10;
                        buttonPanel.GetSurface(0).FontColor = (retrieveAmountIndex > 0) ? Color.Green : Color.Red;
                        buttonPanel.GetSurface(1).WriteText("+");
                        buttonPanel.GetSurface(1).FontSize = 10;
                        buttonPanel.GetSurface(1).FontColor = (retrieveAmountIndex < moneyStepValues.Length - 1 && moneyStepValues[retrieveAmountIndex + 1] <= singleton.GetMoneyForPlayer(currentUser)) ? Color.Green : Color.Red;
                        buttonPanel.GetSurface(2).WriteText("");
                        buttonPanel.GetSurface(3).WriteText("Abheben");
                        buttonPanel.GetSurface(3).FontColor = Color.Green;
                    }
                    else
                    {
                        buttonPanel.GetSurface(0).WriteText("");
                        buttonPanel.GetSurface(1).WriteText("");
                        buttonPanel.GetSurface(2).WriteText("");
                        buttonPanel.GetSurface(3).WriteText("");
                    }

                    frame.Dispose();
                }

                public void ButtonPress(int btnId)
                {
                    if (currentUser != null && currentUser != "")
                    {
                        switch (btnId)
                        {
                            case 0:
                                if (retrieveAmountIndex > 0)
                                    retrieveAmountIndex--;
                                break;
                            case 1:
                                if (retrieveAmountIndex < moneyStepValues.Length - 1)
                                    if (moneyStepValues[retrieveAmountIndex+1] <= singleton.GetMoneyForPlayer(currentUser))
                                        retrieveAmountIndex++;
                                break;
                            case 3:
                                if(moneyStepValues[retrieveAmountIndex]<=singleton.GetMoneyForPlayer(currentUser))
                                Withdraw(moneyStepValues[retrieveAmountIndex]);
                                break;
                            default:
                                break;
                        }
                        UpdateDisplay();
                    }
                }

                public void Withdraw(int amount)
                {
                    singleton.AddMoneyForPlayer(currentUser, -amount);
                    singleton.vault.GetInventory().TransferItemTo(connector.GetInventory(), 0, 0, true, amount);
                }

                public void UpdateTick()
                {
                    if (currentUser!=null && currentUser != "")
                    {
                        MyInventoryItem? nullitem = cargo.GetInventory().GetItemAt(0);
                        if (nullitem.HasValue)
                        {
                            MyInventoryItem item = nullitem.GetValueOrDefault();
                            if (item.Type.SubtypeId == "SpaceCredit")
                            {
                                cargo.GetInventory().TransferItemTo(singleton.vault.GetInventory(), item);
                                singleton.AddMoneyForPlayer(currentUser, item.Amount.ToIntSafe());
                                UpdateDisplay();
                            }
                        }
                    }
                }
            }
        }

        class BlackJack {

            Player[] players;
            IMyTextSurface display;
            public GameState state;
            public List<PlayingCard> gameCards;
            public List<PlayingCard> dealerCards;
            List<Player> playingPlayers = new List<Player>();
            int currentPlayer = -1;
            int[] winAmounts = new int[3];

            public DelayedAction action;

            public enum DelayedAction 
            { 
            none,
            player_pickCard,
            player_doubleDown,
            player_holding,
            player_split,
            dealer_playing
            }

            public enum GameState
            {
                idle,
                playing,
                finished
            }

            public BlackJack(IMyTextSurface display, params Player[] stations) {
                this.players = stations;
                this.display = display;

                gameCards = GetNewDeck();
                state = GameState.idle;
                UpdateDisplay();
            }

            void UpdateDisplay()
            {
                display.ContentType = ContentType.SCRIPT;
                display.ScriptForegroundColor = Color.White;
                display.ScriptBackgroundColor = Color.Black;
                display.Script = "None";

                MySpriteDrawFrame frame = display.DrawFrame();

                for (int i = 0; i < players.Length; i++)
                {
                    int x = i * (512 / 3) + 5;
                    int y = 250;
                    int width = (512 / 3) - 10;
                    int height = 150;

                    players[i].DrawMainScreen(ref frame, x, y, width, height);

                    if (winAmounts[i] != 0)
                    {
                        Util.DrawText(ref frame, x+50, y-25, string.Format("{0,15:N0}$", winAmounts[i]), Color.Green);
                    }
                }

                Util.DrawText(ref frame, 512 / 2, 100, state.ToString(), Color.Yellow);

                //Random r = new Random();
                //for (int i = 0; i < 10; i++)
                //{
                //    int id = r.Next(0, gameCards.Count);
                //    gameCards[id].Display(ref frame, 25 * i + 20, 200);
                //}


                if (dealerCards != null)
                {
                    for (int i = 0; i < dealerCards.Count; i++)
                    {
                        dealerCards[i].Display(ref frame, 512 / 2 - dealerCards.Count * 12 + i * 25, 150);
                    }

                    if (dealerCards.Count > 0)
                    {
                        Util.DrawText(ref frame, 512 / 2, 200, GetValueFromHand(dealerCards).ToString(), Color.White);
                    }
                }

                frame.Dispose();
            }

            public void OnActionCall(string arg, int id)
            {
                Player player = players[id];
                if (arg == "SA") // Sensor Activate
                {
                    if (state != GameState.playing)
                    {
                        player.EnterSensor();
                    }
                }
                if (arg == "SD") // Sensor Disable
                {
                    player.ExitSensor();
                    if (state == GameState.playing)
                    {
                        playingPlayers.Remove(player);
                    }

                    winAmounts[id] = 0;
                    CheckForEverybodyLeft();
                }
                if (arg.StartsWith("B") && arg.Length == 2) // Buttons
                {
                    int btnId = int.Parse(arg.Substring(1));

                    winAmounts[id] = 0;
                    switch (player.state)
                    {
                        case Player.PlayerState.selectingBet:
                            player.SelectBet(btnId);

                            break;
                        case Player.PlayerState.playing_choosing:
                            if (playingPlayers.Contains(player))
                            {
                                CurrentPlayerChoice(btnId);
                            }
                            break;
                        default:
                            break;
                    }
                }

                UpdateDisplay();
                CheckForPlayersReady();
            }

            public void CheckForEverybodyLeft()
            {
                if (playingPlayers.Count == 0)
                {
                    if (state == GameState.playing)
                    {
                        state = GameState.idle;
                    }
                    dealerCards = new List<PlayingCard>();
                }

                if (state == GameState.idle)
                {
                    dealerCards = new List<PlayingCard>();
                }
            }

            public void CheckForPlayersReady()
            {
                if (state == GameState.idle)
                {
                    int readyPlayerCount = 0;
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i].state != Player.PlayerState.empty)
                        {
                            if (players[i].state == Player.PlayerState.selectingBet)
                            {
                                return;
                            }
                            if (players[i].state == Player.PlayerState.playing_idle)
                            {
                                readyPlayerCount++;
                            }
                        }
                    }
                    if (readyPlayerCount > 0)
                    {
                        StartGame();
                    }
                }
            }

            private void StartGame()
            {
                state = GameState.playing;

                playingPlayers = new List<Player>();
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].state == Player.PlayerState.playing_idle)
                    {
                        playingPlayers.Add(players[i]);
                    }
                }

                gameCards = GetNewDeck();
                dealerCards = new List<PlayingCard>();

                foreach (Player p in playingPlayers)
                {
                    p.handCards = new List<PlayingCard>();
                    p.handCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                    p.handCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                }

                dealerCards.Add(RemoveRandomCardFromDeck(ref gameCards));

                currentPlayer = 0;
                GameRound();
            }

            private void GameRound()
            {
                UpdateDisplay();

                Player p = playingPlayers[currentPlayer];
                if (p.state == Player.PlayerState.playing_idle)
                {
                    p.EnableChoice();
                }
                if (p.state == Player.PlayerState.playing_holding)
                {
                    currentPlayer++;
                    if (currentPlayer > playingPlayers.Count - 1)
                    {
                        currentPlayer = 0;
                    }

                    GameRound();
                }
            }

            private void CurrentPlayerChoice(int id)
            {
                Player p = playingPlayers[currentPlayer];
                p.DisableChoice();

                switch (id)
                {
                    case 0: //Card
                        action = DelayedAction.player_pickCard;
                        delayedCooldown = 1;
                        break;
                    case 1: // Hold
                        p.state = Player.PlayerState.playing_holding;
                        action = DelayedAction.player_holding;
                        delayedCooldown = 1;
                        break;
                    case 2: // Split

                        break;
                    case 3: // Double Down
                        action = DelayedAction.player_doubleDown;
                        delayedCooldown = 1;
                        break;
                    default:
                        break;
                }
            }

            private bool CheckForEveryPlayerFinished()
            {
                foreach (Player p in playingPlayers)
                {
                    if (p.state != Player.PlayerState.playing_holding)
                    {
                        return false;
                    }
                }

                return true;
            }

            int delayedCooldown = 10;
            public void ExecuteDaelayedAction()
            {
                if (action != DelayedAction.none)
                {
                    if (delayedCooldown > 0)
                    {
                        delayedCooldown--;
                    }
                    else
                    {
                        delayedCooldown = 10;

                        switch (action)
                        {
                            case DelayedAction.player_pickCard:
                                playingPlayers[currentPlayer].handCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                                break;
                            case DelayedAction.player_doubleDown:
                                playingPlayers[currentPlayer].handCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                                playingPlayers[currentPlayer].state = Player.PlayerState.playing_holding;
                                break;
                            case DelayedAction.player_holding:
                                playingPlayers[currentPlayer].state = Player.PlayerState.playing_holding;
                                break;
                            case DelayedAction.dealer_playing:
                                DealerPlaying();
                                break;
                        }

                        if (GetValueFromHand(playingPlayers[currentPlayer].handCards) >= 21)
                        {
                            playingPlayers[currentPlayer].state = Player.PlayerState.playing_holding;
                        }

                        action = DelayedAction.none;

                        if (CheckForEveryPlayerFinished())
                        {
                            UpdateDisplay();
                            action = DelayedAction.dealer_playing;
                            DealerStartPlaying();
                        }
                        else
                        {
                            currentPlayer++;
                            if (currentPlayer > playingPlayers.Count - 1)
                            {
                                currentPlayer = 0;
                            }

                            GameRound();
                        }
                    }
                }
            }

            int highestPlayerValue;
            private void DealerStartPlaying()
            {
                highestPlayerValue = -1;

                for (int i = 0; i < playingPlayers.Count; i++)
                {
                    int newVal = GetValueFromHand(playingPlayers[i].handCards);
                    if (newVal > highestPlayerValue && newVal <= 21)
                    {
                        highestPlayerValue = newVal;
                    }
                }
            }

            private void DealerPlaying()
            {
                if (highestPlayerValue == -1)
                { // every Player over 21
                    for (int i = 0; i < 3; i++)
                    {
                        winAmounts[i] = 0;
                    }
                    state = GameState.idle;
                }
                else 
                {
                    int dealerValue = GetValueFromHand(dealerCards);

                    if (dealerValue < 21 && (dealerValue <= highestPlayerValue && dealerValue < 19))
                    {
                        dealerCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (players[i].state == Player.PlayerState.playing_holding)
                            {
                                int playerValue = GetValueFromHand(players[i].handCards);
                                if (playerValue > dealerValue || (dealerValue > 21 && playerValue <= 21))
                                {
                                    winAmounts[i] = players[i].betValue * 2;
                                }
                                if (playerValue == dealerValue && playerValue < 21)
                                {
                                    winAmounts[i] = players[i].betValue;
                                }
                                if (playerValue < dealerValue && dealerValue <= 21)
                                {
                                    winAmounts[i] = 0;
                                }
                            }
                            else
                            {
                                winAmounts[i] = 0;
                            }

                            Bank.singleton.AddMoneyForPlayer(players[i].currentUser, winAmounts[i]);
                        }

                        state = GameState.idle;
                    }
                }

                if (state == GameState.idle)
                {
                    action = DelayedAction.none;

                    for (int i = 0; i < playingPlayers.Count; i++)
                    {
                        playingPlayers[i].EnterSensor();
                    }
                }

                UpdateDisplay();
            }

            public class Player
            {
                public IMyTextSurfaceProvider buttonPanel;
                public IMySensorBlock sensor;
                public string currentUser = "";
                private int betIndex = 0;

                public bool doubleDown = false;
                public int betValue { get { return moneyStepValues[betIndex]; } }

                public List<PlayingCard> handCards;

                public PlayerState state;

                public enum PlayerState {
                    empty,
                    selectingBet,
                    playing_idle,
                    playing_choosing,
                    playing_holding
                }
                public Player(IMyBlockGroup blockGroup)
                {
                    List<IMyButtonPanel> buttonPanels = new List<IMyButtonPanel>();
                    blockGroup.GetBlocksOfType(buttonPanels);

                    if (buttonPanels.Count > 0)
                    {
                        buttonPanel = (IMyTextSurfaceProvider)buttonPanels[0];

                        for (int i = 0; i < 4; i++)
                        {
                            IMyTextSurface surface = buttonPanel.GetSurface(i);
                            surface.ContentType = ContentType.TEXT_AND_IMAGE;
                            surface.FontColor = Color.White;
                            surface.BackgroundColor = Color.Black;
                            surface.FontSize = 5;
                            surface.Alignment = TextAlignment.CENTER;

                        }
                    }

                    List<IMySensorBlock> sensors = new List<IMySensorBlock>();
                    blockGroup.GetBlocksOfType(sensors);
                    if (sensors.Count > 0)
                        sensor = sensors[0];

                    state = PlayerState.empty;

                    UpdateButtonPannel();
                }

                public void EnterSensor()
                {
                    currentUser = Util.GetUserNameFromSensor(sensor);

                    int playerMoney = Bank.singleton.GetMoneyForPlayer(currentUser);
                    while (betValue > playerMoney)
                    {
                        betIndex--;
                        if (betIndex <= 0)
                        {
                            betIndex = 0;
                            break;
                        }
                    }

                    handCards = new List<PlayingCard>();
                    state = PlayerState.selectingBet;
                    UpdateButtonPannel();
                }

                public void ExitSensor()
                {
                    betIndex = 0;
                    state = PlayerState.empty;
                    currentUser = "";
                    handCards = new List<PlayingCard>();
                    UpdateButtonPannel();
                }

                public void SelectBet(int i)
                {
                    if (i == 0 && betIndex > 0)
                        betIndex--;
                    if (i == 1 && betIndex < moneyStepValues.Length - 1)
                        if(Bank.singleton.GetMoneyForPlayer(currentUser)>=moneyStepValues[betIndex+1])
                            betIndex++;
                    if (i == 3)
                        SetBet();
                    UpdateButtonPannel();
                }

                public bool CanSplit()
                {
                    for (int ha = 0; ha < handCards.Count-1; ha++)
                    {
                        for (int hb = ha+1; hb < handCards.Count; hb++)
                        {
                            if (handCards[ha].suit == handCards[hb].suit && handCards[ha].value == handCards[hb].value)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }

                private void SetBet()
                {
                    if (Bank.singleton.GetMoneyForPlayer(currentUser) >= betValue)
                    {
                        Bank.singleton.AddMoneyForPlayer(currentUser, -betValue);
                        state = PlayerState.playing_idle;
                        handCards = new List<PlayingCard>();
                        UpdateButtonPannel();
                    }
                }

                public void UpdateButtonPannel() {
                    switch (state)
                    {
                        case PlayerState.empty:
                            for (int i = 0; i < 4; i++)
                            {
                                buttonPanel.GetSurface(i).WriteText("");
                            }
                            break;
                        case PlayerState.selectingBet:
                            buttonPanel.GetSurface(0).WriteText("-");
                            buttonPanel.GetSurface(0).FontSize = 10;
                            buttonPanel.GetSurface(0).FontColor = (betIndex > 0) ? Color.Green : Color.Red;
                            buttonPanel.GetSurface(1).WriteText("+");
                            buttonPanel.GetSurface(1).FontSize = 10;
                            buttonPanel.GetSurface(1).FontColor = (betIndex < moneyStepValues.Length - 1) ? Color.Green : Color.Red;
                            buttonPanel.GetSurface(2).WriteText("");
                            buttonPanel.GetSurface(3).WriteText("Setzen");
                            buttonPanel.GetSurface(3).FontColor = Color.Green;
                            break;
                        case PlayerState.playing_idle:
                            buttonPanel.GetSurface(0).WriteText("");
                            buttonPanel.GetSurface(1).WriteText("");
                            buttonPanel.GetSurface(2).WriteText("");
                            buttonPanel.GetSurface(3).WriteText("");
                            break;
                        case PlayerState.playing_choosing:
                            buttonPanel.GetSurface(0).WriteText("Karte");
                            buttonPanel.GetSurface(0).FontColor = Color.Green;
                            buttonPanel.GetSurface(0).FontSize = 5;
                            buttonPanel.GetSurface(1).WriteText("Halten");
                            buttonPanel.GetSurface(1).FontColor = Color.White;
                            buttonPanel.GetSurface(1).FontSize = 5;
                            //buttonPanel.GetSurface(2).WriteText("Split");
                            //buttonPanel.GetSurface(2).FontColor = Color.Aqua;
                            buttonPanel.GetSurface(3).WriteText("Double Down");
                            buttonPanel.GetSurface(3).FontColor = Color.Yellow;
                            break;
                    }
                }

                public void DrawMainScreen(ref MySpriteDrawFrame frame, int x, int y, int width, int height)
                {

                    Color backgroundColor = (state == PlayerState.empty) ? new Color(2, 2, 2) : new Color(20, 20, 20);

                    Util.DrawCube(ref frame, x, y, width, height, backgroundColor);
                    //Util.DrawText(ref frame, x + width / 2, y, currentUser, Color.White);

                    Util.DrawText(ref frame, x + width / 2, y, state.ToString(), Color.White);

                    if (state != PlayerState.empty)
                        Util.DrawText(ref frame, x + width / 2, y + height - 20, string.Format("{0,15:N0}$", betValue), (state == PlayerState.selectingBet) ? Color.Green : Color.White, 0.5F);

                    if (handCards!=null)
                    {
                        for (int i = 0; i < handCards.Count; i++)
                        {
                            handCards[i].Display(ref frame, x + width/2 - handCards.Count* 12 + i*25, y + height / 2-25);
                        }

                        if (handCards.Count > 0)
                        {
                            Util.DrawText(ref frame, x + width / 2, y + height / 2 + 25, GetValueFromHand(handCards).ToString(), Color.White);
                        }
                    }
                }

                public void EnableChoice()
                {
                    state = PlayerState.playing_choosing;
                    UpdateButtonPannel();
                }
                public void DisableChoice()
                {
                    state = PlayerState.playing_idle;
                    UpdateButtonPannel();
                }
            }

            public class PlayingCard {
                public int value; // Ace,1,....,9,10,Jack,Queen,King     nummer einen höher
                public int suit; // Spades, Diamonds, Clubs, Hearts

                public PlayingCard(int v, int s)
                {
                    value = v;
                    suit = s;
                }

                private string GetSuitString() {
                    switch (suit)
                    {
                        case 0:
                            return "♠";
                        case 1:
                            return "♦";
                        case 2:
                            return "♣";
                        case 3:
                            return "♥";
                        default:
                            return "ERROR";
                    }
                }
                private string GetValueString()
                {
                    switch (value)
                    {
                        case 0:
                            return "A";
                        case 10:
                            return "Q";
                        case 11:
                            return "K";
                        case 12:
                            return "J";
                        default:
                            return (value + 1).ToString();
                    }
                }

                public void Display(ref MySpriteDrawFrame frame, int x, int y) {

                    Color c = (suit == 1 || suit == 3)?Color.Red:Color.Black;

                    MySprite Background = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Color = new Color(50,50,50),
                        Size = new Vector2(20, 40),
                        Alignment = TextAlignment.CENTER,
                        Position = new Vector2(x + 10, y + 20)
                    };

                    MySprite GUIValue = new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = GetValueString(),
                        Position = new Vector2(x+11, y - 2),
                        RotationOrScale = 0.8F,
                        Color = c,
                        Alignment = TextAlignment.CENTER,
                        FontId = "White",
                    };

                    MySprite GUISuit = new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = GetSuitString(),
                        Position = new Vector2(x+11, y + 13),
                        RotationOrScale = 1,
                        Color = c,
                        Alignment = TextAlignment.CENTER,
                        FontId = "Monospace",
                    };

                    frame.Add(Background);
                    frame.Add(GUIValue);
                    frame.Add(GUISuit);
                }
            }

            public static PlayingCard RemoveRandomCardFromDeck(ref List<PlayingCard> cards)
            {
                Random random = new Random();
                int index = random.Next(cards.Count);
                PlayingCard card = cards[index];
                cards.RemoveAt(index);
                return card;
            }

            public static List<PlayingCard> GetNewDeck()
            {
                List<PlayingCard> cards = new List<PlayingCard>();
                for (int s = 0; s < 4; s++)
                {
                    for (int v = 0; v < 13; v++)
                    {
                        cards.Add(new PlayingCard(v, s));
                    }
                }
                return cards;
            }

            private static int GetValueFromHand(List<PlayingCard> cards) {
                int value = 0;
                int aceCount = 0;

                for (int i = 0; i < cards.Count; i++)
                {
                    int cardVal = cards[i].value + 1;
                    if (cardVal >= 10)
                    {
                        value += 10;
                    }
                    else
                    {
                        if (cardVal == 0)
                        {
                            aceCount++;
                        }
                        else
                        {
                            value += cardVal;
                        }
                    }
                }

                for (int i = 0; i < aceCount; i++)
                {
                    if (value + 11 + aceCount-1 > 21)
                    {
                        value += 1;
                    }
                    else
                    {
                        value += 11;
                    }
                }

                return value;
            }
        }

        class Util
        {
            public static void DrawText(ref MySpriteDrawFrame frame, int x, int y, string text, Color c, float textSize = 1)
            {
                MySprite sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = text,
                    Position = new Vector2(x, y),
                    RotationOrScale = textSize,
                    Color = c,
                    Alignment = TextAlignment.CENTER,
                    FontId = "White",
                };
                frame.Add(sprite);
            }
            public static void DrawCube(ref MySpriteDrawFrame frame, int x, int y, int width, int height, Color c)
            {
                MySprite Sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Color = c,
                    Size = new Vector2(width, height),
                    Alignment = TextAlignment.CENTER,
                    Position = new Vector2(x + width / 2, y + height / 2)
                };
                frame.Add(Sprite);
            }

            public static string GetUserNameFromSensor(IMySensorBlock sensor)
            {
                List<MyDetectedEntityInfo> entityInfos = new List<MyDetectedEntityInfo>();
                sensor.DetectedEntities(entityInfos);

                foreach (MyDetectedEntityInfo info in entityInfos)
                {
                    if (info.Type == MyDetectedEntityType.CharacterHuman)
                    {
                        return info.Name;
                    }
                }

                return "null";
            }
        }
    }
}
