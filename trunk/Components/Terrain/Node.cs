using System;
using System.Diagnostics;

namespace Laan.DLOD
{
    abstract class Node
    {
        static private Direction[] DIRECTIONS = new Direction[] 
        {
            Direction.West, 
            Direction.South, 
            Direction.East, 
            Direction.North 
        };

        // unique reference - only required for debugging purposes
        internal int ID;

        // stores the indexes within the patch of this Node
        internal int[]      _indexes;

        internal Node(Patch patch)
        {
            Patch = patch;
            ID = ++uniqueCount;
        }

        // stores any children associated with this Triangle, after splitting
        // indicates the level of recursion of splitting
        // indicates the inherited cardinality of this Node (N, E, S, W)
        // a reference to the owning patch - in order to obtain the split level
        // reference to the parent node in order to 'inherit' orientation

        protected static int uniqueCount;
        protected static int directionCount;
        
        [Conditional("TRACE")]
        protected void TraceChildren()
        {
            //for(int index = 0; index < 2; index++)
            //{
            //    Triangle c = Children[index];
            //    Trace.WriteLine(
            //        String.Format(
            //            "ID:{0}  Depth:{1}  Idx:{2, 2}/{3, 2}/{4, 2}",
            //            c.ID, c.Depth, c.indexes[0], c.indexes[1], c.indexes[2]
            //        )
            //    );
            //}
        }

        protected void Split()
        {
            if(Depth <= Patch.Level || ((Depth - Patch.Level == 1) && SplitOnEdge()))
            //if ()
            {
                InstantiateChildren();

                int[,] data = new int[2, 2] { {0, 1}, {2, 0} };

                for(int index = 0; index < 2; index++)
                {
                    int middleIndex = Math.Abs((int)((_indexes[1] - _indexes[2]) / 2) + _indexes[2]);
                    Children[index]._indexes = new int[3] {
                        middleIndex,
                        _indexes[data[index, 0]],
                        _indexes[data[index, 1]]
                    };
                    InheritDirectionFromParent(Children[index]);
                }

                TraceChildren();
                SplitChildren();
            }
        }

        protected void SplitChildren()
        {
            for(int index = 0; index < 2; index++)
                Children[index].Split();
        }

        protected void InstantiateChildren()
        {
            Children = new Triangle[2];
            for(int index = 0; index < 2; index++)
            {
                Children[index] = new Triangle(this, Patch);

                // level 1 is the first depth that can have a defined orientation -
                // subsequent levels will inherited it from their parent
                if(Depth == 1)
                    Children[index].Orientation = DIRECTIONS[directionCount++];
            }
        }

        private void InheritDirectionFromParent(Node node)
        {
            // all non-internal nodes simple obtain their direction from their parent
            // special case: direction is none is used for level 0 nodes which have no direction
            if(node.IsInternal())
                node.Orientation = Direction.Internal;
            else
                if(node.Parent != null && node.Parent.Orientation != Direction.None)
                    node.Orientation = node.Parent.Orientation;
        }

        private bool IndexIsOnEdge(int index)
        {
            // index touches the tile border if it's x or y coord is 0, or Size - 1
            int width = Patch.Size - 1;
            int x = (int)(index / Patch.Size);
            int y = (int)(index % Patch.Size);
            return (x == 0 || y== 0 || x == width || y == width);
        }

        private bool IsInternal()
        {
            // a node is Internal if it has less than two points (indexes) on the edge
            int count = 0;
            foreach(int index in _indexes)
                if(IndexIsOnEdge(index))
                    count++;

            return (count < 2);
        }

        private bool SplitOnEdge()
        {
            // do not process even patch levels, internal triangles, or level 0 (Direction=None)
            if(Orientation == Direction.Internal || Orientation == Direction.None || Patch.Level % 2 == 0)
                return false;

            Patch sibling = null;
            if (Patch.HasSibling(Orientation, ref sibling) && (Patch.Level == sibling.Level - 1))
                return true;
            
            return false;
        }

        internal protected void Split(ref int pos, ref int[] _allIndexes)
        {
            Children[0].CalcAllIndexes(ref pos, ref _allIndexes);
            Children[1].CalcAllIndexes(ref pos, ref _allIndexes);
        }

        internal virtual void CalcAllIndexes(ref int pos, ref int[] _allIndexes)
        {
            if (Children != null)
            {
                Split(ref pos, ref _allIndexes);
            }
            else
            {
                foreach (int index in _indexes)
                    _allIndexes[pos++] = index;
            }
        }

        public Triangle[] Children { get; set; }
        public int Depth { get; set; }
        // indicates the inherited cardinality of this Node (N, E, S, W)
        public Direction Orientation { get; set; }
        public Node Parent { get; set; }
        public Patch Patch { get; set; }
    }
}

