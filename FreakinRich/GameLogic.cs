using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace FreakinRich
{
    class GameLogic
    {
        public List<Property> m_propertySequence;   //Lista de casas/propriedades do tabuleiro
        public List<Player> m_playerList;           //Lista de jogadores
        public List<Chance> m_chanceList;           //Lista de cartas da sorte
        public Player m_currentPlayer;              //Jogador atual a jogar
        public int m_currentPlayerIndex;            //Indice do jogador atual a jogar
        public int m_doubleCount;                   //Contador do nº de vezes em que saiu valores iguais nos 2 dados 

		private RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

        public GameLogic()
        {
            m_propertySequence = new List<Property>();  
            m_playerList = new List<Player>();          
            m_chanceList = new List<Chance>();           
            m_currentPlayer = null;                             
            m_currentPlayerIndex = -1;                      
            m_doubleCount = 0;                          

            LoadPropertySequence();
            LoadPropertyGroups();
            LoadRandomizedChanceList();
        }

        public void LoadRandomizedChanceList()
        {
            List<Chance> tmpChanceList = new List<Chance>();
            tmpChanceList.Add(new Chance(-1000, "perdeu", Chance.ChanceType.MONEY, 1));
            tmpChanceList.Add(new Chance(-5000, "perdeu", Chance.ChanceType.MONEY, 2));
            tmpChanceList.Add(new Chance(-2500, "perdeu", Chance.ChanceType.MONEY, 3));
            tmpChanceList.Add(new Chance(-2000, "perdeu", Chance.ChanceType.MONEY, 4));
            tmpChanceList.Add(new Chance(-1000, "perdeu", Chance.ChanceType.MONEY, 5));
            tmpChanceList.Add(new Chance(-5000, "perdeu", Chance.ChanceType.MONEY, 6));
            tmpChanceList.Add(new Chance(-1000, "perdeu", Chance.ChanceType.MONEY, 7));
            tmpChanceList.Add(new Chance(5000, "ganhou", Chance.ChanceType.MONEY, 8));
            tmpChanceList.Add(new Chance(1000, "ganhou", Chance.ChanceType.MONEY, 9));
            tmpChanceList.Add(new Chance(2000, "ganhou", Chance.ChanceType.MONEY, 10));
            tmpChanceList.Add(new Chance(5000, "ganhou", Chance.ChanceType.MONEY, 11));
            tmpChanceList.Add(new Chance(2000, "ganhou", Chance.ChanceType.MONEY, 12));
            tmpChanceList.Add(new Chance(10000, "ganhou", Chance.ChanceType.MONEY, 13));
            tmpChanceList.Add(new Chance(3, "avance 3", Chance.ChanceType.POSITION, 14));
            tmpChanceList.Add(new Chance(10, "jail", Chance.ChanceType.JAIL, 15));


			for (byte i = (byte)tmpChanceList.Count; i > 0; i--)
            {
				int index = Server.GetRandomValue(i, rngCsp) - 1; 
                m_chanceList.Add(tmpChanceList[index]);
                tmpChanceList.RemoveAt(index);
            }
        }

        public void LoadPropertySequence()
        {
            m_propertySequence.Add(new Property(Property.PropertyType.START, "partida", null, null, null, null, null, null, null, null));             //0
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Azul 1>", 6000, 200, 1000, 3000, 9000, 16000, 25000, 5000));           //1
            m_propertySequence.Add(new Property(Property.PropertyType.CHANCE, "caixa", null, null, null, null, null, null, null, null));              //2
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Azul 2>", 6000, 400, 2000, 6000, 18000, 32000, 45000, 5000));          //3
            m_propertySequence.Add(new Property(Property.PropertyType.TAX, "Imposto Sobre Capitais", null, 20000, null, null, null, null, null, null));  //4
            m_propertySequence.Add(new Property(Property.PropertyType.TRAIN, "<Estacao Sete Bicas>", 20000, null, null, null, null, null, null, null));             //5
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Azul Claro 1>", 10000, 600, 3000, 9000, 27000, 40000, 55000, 5000));    //6
            m_propertySequence.Add(new Property(Property.PropertyType.CHANCE, "sorte", null, null, null, null, null, null, null, null));              //7
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Azul Claro 2>", 10000, 600, 3000, 9000, 27000, 40000, 55000, 5000));    //8
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Azul Claro 3>", 12000, 800, 4000, 10000, 30000, 45000, 60000, 5000));   //9
            m_propertySequence.Add(new Property(Property.PropertyType.OTHER, "visitante da prisao", null, null, null, null, null, null, null, null)); //10
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Verde 1>", 14000, 1000, 5000, 15000, 45000, 62000, 75000, 10000));     //11
            m_propertySequence.Add(new Property(Property.PropertyType.COMPANY, "<Companhia da Eletricidade>", 15000, null, null, null, null, null, null, null));         //12
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Verde 2>", 14000, 1000, 5000, 15000, 45000, 62000, 75000, 10000));     //13
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Verde 3>", 16000, 1200, 6000, 18000, 50000, 70000, 90000, 10000));     //14
            m_propertySequence.Add(new Property(Property.PropertyType.TRAIN, "<Estacao Lapa>", 20000, null, null, null, null, null, null, null));             //15
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Amarelo 1>", 18000, 1400, 7000, 20000, 55000, 75000, 95000, 10000));    //16
            m_propertySequence.Add(new Property(Property.PropertyType.CHANCE, "caixa", null, null, null, null, null, null, null, null));              //17
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Amarelo 2>", 18000, 1400, 7000, 20000, 55000, 75000, 95000, 10000));    //18
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Amarelo 3>", 20000, 1600, 8000, 22000, 60000, 80000, 100000, 10000));   //19
            m_propertySequence.Add(new Property(Property.PropertyType.OTHER, "rest", null, null, null, null, null, null, null, null));                //20
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Laranja 1>", 22000, 1800, 9000, 25000, 70000, 87500, 105000, 15000));   //21
            m_propertySequence.Add(new Property(Property.PropertyType.CHANCE, "sorte", null, null, null, null, null, null, null, null));              //22
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Laranja 2>", 22000, 1800, 9000, 25000, 70000, 87500, 105000, 15000));   //23
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Laranja 3>", 24000, 2000, 10000, 30000, 75000, 92500, 110000, 15000));  //24
            m_propertySequence.Add(new Property(Property.PropertyType.TRAIN, "Estacao Parque Real", 20000, null, null, null, null, null, null, null));             //25
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Vermelho 1>", 26000, 2200, 11000, 33000, 80000, 97500, 115000, 15000));     //26
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Vermelho 2>", 26000, 2200, 11000, 33000, 80000, 97500, 115000, 15000));     //27
            m_propertySequence.Add(new Property(Property.PropertyType.COMPANY, "<Companhia das Aguas>", 15000, null, null, null, null, null, null, null));         //28
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Vermelho 3>", 28000, 2400, 12000, 36000, 85000, 102500, 120000, 15000));    //29
            m_propertySequence.Add(new Property(Property.PropertyType.JAIL, "va para a prisao", null, null, null, null, null, null, null, null));     //30
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Rosa 1>", 30000, 2600, 13000, 39000, 90000, 110000, 127500, 20000));   //31
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Rosa 2>", 30000, 2600, 13000, 39000, 90000, 110000, 127500, 20000));   //32
            m_propertySequence.Add(new Property(Property.PropertyType.CHANCE, "caixa", null, null, null, null, null, null, null, null));              //33
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Rosa 3>", 32000, 2800, 15000, 45000, 100000, 120000, 140000, 20000));  //34
            m_propertySequence.Add(new Property(Property.PropertyType.TRAIN, "<Estacao Aliados>", 20000, null, null, null, null, null, null, null));             //35
            m_propertySequence.Add(new Property(Property.PropertyType.CHANCE, "sorte", null, null, null, null, null, null, null, null));              //36
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Roxo 1>", 35000, 3500, 17500, 50000, 110000, 130000, 150000, 20000));//37
            m_propertySequence.Add(new Property(Property.PropertyType.TAX, "Imposto de Luxo", null, 10000, null, null, null, null, null, null));      //38
            m_propertySequence.Add(new Property(Property.PropertyType.PROPERTY, "<Grupo Roxo 2>", 40000, 5000, 20000, 60000, 140000, 170000, 200000, 20000));//39
        }

        public void LoadPropertyGroups()
        { 
            PropertyGroup gBlue = new PropertyGroup();
            gBlue.ConstructionBuyValue = 5000;
            gBlue.PropertyList.Add(m_propertySequence[1]);
            gBlue.PropertyList.Add(m_propertySequence[3]);

            m_propertySequence[1].Group = gBlue;
            m_propertySequence[3].Group = gBlue;
            
            PropertyGroup gLightBlue= new PropertyGroup();
            gLightBlue.ConstructionBuyValue = 5000;
            gLightBlue.PropertyList.Add(m_propertySequence[6]);
            gLightBlue.PropertyList.Add(m_propertySequence[8]);
            gLightBlue.PropertyList.Add(m_propertySequence[9]);

            m_propertySequence[6].Group = gLightBlue;
            m_propertySequence[8].Group = gLightBlue;
            m_propertySequence[9].Group = gLightBlue;

            PropertyGroup gGreen= new PropertyGroup();
            gGreen.ConstructionBuyValue = 10000;
            gGreen.PropertyList.Add(m_propertySequence[11]);
            gGreen.PropertyList.Add(m_propertySequence[13]);
            gGreen.PropertyList.Add(m_propertySequence[14]);

            m_propertySequence[11].Group = gGreen;
            m_propertySequence[13].Group = gGreen;
            m_propertySequence[14].Group = gGreen;
                                    
            PropertyGroup gYellow= new PropertyGroup();
            gYellow.ConstructionBuyValue = 10000;
            gYellow.PropertyList.Add(m_propertySequence[16]);
            gYellow.PropertyList.Add(m_propertySequence[18]);
            gYellow.PropertyList.Add(m_propertySequence[19]);

            m_propertySequence[16].Group = gYellow;
            m_propertySequence[18].Group = gYellow;
            m_propertySequence[19].Group = gYellow;

            PropertyGroup gOrange= new PropertyGroup();
            gOrange.ConstructionBuyValue = 15000;
            gOrange.PropertyList.Add(m_propertySequence[21]);
            gOrange.PropertyList.Add(m_propertySequence[23]);
            gOrange.PropertyList.Add(m_propertySequence[24]);

            m_propertySequence[21].Group = gOrange;
            m_propertySequence[23].Group = gOrange;
            m_propertySequence[24].Group = gOrange;

            PropertyGroup gRed= new PropertyGroup();
            gRed.ConstructionBuyValue = 15000;
            gRed.PropertyList.Add(m_propertySequence[26]);
            gRed.PropertyList.Add(m_propertySequence[27]);
            gRed.PropertyList.Add(m_propertySequence[29]);

            m_propertySequence[26].Group = gRed;
            m_propertySequence[27].Group = gRed;
            m_propertySequence[29].Group = gRed;

            PropertyGroup gPink= new PropertyGroup();
            gPink.ConstructionBuyValue = 20000;
            gPink.PropertyList.Add(m_propertySequence[31]);
            gPink.PropertyList.Add(m_propertySequence[32]);
            gPink.PropertyList.Add(m_propertySequence[34]);

            m_propertySequence[31].Group = gPink;
            m_propertySequence[32].Group = gPink;
            m_propertySequence[34].Group = gPink;

            PropertyGroup gPurple= new PropertyGroup();
            gPurple.ConstructionBuyValue = 20000;
            gPurple.PropertyList.Add(m_propertySequence[37]);
            gPurple.PropertyList.Add(m_propertySequence[39]);

            m_propertySequence[37].Group = gPurple;
            m_propertySequence[39].Group = gPurple;

            PropertyGroup gCompany = new PropertyGroup();
            gCompany.ConstructionBuyValue = 0;
            gCompany.PropertyList.Add(m_propertySequence[12]);
            gCompany.PropertyList.Add(m_propertySequence[28]);

            m_propertySequence[12].Group = gCompany;
            m_propertySequence[28].Group = gCompany;

            PropertyGroup gTrain = new PropertyGroup();
            gTrain.ConstructionBuyValue = 0;
            gTrain.PropertyList.Add(m_propertySequence[5]);
            gTrain.PropertyList.Add(m_propertySequence[15]);
            gTrain.PropertyList.Add(m_propertySequence[25]);
            gTrain.PropertyList.Add(m_propertySequence[35]);

            m_propertySequence[5].Group = gTrain;
            m_propertySequence[15].Group = gTrain;
            m_propertySequence[25].Group = gTrain;
            m_propertySequence[35].Group = gTrain;
        }

        public void AddPlayer(TcpClient client, String name)
        {
            Player player = new Player(150000, name, client);
            m_playerList.Add(player);
        }

		public Property ProcessDiceRoll(int diceValue, out int newPosition)
        {            
            //atualiza a posição do jogador
            newPosition = UpdatePlayerPosition(diceValue);

            return m_propertySequence[newPosition];
        }

        public Boolean UpdateCurrentPlayer()
        {
            if (m_doubleCount > 0)
                return false;	// Não é necessário atualizar o current player, porque ele tem que voltar a jogar

            while (true)
            {
                //atualiza currentPlayer para o player seguinte
                //se o seguinte for o ultimo da lista +1, currentPlayer é definido como o primeiro da lista:0
                m_currentPlayerIndex = (m_currentPlayerIndex + 1) % m_playerList.Count;
                m_currentPlayer = m_playerList[m_currentPlayerIndex];


                //verifica se o PROXIMO jogador está bloqueado e atualiza o nº de vezes q/ deve ficar retido
                if (m_currentPlayer.JailCount > 0)
                {
                    m_currentPlayer.JailCount--;
                }
                //se o proximo jogador nao estiver bloqueado, pode jogar (currentPlayer ja foi atualizado)
                else
                    break;
            }
            return true;
        }

        public int UpdatePlayerPosition(int value)
        {
            int newPos;

            newPos = m_currentPlayer.CurrentPosition + value;

            //se a posição passar pela partida recebe 20000
            if (newPos >= m_propertySequence.Count)
            {
                newPos = newPos - m_propertySequence.Count;
                m_currentPlayer.UpdateMoney(20000);
            }
            m_currentPlayer.CurrentPosition = newPos;
            return newPos;
        }

        public bool CheckDouble(int value1, int value2)
        {
            //compara os valores dos dois dados
            if (value1 == value2)
                m_doubleCount++;
            else
                m_doubleCount = 0;

            if (m_doubleCount == 3)
            {
                // manda-o para a cadeia
                m_doubleCount = 0;
                return true;
            }
            return false;
        }

        public int DeterminePropertyCharge(Property property)
        {
            //verifica se o jogador tem o grupo das propriedades
            if (property.Owner.HasPropertyGroup(property))
            {
                if (property.NumHouses == 1)
                    return property.ChargeHouse1.Value;
                
                if (property.NumHouses == 2)
                    return property.ChargeHouse2.Value;

                if (property.NumHouses == 3)
                    return property.ChargeHouse3.Value;

                if (property.NumHouses == 4)
                    return property.ChargeHouse4.Value;

                if (property.NumHouses == 5)
                    return property.ChargeHotel.Value;

                return property.Charge.Value * 2;
            }
            return property.Charge.Value;
        }

        public void PayRent(int value)
        {
            m_currentPlayer.UpdateMoney(-value);
            m_propertySequence[m_currentPlayer.CurrentPosition].Owner.UpdateMoney(value);
        }

        public void PayTax()
        {
            int chargeValue = (int)m_propertySequence[m_currentPlayer.CurrentPosition].Charge;
            m_currentPlayer.UpdateMoney(-chargeValue);
        }

        public Chance TakeChance()
        { 
            m_chanceList.Add(m_chanceList[0]);
            m_chanceList.RemoveAt(0);
            return m_chanceList[m_chanceList.Count-1];
        }

        public Boolean BuyHouse(Property prop, Player player)
        {
            int buildingValue = (int)prop.BuildingValue;

            //verifica se o jogador possui o dinheiro suficiente ou se já comprou as casas todas
            if ((player.Money < buildingValue) || prop.NumHouses >= 5)
                return false;

            player.UpdateMoney(-buildingValue);

            prop.NumHouses++;
            
            return true;
        }

        public void SellHouse(Property prop, Player player)
        {
            int buildingValue = (int)prop.BuildingValue;

            player.UpdateMoney((int)(buildingValue * 0.5));

			if (prop.NumHouses == 5)
				prop.NumHouses = 0;
			else if (prop.NumHouses > 1)
				prop.NumHouses--;
			else
				prop.NumHouses = 0;
        }

		public Boolean SellProperty(Player player, Property prop)
        {
            switch (prop.Type)
            {
                case Property.PropertyType.PROPERTY:
                {
                    PropertyGroup group = prop.Group;
                    foreach (Property property in group.PropertyList)
                    {
                        if (property.NumHouses > 0)
                        {
                            return false;
                        }
                    }
                    player.BoughtProperty.Remove(prop);

                    break;
                }

                case Property.PropertyType.TRAIN:
                    player.NumTrains--;
                    break;

                case Property.PropertyType.COMPANY:
                    player.NumCompanys--;
                    break;
            }

            int propValue = (int)prop.Value;
            player.UpdateMoney((int)(propValue * 0.5));

            prop.Owner = null;

            return true;
        }

        public int DetermineTrainCharge(Property property)
        { 
            if (property.Owner.NumTrains == 1)
                return 2500;
            if (property.Owner.NumTrains == 2)
                return 5000;
            if (property.Owner.NumTrains == 3)
                return 10000;
            return 20000;
        }

        public int DetermineCompanyCharge(Property property, int diceValue)
        {
            if (property.Owner.NumCompanys > 1)
                return (1000 * diceValue);
            else
                return (400 * diceValue);
        }

        public Player DetermineGameWinner()
        {
            float money = 0;
            float maxMoney = 0;
            Player maxPlayer = null;

            foreach (Player player in m_playerList)
            {
                money = player.Money;
                foreach (Property prop in player.BoughtProperty)
                {
                    money += prop.Value.Value * 0.5f;
                    money += prop.BuildingValue.Value * 0.5f * prop.NumHouses;
                }
                 
                money += player.NumCompanys * 15000 * 0.5f;
                money += player.NumTrains * 20000 * 0.5f;

                if (money >= maxMoney)
                {
                    maxMoney = money;
                    maxPlayer = player;
                }
            }
            return maxPlayer;
        }
    }
}
