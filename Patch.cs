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

                    _vertexBuffer[x + y * _size] = 
                        new VertexPositionColor(
                            new Vector3(p.X, p.Y, _terrain.HeightAt(p)),
                            _terrain.ColorAt(p)
                        );
				}

			_root = new RootNode(this);      
        }

        private double distance;

		public void Update(TerrainCamera camera)
		{
            int half = _terrain.PatchesPerRow / 2;
            Point p = _position;
            p.X -= half;
            p.Y -= half;
            p.X *= _size *_terrain._scale;
            p.Y *= _size * _terrain._scale;

            Point capped = new Point((int)camera.LookAt.X, (int)camera.LookAt.Y);
            //capped.X = Math.Min(capped.X, _terrain.Height);
            //capped.Y = Math.Min(capped.Y, _terrain.Height);

            distance = _terrain.Distance(p, capped);

            Level = 1 + (int)(((_terrain.MaxDistance - distance) / _terrain.MaxDistance) * _terrain.MaxPatchDepth);
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
				siblingPos.X++;
				break;
			  case Direction.West:
				siblingPos.X--;
				break;
			}

            if (siblingPos.X >= 0 &&
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
//                    Trace.WriteLine(String.Format("{0}: {1}", _position, Level));
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

        public override string ToString()
        {
            return String.Format("P: {0} d: {1:0.00} L: {2}", _position, distance, Level);
        }

		internal int Level
		{
			get { return _level; }
			set {
                // ensure the patch level doesn't exceed the terrain's max
                int newLevel = Math.Max(0, Math.Min(value, _terrain.MaxPatchDepth));
//                if (newLevel != _level)
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
