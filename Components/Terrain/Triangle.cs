using System;

namespace Laan.DLOD
{
    class Triangle : Node
    {
        internal Triangle(Node parent, Patch patch) : base(patch)
        {
            Parent = parent;
            Depth = parent.Depth + 1;
        }
    }
}
