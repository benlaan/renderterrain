using System;
using System.Diagnostics;

namespace Laan.DLOD
{
    class RootNode : Node
    {

        internal RootNode(Patch patch) : base(patch)
        {
            Depth = 0;
            Orientation = Direction.None;

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

        internal override void CalcAllIndexes(ref int pos, ref int[] _allIndexes)
        {
            Split(ref pos, ref _allIndexes);
        }
    }
}
