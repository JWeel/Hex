using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Forms
{
    internal interface IControls
    {
        List<Control> Controls { get; }

        Control FindControlAt(Point position);
    }
}