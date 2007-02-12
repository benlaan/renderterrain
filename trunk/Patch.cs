using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Configuration;

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


        private int               _levelBias = 1;
        private GraphicsDevice    _device;
		private SplattingVertex[] _vertexBuffer;
		private int[]             _allIndexes;
		private int               _level;
		private Point             _position;
		private int               _size;
		private RootNode          _root;
		private Terrain           _terrain;
        private Point             _midPoint;
        private bool              _visible;

        public VertexBuffer       Buffer;
        public IndexBuffer        IndexBuffer;

		internal Patch(Terrain terrain, int size, Point position)
		{
			// by default, all patches have zero level - this will be updated each frame
			_level = 0;
            _levelBias = Int32.Parse(ConfigurationSettings.AppSettings["patchLevelBias"]);

			_terrain = terrain;
			_position = position;
			_size = size + 1;
            _visible = true;

            int half = _terrain.PatchesPerRow / 2;
            _midPoint = _position;
            _midPoint.X -= half;
            _midPoint.Y -= half;
            _midPoint.X *= _size * _terrain._scale;
            _midPoint.Y *= _size * _terrain._scale;

            _vertexBuffer = new SplattingVertex[_size * _size];
            for(int y = 0; y < _size; y++)
				for(int x = 0; x < _size; x++)
				{
                    System.Drawing.Point p = new System.Drawing.Point(
                        (position.X * (_size - 1)) + x,
                        (position.Y * (_size - 1)) + y
                    );

                    _vertexBuffer[x + y * _size] =
                        new SplattingVertex(
                            new Vector3(p.X, p.Y, _terrain.HeightAt(p)),
                            new Vector2(((float)p.X) / _terrain.Height, ((float)p.Y) / _terrain.Height),
                            new Vector2((float)x / _size, ((float)y / _size)),
                            new Vector3(0, 0, 1)
                        );
				}

			_root = new RootNode(this);      
        }

        internal void GenerateNormalMap()
        {
            for (int x = 1; x < _size - 1; x++)
            {
                for (int y = 1; y < _size - 1; y++)
                {
                    Vector3 normX = new Vector3(
                        (_vertexBuffer[x - 1 + y * _size].Position.Z - _vertexBuffer[x + 1 + y * _size].Position.Z) / 2, 
                        0, 
                        1
                    );

                    Vector3 normY = new Vector3(
                        0, 
                        (_vertexBuffer[x + (y - 1) * _size].Position.Z - _vertexBuffer[x + (y + 1) * _size].Position.Z) / 2, 
                        1)
                    ;
                    _vertexBuffer[x + y * _size].Normal = normX + normY;
                    _vertexBuffer[x + y * _size].Normal.Normalize();
                }
            }
        }

        internal void InitialiseBuffer(GraphicsDevice device)
        {
            _device = device;
            Buffer = new VertexBuffer(
                device,
                SplattingVertex.SizeInBytes * VertexBuffer.Length,
                ResourceUsage.WriteOnly,
                ResourceManagementMode.Automatic
                    );
            Buffer.SetData<SplattingVertex>(VertexBuffer);
        }

        private double distance;

		public void Update(Camera camera)
		{
            Point capped = new Point((int)camera.LookAt.X, (int)camera.LookAt.Y);
            distance = _terrain.Distance(_midPoint, capped);

            Level = _levelBias + (int)(Math.Round(
                ((_terrain.MaxDistance - distance) / _terrain.MaxDistance) * _terrain.MaxPatchDepth,
                MidpointRounding.AwayFromZero));
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

        public SplattingVertex[] VertexBuffer
		{
			get { return _vertexBuffer; }
		}

        public int VerticesCount
        {
            get { return _vertexBuffer.Length; }
        }

        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
            }
        }

        public override string ToString()
        {
            return String.Format(
                "P: {0}/{1} d: {2:0.0} L: {3} C: {4}", 
                _position, _midPoint, distance, Level, _allIndexes.Length
            );
        }

        internal void Recalculate()
        {
            // recalculate the entire patch if the level changes
            _root.Initialize();

            int[] _indexes = new int[10000];
            int pos = 0;
            _root.CalcAllIndexes(ref pos, ref _indexes);

            _allIndexes = new int[pos];
            Array.ConstrainedCopy(_indexes, 0, _allIndexes, 0, pos);

            if (_device != null)
            {
                IndexBuffer = new IndexBuffer(
                _device, typeof(int),
                _allIndexes.Length,
                ResourceUsage.WriteOnly,
                ResourceManagementMode.Automatic
                );
                IndexBuffer.SetData<int>(_allIndexes);
            }
        }

        internal int IndexBufferLength
        {
            get
            {
                return _allIndexes.Length;
            }
        }

		internal int Level
		{
			get { return _level; }
			set {
                // ensure the patch level doesn't exceed the terrain's max
                _level = Math.Max(0, Math.Min(value, _terrain.MaxPatchDepth));
			}
		}


		internal int Size
		{
			get { return _size; }
			set { _size = value; }
		}
	}
}
