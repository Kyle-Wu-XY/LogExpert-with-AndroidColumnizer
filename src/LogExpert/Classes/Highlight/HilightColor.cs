using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;

namespace LogExpert
{
    public class HighlightColor
    {
        public HighlightColor(Color bg, Color fg)
        {
            this.bg = bg;
            this.fg = fg;
            this.text = "";
        }

        public Color bg;
        public Color fg;
        public String text = "";
    }

}
