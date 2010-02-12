using System;

using DLOD31.Properties;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Laan.DLOD.Discrete
{

    internal class Patch
    {
        private int[] _allIndexes;
        public static int Count = 0;

        public int ID = Count++;

        private Point _position;
        private Point _midPoint;
        private bool _updateRequired;
        private Terrain _terrain;
        private double _distance;
        private int _levelBias = -1;
        private GraphicsDevice _device;
        private int _level;

        public VertexBuffer Buffer;
        public IndexBuffer IndexBuffer;

        internal Patch(Terrain terrain, int size, Point position)
        {
            // by default, all patches have zero level - this will be updated each frame
            _level = 0;
            _levelBias = Settings.Default.PatchLevelBias;

            _terrain = terrain;
            _position = position;
            Size = size + 1;
            Visible = true;

            int half = size / 2;
            _midPoint = _position;
            _midPoint.X *= size;
            _midPoint.Y *= size;
            _midPoint.X += half;
            _midPoint.Y += half;

            VertexBuffer = new VertexMultiTextured[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    System.Drawing.Point p = new System.Drawing.Point(
                        (position.X * (Size - 1)) + x,
                        (position.Y * (Size - 1)) + y
                    );

                    VertexBuffer[x + y * Size] = new VertexMultiTextured(p, _terrain);
                }

            Root = new RootNode(this);
        }

        internal void GenerateNormalMap()
        {
            for (int x = 1; x < Size - 1; x++)
                for (int y = 1; y < Size - 1; y++)
                {
                    Vector3 normX = new Vector3((VertexBuffer[x - 1 + y * Size].Position.Z - VertexBuffer[x + 1 + y * Size].Position.Z) / 2, 0, 1);
                    Vector3 normY = new Vector3(0, (VertexBuffer[x + (y - 1) * Size].Position.Z - VertexBuffer[x + (y + 1) * Size].Position.Z) / 2, 1);

                    VertexBuffer[x + y * Size].Normal = normX + normY;
                    VertexBuffer[x + y * Size].Normal.Normalize();
                }
        }

        internal void InitialiseBuffer(GraphicsDevice device)
        {
            _device = device;
            Buffer = new VertexBuffer(
                device,
                VertexMultiTextured.SizeInBytes * VertexBuffer.Length,
                BufferUsage.WriteOnly
            );
            Buffer.SetData<VertexMultiTextured>(VertexBuffer);
        }

        public void Update(Camera camera)
        {
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
            return sibling != null;
        }

        public Point MidPoint
        {
            get { return _midPoint; }
            set { _midPoint = value; }
        }
        public RootNode Root { get; set; }
        public bool Visible { get; set; }
        internal int Size { get; set; }
        public VertexMultiTextured[] VertexBuffer { get; private set; }

        public int VerticesCount
        {
            get { return VertexBuffer.Length; }
        }

        public override string ToString()
        {
            return String.Format(
                "P: {0}/{1} d: {2:0.0} L: {3} C: {4}",
                _position, MidPoint, _distance, Level, IndexBufferLength
            );
        }

        internal void Recalculate()
        {
            // recalculate the entire patch if the level changes
            Root.Initialize();

            var allIndexes = new List<Int32>();
            Root.CalcAllIndexes(allIndexes);

            if (_device != null)
            {
                IndexBuffer = new IndexBuffer(
                    _device, typeof(int),
                    allIndexes.Count,
                    BufferUsage.Points
                );
                _allIndexes = allIndexes.ToArray();
                IndexBuffer.SetData<int>(_allIndexes);
            }
        }

        internal int IndexBufferLength
        {
            get { return _allIndexes != null ? _allIndexes.Length : 0; }
        }

        public int Level { get; set; }

        internal bool GetActive( Vector3 vertex )
        {
            return _terrain.IsPointActive(vertex.X, vertex.Y);
        }

        public int MaxTerrainDepth
        {
            get { return _terrain.MaxPatchDepth; }
        }
    }
}
