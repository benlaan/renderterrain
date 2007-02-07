using System;
using System.Diagnostics;

namespace Laan.DLOD
{

	abstract class Node
	{
		// unique reference - only required for debugging purposes
		internal int ID;

		// stores the indexes within the patch of this Node
		internal int[]      indexes;

		internal Node(Patch patch)
		{
			_owningPatch = patch;
			ID = ++uniqueCount;
		}

		// stores any children associated with this Triangle, after splitting
		private Triangle[] _children;
		// indicates the level of recursion of splitting
		private int _depth;
		// indicates the inherited cardinality of this Node (N, E, S, W)
		private Direction _orientation;
		// a reference to the owning patch - in order to obtain the split level
		private Patch _owningPatch;
		// reference to the parent node in order to 'inherit' orientation
		private Node _parent;

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
			{
				InstantiateChildren();

				int[,] data = new int[2, 2] { {0, 1}, {2, 0} };

				for(int index = 0; index < 2; index++)
				{
					int middleIndex = Math.Abs((int)((indexes[1] - indexes[2]) / 2) + indexes[2]);
					Children[index].indexes = new int[3] {
						middleIndex,
						indexes[data[index, 0]],
						indexes[data[index, 1]]
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
			Direction[] cDIRECTIONS = new Direction[4]
			{
				Direction.West, Direction.South, Direction.East, Direction.North
			};

			Children = new Triangle[2];
			for(int index = 0; index < 2; index++)
			{
				Children[index] = new Triangle(this, Patch);

				// level 1 is the first depth that can have a defined orientation -
				// subsequent levels will inherited it from their parent
				if(Depth == 1)
					Children[index].Orientation = cDIRECTIONS[directionCount++];
			}
		}

		private void InheritDirectionFromParent(Node node)
		{
			// all non-internal nodes simple ontain their direction from their parent
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
			foreach(int index in indexes)
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
                foreach (int index in indexes)
                    _allIndexes[pos++] = index;
            }
        }

        public Triangle[] Children
		{
			get { return _children; }
			set { _children = value; }
		}

		public int Depth
		{
			get { return _depth; }
			set { _depth = value; }
		}

		// indicates the inherited cardinality of this Node (N, E, S, W)
		public Direction Orientation
		{
			get { return _orientation; }
			set { _orientation = value; }
		}

		public Node Parent
		{
			get { return _parent; }
			set { _parent = value; }
		}

		public Patch Patch
		{
			get { return _owningPatch; }
			set { _owningPatch = value; }
		}
	}

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
			Children[0].indexes = new int[3]{0, Patch.Size * width, width};
			Children[1].indexes = new int[3]{(Patch.Size * Patch.Size) - 1, width, Patch.Size * width};

			TraceChildren();
			SplitChildren();
		}

        internal override void CalcAllIndexes(ref int pos, ref int[] _allIndexes)
        {
            Split(ref pos, ref _allIndexes);
        }
    }

	class Triangle : Node
	{

		internal Triangle(Node parent, Patch patch) : base(patch)
		{
			Parent = parent;
			Depth = parent.Depth + 1;
		}
	}

}

