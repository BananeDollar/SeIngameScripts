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

        public Program()
        {
            IMyTextSurface lcd = GridTerminalSystem.GetBlockWithName("Monitor") as IMyTextSurface;

            blackJack = new BlackJack(new BlackJack.Player[] {
            new BlackJack.Player(GridTerminalSystem.GetBlockGroupWithName("BJ0")),
            new BlackJack.Player(GridTerminalSystem.GetBlockGroupWithName("BJ1")),
            new BlackJack.Player(GridTerminalSystem.GetBlockGroupWithName("BJ2"))
            },
            lcd
            );

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Arg: " + argument);
            string[] args = argument.Split('-');
            if (args.Length == 3)
            {
                int id = int.Parse(args[1]);

                switch (args[0])
                {
                    case "BJ":
                        blackJack.OnActionCall(args[2], id);
                        break;
                    default:
                        Echo("Wrong argument!");
                        break;
                }
            }
        }

        class BlackJack {

            public static int[] betValues = new int[] { 50, 100, 150, 200, 250, 500, 1000, 1500, 2000, 2500, 5000, 10000, 15000, 20000, 25000, 50000, 100000, 150000, 200000, 250000, 500000, 1000000, 1500000, 2000000, 2500000, 5000000, 10000000 };

            Player[] stations;
            IMyTextSurface display;
            public GameState state;
            public List<PlayingCard> gameCards;
            public List<PlayingCard> dealerCards;

            public enum GameState
            {
                idle,
                waiting_for_bets,
                round_start
            }

            public BlackJack(Player[] stations, IMyTextSurface display) {
                this.stations = stations;
                this.display = display;

                gameCards = GetNewDeck();

                UpdateDisplay();
            }

            void UpdateDisplay()
            {
                display.ContentType = ContentType.SCRIPT;
                display.ScriptForegroundColor = Color.White;
                display.ScriptBackgroundColor = Color.Black;
                display.Script = "None";

                MySpriteDrawFrame frame = display.DrawFrame();

                for (int i = 0; i < stations.Length; i++)
                {
                    int x = i * (512 / 3) + 5;
                    int y = 250;
                    int width = (512 / 3) - 10;
                    int height = 150;

                    stations[i].DrawMainScreen(ref frame, x, y, width, height);
                }

                if (!CheckForPlayersReady()) {
                    Util.DrawText(ref frame, 512 / 2, 100, "Warte auf Wetteinsätze", Color.Yellow);
                }

                //Random r = new Random();
                //for (int i = 0; i < 10; i++)
                //{
                //    int id = r.Next(0, gameCards.Count);
                //    gameCards[id].Display(ref frame, 25 * i + 20, 200);
                //}

                frame.Dispose();
            }

            public void OnActionCall(string arg, int id)
            {
                if (arg == "SA") // Sensor Activate
                {
                    stations[id].EnterSensor();
                }
                if (arg == "SD") // Sensor Disable
                {
                    stations[id].ExitSensor();
                }
                if (arg.StartsWith("B") && arg.Length == 2) // Buttons
                {
                    int btnId = int.Parse(arg.Substring(1));
                    stations[id].ButtonPress(btnId);
                }

                UpdateDisplay();

                if (CheckForPlayersReady())
                {
                    StartGame();
                }
            }

            public bool CheckForPlayersReady() {
                for (int i = 0; i < stations.Length; i++)
                {
                    if (stations[i].state == Player.PlayerState.selectingBet)
                    {
                        return false;
                    }
                }

                return true;
            }

            public void StartGame()
            {
                gameCards = GetNewDeck();
            }

            public void Round()
            { 

            }

            public class Player
            {
                public IMyTextSurfaceProvider buttonPanel;
                public IMySensorBlock sensor;
                public string currentUser = "";
                private int betIndex = 0;
                public int betValue { get { return betValues[betIndex]; } }

                public List<PlayingCard> handCards;

                public PlayerState state;

                public enum PlayerState {
                    selectingBet,
                    playing_idle,
                    empty
                }
                public Player(IMyBlockGroup blockGroup)
                {
                    List<IMyTextSurfaceProvider> buttonPanels = new List<IMyTextSurfaceProvider>();
                    blockGroup.GetBlocksOfType(buttonPanels);

                    if (buttonPanels.Count > 0)
                    {
                        buttonPanel = buttonPanels[0];

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
                    state = PlayerState.selectingBet;
                    currentUser = Util.GetUserNameFromSensor(sensor);
                    betIndex = 0;
                    UpdateButtonPannel();
                }

                public void ExitSensor()
                {
                    state = PlayerState.empty;
                    currentUser = "";
                    UpdateButtonPannel();
                }

                public void ButtonPress(int i) {
                    switch (state)
                    {
                        case PlayerState.selectingBet:
                            if (i == 0 && betIndex > 0)
                                betIndex--;
                            if (i == 1 && betIndex < betValues.Length - 1)
                                betIndex++;
                            if (i == 3)
                                SetBet();
                            break;
                        case PlayerState.playing_idle:

                            break;
                        case PlayerState.empty:
                            break;
                    }
                    UpdateButtonPannel();
                }

                private void SetBet()
                {
                    state = PlayerState.playing_idle;
                    UpdateButtonPannel();
                }

                private void UpdateButtonPannel() {
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
                            buttonPanel.GetSurface(0).FontColor = (betIndex > 0) ? Color.Green : Color.Red;
                            buttonPanel.GetSurface(1).WriteText("+");
                            buttonPanel.GetSurface(1).FontColor = (betIndex < betValues.Length - 1) ? Color.Green : Color.Red;
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
                    }
                }

                public void DrawMainScreen(ref MySpriteDrawFrame frame, int x, int y, int width, int height)
                {

                    Color backgroundColor = (state == PlayerState.empty) ? new Color(2, 2, 2) : new Color(20, 20, 20);

                    Util.DrawCube(ref frame, x, y, width, height, backgroundColor);
                    Util.DrawText(ref frame, x + width / 2, y, currentUser, Color.White);

                    if (state != PlayerState.empty)
                        Util.DrawText(ref frame, x + width / 2, y + height - 20, string.Format("{0,15:N0}$", betValue), (state == PlayerState.selectingBet) ? Color.Green : Color.White, 0.5F);

                    if (state == PlayerState.playing_idle)
                    {
                        for (int i = 0; i < handCards.Count; i++)
                        {
                            handCards[i].Display(ref frame, x + width/2 - handCards.Count* 12 + i*25, y + height / 2);
                        }

                        if (handCards.Count > 0)
                        {
                            Util.DrawText(ref frame, x + width / 2, y + height / 2 + 25, GetValueFromHand(handCards).ToString(), Color.White);
                        }
                    }
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


        class Util{
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
