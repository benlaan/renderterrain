using System;
using System.Collections.Generic;

namespace Laan.DLOD.Discrete
{
    class RootNode : Node
    {

        internal RootNode(Patch patch) : base(patch)
        {
            Initialize();
        }

        internal void Initialize()
        {
            uniqueCount = 0;
            directionCount = 0;

            InstantiateChildren();

            int width = Patch.Size - 1;
            Children[0]._indexes = new int[3] { 0, Patch.Size * width, width };
            Children[1]._indexes = new int[3] { (Patch.Size * Patch.Size) - 1, width, Patch.Size * width };

            TraceChildren();
            SplitChildren();
        }

        internal override void CalcAllIndexes(List<Int32> indexes)
        {
            Split(indexes);
        }
    }
}
