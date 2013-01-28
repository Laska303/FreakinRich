using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net.Sockets;

namespace FreakinRich
{
    class Player
    {
        private int m_currentPosition;              //Posição atual no tabuleiro
        private int m_money;                        //Dinheiro q/ o jogador detem
        private int m_jailCount;                    //Contador do nº de vezes q/ o jogador esteve retido sem jogar
        private String m_name;                      //Nome do jogador
        
        private List<Property> m_boughtProperty;    //Propriedades que o jogador detem
        private TcpClient m_client;                 //Endereço do ciente/jogador
        private int m_numTrains;                    //Num. de estações compradas 
        private int m_numCompanys;                  //Num. de companhias compradas

        public Player(int money, String name, TcpClient client)
        {
            m_currentPosition = 0;
            m_money = money;
            m_name = name;
            m_client = client;
            m_numTrains = 0;
            m_numCompanys = 0;
            m_jailCount = 0;
            m_boughtProperty = new List<Property>();
        }

        public String Name
        {
            get { return m_name; }
        }

        public int CurrentPosition
        {
            get { return m_currentPosition;}
            set { m_currentPosition = value; }
        }

        internal List<Property> BoughtProperty
        {
            get { return m_boughtProperty; }
            set { m_boughtProperty = value; }
        }

        public int Money
        {
            get { return m_money; }
            set { m_money = value; }
        }

        public TcpClient Client
        {
            get { return m_client; }
        }

        public int NumTrains
        {
            get { return m_numTrains; }
            set { m_numTrains = value; }
        }

        public int NumCompanys
        {
            get { return m_numCompanys; }
            set { m_numCompanys = value; }
        }

        public int JailCount
        {
            get { return m_jailCount; }
            set { m_jailCount = value; }
        }

        public void GoToJail()
        {
            m_currentPosition = 10;
            m_jailCount = 2;
        }

        public void UpdateMoney(int value)
        {
            m_money += value;
        }

        public Boolean HasPropertyGroup(Property property)
        {
            PropertyGroup group = property.Group;
            Player player = property.Owner;
            int count = 0;

            foreach (Property prop in group.PropertyList)
            {
                if (prop.Owner == player)
                    count++;
            }

            if (count == group.PropertyList.Count)
                return true;
            else
                return false;
        }

        public Boolean BuyProperty(Property property)
        {
            //verifica se o jogador possui o dinheiro suficiente
            if (m_money < property.Value)
                return false;

            UpdateMoney(-property.Value.Value);

            switch (property.Type)
	        {
		        case Property.PropertyType.PROPERTY:
                    m_boughtProperty.Add(property);
                    break;

                case Property.PropertyType.TRAIN:
                    m_numTrains++;
                    break;

                case Property.PropertyType.COMPANY:
                    m_numCompanys++;
                    break;
	        }

            property.Owner = this;
            return true;
            
        }

        public Boolean OwnsProperty()
        { 
            if (m_boughtProperty.Count == 0 && m_numCompanys == 0 && m_numTrains==0 )
                return false;
            
            return true;
        }

        public Boolean HasMoneyToPay(int value)
        { 
            if (m_money >= value)
                return true;
            else
                return false;
        }
        
    }
}
