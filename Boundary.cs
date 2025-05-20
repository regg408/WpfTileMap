using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfTileMap
{
    internal class Boundary
    {
        public double East;
        public double South;
        public double West;
        public double North;

        public double Width
        {
            get
            {
                return this.East - this.West;
            }
        }

        public double Height
        {
            get
            {
                return this.North - this.South;
            }
        }

        public Boundary()
        {
            this.East = 0;
            this.South = 0;
            this.West = 0;
            this.North = 0;
        }

        public Boundary(double east, double south, double west, double north)
        {
            this.East = east;
            this.South = south;
            this.West = west;
            this.North = north;
        }

        public bool IsIntersect(Boundary bound)
        {
            return !(this.East < bound.West ||
                this.West > bound.East ||
                this.North < bound.South ||
                this.South > bound.North);
        }

        public void Set(double east, double south, double west, double north)
        {
            this.East = east;
            this.South = south;
            this.West = west;
            this.North = north;
        }
    }
}
