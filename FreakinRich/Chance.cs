using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace FreakinRich
{
    class Chance
    {
        public enum ChanceType { MONEY, JAIL, POSITION}
        private ChanceType m_type;
        private int m_value;
        private String m_description; 
        private int m_id;

        public Chance(int value, String description, ChanceType type, int id)
        {
            m_description = description;
            m_value = value;
            m_type = type;
            m_id = id;
        }

        public ChanceType Type
        {
            get { return m_type; }
        }
        
        public int Value
        {
            get { return m_value; }
        }

        public int Id
        {
            get { return m_id; }
        }
    }
}
