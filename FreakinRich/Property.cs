using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace FreakinRich
{
    class Property
    {
        public enum PropertyType { PROPERTY, JAIL, CHANCE, TAX, START, TRAIN, COMPANY, OTHER };
        private PropertyType m_type;    //Tipo de propriedade
        private PropertyGroup m_group;  //Grupo a que a propriedade pertence
        private String m_description;   //Descrição 
        private int? m_value;           //valor da compra 
        private int? m_buildingValue;   //valor da compra de casas/hotel
        private Player m_owner;         //Jogador proprietario 
        private int m_numHouses;        //Num. de casas compradas pelo proprietario
        private int? m_charge;          //Combrança pela propriedade
        private int? m_chargeHouse1;    //Cobrança por 1 casa construida
        private int? m_chargeHouse2;    //Cobrança por 2 casas construidas
        private int? m_chargeHouse3;    //Cobrança por 3 casas construidas
        private int? m_chargeHouse4;    //Cobrança por 4 casa construidas
        private int? m_chargeHotel;     //Cobrança por hotel

        public Property(PropertyType type, String desc, int? value, int? charge, int? house1, int? house2, int? house3, int? house4, int? hotel, int? buildingValue)
        {
            m_type = type;
            m_description = desc;
            m_value = value;
            m_owner = null;
            m_numHouses = 0;
            m_charge = charge;
            m_chargeHouse1 = house1;
            m_chargeHouse2 = house2;
            m_chargeHouse3 = house3;
            m_chargeHouse4 = house4;
            m_chargeHotel = hotel;
            m_buildingValue = buildingValue;
        }

        public String Description
        {
            get { return m_description; }
        }

        public int? BuildingValue
        {
            get { return m_buildingValue; }
        }

        public int? ChargeHotel
        {
            get { return m_chargeHotel; }
        }

        public int? ChargeHouse4
        {
            get { return m_chargeHouse4; }
        }
        
        public int? ChargeHouse3
        {
            get { return m_chargeHouse3; }
        }

        public int? ChargeHouse2
        {
            get { return m_chargeHouse2; }
        }

        public int? ChargeHouse1
        {
            get { return m_chargeHouse1; }
        }

        public int? Charge
        {
            get { return m_charge; }
        }

        public int NumHouses
        {
            get { return m_numHouses; }
            set { m_numHouses = value; }
        }
        
        internal PropertyGroup Group
        {
            get { return m_group; }
            set { m_group = value; }
        }

        public PropertyType Type
        {
            get { return m_type; }
        }

        internal Player Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
        
        public int? Value
        {
            get { return m_value; }
        }

    }
}
