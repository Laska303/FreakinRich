using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace FreakinRich
{
    class ButtonPictureBox : PictureBox
    {
        [EditorBrowsable(EditorBrowsableState.Always)]
        [Browsable(true)]
        public override string Text { get; set; }

        private Bitmap mNormalBackgroundImage = null;

        public Bitmap NormalBackgroundImage {
            get { return mNormalBackgroundImage; }
            set { base.BackgroundImage = mNormalBackgroundImage = value; } 
        }
        public Bitmap ClickBackgroundImage { get; set; }

        public ButtonPictureBox()
        {
            base.BackgroundImageLayout = ImageLayout.Stretch;
            base.BackColor = Color.Transparent;
            base.Size = new Size(137, 27);

            NormalBackgroundImage = Properties.Resources.bt_off;
            ClickBackgroundImage = Properties.Resources.bt_on;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            using (Font myFont = new Font("Microsoft Sans Serif", 8.25f))
            {
                StringFormat format = new StringFormat();
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Center;
                float x = this.Size.Width * 0.5f;
                float y = this.Size.Height * 0.5f;
                pe.Graphics.DrawString(Text, myFont, Brushes.Black, x, y, format);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;            
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (ClickBackgroundImage != null)
                base.BackgroundImage = ClickBackgroundImage;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (NormalBackgroundImage != null)
                base.BackgroundImage = NormalBackgroundImage;
        }


    }
}
