using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using NAudio.Wave;
using System.Security.Cryptography;
using System.IO;

namespace FreakinRich
{
    public partial class FormInterface : XCoolForm.XCoolForm
    {
        enum SoundEffects { PlayerTurn, OthersTurn, Jail, ChanceWin, ChanceLost, Winner };

        // This delegate enables asynchronous calls for setting 
        // the text property on a TextBox control or others 
        delegate void SetTextCallback(string text, Control ctr);
        delegate void AddControlCallback(Control ctr2, Control ctr1);
        delegate void SetControlVisibleCallback(Boolean flag, Control ctr);
        delegate void SetControlEnabledCallback(Boolean flag, Control ctr);
        delegate void SetImageControlCallback(Bitmap bitmap, Control ctr);
        delegate void voidNoParamsCallback();
        delegate void voidIntIntCallback(int value1, int value2);
        delegate void voidCtrIntCallback(Control pbDado, int value);
        delegate void voidCtrStringCallback(Control rt_Chat, String message);

        private Server m_server = null;
        private TcpClient m_client;
        private NetworkStream m_clientStream;
		private RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private String  m_chargeRent;
        private List<int> m_playerMarkerOrder;
        private int m_chanceCard;
        private int m_clickedProperty;
        private PictureBox m_pb_Shade = new PictureBox();
        private PictureBox m_pb_Print = new PictureBox();
        private static Point m_startMarkerLocation1 = new Point(718, 605);
        private static Point m_startMarkerLocation2 = new Point(718, 633);
        private static Point m_startMarkerLocation3 = new Point(760, 633);
        private static Point m_startMarkerLocation4 = new Point(760, 605);
        private Boolean m_gameOver = false;
        private Boolean? m_chanceWin;

        private WaveOut m_soundOutput_playerTurn;
        private WaveOut m_soundOutput_othersPlayers;
        private WaveOut m_soundOutput_jail;
        private WaveOut m_soundOutput_chanceWin;
        private WaveOut m_soundOutput_chanceLost;
        private WaveOut m_soundOutput_winner;

        private WaveStream m_outStream_playerTurn;
        private WaveStream m_outStream_othersPlayers;
        private WaveStream m_outStream_jail;
        private WaveStream m_outStream_chanceWin;
        private WaveStream m_outStream_chanceLost;
        private WaveStream m_outStream_winner;
        
        private Boolean m_audioOk = true;
		private Point m_gameBoardScreenLocation;
        private List<PictureBox> m_playerList = new List<PictureBox>();

        private XmlThemeLoader xtl = new XmlThemeLoader();

        static System.Windows.Forms.Timer timer; 

        public FormInterface()
        {
            InitializeComponent();
            SetTheme();
        }

        private void FormInterface_Load(object sender, EventArgs e)
        {
            //converte sist de coordenadas da janela para o sist de coordenadas no monitor ->requisito do printScreen
            m_gameBoardScreenLocation = PointToScreen(pb_GameBoard.Location);

            Point location = new Point();
            Size size = new Size(95, 27);

            //picbox com o filtro semi-transparente 
            m_pb_Shade.Name = "pb_Shade";
            m_pb_Shade.Size = new Size(1364, 740);
            m_pb_Shade.BackgroundImageLayout = ImageLayout.Zoom;
            m_pb_Shade.BackColor = System.Drawing.Color.Transparent;
            m_pb_Shade.Visible = false;
            m_pb_Shade.BackgroundImage = Properties.Resources.filtro;
			location.X = 0;
			location.Y = -5;
			m_pb_Shade.Location = location;
            
            //picbox com printscreen 
            m_pb_Print.Name = "pb_Print";
			m_pb_Print.Size = pb_GameBoard.Size;
			m_pb_Print.Location = pb_GameBoard.Location;
            m_pb_Print.BackgroundImageLayout = ImageLayout.None;
            m_pb_Print.BackColor = System.Drawing.Color.Transparent;
            m_pb_Print.Visible = false;            
            m_pb_Print.Controls.Add(m_pb_Shade);
            
            //adiciona a picBox q tem o filtro os controlos
            m_pb_Shade.Controls.Add(pb_Info);
            m_pb_Shade.Controls.Add(pb_Card);

            //adiciona a picBox pb_Card os controlos
            pb_Card.Controls.Add(bt_Buy);
            pb_Card.Controls.Add(bt_Pass);
            pb_Card.Controls.Add(bt_Pay);
            pb_Card.Controls.Add(lb_Close);
            pb_Card.Controls.Add(bt_SellProperty);

            //adiciona a picBox GameBoard os controlos
            pb_GameBoard.Controls.Add(btDice);
            pb_GameBoard.Controls.Add(pbDado1);
            pb_GameBoard.Controls.Add(pbDado2);

            //adiciona a picBox pb_Info os controlos
            pb_Info.Controls.Add(bt_Jail);
            pb_Info.Controls.Add(bt_Double);
            pb_Info.Controls.Add(bt_SellToPayDebt);
            pb_Info.Controls.Add(bt_PayRent);
            pb_Info.Controls.Add(bt_OkInfo);
            pb_Info.Controls.Add(bt_Chance);
            pb_Info.Controls.Add(bt_ChanceCollection);
            pb_Info.Controls.Add(bt_BuyHouse);
            pb_Info.Controls.Add(bt_SellHouse);
            pb_Info.Controls.Add(lb_CloseInfo);
            pb_Info.Controls.Add(lb_Winner);

            //remove da form os controlos
            this.Controls.Add(m_pb_Print);
            this.Controls.Remove(lb_CloseInfo);
            this.Controls.Remove(pb_Info);
            this.Controls.Remove(pb_Card);
            this.Controls.Remove(m_pb_Shade);
            this.Controls.Remove(bt_Buy);
            this.Controls.Remove(bt_Pass);
            this.Controls.Remove(bt_SellToPayDebt);
            this.Controls.Remove(btDice);
            this.Controls.Remove(bt_Pay);
            this.Controls.Remove(pbDado1);
            this.Controls.Remove(pbDado2);
            this.Controls.Remove(bt_OkInfo);
            this.Controls.Remove(bt_Chance);
            this.Controls.Remove(bt_PayRent);
            this.Controls.Remove(bt_Jail);
            this.Controls.Remove(bt_Double);
            this.Controls.Remove(bt_ChanceCollection);
            this.Controls.Remove(lb_Close);
            this.Controls.Remove(bt_SellProperty);
            this.Controls.Remove(bt_BuyHouse);
            this.Controls.Remove(bt_SellHouse);
            this.Controls.Remove(lb_Winner);

            bt_Buy.Size = size;
            location.X = (int)((pb_Card.Size.Width * 0.25f) - (bt_Buy.Size.Width * 0.5f));
            location.Y = pb_Card.Size.Height - 55;
            bt_Buy.Location = location;

            bt_Pass.Size = size;
            location.X = (int)((pb_Card.Size.Width * 0.75f) - (bt_Buy.Size.Width * 0.5f));
            location.Y = pb_Card.Size.Height - 55;
            bt_Pass.Location = location;

            location.X = (int)((pb_Card.Size.Width * 0.5) - (bt_Pay.Size.Width * 0.5f));
            location.Y = pb_Card.Size.Height - 60;
            bt_Pay.Location = location;
            bt_SellProperty.Location = location;

            location.X = (int)((pb_Info.Size.Width * 0.5) - (bt_OkInfo.Size.Width * 0.5f));
            location.Y = pb_Info.Size.Height - 60;
            bt_SellToPayDebt.Location = location;
            bt_OkInfo.Location = location;
            bt_PayRent.Location = location;
            bt_Double.Location = location;
            bt_Jail.Location = location;
            bt_Chance.Location = location;
            bt_ChanceCollection.Location = location;

            location.X = (int)(pb_Info.Size.Width * 0.15);
            location.Y = (int)(pb_Info.Size.Width * 0.23);
            lb_Winner.Location = location;

            location.X = pb_Info.Size.Width - 48;
            location.Y = 23;
            lb_CloseInfo.Location = location;

            location.X = pb_Card.Size.Width - 38;
            location.Y = 9;
            lb_Close.Location = location;

            SetupPbProperty();
            SetupPb_Houses();
            m_pb_Print.BringToFront();
            m_pb_Shade.BringToFront();
            pb_Card.BringToFront();
            pb_Info.BringToFront();
            panel1.BringToFront();
            pb_GameBoard.Visible = false;

            // definir imagens para o botão de lançar dados
            btDice.NormalBackgroundImage = Properties.Resources.btdados;
            btDice.ClickBackgroundImage = Properties.Resources.btdados_on;

            InitAudio();
        }

        private void ReceiveFromServer()
        {
            while (true)
            {
                String message;
                StreamConformer.MessageType type;
                int bytesCount = 0;

                byte[] buffer = Server.GetFullMessage(m_clientStream, out bytesCount);
                System.Diagnostics.Debug.Write("msg recebida: ");
                if (bytesCount == 0)
                    break;

                type = StreamConformer.Decode(buffer, bytesCount, out message);
                System.Diagnostics.Debug.WriteLine(type + " " + message);
                switch (type)
                {
                    case StreamConformer.MessageType.PLAYERS_FULL:
                        MessageBox.Show("O jogo já possui o número máximo de jogadores permitido.");
                        Application.Exit();
                        break;
                    case StreamConformer.MessageType.UPDATE_PLAYERS_NAMES:
                        SetControlText(message, lbPlayer);
                        break;
                    case StreamConformer.MessageType.UPDATE_MONEY:
                        SetControlText(message, lb_Money);
                        break;
                    case StreamConformer.MessageType.UPDATE_PLAYERS_COUNT:
                        AddPlayerMarkers(int.Parse(message));
                        break;
                    case StreamConformer.MessageType.SET_MARKER_ORDER:
                        m_playerMarkerOrder = Server.StringToIntList(message);
                        break;
                    case StreamConformer.MessageType.ENABLE_DICE_ROLL:
                        PlayAudio(SoundEffects.PlayerTurn);
                        SetControlVisible(true, btDice);
                        break;
                    case StreamConformer.MessageType.SHOW_DICE_VALUES:
                        ShowDiceValues(message);
                        break;
                    case StreamConformer.MessageType.OTHERS_TURN:
                        PlayAudio(SoundEffects.OthersTurn);
                        break;
                    case StreamConformer.MessageType.UPDATE_CURRENT_PLAYER:
                        this.Invoke(new voidCtrIntCallback(UpdateCurrentPlayer), new object[] { pb_TurnMarker, int.Parse(message) });
                        break;
                    case StreamConformer.MessageType.UPDATE_PLAYER_POSITION:
                        UpdatePlayerBoardPosition(message);
                        break;
                    case StreamConformer.MessageType.PROPERTY_ON_SALE:
                        EnableShade();
                        SetBackgroundCard(int.Parse(message));
                        SetControlVisible(true, pb_Card);
                        SetControlVisible(true, bt_Buy);
                        SetControlVisible(true, bt_Pass);
                        break;
                    case StreamConformer.MessageType.TAX_POSITION:
                        EnableShade();
                        SetBackgroundCard(int.Parse(message));
                        SetControlVisible(true, pb_Card);
                        SetControlVisible(true, bt_Pay);
                        break;
                    case StreamConformer.MessageType.JAIL_POSITION:
                        EnableShade();
                        pb_Info.BackgroundImage = Properties.Resources.info_gotojail;
                        SetControlVisible(true, pb_Info);
                        SetControlVisible(true, bt_Jail);
                        PlayAudio(SoundEffects.Jail);
                        break;
                    case StreamConformer.MessageType.TIMES3_DOUBLE:
                        EnableShade();
                        pb_Info.BackgroundImage = Properties.Resources.info_gotojail;
                        SetControlVisible(true, pb_Info);
                        SetControlVisible(true, bt_Jail);
                        PlayAudio(SoundEffects.Jail);
                        break;
                    case StreamConformer.MessageType.PLAYER_HASNO_MONEY_2BUY:
                        EnableShade();
                        pb_Info.BackgroundImage = Properties.Resources.info_nomoney_tobuy;
                        SetControlVisible(true, pb_Info);
                        SetControlVisible(true, bt_OkInfo);
                        break;
                    case StreamConformer.MessageType.CHARGE_RENT:
                        m_chargeRent = message;
                        double value = double.Parse(message);
                        pb_Info.BackgroundImage = Properties.Resources.info_charge_rent;
                        EnableShade();
                        SetControlVisible(true, pb_Info);
                        SetControlText("Pagar " + value.ToString("0,0", CultureInfo.CreateSpecificCulture("el-GR")) + " €", bt_PayRent);
                        SetControlVisible(true, bt_PayRent);
                        break;
                    case StreamConformer.MessageType.PLAYER_HASNO_MONEY_2PAY:
                        EnableShade();
                        pb_Info.BackgroundImage = Properties.Resources.info_nomoney_topay;
                        SetControlVisible(true, pb_Info);
                        SetControlVisible(true, bt_SellToPayDebt);
                        break;
                    case StreamConformer.MessageType.UPDATE_OWNER:
                        EnableOwner(message);
                        break;
                    case StreamConformer.MessageType.IS_DOUBLE:
                        EnableShade();
                        pb_Info.BackgroundImage = Properties.Resources.info_double;
                        SetControlVisible(true, pb_Info);
                        SetControlVisible(true, bt_Double);
                        break;
                    case StreamConformer.MessageType.CHANCE_POSITION:
                        ShowTakeChanceMessage(message);
                        break;
                    case StreamConformer.MessageType.ENABLE_SELL_PROPERTY:
                        SetControlVisible(true, bt_SellProperty);
                        break;
                    case StreamConformer.MessageType.ENABLE_GROUP_HOUSES:
                        EnableHouses(message);
                        break;
					case StreamConformer.MessageType.DISABLE_GROUP_HOUSES:
						DisableHouses(message);
						break;
                    case StreamConformer.MessageType.CAN_BUY_HOUSE:
                        ShowBuySellHouseInfo(true, false, message);
                        break;
                    case StreamConformer.MessageType.CAN_SELL_HOUSE:
                        ShowBuySellHouseInfo(false, true, message);
                        break;
                    case StreamConformer.MessageType.CAN_BUY_SELL_HOUSE:
                        ShowBuySellHouseInfo(true, true, message);
                        break;
                    case StreamConformer.MessageType.PLAYER_HAS_HOUSES_2SELL:
                        EnableShade();
                        pb_Info.BackgroundImage = Properties.Resources.info_sell_houses_first;
                        pb_Info.BackgroundImage = Properties.Resources.info_takechance;
                        SetControlVisible(true, pb_Info);
                        SetControlVisible(true, lb_CloseInfo);
                        SetControlVisible(true, bt_OkInfo);
                        break;
                    case StreamConformer.MessageType.GAME_IS_OVER:
                        GameIsOver(message);
                        break;
                    case StreamConformer.MessageType.CHAT:
                        this.Invoke(new voidCtrStringCallback(ShowMessageChat), new object[] { rt_Chat, message });
                        break;
                    case StreamConformer.MessageType.PONG:
                        SetTimer();
                        break;
                    case StreamConformer.MessageType.CONNECTION_FAILED:
                        MessageBox.Show("Um jogador da partida abandonou o jogo.");
                        Application.Exit();
                        break;
                    default:
                        break;
                }
            }

            m_client.Close();
        }

        private void SetTheme()
        {

            this.IconHolder.HolderButtons.Add(new XCoolForm.XTitleBarIconHolder.XHolderButton(FreakinRich.Properties.Resources._hotel.GetThumbnailImage(20, 20, null, IntPtr.Zero)));
            this.IconHolder.HolderButtons.Add(new XCoolForm.XTitleBarIconHolder.XHolderButton(FreakinRich.Properties.Resources._house.GetThumbnailImage(20, 20, null, IntPtr.Zero)));
            this.IconHolder.HolderButtons.Add(new XCoolForm.XTitleBarIconHolder.XHolderButton(FreakinRich.Properties.Resources.regras_icon.GetThumbnailImage(20, 20, null, IntPtr.Zero)));

            this.IconHolder.HolderButtons[0].FrameBackImage = FreakinRich.Properties.Resources._hotel;
            this.IconHolder.HolderButtons[0].XHolderButtonCaption = "Novo jogo";
            this.IconHolder.HolderButtons[0].XHolderButtonDescription = "";
            this.IconHolder.HolderButtons[0].XHolderButtonCaptionFont = new Font("Comic Sans MS", 12, FontStyle.Italic);

            this.IconHolder.HolderButtons[1].FrameBackImage = FreakinRich.Properties.Resources._house;
            this.IconHolder.HolderButtons[1].XHolderButtonCaption = "Participar";
            this.IconHolder.HolderButtons[1].XHolderButtonCaptionFont = new Font("Comic Sans MS", 12, FontStyle.Italic);
            this.IconHolder.HolderButtons[1].XHolderButtonDescription = " em jogo...";
            this.IconHolder.HolderButtons[1].XHolderButtonDescriptionFont = new Font("Comic Sans MS", 9, FontStyle.Italic);

            this.IconHolder.HolderButtons[2].FrameBackImage = FreakinRich.Properties.Resources.regras_icon;
            this.IconHolder.HolderButtons[2].XHolderButtonCaption = "Regras";
            this.IconHolder.HolderButtons[2].XHolderButtonDescription = "";
            this.IconHolder.HolderButtons[2].XHolderButtonCaptionFont = new Font("Comic Sans MS", 12, FontStyle.Italic);

            this.XCoolFormHolderButtonClick += new XCoolFormHolderButtonClickHandler(XCoolFormButtonClick);
            xtl.ThemeForm = this;

            xtl.ApplyTheme(Path.Combine(Environment.CurrentDirectory, @"..\..\Themes\BlueWinterTheme.xml"));
            
            //remove botao maximizar
            this.TitleBar.TitleBarButtons.RemoveAt(1);

            this.TitleBar.TitleBarCaption = "F R E A K I N   R I C H !";
            this.TitleBar.TitleBarCaptionFont = new Font("Comic Sans MS", 10, FontStyle.Bold);
            this.TitleBar.TitleBarCaptionColor = Color.DarkMagenta;

            this.IconHolder.HolderButtons[1].XHolderButtonCaptionColor = Color.White;
            this.IconHolder.HolderButtons[0].XHolderButtonCaptionColor = Color.White;
            this.IconHolder.HolderButtons[2].XHolderButtonCaptionColor = Color.White;

            //estilo dos botoes fechar e minimizar
            this.TitleBar.TitleBarButtons[1].ButtonFillMode = XCoolForm.XTitleBarButton.XButtonFillMode.FullFill;
            this.TitleBar.TitleBarButtons[0].ButtonFillMode = XCoolForm.XTitleBarButton.XButtonFillMode.FullFill;

            //imagem da barra de titulo
            this.TitleBar.TitleBarBackImage = FreakinRich.Properties.Resources.money_small;
            
            //Barra de titulo com gradiente de cores
            List<Color> MenuIconMix = new List<Color>();
            MenuIconMix.Add(Color.FromArgb(227, 235, 247));
            MenuIconMix.Add(Color.FromArgb(221, 234, 251));
            MenuIconMix.Add(Color.FromArgb(205, 224, 248));
            MenuIconMix.Add(Color.FromArgb(217, 232, 250));
            MenuIconMix.Add(Color.FromArgb(223, 236, 252));
            
            this.TitleBar.TitleBarMixColors = MenuIconMix;
            this.TitleBar.TitleBarFill = XCoolForm.XTitleBar.XTitleBarFill.AdvancedRendering;

        }

        private void XCoolFormButtonClick(XCoolForm.XCoolForm.XCoolFormHolderButtonClickArgs e)
        {
            switch (e.ButtonIndex)
            {
                case 0:
                    {
                        String name = Microsoft.VisualBasic.Interaction.InputBox("Introduza o seu nome:", "Novo Jogo!");

                        if (name != "")
                        {
                            //create server
                            m_server = new Server();

                            //create client
                            m_client = new TcpClient();

                            //connect to server
                            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55532);
                            m_client.Connect(serverEndPoint);

                            //setup client and listener to receive from server
                            m_clientStream = m_client.GetStream();
                            Thread clientReceiverThread = new Thread(new ThreadStart(ReceiveFromServer));
                            clientReceiverThread.Name = "ReceiveFromServer";
                            clientReceiverThread.IsBackground = true;
                            clientReceiverThread.Start();

                            //codifica a mensagem com: tipoDeMsg + nomeDoPlayer e envia
                            SendToServer(StreamConformer.MessageType.SET_PLAYER_NAME, name);

                            SetPropertyVisibility(true);
                            SetControlVisible(true, pb_GameBoard);
                            SetControlVisible(true, panel1);
                            SetControlVisible(true, panelChat);
                            SetControlEnabled(true, tb_Chat);
                        }
                        else
                        {
                            MessageBox.Show("O nome do jogador deve conter pelo menos um caracter. Tente novamente.");
                        }
                    }
                    break;
                case 1:
                    {
                        try
                        {
                            String end = Microsoft.VisualBasic.Interaction.InputBox("Introduza o endereço do servidor:", "Participar em Jogo!");
                            if (end != "")
                            {
                                //create client
                                m_client = new TcpClient();

                                //connect to server
                                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(end), 55532);
                                m_client.Connect(serverEndPoint);


                                String name = Microsoft.VisualBasic.Interaction.InputBox("Introduza o seu nome:", "Participar em Jogo!");
                                if (name != "")
                                {
                                    //setup client and listener to receive from server
                                    m_clientStream = m_client.GetStream();
                                    Thread clientReceiverThread = new Thread(new ThreadStart(ReceiveFromServer));
                                    clientReceiverThread.IsBackground = true;
                                    clientReceiverThread.Start();

                                    SendToServer(StreamConformer.MessageType.SET_PLAYER_NAME, name);

                                    //inicia timer p/ iniciar conversacao ping pong cliente/servidor
                                    SetTimer();

                                    SetPropertyVisibility(true);
                                    SetControlVisible(true, pb_GameBoard);
                                    SetControlVisible(true, panel1);
                                    SetControlVisible(true, panelChat);
                                    SetControlEnabled(true, tb_Chat);
                                }
                                else
                                {
                                    MessageBox.Show("O nome do jogador deve conter pelo menos um caracter. Tente novamente.");
                                }
                            }
                            else
                            {
                                MessageBox.Show("Introduza um endereço válido.");
                            }
                        }
                        catch (SocketException)
                        {
                            MessageBox.Show("Ainda nenhuma partida foi iniciada para poder participar.");
                        }
                        catch (FormatException)
                        {
                            MessageBox.Show("Endereço inválido. Tente novamente.");
                        }
                    }
                    break;
                case 2:
                    {
                        if (pbRegras.Visible == true)
                            SetControlVisible(false, pbRegras);
                        else
                            SetControlVisible(true, pbRegras);
                    }
                    break;
            }
        }

        private void InitAudio()
        {
            try
            {
                m_outStream_playerTurn = CreateWaveStream(@"..\..\audio\sound_player_turn.wav");
                m_outStream_othersPlayers = CreateWaveStream(@"..\..\audio\sound_others_players_turn.wav");
                m_outStream_chanceLost = CreateWaveStream(@"..\..\audio\sound_chance_perdeu.wav");
                m_outStream_chanceWin = CreateWaveStream(@"..\..\audio\sound_chance_ganhou.wav");
                m_outStream_jail = CreateWaveStream(@"..\..\audio\sound_prisao.wav");
                m_outStream_winner = CreateWaveStream(@"..\..\audio\sound_winner.wav");

                m_soundOutput_playerTurn = CreateWavePlayer(m_outStream_playerTurn);
                m_soundOutput_othersPlayers = CreateWavePlayer(m_outStream_othersPlayers);
                m_soundOutput_jail = CreateWavePlayer(m_outStream_jail);
                m_soundOutput_chanceWin = CreateWavePlayer(m_outStream_chanceWin);
                m_soundOutput_chanceLost = CreateWavePlayer(m_outStream_chanceLost);
                m_soundOutput_winner = CreateWavePlayer(m_outStream_winner);
                
            }
            catch (Exception)
            {
                MessageBox.Show("O áudio encontra-se temporariamente indisponível. Lamentamos o incómodo.");
                m_audioOk = false;
            }
        }

		private WaveStream CreateWaveStream(String fileName)
        {
            WaveChannel32 inputStream;
            if (fileName.EndsWith(".wav"))
            {
                WaveStream readerStream = new WaveFileReader(fileName);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }

                if (readerStream.WaveFormat.BitsPerSample != 16)
                {
                    var format = new WaveFormat(readerStream.WaveFormat.SampleRate,
                       16, readerStream.WaveFormat.Channels);
                    readerStream = new WaveFormatConversionStream(format, readerStream);
                }

                inputStream = new WaveChannel32(readerStream);
                return inputStream;
            }
            else
            {
                MessageBox.Show("Áudio não suportado. Lamentamos o incómodo.");
            }
            return null;
        }

        private WaveOut CreateWavePlayer (WaveStream audio)
        {
            if (m_audioOk == false)
            {
                return null;
            }

            try
            {
                WaveOut wout = new WaveOut();
                wout.Init(audio);
                return wout;
            }
            catch (Exception)
            {
                MessageBox.Show("O áudio encontra-se temporariamente indisponível :( Lamentamos o incómodo.");
                m_audioOk = false;
            }
            return null;
        }

        private void PlayAudio(SoundEffects sfx)
        {
            WaveOut soundOutput = null;
            WaveStream stream = null;
            if (m_audioOk == false)
            {
                return;
            }

            try
            {
                switch (sfx)
                {
                    case SoundEffects.PlayerTurn:
                        soundOutput = m_soundOutput_playerTurn;
                        stream = m_outStream_playerTurn;
                        break;
                    case SoundEffects.OthersTurn:
                        soundOutput = m_soundOutput_othersPlayers;
                        stream = m_outStream_othersPlayers;
                        break;
                    case SoundEffects.Jail:
                        soundOutput = m_soundOutput_jail;
                        stream = m_outStream_jail;
                        break;
                    case SoundEffects.ChanceWin:
                        soundOutput = m_soundOutput_chanceWin;
                        stream = m_outStream_chanceWin;
                        break;
                    case SoundEffects.ChanceLost:
                        soundOutput = m_soundOutput_chanceLost;
                        stream = m_outStream_chanceLost;
                        break;
                    case SoundEffects.Winner:
                        soundOutput = m_soundOutput_winner;
                        stream = m_outStream_winner;
                        break;
                    default:
                        return;
                }

                //coloca o audio na posicao 0
                stream.CurrentTime = TimeSpan.Zero;
                soundOutput.Play();
            }
            catch (Exception)
            {
                MessageBox.Show("O áudio encontra-se temporariamente indisponível :( Lamentamos o incómodo.");
                m_audioOk = false;
            }
        }

        private Bitmap PrintScreen()
        {
            Rectangle bounds = this.Bounds;
			Point pos = m_gameBoardScreenLocation;
            Bitmap bitmap = new Bitmap(pb_GameBoard.Width, pb_GameBoard.Height-5);
            
            using (Graphics g = Graphics.FromImage(bitmap))
            {
				g.CopyFromScreen(pos, Point.Empty, pb_GameBoard.Size);
            }

            return bitmap;
        }

        private void EnableShade()
        {
            m_pb_Print.BackgroundImage = PrintScreen();
            SetControlVisible(true, m_pb_Print);
            SetControlVisible(true, m_pb_Shade);
        }

        private void DisableShade()
        {
            SetControlVisible(false, m_pb_Print);
            SetControlVisible(false, m_pb_Shade);
        }

        private void ChangeParentControl()
        {
            m_pb_Print.Controls.Remove(m_pb_Shade);
            pb_GameBoard.Controls.Add(m_pb_Shade);
        }

        private void SetupPbProperty()
        {
            pb_Property1.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property3.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property5.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property6.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property8.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property9.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property11.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property12.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property13.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property14.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property15.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property16.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property18.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property19.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property21.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property23.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property24.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property25.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property26.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property27.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property28.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property29.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property31.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property32.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property34.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property35.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property37.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);
            pb_Property39.MouseEnter += new System.EventHandler(this.pb_Property_MouseEnter);

            pb_Property1.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property3.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property5.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property6.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property8.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property9.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property11.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property12.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property13.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property14.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property15.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property16.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property18.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property19.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property21.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property23.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property24.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property25.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property26.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property27.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property28.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property29.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property31.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property32.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property34.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property35.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property37.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);
            pb_Property39.MouseLeave += new System.EventHandler(this.pb_Property_MouseLeave);

            this.Controls.Remove(pb_Property1);
            this.Controls.Remove(pb_Property3);
            this.Controls.Remove(pb_Property5);
            this.Controls.Remove(pb_Property6);
            this.Controls.Remove(pb_Property8);
            this.Controls.Remove(pb_Property9);
            this.Controls.Remove(pb_Property11);
            this.Controls.Remove(pb_Property12);
            this.Controls.Remove(pb_Property13);
            this.Controls.Remove(pb_Property14);
            this.Controls.Remove(pb_Property15);
            this.Controls.Remove(pb_Property16);
            this.Controls.Remove(pb_Property18);
            this.Controls.Remove(pb_Property19);
            this.Controls.Remove(pb_Property21);
            this.Controls.Remove(pb_Property23);
            this.Controls.Remove(pb_Property24);
            this.Controls.Remove(pb_Property25);
            this.Controls.Remove(pb_Property26);
            this.Controls.Remove(pb_Property27);
            this.Controls.Remove(pb_Property28);
            this.Controls.Remove(pb_Property29);
            this.Controls.Remove(pb_Property31);
            this.Controls.Remove(pb_Property32);
            this.Controls.Remove(pb_Property34);
            this.Controls.Remove(pb_Property35);
            this.Controls.Remove(pb_Property37);
            this.Controls.Remove(pb_Property39);        

            pb_GameBoard.Controls.Add(pb_Property1);
            pb_GameBoard.Controls.Add(pb_Property3);
            pb_GameBoard.Controls.Add(pb_Property5);
            pb_GameBoard.Controls.Add(pb_Property6);
            pb_GameBoard.Controls.Add(pb_Property8);
            pb_GameBoard.Controls.Add(pb_Property9);
            pb_GameBoard.Controls.Add(pb_Property11);
            pb_GameBoard.Controls.Add(pb_Property12);
            pb_GameBoard.Controls.Add(pb_Property13);
            pb_GameBoard.Controls.Add(pb_Property14);
            pb_GameBoard.Controls.Add(pb_Property15);
            pb_GameBoard.Controls.Add(pb_Property16);
            pb_GameBoard.Controls.Add(pb_Property18);
            pb_GameBoard.Controls.Add(pb_Property19);
            pb_GameBoard.Controls.Add(pb_Property21);
            pb_GameBoard.Controls.Add(pb_Property23);
            pb_GameBoard.Controls.Add(pb_Property24);
            pb_GameBoard.Controls.Add(pb_Property25);
            pb_GameBoard.Controls.Add(pb_Property26);
            pb_GameBoard.Controls.Add(pb_Property27);
            pb_GameBoard.Controls.Add(pb_Property28);
            pb_GameBoard.Controls.Add(pb_Property29);
            pb_GameBoard.Controls.Add(pb_Property31);
            pb_GameBoard.Controls.Add(pb_Property32);
            pb_GameBoard.Controls.Add(pb_Property34);
            pb_GameBoard.Controls.Add(pb_Property35);
            pb_GameBoard.Controls.Add(pb_Property37);
            pb_GameBoard.Controls.Add(pb_Property39);

            // move para cima e para a direita, devido a deixar de estar na form e passar para o board
			MoveControl(pb_Property1, -7, -35);
			MoveControl(pb_Property3, -7, -35);
			MoveControl(pb_Property5, -7, -35);
			MoveControl(pb_Property6, -7, -35);
			MoveControl(pb_Property8, -7, -35);
			MoveControl(pb_Property9, -7, -35);
			MoveControl(pb_Property11, -12, -38);
			MoveControl(pb_Property12, -12, -38);
			MoveControl(pb_Property13, -12, -38);
			MoveControl(pb_Property14, -12, -38);
			MoveControl(pb_Property15, -12, -38);
			MoveControl(pb_Property16, -12, -38);
			MoveControl(pb_Property18, -12, -38);
			MoveControl(pb_Property19, -12, -38);
			MoveControl(pb_Property21, -7, -40);
			MoveControl(pb_Property23, -7, -40);
			MoveControl(pb_Property24, -7, -40);
			MoveControl(pb_Property25, -7, -40);
			MoveControl(pb_Property26, -7, -40);
			MoveControl(pb_Property27, -7, -40);
			MoveControl(pb_Property28, -7, -40);
			MoveControl(pb_Property29, -7, -40);
			MoveControl(pb_Property31, -2, -38);
			MoveControl(pb_Property32, -2, -38);
			MoveControl(pb_Property34, -2, -38);
			MoveControl(pb_Property35, -2, -38);
			MoveControl(pb_Property37, -2, -38);
			MoveControl(pb_Property39, -2, -38);

            SetPropertyVisibility(false);
        }

        private void SetPropertyVisibility(Boolean flag)
        {
            pb_Property1.Visible = flag;
            pb_Property3.Visible = flag;
            pb_Property5.Visible = flag;
            pb_Property6.Visible = flag;
            pb_Property8.Visible = flag;
            pb_Property9.Visible = flag;
            pb_Property11.Visible = flag;
            pb_Property12.Visible = flag;
            pb_Property13.Visible = flag;
            pb_Property14.Visible = flag;
            pb_Property15.Visible = flag;
            pb_Property16.Visible = flag;
            pb_Property18.Visible = flag;
            pb_Property19.Visible = flag;
            pb_Property21.Visible = flag;
            pb_Property23.Visible = flag;
            pb_Property24.Visible = flag;
            pb_Property25.Visible = flag;
            pb_Property26.Visible = flag;
            pb_Property27.Visible = flag;
            pb_Property28.Visible = flag;
            pb_Property29.Visible = flag;
            pb_Property31.Visible = flag;
            pb_Property32.Visible = flag;
            pb_Property34.Visible = flag;
            pb_Property35.Visible = flag;
            pb_Property37.Visible = flag;
            pb_Property39.Visible = flag;
        }

        private void SetupPb_Houses()
        {
            pb_Houses1.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses3.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses6.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses8.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses9.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses11.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses13.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses14.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses16.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses18.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses19.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses21.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses23.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses24.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses26.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses27.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses29.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses31.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses32.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses34.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses37.NormalBackgroundImage = Properties.Resources.casas0;
            pb_Houses39.NormalBackgroundImage = Properties.Resources.casas0;

            pb_Houses1.ClickBackgroundImage = null;
            pb_Houses3.ClickBackgroundImage = null;
            pb_Houses6.ClickBackgroundImage = null;
            pb_Houses8.ClickBackgroundImage = null;
            pb_Houses9.ClickBackgroundImage = null;
            pb_Houses11.ClickBackgroundImage = null;
            pb_Houses13.ClickBackgroundImage = null;
            pb_Houses14.ClickBackgroundImage = null;
            pb_Houses16.ClickBackgroundImage = null;
            pb_Houses18.ClickBackgroundImage = null;
            pb_Houses19.ClickBackgroundImage = null;
            pb_Houses21.ClickBackgroundImage = null;
            pb_Houses23.ClickBackgroundImage = null;
            pb_Houses24.ClickBackgroundImage = null;
            pb_Houses26.ClickBackgroundImage = null;
            pb_Houses27.ClickBackgroundImage = null;
            pb_Houses29.ClickBackgroundImage = null;
            pb_Houses31.ClickBackgroundImage = null;
            pb_Houses32.ClickBackgroundImage = null;
            pb_Houses34.ClickBackgroundImage = null;
            pb_Houses37.ClickBackgroundImage = null;
            pb_Houses39.ClickBackgroundImage = null;


            this.Controls.Remove(pb_Houses1);
            this.Controls.Remove(pb_Houses3);
            this.Controls.Remove(pb_Houses6);
            this.Controls.Remove(pb_Houses8);
            this.Controls.Remove(pb_Houses9);
            this.Controls.Remove(pb_Houses11);
            this.Controls.Remove(pb_Houses13);
            this.Controls.Remove(pb_Houses14);
            this.Controls.Remove(pb_Houses16);
            this.Controls.Remove(pb_Houses18);
            this.Controls.Remove(pb_Houses19);
            this.Controls.Remove(pb_Houses21);
            this.Controls.Remove(pb_Houses23);
            this.Controls.Remove(pb_Houses24);
            this.Controls.Remove(pb_Houses26);
            this.Controls.Remove(pb_Houses27);
            this.Controls.Remove(pb_Houses29);
            this.Controls.Remove(pb_Houses31);
            this.Controls.Remove(pb_Houses32);
            this.Controls.Remove(pb_Houses34);
            this.Controls.Remove(pb_Houses37);
            this.Controls.Remove(pb_Houses39);

            pb_GameBoard.Controls.Add(pb_Houses1);
            pb_GameBoard.Controls.Add(pb_Houses3);
            pb_GameBoard.Controls.Add(pb_Houses6);
            pb_GameBoard.Controls.Add(pb_Houses8);
            pb_GameBoard.Controls.Add(pb_Houses9);
            pb_GameBoard.Controls.Add(pb_Houses11);
            pb_GameBoard.Controls.Add(pb_Houses13);
            pb_GameBoard.Controls.Add(pb_Houses14);
            pb_GameBoard.Controls.Add(pb_Houses16);
            pb_GameBoard.Controls.Add(pb_Houses18);
            pb_GameBoard.Controls.Add(pb_Houses19);
            pb_GameBoard.Controls.Add(pb_Houses21);
            pb_GameBoard.Controls.Add(pb_Houses23);
            pb_GameBoard.Controls.Add(pb_Houses24);
            pb_GameBoard.Controls.Add(pb_Houses26);
            pb_GameBoard.Controls.Add(pb_Houses27);
            pb_GameBoard.Controls.Add(pb_Houses29);
            pb_GameBoard.Controls.Add(pb_Houses31);
            pb_GameBoard.Controls.Add(pb_Houses32);
            pb_GameBoard.Controls.Add(pb_Houses34);
            pb_GameBoard.Controls.Add(pb_Houses37);
            pb_GameBoard.Controls.Add(pb_Houses39);

			//Move para cima e para a direita, devido a deixar de estar na form e passar para o board
			MoveControl(pb_Houses1, -7, -38);
			MoveControl(pb_Houses3, -7, -38);
			MoveControl(pb_Houses6, -7, -38);
			MoveControl(pb_Houses8, -7, -38);
			MoveControl(pb_Houses9, -7, -38);
			MoveControl(pb_Houses11, -7, -38);
			MoveControl(pb_Houses13, -7, -38);
			MoveControl(pb_Houses14, -7, -38);
			MoveControl(pb_Houses16, -7, -38);
			MoveControl(pb_Houses18, -7, -38);
			MoveControl(pb_Houses19, -7, -38);
			MoveControl(pb_Houses21, -7, -38);
			MoveControl(pb_Houses23, -7, -38);
			MoveControl(pb_Houses24, -7, -38);
			MoveControl(pb_Houses26, -7, -38);
			MoveControl(pb_Houses27, -7, -38);
			MoveControl(pb_Houses29, -7, -38);
			MoveControl(pb_Houses31, -7, -38);
			MoveControl(pb_Houses32, -7, -38);
			MoveControl(pb_Houses34, -7, -38);
			MoveControl(pb_Houses37, -7, -38);
			MoveControl(pb_Houses39, -7, -38);

            pb_Houses1.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses3.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses6.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses8.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses9.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses11.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses13.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses14.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses16.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses18.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses19.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses21.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses23.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses24.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses26.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses27.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses29.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses31.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses32.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses34.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses37.MouseClick += new MouseEventHandler(pb_Houses_Click);
            pb_Houses39.MouseClick += new MouseEventHandler(pb_Houses_Click);
        }

        private static void MoveControl(Control ctr, int x, int y)
        {
            Point loc = ctr.Location;
            loc.X += x;
            loc.Y += y;

            ctr.Location = loc;
        }

        //Menu Novo Jogo é clicado
        private void novoJogoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String name = Microsoft.VisualBasic.Interaction.InputBox("Introduza o seu nome:", "Novo Jogo!");

            if (name != "")
            {
                //create server
                m_server = new Server();

                //create client
                m_client = new TcpClient();

                //connect to server
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55532);
                m_client.Connect(serverEndPoint);

                //setup client and listener to receive from server
                m_clientStream = m_client.GetStream();
                Thread clientReceiverThread = new Thread(new ThreadStart(ReceiveFromServer));
                clientReceiverThread.IsBackground = true;
                clientReceiverThread.Name = "ReceiveFromServer";
                clientReceiverThread.Start();

                //codifica a mensagem com: tipoDeMsg + nomeDoPlayer e envia
                SendToServer(StreamConformer.MessageType.SET_PLAYER_NAME, name);

                SetPropertyVisibility(true);
            }
        }

        //Menu Participar em Jogo é clicado
        private void participarEmJogoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                String end = Microsoft.VisualBasic.Interaction.InputBox("Introduza o endereço do servidor:", "Participar em Jogo!");
                //String end = "127.0.0.1";
                if (end != "")
                {
                    //create client
                    m_client = new TcpClient();

                    //connect to server
                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(end), 55532);
                    m_client.Connect(serverEndPoint);

                    
                    String name = Microsoft.VisualBasic.Interaction.InputBox("Introduza o seu nome:", "Participar em Jogo!");
                    if (name != "")
                    {
                        //setup client and listener to receive from server
                        m_clientStream = m_client.GetStream();
                        Thread clientReceiverThread = new Thread(new ThreadStart(ReceiveFromServer));
                        clientReceiverThread.IsBackground = true;
                        clientReceiverThread.Start();

                        SendToServer(StreamConformer.MessageType.SET_PLAYER_NAME, name);

                        SetPropertyVisibility(true);
                    }
                }
            }
            catch (SocketException)
            {
                MessageBox.Show("Ainda nenhuma partida foi iniciada para poder participar.");
            }
            catch (FormatException)
            {
                MessageBox.Show("Endereço inválido. Tente novamente.");
            }
        }

        private void AddPlayerMarkers(int playersCount)
        {
            while(m_playerList.Count < playersCount)
                this.Invoke(new voidNoParamsCallback(AddPlayerMarker));
        }

        private void AddPlayerMarker()
        {
            Bitmap bitmap;
            int bitmapIndex = 0;
            List<Bitmap> bitmaplist = new List<Bitmap>(6);
            bitmaplist.Add(new Bitmap(Properties.Resources.smile_blue));
            bitmaplist.Add(new Bitmap(Properties.Resources.smile_green));
            bitmaplist.Add(new Bitmap(Properties.Resources.smile_lightBlue));
            bitmaplist.Add(new Bitmap(Properties.Resources.smile_pink));
            bitmaplist.Add(new Bitmap(Properties.Resources.smile_red));
            bitmaplist.Add(new Bitmap(Properties.Resources.smile_yellow));
            bitmaplist.Add(new Bitmap(Properties.Resources.purple));

            Point location = new Point(12, 57);

            //picbox com o marker de tabuleiro do jogador 
            PictureBox boardMarker = new PictureBox
            {
                Name = "player" + m_playerList.Count.ToString() + 1,
                Size = new Size(30, 30),
                BackgroundImageLayout = ImageLayout.Zoom,
                BackColor = System.Drawing.Color.Transparent
            };

            //picbox para associar marker ao jogador na lista de nomes
            PictureBox playerMarker = new PictureBox
            {
                Name = "player" + m_playerList.Count.ToString() + 1,
                Size = new Size(35, 35),
                BackgroundImageLayout = ImageLayout.Zoom,
                BackColor = System.Drawing.Color.Transparent
            };

            switch (m_playerList.Count)
            {

                case 0:
                    bitmapIndex = m_playerMarkerOrder[0];
                    boardMarker.Location = m_startMarkerLocation1;
                    break;
                case 1:
                    bitmapIndex = m_playerMarkerOrder[1];
                    boardMarker.Location = m_startMarkerLocation2;
                    location.Y = location.Y + 40;
                    break;
                case 2:
                    bitmapIndex = m_playerMarkerOrder[2];
                    boardMarker.Location = m_startMarkerLocation3;
                    location.Y = location.Y + 80;
                    break;
                case 3:
                    bitmapIndex = m_playerMarkerOrder[3];
                    boardMarker.Location = m_startMarkerLocation4;
                    location.Y = location.Y + 120;
                    break;
                default:
                    break;
            }

            bitmap = bitmaplist[bitmapIndex];
            boardMarker.BackgroundImage = bitmap;
            playerMarker.BackgroundImage = bitmap;

            pb_GameBoard.Controls.Add(boardMarker);
            //AddtoControl(pb_GameBoard);
            m_playerList.Add(boardMarker);
            boardMarker.BringToFront();

            playerMarker.Location = location;
            panel1.Controls.Add(playerMarker);

            //aponta na lista de nomes o jogador a jogar
            pb_TurnMarker.BackColor = System.Drawing.Color.Transparent;
            pb_TurnMarker.Visible = true;

            //mostra simbolo do € a frente do dinheiro
            String str = lbEuro.Text + "€\r\n\r\n"; 
            SetControlText(str, lbEuro);
            SetControlVisible(true, lbEuro);
        }

        private void btDice_Click(object sender, EventArgs e)
        {
            btDice.Visible = false;

			int diceValue1 = Server.GetRandomValue(6, rngCsp);
			int diceValue2 = Server.GetRandomValue(6, rngCsp);

            SendToServer(StreamConformer.MessageType.SET_DICE_VALUE, (diceValue1).ToString() + diceValue2.ToString());
        }

        private void ShowDiceValues(String diceValues)
        {
            int value1 = int.Parse(diceValues.Substring(0, 1));
            int value2 = int.Parse(diceValues.Substring(1));

            this.Invoke(new voidCtrIntCallback(SetDiceImage), new object[] { pbDado1, value1 });
            this.Invoke(new voidCtrIntCallback(SetDiceImage), new object[] { pbDado2, value2 });
            
        }

        private void SetDiceImage(Control pbDado, int value)
        {
            switch (value)
            {
                case 1:
                     pbDado.BackgroundImage = Properties.Resources.dice_1_md;
                    break;
                case 2:
                    pbDado.BackgroundImage = Properties.Resources.dice_2_md;
                    break;
                case 3:
                    pbDado.BackgroundImage = Properties.Resources.dice_3_md;
                    break;
                case 4:
                    pbDado.BackgroundImage = Properties.Resources.dice_4_md;
                    break;
                case 5:
                    pbDado.BackgroundImage = Properties.Resources.dice_5_md;
                break;
                case 6:
                    pbDado.BackgroundImage = Properties.Resources.dice_6_md;
                break;
                default:
                    break;
            }
        }

        //Atualiza marcador que indica proximo jogador a jogar
        private void UpdateCurrentPlayer(Control turnMarker, int playerIndex)
        {
            Point startLocation = new Point(891, 135);

            Point location = turnMarker.Location;

            if (playerIndex == 0)
                location = startLocation;
            else
                if (playerIndex == 1)
                    location.Y = startLocation.Y + 40;
                else
                    if (playerIndex == 2)
                            location.Y = startLocation.Y + 80;
                        else
                            if (playerIndex == 3)
                                location.Y = startLocation.Y + 120;

            turnMarker.Location = location;
        }

        //Move marcadores dos jogadores no tabuleiro
        private void UpdatePlayerBoardPosition(String message)
        {
            int playerIndex = int.Parse(message.Substring(0,1));
            int playerPosition = int.Parse(message.Substring(1));

            this.Invoke(new voidIntIntCallback(MovePlayerMarker), new object[] { playerIndex, playerPosition });
            
        }

        //Move marcadores dos jogadores no tabuleiro
        private void MovePlayerMarker(int playerInd, int playerPos)
        {
            Point location = m_playerList[playerInd].Location;
            
            Point startLocation = new Point();

            if (playerInd == 0)
                startLocation = m_startMarkerLocation1;
            else 
                if (playerInd == 1)
                    startLocation = m_startMarkerLocation2;
                else
                    if (playerInd == 2)
                        startLocation = m_startMarkerLocation3;
                    else
                        if (playerInd == 3)
                            startLocation = m_startMarkerLocation4;
            
            if (playerPos >= 0 && playerPos <= 10)
            {
                //70 representa o espaço entre casas no eixo X
                location.X = startLocation.X - 70 * playerPos;
                location.Y = startLocation.Y;
            }
            else
                if (playerPos > 10 && playerPos <= 20)
                {
                    //*10 representa o espaço ate à coluna da esquerda
                    //55 representa o espaço entre casas no eixo Y
                    //28 representa o espaço do marcador até ao ponto dentro da proxima casa
                    location.X = startLocation.X - 70 * 10;
                    location.Y = startLocation.Y - 28 - 55 * (playerPos-10);
                }
                else
                    if (playerPos >= 20 && playerPos < 30)
                    {
                        location.X = startLocation.X - 70 * (10 - (playerPos-20));
						location.Y = startLocation.Y - 53 - 55 * 10;
                    }
                    else
                        if (playerPos >= 30 && playerPos < 40)
                        {
                            location.X = startLocation.X + 30;
							location.Y = startLocation.Y - 28 - 55 * (10 - (playerPos - 30));
                        }

            m_playerList[playerInd].Location = location;
        }

        private void EnableOwner(String message)
        {
            String[] lines = Regex.Split(message, "\r\n");

            String name = lines[0];

            int positionBoard = int.Parse(lines[1]); 

            Point location = new Point();

            Point startLocation = new Point(738, 570);
            
            if (positionBoard > 0 && positionBoard < 10)
            {
                location.X = startLocation.X - 70 * positionBoard;
                location.Y = startLocation.Y;
            }
            else
                if (positionBoard > 10 && positionBoard < 20)
                {
                    location.X = startLocation.X - 63 * 10 - 17;
                    location.Y = startLocation.Y - 55 * (positionBoard - 10) + 30;
                }
                else
                    if (positionBoard > 20 && positionBoard < 30)
                    {
                        location.X = startLocation.X - 70 * (10 - (positionBoard - 20)); 
                        location.Y = startLocation.Y - 55 * 10 + 70;
                    }
                    else
                        if (positionBoard > 30 && positionBoard < 40)
                        {
                            location.X = startLocation.X - 55;
                            location.Y = startLocation.Y - 55 * (10 - (positionBoard - 30)) + 30;
                        }

            Label lbOwner = null;
            string lblName = "label" + positionBoard.ToString();

            //encontra controlo da propriedade com a posicao recebida na msg
            lbOwner = pb_GameBoard.Controls.Find(lblName, true).FirstOrDefault() as Label;

            if (lbOwner == null)
            {
                // ainda não existe label para esta propriedade -> criar uma nova
                lbOwner = new Label
                {
                    Name = "label" + positionBoard.ToString(),
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    BackColor = System.Drawing.Color.Transparent,
                    Size = new Size(43, 13),
                    Location = location,
                    ForeColor = Color.Firebrick
                };
                lbOwner.Font = new Font(FontFamily.GenericSansSerif, 6.5F, FontStyle.Bold);
                
                AddControl(pb_GameBoard, lbOwner);
            }

            SetControlText(name.ToUpper(), lbOwner);
        }

        private void bt_Buy_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Card);
            SetControlVisible(false, bt_Buy);
            SetControlVisible(false, bt_Pass);
            SendToServer(StreamConformer.MessageType.BUY_PROPERTY, "");
        }

        private void bt_Pass_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Card);
            SetControlVisible(false, bt_Pass);
            SetControlVisible(false, bt_Buy);
            SendToServer(StreamConformer.MessageType.UPDATE_CURRENT_PLAYER, "");
        }

        private void bt_OkInfo_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, bt_OkInfo);
            if (m_gameOver)
            {
                foreach (Control c in Controls)
                {
                    c.Enabled = false;
                }
            }
        }

        private void bt_Pay_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Card);
            SetControlVisible(false, bt_Pay);

            SendToServer(StreamConformer.MessageType.PAY_TAX, "");
        }

        private void bt_SellToPayDebt_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, bt_SellToPayDebt);
        }

        private void bt_PayRent_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, bt_PayRent);
            SendToServer(StreamConformer.MessageType.PAY_RENT, m_chargeRent); 
        }

        private void bt_Jail_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, bt_Jail);
            SendToServer(StreamConformer.MessageType.GO_TO_JAIL, "");
        }

        private void bt_Double_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, bt_Double);
        }

        private void ShowTakeChanceMessage(String message)
        {
            String[] messageInfo = Regex.Split(message, ",");
            
            m_chanceCard = int.Parse(messageInfo[0]);

            if (messageInfo[1] == "+")
            {
                m_chanceWin = true;
            }
            else if (messageInfo[1] == "-")
            {
                m_chanceWin = false;
            }
            else if (messageInfo[1] == " ")
            {
                m_chanceWin = null;
            }

            EnableShade();
            pb_Info.BackgroundImage = Properties.Resources.info_takechance;
            SetControlVisible(true, pb_Info);
            SetControlVisible(true, bt_ChanceCollection);
        }

        private void EnableHouses(String message)
        {
            String[] messageInfo = Regex.Split(message, ",");
            int propIndex = int.Parse(messageInfo[0]);
            int nrHouses = int.Parse(messageInfo[1]);

            //encontra controlo da propriedade com a posicao recebida na msg
            ButtonPictureBox pb_Houses = pb_GameBoard.Controls.Find(
                "pb_Houses" + propIndex, true).FirstOrDefault() as ButtonPictureBox;

            switch (nrHouses)
            {
                case 0:
                    SetControlVisible(true, pb_Houses);
                    if ((propIndex > 0 && propIndex < 10) || propIndex > 20 && propIndex < 30)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.casas0;
                    }
                    else if (propIndex > 10 && propIndex < 20)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.esq_casas0;
                    }
                    else if (propIndex > 30 && propIndex < 40)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.dir_casas0;
                    }
                    break;
                case 1:
                    // Mostrar picbox (ate este ponto esteve oculta nos que nao sao donos)
                    SetControlVisible(true, pb_Houses);
                    if ((propIndex > 0 && propIndex < 10) || propIndex > 20 && propIndex < 30)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.casas1;
                    }
                    else if (propIndex > 10 && propIndex < 20)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.esq_casas1;
                    }
                    else if (propIndex > 30 && propIndex < 40)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.dir_casas1;
                    }
                    break;
                case 2:
                    if ((propIndex > 0 && propIndex < 10) || propIndex > 20 && propIndex < 30)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.casas2;
                    }
                    else if (propIndex > 10 && propIndex < 20)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.esq_casas2;
                    }
                    else if (propIndex > 30 && propIndex < 40)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.dir_casas2;
                    }
                    break;
                case 3:
                    if ((propIndex > 0 && propIndex < 10) || propIndex > 20 && propIndex < 30)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.casas3;
                    }
                    else if (propIndex > 10 && propIndex < 20)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.esq_casas3;
                    }
                    else if (propIndex > 30 && propIndex < 40)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.dir_casas3;
                    }
                    break;
                case 4:
                    if ((propIndex > 0 && propIndex < 10) || propIndex > 20 && propIndex < 30)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.casas4;
                    }
                    else if (propIndex > 10 && propIndex < 20)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.esq_casas4;
                    }
                    else if (propIndex > 30 && propIndex < 40)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.dir_casas4;
                    }
                    break;
                case 5:
                    if ((propIndex > 0 && propIndex < 10) || propIndex > 20 && propIndex < 30)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.hotel;
                    }
                    else if (propIndex > 10 && propIndex < 20)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.esq_hotel;
                    }
                    else if (propIndex > 30 && propIndex < 40)
                    {
                        pb_Houses.NormalBackgroundImage = Properties.Resources.dir_hotel;
                    }
                    break;
            }
		}

		private void DisableHouses(String message)
		{
			int propIndex = int.Parse(message);

			//encontra controlo da propriedade com a posicao recebida na msg
			ButtonPictureBox pb_Houses = pb_GameBoard.Controls.Find(
				"pb_Houses" + propIndex, true).FirstOrDefault() as ButtonPictureBox;

			SetControlVisible(false, pb_Houses);
		}

        private void GameIsOver(String message)
        {
            m_gameOver = true;
            String[] lines = Regex.Split(message, "\r\n");

            String looser = lines[0];
            String winner = lines[1];
            String msg = "   O jogador " + looser + " abriu falência.\r\n" + winner + " é o jogador com maior capital e" + "\r\n       vencedor desta partida!";

            EnableShade();
            pb_Info.BackgroundImage = Properties.Resources.info_winner;
            SetControlVisible(true, bt_OkInfo);
            SetControlVisible(true, pb_Info);
            SetControlText("Congratz!", bt_OkInfo);
            SetControlText(msg, lb_Winner);
            PlayAudio(SoundEffects.Winner);
        }

        private void ShowMessageChat(Control rt_Chat, String message)
        {
            Boolean containStatus;
            String[] lines = Regex.Split(message, "\r\n");

            String player = lines[0];
            String msgChat = lines[1];

            containStatus= player.Contains("Status");
            if (containStatus)
            {
                this.rt_Chat.SelectionFont = new Font("Calibri", 11, FontStyle.Regular);
                this.rt_Chat.SelectionColor = Color.Blue;
            }
            else
            {
                this.rt_Chat.SelectionFont = new Font("Calibri", 11, FontStyle.Regular);
                this.rt_Chat.SelectionColor = Color.Firebrick;
            }
            
            this.rt_Chat.AppendText(player + ": ");

            if (containStatus)
            {
                this.rt_Chat.SelectionFont = new Font("Calibri", 11, FontStyle.Regular);
                this.rt_Chat.SelectionColor = Color.Blue;
            }
            else
            {
                this.rt_Chat.SelectionFont = new Font("Calibri", 11, FontStyle.Regular);
                this.rt_Chat.SelectionColor = Color.Black;
            }

            this.rt_Chat.AppendText(msgChat + "\r\n\r\n");
            this.rt_Chat.Refresh();
            this.rt_Chat.SelectionStart = rt_Chat.Text.Length;
            this.rt_Chat.ScrollToCaret();
        }

        private void bt_ChanceCollection_Click(object sender, EventArgs e)
        {
            SetControlVisible(false, bt_ChanceCollection);
            SetControlVisible(true, bt_Chance);

            switch (m_chanceCard)
            {
                case 1:
                    pb_Info.BackgroundImage = Properties.Resources.chance1;
                    break;
                case 2:
                    pb_Info.BackgroundImage = Properties.Resources.chance2;
                    break;
                case 3:
                    pb_Info.BackgroundImage = Properties.Resources.chance3;
                    break;
                case 4:
                    pb_Info.BackgroundImage = Properties.Resources.chance4;
                    break;
                case 5:
                    pb_Info.BackgroundImage = Properties.Resources.chance5;
                    break;
                case 6:
                    pb_Info.BackgroundImage = Properties.Resources.chance6;
                    break;
                case 7:
                    pb_Info.BackgroundImage = Properties.Resources.chance7;
                    break;
                case 8:
                    pb_Info.BackgroundImage = Properties.Resources.chance8;
                    break;
                case 9:
                    pb_Info.BackgroundImage = Properties.Resources.chance9;
                    break;
                case 10:
                    pb_Info.BackgroundImage = Properties.Resources.chance10;
                    break;
                case 11:
                    pb_Info.BackgroundImage = Properties.Resources.chance11;
                    break;
                case 12:
                    pb_Info.BackgroundImage = Properties.Resources.chance12;
                    break;
                case 13:
                    pb_Info.BackgroundImage = Properties.Resources.chance13;
                    break;
                case 14:
                    pb_Info.BackgroundImage = Properties.Resources.chance14;
                    break;
                case 15:
                    pb_Info.BackgroundImage = Properties.Resources.chance15;
                    break;
                default:
                    break;
            }

            if (m_chanceWin == true)
            {
                PlayAudio(SoundEffects.ChanceWin);
            }
            else
            {
                if (m_chanceWin == false)
                    PlayAudio(SoundEffects.ChanceLost);
            }
        }

        private void bt_Chance_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, bt_Chance);
            SendToServer(StreamConformer.MessageType.TAKE_CHANCE_ACTION, "");
        }

        private void pb_Property_Click(object sender, EventArgs e)
        {
            EnableShade();
            SetControlVisible(true, pb_Card);
            SetControlVisible(true, lb_Close);
            PictureBox pbProperty = (PictureBox)sender;

            int position = int.Parse(pbProperty.Name.Substring(11));
            m_clickedProperty = position;

            SetBackgroundCard(position);
            
            SendToServer(StreamConformer.MessageType.CHECK_IFIS_OWNER, position.ToString());
        }

        private void lb_Close_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Card);
            SetControlVisible(false, lb_Close);
            SetControlVisible(false, bt_SellProperty);
        }

        private void bt_SellProperty_Click(object sender, EventArgs e)
        {
            SendToServer(StreamConformer.MessageType.SELL_PROPERTY, m_clickedProperty.ToString());
            
            DisableShade();
            SetControlVisible(false, pb_Card);
            SetControlVisible(false, lb_Close);
            SetControlVisible(false, bt_SellProperty);
        }

        private void pb_Property_MouseEnter(Object sender, System.EventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void pb_Property_MouseLeave(Object sender, System.EventArgs e)
        {
            Cursor = Cursors.Default;
        }
        
        private void pb_Houses_Click(object sender, EventArgs e)
        {
            PictureBox pbHouses = (PictureBox)sender;

            //descobre a posicao da propriedade
            String propIndex = pbHouses.Name.Substring(9);
            SendToServer(StreamConformer.MessageType.QUERY_HOUSES, propIndex);
        }

        private void lb_CloseInfo_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, lb_CloseInfo);
            SetControlVisible(false, bt_SellProperty);
            SetControlVisible(false, bt_SellHouse);
            SetControlVisible(false, bt_BuyHouse);
        }

        private void ShowBuySellHouseInfo(bool canBuy, bool canSell, string message)
        {
            EnableShade();
            SetControlVisible(true, pb_Info);
            SetControlVisible(true, lb_CloseInfo);

            m_clickedProperty = int.Parse(message);

            if (canBuy && canSell)
            {
                pb_Info.BackgroundImage = Properties.Resources.info_buy_sell_house;
                SetControlVisible(true, bt_BuyHouse);
                SetControlVisible(true, bt_SellHouse);
                this.Invoke(new voidNoParamsCallback(SetButtonsLocation)); 
            }
            else if (canBuy)
            {
                pb_Info.BackgroundImage = Properties.Resources.info_buy_house;
                SetControlVisible(true, bt_BuyHouse);
                this.Invoke(new voidNoParamsCallback(SetButtonsLocationCenter)); 
            }
            else if (canSell)
            {
                pb_Info.BackgroundImage = Properties.Resources.info_sell_house;
                SetControlVisible(true, bt_SellHouse);
                this.Invoke(new voidNoParamsCallback(SetButtonsLocationCenter)); 
            }
        }

        private void SetButtonsLocation()
        {
            Point location = new Point();

            location.X = (int)((pb_Info.Size.Width * 0.30f) - (bt_BuyHouse.Size.Width * 0.5f));
            location.Y = pb_Info.Size.Height - 60;
            bt_BuyHouse.Location = location;

            location.X = (int)((pb_Info.Size.Width * 0.70f) - (bt_BuyHouse.Size.Width * 0.5f));
            location.Y = pb_Info.Size.Height - 60;
            bt_SellHouse.Location = location;
        }

        private void SetButtonsLocationCenter()
        {
            Point location = new Point();

            location.X = (int)((pb_Info.Size.Width * 0.5) - (bt_BuyHouse.Size.Width * 0.5f));
            location.Y = pb_Info.Size.Height - 60;
            bt_BuyHouse.Location = location;
            bt_SellHouse.Location = location;
        }

        private void bt_SellHouse_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, lb_CloseInfo);
            SetControlVisible(false, bt_SellHouse);
            SetControlVisible(false, bt_BuyHouse);
            SendToServer(StreamConformer.MessageType.SELL_HOUSE, m_clickedProperty.ToString());
        }

        private void bt_BuyHouse_Click(object sender, EventArgs e)
        {
            DisableShade();
            SetControlVisible(false, pb_Info);
            SetControlVisible(false, lb_CloseInfo);
            SetControlVisible(false, bt_BuyHouse);
            SetControlVisible(false, bt_SellHouse);
            SendToServer(StreamConformer.MessageType.BUY_HOUSE, m_clickedProperty.ToString());
        }

        private void bt_ChatSend_Click(object sender, EventArgs e)
        {
            if (tb_Chat.Text != "")
            {
                SendToServer(StreamConformer.MessageType.CHAT, tb_Chat.Text);
                SetControlText("", tb_Chat);
            }
        }

        private void SetBackgroundCard(int position)
        {
            switch (position)
            {
                case 1:
                    pb_Card.BackgroundImage = Properties.Resources.card1;
                    break;
                case 3:
                    pb_Card.BackgroundImage = Properties.Resources.card3;
                    break;
                case 4:
                    pb_Card.BackgroundImage = Properties.Resources.card4;
                    break;
                case 5:
                    pb_Card.BackgroundImage = Properties.Resources.card5;
                    break;
                case 6:
                    pb_Card.BackgroundImage = Properties.Resources.card6;
                    break;
                case 8:
                    pb_Card.BackgroundImage = Properties.Resources.card8;
                    break;
                case 9:
                    pb_Card.BackgroundImage = Properties.Resources.card9;
                    break;
                case 11:
                    pb_Card.BackgroundImage = Properties.Resources.card11;
                    break;
                case 12:
                    pb_Card.BackgroundImage = Properties.Resources.card12;
                    break;
                case 13:
                    pb_Card.BackgroundImage = Properties.Resources.card13;
                    break;
                case 14:
                    pb_Card.BackgroundImage = Properties.Resources.card14;
                    break;
                case 15:
                    pb_Card.BackgroundImage = Properties.Resources.card15;
                    break;
                case 16:
                    pb_Card.BackgroundImage = Properties.Resources.card16;
                    break;
                case 18:
                    pb_Card.BackgroundImage = Properties.Resources.card18;
                    break;
                case 19:
                    pb_Card.BackgroundImage = Properties.Resources.card19;
                    break;
                case 21:
                    pb_Card.BackgroundImage = Properties.Resources.card21;
                    break;
                case 23:
                    pb_Card.BackgroundImage = Properties.Resources.card23;
                    break;
                case 24:
                    pb_Card.BackgroundImage = Properties.Resources.card24;
                    break;
                case 25:
                    pb_Card.BackgroundImage = Properties.Resources.card25;
                    break;
                case 26:
                    pb_Card.BackgroundImage = Properties.Resources.card26;
                    break;
                case 27:
                    pb_Card.BackgroundImage = Properties.Resources.card27;
                    break;
                case 28:
                    pb_Card.BackgroundImage = Properties.Resources.card28;
                    break;
                case 29:
                    pb_Card.BackgroundImage = Properties.Resources.card29;
                    break;
                case 31:
                    pb_Card.BackgroundImage = Properties.Resources.card31;
                    break;
                case 32:
                    pb_Card.BackgroundImage = Properties.Resources.card32;
                    break;
                case 34:
                    pb_Card.BackgroundImage = Properties.Resources.card34;
                    break;
                case 35:
                    pb_Card.BackgroundImage = Properties.Resources.card35;
                    break;
                case 37:
                    pb_Card.BackgroundImage = Properties.Resources.card37;
                    break;
                case 38:
                    pb_Card.BackgroundImage = Properties.Resources.card38;
                    break;
                case 39:
                    pb_Card.BackgroundImage = Properties.Resources.card39;
                    break;
                default:
                    break;
            }
        }

        private void SetControlText(string text, Control ctr)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctr.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetControlText);
                this.Invoke(d, new object[] { text, ctr });
            }
            else
            {
                ctr.Text = text;
            }
        }

        private void SetControlEnabled(Boolean flag, Control ctr)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctr.InvokeRequired)
            {
                SetControlEnabledCallback d = new SetControlEnabledCallback(SetControlEnabled);
                this.Invoke(d, new object[] { flag, ctr });
            }
            else
            {
                ctr.Enabled = flag;
            }
        }

        private void SetImageControl(Bitmap bitmap, Control ctr)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctr.InvokeRequired)
            {
                SetImageControlCallback d = new SetImageControlCallback(SetImageControl);
                this.Invoke(d, new object[] { bitmap, ctr });
            }
            else
            {
                ctr.BackgroundImage = bitmap;
            }
        }

        private void SetControlVisible(Boolean flag, Control ctr)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctr.InvokeRequired)
            {
                SetControlVisibleCallback d = new SetControlVisibleCallback(SetControlVisible);
                this.Invoke(d, new object[] { flag, ctr });
            }
            else
            {
                ctr.Visible = flag;
            }
        }

        private void AddControl(Control ctr1, Control ctr2)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctr1.InvokeRequired)
            {
                AddControlCallback d = new AddControlCallback(AddControl);
                this.Invoke(d, new object[] { ctr1, ctr2 });
            }
            else
            {
                ctr1.Controls.Add(ctr2);
            }
        }

        private void SendToServer(StreamConformer.MessageType type, String data)
        {
            if (!m_clientStream.CanWrite)
            {
                MessageBox.Show("A ligação com o servidor perdeu-se. Lamentamos o incómodo.");
                Application.Exit();
            }
            //codifica a mensagem com: tipoDeMsg + nomeDoPlayer
            byte[] message = StreamConformer.Encode(type, data);

            //escreve no stream de dados a enviar, a msg codificada
            m_clientStream.Write(message, 0, message.Length);
            m_clientStream.Flush();
        }

        private void FormInterface_KeyPress(object sender, KeyPressEventArgs e)
        {
             // Debug: forçar dados
            if (e.KeyChar == 'd' || e.KeyChar == 'D')
            {
                btDice.Visible = false;

                String d1 = Microsoft.VisualBasic.Interaction.InputBox("Valor 1?");
                String d2 = Microsoft.VisualBasic.Interaction.InputBox("Valor 2?");
                int diceValue1 = int.Parse(d1);
                int diceValue2 = int.Parse(d2);

                SendToServer(StreamConformer.MessageType.SET_DICE_VALUE, (diceValue1).ToString() + diceValue2.ToString());
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_server != null)
                m_server.Dispose();

            if (m_client != null && m_client.Connected)
                m_client.Close();
        }

        private void SetTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = (5000);
            timer.Tick += new EventHandler(SendPing);
            timer.Start();
            
        }

        private void SendPing(object sender, EventArgs e)
        {
            SendToServer(StreamConformer.MessageType.PING, "");
        }
    }
}
