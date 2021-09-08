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

            Player[] players;
            IMyTextSurface display;
            public GameState state;
            public List<PlayingCard> gameCards;
            public List<PlayingCard> dealerCards;
            List<Player> playingPlayers;
            int currentPlayer = -1;


            public enum GameState
            {
                idle,
                waiting_for_bets,
                playing
            }

            public BlackJack(Player[] stations, IMyTextSurface display) {
                this.players = stations;
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

                for (int i = 0; i < players.Length; i++)
                {
                    int x = i * (512 / 3) + 5;
                    int y = 250;
                    int width = (512 / 3) - 10;
                    int height = 150;

                    players[i].DrawMainScreen(ref frame, x, y, width, height);
                }

                Util.DrawText(ref frame, 512 / 2, 100, state.ToString(), Color.Yellow);

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
                }
                if (arg.StartsWith("B") && arg.Length == 2) // Buttons
                {
                    int btnId = int.Parse(arg.Substring(1));
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


            public void CheckForPlayersReady()
            {
                if (state == GameState.waiting_for_bets)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i].state != Player.PlayerState.playing_idle) {
                            return;
                        }
                    }
                    StartGame();
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
                    CheckForEveryPlayerFinished();
                    currentPlayer++;
                    if (currentPlayer > playingPlayers.Count - 1)
                    {
                        currentPlayer = 0;
                    }
                }
            }

            private void CurrentPlayerChoice(int id)
            {
                Player p = playingPlayers[currentPlayer];
                p.DisableChoice();

                switch (id)
                {
                    case 0: //Card
                        p.handCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                        break;
                    case 1: // Hold
                        p.state = Player.PlayerState.playing_holding;
                        break;
                    case 2: // Split

                        break;
                    case 3: // Double Down
                        p.handCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                        p.state = Player.PlayerState.playing_holding;
                        break;
                    default:
                        break;
                }

                CheckForEveryPlayerFinished();
                currentPlayer++;
                if (currentPlayer > playingPlayers.Count - 1)
                {
                    currentPlayer = 0;
                }

                GameRound();
            }

            private void CheckForEveryPlayerFinished()
            {
                foreach (Player p in playingPlayers)
                {
                    if (p.state != Player.PlayerState.playing_holding) {
                        return;
                    }
                }

                UpdateDisplay();
                GameFinished();
            }

            private void GameFinished()
            {
                dealerCards.Add(RemoveRandomCardFromDeck(ref gameCards));
                UpdateDisplay();
            }

            public class Player
            {
                public IMyTextSurfaceProvider buttonPanel;
                public IMySensorBlock sensor;
                public string currentUser = "";
                private int betIndex = 0;

                public bool doubleDown = false;
                public int betValue { get { return betValues[betIndex]; } }

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
                    handCards = new List<PlayingCard>();
                    UpdateButtonPannel();
                }

                public void SelectBet(int i)
                {
                    if (i == 0 && betIndex > 0)
                        betIndex--;
                    if (i == 1 && betIndex < betValues.Length - 1)
                        betIndex++;
                    if (i == 3)
                        SetBet();
                    UpdateButtonPannel();
                }

                public bool CanSplit() {
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
                    state = PlayerState.playing_idle;
                    handCards = new List<PlayingCard>();
                    UpdateButtonPannel();
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
                        case PlayerState.playing_choosing:
                            buttonPanel.GetSurface(0).WriteText("Karte");
                            buttonPanel.GetSurface(3).FontColor = Color.White;
                            buttonPanel.GetSurface(1).WriteText("Halten");
                            buttonPanel.GetSurface(3).FontColor = Color.White;
                            buttonPanel.GetSurface(2).WriteText("Split");
                            buttonPanel.GetSurface(3).FontColor = Color.Red;
                            buttonPanel.GetSurface(3).WriteText("Double Down");
                            buttonPanel.GetSurface(3).FontColor = Color.Aqua;
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
                            handCards[i].Display(ref frame, x + width/2 - handCards.Count* 12 + i*25, y + height / 2);
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
