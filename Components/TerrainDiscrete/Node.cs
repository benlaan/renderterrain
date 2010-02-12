using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Laan.DLOD.Discrete
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
            int middleIndex = Math.Abs((int)((_indexes[1] - _indexes[2]) / 2) + _indexes[2]);
            var middleVertex = Patch.VertexBuffer[middleIndex].Position;

            if (Depth <= Patch.MaxTerrainDepth + 1 && Patch.GetActive(middleVertex))
            {
                InstantiateChildren();

                int[,] data = new int[2, 2] { { 0, 1 }, { 2, 0 } };

                for (int index = 0; index < 2; index++)
                {
                    Children[index]._indexes = new int[3] {
                        middleIndex,
                        _indexes[data[index, 0]],
                        _indexes[data[index, 1]]
                    };
                }

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
            for (int index = 0; index < 2; index++)
                Children[index] = new Triangle(this, Patch);
        }

        internal protected void Split(List<Int32> indexes)
        {
            Children[0].CalcAllIndexes(indexes);
            Children[1].CalcAllIndexes(indexes);
        }

        internal virtual void CalcAllIndexes(List<Int32> indexes)
        {
            if (Children != null)
                Split(indexes);
            else
                indexes.AddRange(_indexes);
        }

        public Triangle[] Children { get; set; }
        public int Depth { get; set; }
        public Node Parent { get; set; }
        public Patch Patch { get; set; }
    }
}

