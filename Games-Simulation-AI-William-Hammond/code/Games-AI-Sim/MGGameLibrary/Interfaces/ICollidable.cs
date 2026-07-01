using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;

namespace MGGameLibrary.Interfaces
{
    public interface ICollidable
    {
        Shape Shapes { get; }
        bool CollidesWith(ICollidable other);
        bool CollidesWith(ICollidable other, ref Vector2 collisionNormal);
    }
}
