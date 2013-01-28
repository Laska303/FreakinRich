using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreakinRich
{
    class PropertyGroup
    {
        private List<Property> m_propertyList = new List<Property>();   //Lista de propriedades
        private int m_constructionBuyValue;                             //Valor da compra de casas/hoteis
        
        public int ConstructionBuyValue
        {
            get { return m_constructionBuyValue; }
            set { m_constructionBuyValue = value; }
        }

        internal List<Property> PropertyList
        {
            get { return m_propertyList; }
            set { m_propertyList = value; }
        }
        


    }
}
