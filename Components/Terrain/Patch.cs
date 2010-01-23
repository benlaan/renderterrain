using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Configuration;
using DLOD31.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Laan.DLOD
{

    enum Direction
    {
        None,
        North,
        South,
        East,
        West,
        Internal
    };

    class PatchSibling
    {
        public Patch Sibling;
    }

    internal class Patch
    {

        public static int Count = 0;

        public int ID = Count++;

        private double distance;
        private int _levelBias = -1;
        private GraphicsDevice _device;
        private int[] _allIndexes;
        private int _level;
        private Point _position;
        private Point _midPoint;

        public VertexBuffer Buffer;
        public IndexBuffer IndexBuffer;

        internal Patch(Terrain terrain, int size, Point position)
        {
            // by default, all patches have zero level - this will be updated each frame
            _level = 0;
            _levelBias = Settings.Default.PatchLevelBias;

            Terrain = terrain;
            _position = position;
            Size = size + 1;
            Visible = true;

            int half = Terrain.PatchesPerRow / 2;
            _midPoint = _position;
            _midPoint.X -= half;
            _midPoint.Y -= half;
            _midPoint.X *= Size * Terrain._scale;
            _midPoint.Y *= Size * Terrain._scale;

            VertexBuffer = new SplattingVertex[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    System.Drawing.Point p = new System.Drawing.Point(
                        (position.X * (Size - 1)) + x,
                        (position.Y * (Size - 1)) + y
                    );

                    VertexBuffer[x + y * Size] =
                        new SplattingVertex(
                            new Vector3(p.X, p.Y, Terrain.HeightAt(p)),
                            new Vector2(((float)p.X) / Terrain.Height, ((float)p.Y) / Terrain.Height),
                            new Vector2((float)x / Size, ((float)y / Size)),
                            new Vector3(0, 0, 1)
                        );
                }

            Root = new RootNode(this);
            //_level = CalcLevel();
        }

        internal void GenerateNormalMap()
        {
            for (int x = 1; x < Size - 1; x++)
            {
                for (int y = 1; y < Size - 1; y++)
                {
                    Vector3 normX = new Vector3(
                        (VertexBuffer[x - 1 + y * Size].Position.Z - VertexBuffer[x + 1 + y * Size].Position.Z) / 2,
                        0,
                        1
                    );

                    Vector3 normY = new Vector3(
                        0,
                        (VertexBuffer[x + (y - 1) * Size].Position.Z - VertexBuffer[x + (y + 1) * Size].Position.Z) / 2,
                        1)
                    ;
                    VertexBuffer[x + y * Size].Normal = normX + normY;
                    VertexBuffer[x + y * Size].Normal.Normalize();
                }
            }
        }

        internal void InitialiseBuffer(GraphicsDevice device)
        {
            _device = device;
            Buffer = new VertexBuffer(
                device,
                SplattingVertex.SizeInBytes * VertexBuffer.Length,
                BufferUsage.WriteOnly
            );
            Buffer.SetData<SplattingVertex>(VertexBuffer);
        }

        public void Update(Camera camera)
        {
            Point capped = new Point((int)camera.LookAt.X, (int)camera.LookAt.Y);
            distance = Terrain.Distance(_midPoint, capped);

            //Level = Terrain.MaxPatchDepth / 2;
            Level = Terrain.Level(distance, _levelBias);
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
               siblingPos.X < Terrain.PatchesPerRow &&
               siblingPos.Y < Terrain.PatchesPerRow)
            {
                sibling = Terrain._patches[siblingPos.X, siblingPos.Y];
            }
            return sibling != null;
        }

        public RootNode Root { get; set; }
        public bool Visible { get; set; }
        internal int Size { get; set; }
        public Terrain Terrain { get; private set; }
        public SplattingVertex[] VertexBuffer { get; private set; }

        public int VerticesCount
        {
            get { return VertexBuffer.Length; }
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
            Root.Initialize();

            int[] _indexes = new int[5000];
            int pos = 0;
            Root.CalcAllIndexes(ref pos, ref _indexes);

            _allIndexes = new int[pos];
            Array.ConstrainedCopy(_indexes, 0, _allIndexes, 0, pos);

            if (_device != null)
            {
                IndexBuffer = new IndexBuffer(
                    _device, typeof(int),
                    _allIndexes.Length,
                    BufferUsage.Points
                );
                IndexBuffer.SetData<int>(_allIndexes);
            }
        }

        internal int IndexBufferLength
        {
            get { return _allIndexes.Length; }
        }

        internal int Level
        {
            get { return _level; }
            set
            {
                // ensure the patch level doesn't exceed the terrain's max
                _level = Math.Max(0, Math.Min(value, Terrain.MaxPatchDepth));
            }
        }
    }
}
