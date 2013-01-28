using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace FreakinRich
{
    class PlayerMarker
    {
        PictureBox m_pb_playerMarker = null;
        Form m_mainForm = null;
        Point m_direction;
        float m_unitsPerMs;
        long m_MsToComplete;
        Point m_newPosition;

        public PlayerMarker(Bitmap pic, Point location, int playerId, Form main)
        {
            m_pb_playerMarker = new PictureBox
            {
                Name = "player" + playerId.ToString() + 1,
                Size = new Size(30, 30),
                BackgroundImageLayout = ImageLayout.Zoom,
                BackColor = System.Drawing.Color.Transparent,
                Location = location,
                BackgroundImage = pic
            };

            m_mainForm = main;
        }

        public void AddtoControl(Control ctr)
        {
            ctr.Controls.Add(m_pb_playerMarker);
        }

        public void Move(Point location)
        {
            float unitsPerSec = 0.5f;
            Point amount = SubVector(location, m_pb_playerMarker.Location);
            double lenght = LenghtVector(amount);

            m_direction = DivideVectorByScalar(amount, lenght); 
            m_unitsPerMs = unitsPerSec / 1000;
            m_MsToComplete = (long)(lenght / m_unitsPerMs) * 1000;
            m_newPosition.X = m_pb_playerMarker.Location.X + amount.X; 
            m_newPosition.Y = m_pb_playerMarker.Location.Y + amount.Y;
        }

        static public Point SubVector(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        static public double LenghtVector(Point p)
        {
            float sqrdlength = p.X * p.X + p.Y * p.Y;

            return Math.Sqrt(sqrdlength);
        }

        static public Point DivideVectorByScalar(Point p, double scalar)
        {
            return new Point((int)(p.X / scalar), (int)(p.Y / scalar));
        }
 
    }
    
}
