using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace Laan.DLOD
{

	enum Direction {
		None,
		North,
		South,
		East,
		West,
		Internal
	};

	internal class Patch
	{

		public static int Count = 0;

		public int ID = Count++;

		private VertexPositionColor[] _vertexBuffer;
		private int[]                 _allIndexes;
		private int                   _level;
        private bool                  _levelChanged = false;
		private Point                 _position;
		private int                   _size;
		private RootNode              _root;
		private Terrain               _terrain;

		internal Patch(Terrain terrain, int size, Point position)
		{
			// by default, all patches have zero level - this will be updated each frame
			_level = 0;

			_terrain = terrain;
			_position = position;
			_size = size + 1;

            _vertexBuffer = new VertexPositionColor[_size * _size];
            for(int y = 0; y < _size; y++)
				for(int x = 0; x < _size; x++)
				{
                    System.Drawing.Point p = new System.Drawing.Point(
						(position.X * (_size - 1)) + x,
						(position.Y * (_size - 1)) + y
					);

                    _vertexBuffer[x + y * _size] = new VertexPositionColor(new Vector3(p.X, p.Y, _terrain.HeightAt(p)), Color.White);
				}

			_root = new RootNode(this);
		}

        private double Distance(Point a, Point b)
        {
            int dx = (a.X - b.X);
            int dy = (a.Y - b.Y);
            double l = Math.Pow(dy, 2) + Math.Pow(dx, 2);
            return Math.Sqrt(l);
        }

		public void Update(TerrainCamera camera)
		{
            int half = _terrain.Height / 2;
            double maxDistance = Distance(
                new Point(-half, -half),
                new Point(half, half)
            ) / 2;

            half = _terrain.PatchesPerRow / 2;
            Point p = _position;
            p.X -= half;
            p.Y -= half;
            p.X *= _size / 2;
            p.Y *= _size / 2;

            Point capped = new Point((int)camera.LookAt.X, (int)camera.LookAt.Y);
            capped.X = Math.Min(capped.X, _terrain.Height);
            capped.Y = Math.Min(capped.Y, _terrain.Height);

            double distance = Distance(p, capped);

            Level = (int)(_terrain.MaxPatchDepth - (((distance - maxDistance) / maxDistance) * _terrain.MaxPatchDepth));
		}

		internal bool HasSibling(Direction direction, ref Patch sibling)
		{
            System.Drawing.Point siblingPos = new System.Drawing.Point(_position.X, _position.Y);

			switch (direction)
			{
			  case Direction.North:
				siblingPos.Y++;
				break;
			  case Direction.South:
				siblingPos.Y--;
				break;
			  case Direction.East:
				siblingPos.X--;
				break;
			  case Direction.West:
				siblingPos.X++;
				break;
			}
			if(siblingPos.X >= 0 &&
			   siblingPos.Y >= 0 &&
			   siblingPos.X < _terrain.PatchesPerRow &&
			   siblingPos.Y < _terrain.PatchesPerRow)
			{
			   sibling = _terrain._patches[siblingPos.X, siblingPos.Y];
			}
			return (sibling != null);
		}

		public RootNode Root
		{
			get { return _root; }
			set { _root = value; }
		}

		public Terrain Terrain
		{
			get { return _terrain; }
		}

        public VertexPositionColor[] VertexBuffer
		{
			get { return _vertexBuffer; }
		}

        public int VerticesCount
        {
            get { return _vertexBuffer.Length; }
        }

        public int[] IndexBuffer
		{
			get {
                if (_levelChanged || _allIndexes == null)
                {
                    Trace.WriteLine(String.Format("{0}: {1}", _position, Level));
                    int[] _indexes = new int[10000];
                    int pos = 0;
                    _root.CalcAllIndexes(ref pos, ref _indexes);

                    _allIndexes = new int[pos];
                    Array.ConstrainedCopy(_indexes, 0, _allIndexes, 0, pos);

                    _levelChanged = false;
                }
                return _allIndexes;
			}
		}

		internal int Level
		{
			get { return _level; }
			set {
                // ensure the patch level doesn't exceed the terrain's max
                int newLevel = Math.Min(value, _terrain.MaxPatchDepth);
                if (newLevel != _level)
				{
                    _level = newLevel;
                    _levelChanged = true;
                    // recalculate the entire patch if the level changes
                    _root.Initialize();
                }
			}
		}

		internal int Size
		{
			get { return _size; }
			set { _size = value; }
		}
	}
}
