using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace FreakinRich
{
    class Server
    {
        private TcpListener m_tcpListener;
        private Thread m_listenThread;
        private List<TcpClient> m_clientList = new List<TcpClient>();
        private GameLogic m_gameLogic;
        private List<int> m_playerMarkerOrder;
        private Chance m_chanceCard;
        private Boolean m_playerHasDebt = false;
        private object m_locker = new object();
        private Boolean? m_chanceWin;

        public Server()
        {
            m_playerMarkerOrder = new List<int>(7);
            m_playerMarkerOrder.Add(0);
            m_playerMarkerOrder.Add(1);
            m_playerMarkerOrder.Add(2);
            m_playerMarkerOrder.Add(3);
            m_playerMarkerOrder.Add(4);
            m_playerMarkerOrder.Add(5);
            m_playerMarkerOrder.Add(6);
            ShuffleList(m_playerMarkerOrder);

            m_gameLogic = new GameLogic();

            //o server é iniciado na porta 55532
            m_tcpListener = new TcpListener(IPAddress.Any, 55532);
            m_tcpListener.Start();
            
            //é iniciada a thread que escuta novos clientes
            m_listenThread = new Thread(new ThreadStart(ListenForClients));
            m_listenThread.IsBackground = true;
            m_listenThread.Name = "ListenerForClients";
            m_listenThread.Start();
        }

        private void ListenForClients()
        {
            while (true)
            {
                try
                {
                    //blocks until a client has connected to the server
                    TcpClient client = m_tcpListener.AcceptTcpClient();

                    if (m_clientList.Count == 4)
                    {
                        SendToClient(StreamConformer.MessageType.PLAYERS_FULL, "", client.GetStream());
                    }
                    else
                    {
                        m_clientList.Add(client);

                        //create a thread to handle a solo communication with connected client
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        clientThread.IsBackground = true;
                        clientThread.Name = "HandleClientComm" + m_clientList.Count;
                        clientThread.Start(client);
                    }
                 }

                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                    break;
                }
            }
        }

        //thread exclusiva para comunicacao com o cliente 
        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            while (true)
            {
                String message;
                StreamConformer.MessageType type;
                int bytesRead = 0;

                //adquire a mensagem completa do cliente
                byte[] dataBuffer = GetFullMessage(clientStream, out bytesRead);

                if (bytesRead == 0)
                {
                    Player player = FindPlayer(tcpClient);
                    m_clientList.Remove(tcpClient);
                    m_gameLogic.m_playerList.Remove(player);
                    SendToAllClients(StreamConformer.MessageType.CONNECTION_FAILED, "");
                    break;
                }

                //obtem mensagem e tipo de mensagem 
                type = StreamConformer.Decode(dataBuffer, bytesRead, out message);

                //impede a execucacao do bloco por + q uma thread ao mm tempo
                lock (m_locker)
                {
                    switch (type)
                    {
                        //NOVO JOGADOR
                        case StreamConformer.MessageType.SET_PLAYER_NAME:
                            {
                                // Enviar ordem de marcadores para o novo jogador
                                SendToClient(StreamConformer.MessageType.SET_MARKER_ORDER, IntListToString(m_playerMarkerOrder), clientStream);

                                m_gameLogic.AddPlayer(tcpClient, message);
                                SendToAllClients(StreamConformer.MessageType.UPDATE_PLAYERS_NAMES, GetPlayersNames());
                                SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
                                SendToAllClients(StreamConformer.MessageType.UPDATE_PLAYERS_COUNT, m_gameLogic.m_playerList.Count.ToString());

                                //viabiliza botao p/ rolar dados ao primeiro jogador se existirem 2 players
                                if (m_gameLogic.m_playerList.Count == 2)
                                {
                                    UpdateCurrentPlayer();
                                }
                                break;
                            }

                        //JOGADOR ROLOU OS DADOS
                        case StreamConformer.MessageType.SET_DICE_VALUE:
                            {
                                SendToAllClients(StreamConformer.MessageType.SHOW_DICE_VALUES, message); 
                                SendToAllClientsExceptCurrent(StreamConformer.MessageType.OTHERS_TURN, "");
                                Property property;
                                int newPosition, diceValue;

                                // verifica se o client que rolou os dados, é o currentPlayer
                                if (tcpClient == m_gameLogic.m_playerList[m_gameLogic.m_currentPlayerIndex].Client)
                                {
                                    // Obtem valores dos dados
                                    int diceValue1 = int.Parse(message.Substring(0, 1));
                                    int diceValue2 = int.Parse(message.Substring(1));
                                    diceValue = diceValue1 + diceValue2;

                                    if (m_gameLogic.CheckDouble(diceValue1, diceValue2) == true)
                                    {
                                        // too many doubles... suspeito! foi para a cadeia, o malandro! :)
                                        SendToClient(StreamConformer.MessageType.TIMES3_DOUBLE, "", clientStream);
                                    }
                                    else
                                    {
                                        //retorna casa em q/ o jogador calhou, valor de dados e posicao no tabuleiro
                                        property = m_gameLogic.ProcessDiceRoll(diceValue, out newPosition);

                                        //envia para todos os jogadores o jogador actual a jogar e a sua nova posicao  
                                        SendToAllClients(StreamConformer.MessageType.UPDATE_PLAYER_POSITION, m_gameLogic.m_currentPlayerIndex.ToString() + newPosition.ToString());

                                        SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney().ToString());
                                        //com o tipo de casa definido, atua conforme e envia para o jogador atual a acao
                                        ProcessPosition(property, diceValue, clientStream, tcpClient);
                                    }
                                }
                                break;
                            }

                        //PROXIMO JOGADOR A JOGAR
                        case StreamConformer.MessageType.UPDATE_CURRENT_PLAYER:
                            UpdateCurrentPlayer();
                            break;
                        //JOGADOR QUER COMPRAR PROPRIEDADE
                        case StreamConformer.MessageType.BUY_PROPERTY:
                            BuyProperty();
                            break;
                        //JOGADOR CLICOU EM "PAGAR RENDA"
                        case StreamConformer.MessageType.PAY_RENT:
                            PayRent(message);
                            break;
                        //JOGADOR CLICOU EM PAGAR "PAGAR IMPOSTO" 
                        case StreamConformer.MessageType.PAY_TAX:
                            PayTax();
                            break;
                        //JOGADOR VIU CARTA DA SORTE
                        case StreamConformer.MessageType.TAKE_CHANCE_ACTION:
							TakeChanceAction(clientStream, tcpClient);
                            break;
                        //JOGADOR CALHOU NA CASA JAIL
                        case StreamConformer.MessageType.GO_TO_JAIL:
                            m_gameLogic.m_currentPlayer.GoToJail();
                            m_gameLogic.m_doubleCount = 0;
                            SendToAllClients(StreamConformer.MessageType.UPDATE_PLAYER_POSITION, m_gameLogic.m_currentPlayerIndex.ToString() + "10");
                            SendToAllClients(StreamConformer.MessageType.CHAT, m_gameLogic.m_currentPlayer.Name + " Status\r\nFoi para a Prisao!");
                            UpdateCurrentPlayer();
                            break;
                        //VERIFICA SE É OWNER da propriedade p/ mostrar botao de venda
                        case StreamConformer.MessageType.CHECK_IFIS_OWNER:
                            {
                                Player player = FindPlayer(tcpClient);
                                if (m_gameLogic.m_currentPlayer != null)
                                {
                                    Property prop = m_gameLogic.m_propertySequence[int.Parse(message)];

                                    if (prop.Owner != null && prop.Owner == player)
                                        SendToClient(StreamConformer.MessageType.ENABLE_SELL_PROPERTY, "", clientStream);
                                }
                                break;
                            }
                        //JOGADOR CLICOU EM VENDER PROPRIEDADE
                        case StreamConformer.MessageType.SELL_PROPERTY:
                            SellProperty(message, tcpClient);
							break;
                        //JOGADOR CLICOU EM COMPRAR IMOVEL
                        case StreamConformer.MessageType.BUY_HOUSE:
                            BuyHouse(message, tcpClient);
                            break;
                        //JOGADOR CLICOU EM VENDER IMOVEL
                        case StreamConformer.MessageType.SELL_HOUSE:
                            SellHouse(message, tcpClient);
                            break;
                        //VERIFICA QTS IMOVEIS O JOGADOR POSSUI p/ disponibilizar botao comprar ou vender
                        case StreamConformer.MessageType.QUERY_HOUSES:
                            QueryHouses(message, tcpClient);
                            break;
                        //JOGADOR ESCREVEU NO CHAT
                        case StreamConformer.MessageType.CHAT:
                            {
                                Player player = FindPlayer(tcpClient);
                                if (message != "")
                                {
                                    SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " diz\r\n" + message);
                                }
                                break;
                            }
                        //CLIENTE ENVIOU PING AO SERVER
                        case StreamConformer.MessageType.PING:
                            SendToAllClients(StreamConformer.MessageType.PONG, "");
                            break;
                        default:
                            break;
                    }
                }
            }

            tcpClient.Close();
        }

        public static byte[] GetFullMessage(NetworkStream stream, out int totalBytesCount)
        {
            int data = 0;
            MemoryStream ms = new MemoryStream();
            totalBytesCount = 0;

            while (true)
            {
                try
                {
                    //blocks until the server sends a message
                    data = stream.ReadByte();

                    //connection with client aborted
                    if (data == -1)
                    {
                        break;
                    }
                    else
                    {
                        ms.WriteByte((byte)data);
                        totalBytesCount++;
                        //enquanto nao le o char ENDOFText continua a leitura
                        if (data == 3)
                            break;
                    }
                }
                catch
                {
                    //a socket error has occured
                    break;
                }
            }
            return ms.ToArray();
        }

        private String GetPlayersNames()
        {
            StringBuilder str = new StringBuilder();
            foreach (Player player in m_gameLogic.m_playerList)
                str.Append(player.Name + "\r\n\r\n");

            return str.ToString();
        }

        private String GetPlayersMoney()
        {
            double money;
            StringBuilder str = new StringBuilder();
            foreach (Player player in m_gameLogic.m_playerList)
            {
                money = (double)player.Money;
                str.Append(money.ToString("0,0", CultureInfo.CreateSpecificCulture("el-GR")) + "\r\n\r\n");
            }

            return str.ToString();
        }

        private void ProcessPosition(Property property, int diceValue, NetworkStream clientStream, TcpClient client)
        {
            Player player = FindPlayer(client);
            //se o jogador possui a carta, joga o proximo jogador
            if (property.Owner == m_gameLogic.m_currentPlayer)
            {
                UpdateCurrentPlayer();
                
            }
            else
            {
                switch (property.Type)
                {
                    case Property.PropertyType.PROPERTY:
                        {
                            //PROPERTY_ON_SALE
                            if (property.Owner == null)
							{
								if(m_gameLogic.m_currentPlayer.Money >= property.Value.Value)
								{
									SendToClient(StreamConformer.MessageType.PROPERTY_ON_SALE,
										m_gameLogic.m_currentPlayer.CurrentPosition.ToString(), clientStream);
								}
								else
								{
									UpdateCurrentPlayer();
								}
							}
                            else
                            {
                                //CHARGE_RENT
                                int charge = m_gameLogic.DeterminePropertyCharge(property);
                                SendToClient(StreamConformer.MessageType.CHARGE_RENT, charge.ToString(), clientStream);
                                SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nPagou o aluguer de " +
                                    charge + " Euros ao propietario " + property.Owner.Name);
                            }
                            break;
                        }
                    case Property.PropertyType.COMPANY:
                        {
                            if (property.Owner == null)
							{
								if(m_gameLogic.m_currentPlayer.Money >= property.Value.Value)
								{
									SendToClient(StreamConformer.MessageType.PROPERTY_ON_SALE,
										m_gameLogic.m_currentPlayer.CurrentPosition.ToString(), clientStream);
								}
								else
								{
									UpdateCurrentPlayer();
								}
							}
                            else
                            {
                                int charge = m_gameLogic.DetermineCompanyCharge(property, diceValue);
                                SendToClient(StreamConformer.MessageType.CHARGE_RENT, charge.ToString(), clientStream);
                                SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nPagou o aluguer de " +
                                    charge + " Euros ao propietario " + property.Owner.Name);
                            }
                            break;
                        }
                    case Property.PropertyType.TRAIN:
                        {
                            if (property.Owner == null)
							{
								if(m_gameLogic.m_currentPlayer.Money >= property.Value.Value)
								{
									SendToClient(StreamConformer.MessageType.PROPERTY_ON_SALE,
										m_gameLogic.m_currentPlayer.CurrentPosition.ToString(), clientStream);
								}
								else
								{
									UpdateCurrentPlayer();
								}
							}
                            else
                            {
                                int charge = m_gameLogic.DetermineTrainCharge(property);
                                SendToClient(StreamConformer.MessageType.CHARGE_RENT, charge.ToString(), clientStream);
                                SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nPagou o aluguer de " +
                                    charge + " Euros ao propietario " + property.Owner.Name);
                            }
                            break;
                        }
                    case Property.PropertyType.JAIL:
                        SendToClient(StreamConformer.MessageType.JAIL_POSITION, "", clientStream);
                        break;

                    case Property.PropertyType.CHANCE:
                        {
                            String str = " ";
                            m_chanceCard = m_gameLogic.TakeChance();

                            //cria string com + ou - conforme o jogador perca ou ganhe
                            if (m_chanceCard.Value > 0)
                            {
                                str = "+";
                                m_chanceWin = true;
                            }
                            else if (m_chanceCard.Value < 0)
                            {
                                str = "-";
                                m_chanceWin = false;
                            }
                            else if (m_chanceCard.Value == 0)
                            {
                                str = " ";
                                m_chanceWin = null;
                            }

                            //manda na msg o id da carta da sorte e a string criada
                            SendToClient(StreamConformer.MessageType.CHANCE_POSITION, m_chanceCard.Id.ToString() + "," + str,
                                    m_gameLogic.m_currentPlayer.Client.GetStream());
                            break;
                        }
                    case Property.PropertyType.TAX:
                        SendToClient(StreamConformer.MessageType.TAX_POSITION, m_gameLogic.m_currentPlayer.CurrentPosition.ToString(), clientStream);
                        SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nPagou " + property.Charge + " Euros pelo " + property.Description);
                        break;

                    default://casas de descanso
                        UpdateCurrentPlayer();
                        break;
                }
            }
        }

        private void UpdateCurrentPlayer()
        {
            if (m_gameLogic.UpdateCurrentPlayer())
            {
                SendToAllClients(StreamConformer.MessageType.UPDATE_CURRENT_PLAYER, m_gameLogic.m_currentPlayerIndex.ToString());
                SendToClient(StreamConformer.MessageType.ENABLE_DICE_ROLL, "", m_gameLogic.m_currentPlayer.Client.GetStream());
            }
            else
            {
                SendToClient(StreamConformer.MessageType.IS_DOUBLE, "", m_gameLogic.m_currentPlayer.Client.GetStream());
                SendToAllClients(StreamConformer.MessageType.CHAT, m_gameLogic.m_currentPlayer.Name + " Status\r\nSaiu Double!");
                SendToClient(StreamConformer.MessageType.ENABLE_DICE_ROLL, "", m_gameLogic.m_currentPlayer.Client.GetStream());
            }
        }

        private void BuyProperty()
        {
            Player player = m_gameLogic.m_currentPlayer;
            Property property = m_gameLogic.m_propertySequence[player.CurrentPosition];
            Boolean playerHasMoney;

            //chama metodo BuyProperty da classe Player passando a propriedade atual em q/ está
            playerHasMoney = player.BuyProperty(property);

            //jogador tem dinheiro para comprar
            if (playerHasMoney)
            {
                //UPDATE_OWNER
                SendToAllClients(StreamConformer.MessageType.UPDATE_OWNER, m_gameLogic.m_currentPlayer.Name + "\r\n"  + player.CurrentPosition.ToString());
                SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
                SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nComprou a propriedade "+ property.Description);
                
                //ENABLE_GROUP_HOUSES
                //envia msg p interface para disponiblizar os botoes p comprar casa no grupo
                if (property.Type == Property.PropertyType.PROPERTY && m_gameLogic.m_currentPlayer.HasPropertyGroup(property))
                {
                    foreach (Property prop in property.Group.PropertyList)
                    {
                        String propIndex = m_gameLogic.m_propertySequence.IndexOf(prop).ToString();

                        SendToClient(StreamConformer.MessageType.ENABLE_GROUP_HOUSES,
                            propIndex + ",0", m_gameLogic.m_currentPlayer.Client.GetStream());
                    }
                }
            }
            else //PLAYER_HASNO_MONEY_2BUY
                SendToClient(StreamConformer.MessageType.PLAYER_HASNO_MONEY_2BUY, "", m_gameLogic.m_currentPlayer.Client.GetStream());

            UpdateCurrentPlayer();
        }

        private void BuyHouse(String message, TcpClient tcpClient)
        {
            int propIndex = int.Parse(message);
            Property prop = m_gameLogic.m_propertySequence[propIndex];
            Player player = FindPlayer(tcpClient);
            bool purchased = m_gameLogic.BuyHouse(prop, player);

            if (purchased)
            {
                SendToAllClients(StreamConformer.MessageType.ENABLE_GROUP_HOUSES, propIndex + "," + prop.NumHouses);
                SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
                SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nComprou um imovel na propriedade " + prop.Description);
            }
            else //PLAYER_HASNO_MONEY_2BUY
                SendToClient(StreamConformer.MessageType.PLAYER_HASNO_MONEY_2BUY, "", player.Client.GetStream());
        }

		private void SellProperty(String message, TcpClient tcpClient)
		{
			Player player = FindPlayer(tcpClient);
			int propInd = int.Parse(message);
			Property property = m_gameLogic.m_propertySequence[propInd];

			if (m_gameLogic.SellProperty(player, property))
			{
				SendToAllClients(StreamConformer.MessageType.UPDATE_OWNER, " \r\n" + propInd.ToString());
				SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
                SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nVendeu a propriedade " + property.Description);

				//DISABLE_GROUP_HOUSES
				//envia msg p interface para deixar de disponiblizar os botoes p comprar casa no grupo
				if (property.Type == Property.PropertyType.PROPERTY)
				{
					foreach (Property prop in property.Group.PropertyList)
					{
						String propIndex = m_gameLogic.m_propertySequence.IndexOf(prop).ToString();
						SendToAllClients(StreamConformer.MessageType.DISABLE_GROUP_HOUSES, propIndex);
					}
				}
                //Se está em liquidacao de dividas verifica se o saldo ja esta positivo
                if (m_playerHasDebt)
                {
                    CheckPlayerDebtAndUpdatePlayer();
                }
			}
			else
				SendToClient(StreamConformer.MessageType.PLAYER_HAS_HOUSES_2SELL, "", tcpClient.GetStream());
		}

        private void SellHouse(String message, TcpClient tcpClient)
        {
            int propIndex = int.Parse(message);
            Property prop = m_gameLogic.m_propertySequence[propIndex];
            Player player = FindPlayer(tcpClient);
            
            m_gameLogic.SellHouse(prop, player);
            
            //envia a posicao da propriedade e o seu nr de casas
			if (prop.NumHouses == 0)
			{
				SendToAllClients(StreamConformer.MessageType.DISABLE_GROUP_HOUSES, propIndex.ToString());
				SendToClient(StreamConformer.MessageType.ENABLE_GROUP_HOUSES,
				propIndex + "," + prop.NumHouses, tcpClient.GetStream());
			}
			else
			{
				SendToAllClients(StreamConformer.MessageType.ENABLE_GROUP_HOUSES,
				propIndex + "," + prop.NumHouses);
			}
            
            SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
            SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nVendeu um imovel na propriedade " + prop.Description);
            
            //Se está em liquidacao de dividas verifica se o saldo ja esta positivo
            if (m_playerHasDebt)
            {
                CheckPlayerDebtAndUpdatePlayer();
            }
            
        }

        private Player FindPlayer(TcpClient tcpClient)
        {
            foreach (Player p in m_gameLogic.m_playerList)
            {
                if (p.Client == tcpClient)
                {
                    return p;
                }
            }
            return null;
        }

        private void QueryHouses(String message, TcpClient tcpClient)
        {
            int propIndex = int.Parse(message);
            Property prop = m_gameLogic.m_propertySequence[propIndex];
            Player player = FindPlayer(tcpClient);

            if (prop.Owner != null && prop.Owner == player)
            {
                if (prop.NumHouses == 0)
                {
                    SendToClient(StreamConformer.MessageType.CAN_BUY_HOUSE, propIndex.ToString(), tcpClient.GetStream());
                }
                else if (prop.NumHouses < 5)
                {
                    SendToClient(StreamConformer.MessageType.CAN_BUY_SELL_HOUSE, propIndex.ToString(), tcpClient.GetStream());
                }
                else
                {
                    SendToClient(StreamConformer.MessageType.CAN_SELL_HOUSE, propIndex.ToString(), tcpClient.GetStream());
                }
            }
        }

        private void PayRent(String value)
        {
            m_gameLogic.PayRent(int.Parse(value));
            SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
            CheckPlayerDebtAndUpdatePlayer();
        }

        private void PayTax()
        {
            m_gameLogic.PayTax();
            SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
            CheckPlayerDebtAndUpdatePlayer();
        }

        private void CheckPlayerDebtAndUpdatePlayer()
        {
            Player player = m_gameLogic.m_currentPlayer;

            //PLAYER_HASNO_MONEY_2PAY
            if (player.Money < 0)
            {
                //jogador nao tem mais propriedades p/ vender -> faliu
                if (player.BoughtProperty.Count == 0 && player.NumTrains == 0 && player.NumCompanys == 0)
                {
                    Player winner = m_gameLogic.DetermineGameWinner();
                    SendToAllClients(StreamConformer.MessageType.GAME_IS_OVER, player.Name + "\r\n" + winner.Name);//TODO manda esta msg mas depois nada acontece nem erro da
                }
                else
                {
                    SendToClient(StreamConformer.MessageType.PLAYER_HASNO_MONEY_2PAY, "", m_gameLogic.m_currentPlayer.Client.GetStream());
                    m_playerHasDebt = true;
                }
            }
            else
            {
                UpdateCurrentPlayer();
                m_playerHasDebt = false;
            }
        }

        private void TakeChanceAction(NetworkStream clientStream, TcpClient client)
        {
            Player player = m_gameLogic.m_currentPlayer;
            switch (m_chanceCard.Type)
            {
                case Chance.ChanceType.MONEY:
                    {
                        player.UpdateMoney(m_chanceCard.Value);
                        SendToAllClients(StreamConformer.MessageType.UPDATE_MONEY, GetPlayersMoney());
                        CheckPlayerDebtAndUpdatePlayer();

                        if (m_chanceWin == true)
                        {
                            SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nTirou Carta da Sorte e ganhou " + m_chanceCard.Value + " Euros");
                        }
                        else if (m_chanceWin == false)
                        {
                            SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nTirou Carta da Sorte e perdeu " + m_chanceCard.Value + " Euros");
                        }
                        break;
                    }
                case Chance.ChanceType.POSITION:
                    int newPos = m_gameLogic.UpdatePlayerPosition(m_chanceCard.Value);
                    SendToAllClients(StreamConformer.MessageType.UPDATE_PLAYER_POSITION,
                        m_gameLogic.m_currentPlayerIndex.ToString() + m_gameLogic.m_currentPlayer.CurrentPosition);
                    ProcessPosition(m_gameLogic.m_propertySequence[newPos], 0, clientStream, client);                    
                    break;
                case Chance.ChanceType.JAIL:
                    player.GoToJail();
                    SendToAllClients(StreamConformer.MessageType.UPDATE_PLAYER_POSITION, m_gameLogic.m_currentPlayerIndex.ToString() + "10");
                    SendToAllClients(StreamConformer.MessageType.CHAT, player.Name + " Status\r\nTirou Carta da Sorte e foi para a Prisao!");
                    UpdateCurrentPlayer();
                    break;
                default:
                    break;
            }
        }

        private void SendToAllClients(StreamConformer.MessageType type, String message)
        {
            byte[] dataBuffer = StreamConformer.Encode(type, message);
            SendToAllClients(dataBuffer);
        }

        private void SendToAllClients(byte[] message)
        {
            foreach (Player player in m_gameLogic.m_playerList)
            {
                NetworkStream stream = player.Client.GetStream();
                stream.Write(message, 0, message.Length);
                stream.Flush();
            }
        }

        private void SendToAllClientsExceptCurrent(StreamConformer.MessageType type, String msg)
        {
            byte[] message = StreamConformer.Encode(type, msg);

            foreach (Player player in m_gameLogic.m_playerList)
            {
                if (player != m_gameLogic.m_currentPlayer)
                {
                    NetworkStream stream = player.Client.GetStream();
                    stream.Write(message, 0, message.Length);
                    stream.Flush();
                }
            }
        }

        private void SendToClient(StreamConformer.MessageType type, String message, NetworkStream stream)
        {
            byte[] dataBuffer = StreamConformer.Encode(type, message);
            stream.Write(dataBuffer, 0, dataBuffer.Length);
            stream.Flush();
        }

        public void Dispose()
        {
            m_tcpListener.Stop();
            foreach (TcpClient cli in m_clientList)
                cli.Close();
        }

        public static void ShuffleList<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static String IntListToString(List<int> list)
        {
            StringBuilder strBldr = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                strBldr.Append(list[i]);
                if (i < list.Count - 1)
                {
                    strBldr.Append(',');
                }
            }

            return strBldr.ToString();
        }

        public static List<int> StringToIntList(String str)
        {
            List<int> list = new List<int>();
            String nrBuffer = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ',')
                {
                    list.Add(int.Parse(nrBuffer));
                    nrBuffer = "";
                }
                else
                {
                    nrBuffer += str[i];
                }
            }
            return list;
        }

		// Obtem valores random decentes. Apenas aceita valores até 127
		public static byte GetRandomValue(byte maxValue, RNGCryptoServiceProvider generator)
		{
			if (maxValue <= 0)
				throw new ArgumentOutOfRangeException("numberSides");

			// Create a byte array to hold the random value. 
			byte[] randomNumber = new byte[1];
			do
			{
				// Fill the array with a random value.
				generator.GetBytes(randomNumber);
			}
			while (!IsValidRandom(randomNumber[0], maxValue));
			// Return the random number mod the number 
			// of sides.  The possible values are zero- 
			// based, so we add one. 
			return (byte)((randomNumber[0] % maxValue) + 1);
		}

		private static bool IsValidRandom(byte value, byte maxValue)
		{
			// There are MaxValue / numSides full sets of numbers that can come up 
			// in a single byte.  For instance, if we have a 6 sided dice, there are 
			// 42 full sets of 1-6 that come up.  The 43rd set is incomplete. 
			int fullSetsOfValues = Byte.MaxValue / maxValue;

			// If the roll is within this range of fair values, then we let it continue. 
			// In the 6 sided dice case, a roll between 0 and 251 is allowed.  (We use 
			// < rather than <= since the = portion allows through an extra 0 value). 
			// 252 through 255 would provide an extra 0, 1, 2, 3 so they are not fair 
			// to use. 
			return value < maxValue * fullSetsOfValues;
		}
	}
}
