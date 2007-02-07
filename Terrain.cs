using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Laan.DLOD
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public partial class Terrain : Microsoft.Xna.Framework.DrawableGameComponent
    {
        //VertexPositionColor[] vertices;

        internal GraphicsDevice       _device;
        internal Patch[,]             _patches;
        internal int                  _scale;

        //private BasicEffect _effect;
        private Effect _effect;
        private System.Drawing.Bitmap _heightMap;
        private int                   _maxPatchDepth;
        private int                   _patchesPerRow;
        private int                   _patchWidth;
        private int                   _height;
        private TerrainCamera         _camera;
        private double                _maxDistance;
        private bool                  _wireFrame = false;
        private float[,]              _heightField;

		public Terrain(Game game, string heightMapName, int patchWidth) : base(game)
        {
            _scale = 3;
			_patchWidth = patchWidth;
			_heightMap = new System.Drawing.Bitmap(heightMapName);

			// Map must be a square
			if(_heightMap.Height != _heightMap.Width)
				throw new ArgumentException("Height map must be a square");

			// must be a power of 2 plus 1
			_height = _heightMap.Height - 1;
			double log2 = Math.Log(_height, 2);

			// check by ensuring that converting to int has the same value as the
			// raw double
			if(!IsWholeNumber(log2))
				throw new ArgumentException("Height map must be one plus a power of 2 (ie 5, 9, 17, 65, 257, etc.");

            _maxPatchDepth = 2 + (int)Math.Log(_height / _patchWidth, 2);

			double patchCount = _height / _patchWidth;
                                                  
			if(!IsWholeNumber(patchCount))
				throw new ArgumentException(
					"PatchLevel incompatable with heightMap - must allow an NxN number of patches");

			_patchesPerRow = (int)patchCount;

            int half = Height / 2;
            _maxDistance = Distance(
                new Point(-half, -half),
                new Point(half, half)
            );

            GenerateHeightField();
        }

        private void GenerateHeightField()
        {
            Trace.WriteLine("Loading HeightMap");

            _heightField = new float[_height + 1, _height + 1];
            float fScale = (_height / 5f) / 255.0f;

            BitmapData data = _heightMap.LockBits(
                new System.Drawing.Rectangle(0, 0, _heightMap.Height, _heightMap.Width), 
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            );
            try
            {
                // Declare an array to hold the bytes of the bitmap.
                // This code is specific to a bitmap with 24 bits per pixels.
                int bytes = Height * Height * 4;
                byte[] rgbValues = new byte[bytes];
                IntPtr ptr = data.Scan0;
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                for (int y = 0; y < _height; y++)
                    for (int x = 0; x < _height; x++)
                    {
                        byte b = rgbValues[4 * (y * Height + x)];
                        _heightField[x, y] = b * fScale;
                    }
            }
            finally
            {
                _heightMap.UnlockBits(data);
                _heightMap.Dispose();
                _heightMap = null;
            }
        }

        internal float HeightAt(System.Drawing.Point offset)
        {
            return _heightField[offset.X, offset.Y];
        }

        internal Color ColorAt(System.Drawing.Point offset)
        {
            float f = _heightField[offset.X, offset.Y];

            if (f < 30)
                return Color.Blue;
            if (f < 60)
                return Color.Yellow;
            if (f < 70)
                return Color.Green;
            if (f < 220)
                return Color.Brown;
            else
                return Color.White;
        }

        private bool IsWholeNumber(double value)
        {
            return (value == (double)((int)(value)));
        }

        public int MaxPatchDepth
        {
            get { return _maxPatchDepth; }
            set { _maxPatchDepth = value; }
        }

        public int PatchesPerRow
        {
            get { return _patchesPerRow; }
            set { _patchesPerRow = value; }
        }

        public int PatchWidth
        {
            get { return _patchWidth; }
            set { _patchWidth = value; }
        }

        public int Height
        {
            get { return _height; }
        }

        public TerrainCamera Camera
        {
            get { return _camera; }
            set { _camera = value; }
        }

        public double MaxDistance
        {
            get { return _maxDistance; }
        }

        internal double Distance(Point a, Point b)
        {
            int dx = (a.X - b.X);
            int dy = (a.Y - b.Y);
            double l = Math.Pow(dy, 2) + Math.Pow(dx, 2);
            return Math.Sqrt(l);
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {

            base.Initialize();

            _device = this.GraphicsDevice;

            // generate a matrix of patches, giving each an offset so it knows it's
            // position within the matrix
            Patch.Count = 0;

            Trace.WriteLine("Building Patch Array");
            _patches = new Patch[_patchesPerRow, _patchesPerRow];

            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    _patches[x, y] = new Patch(this, _patchWidth, new Point(x, y));

            Trace.WriteLine("Compiling Effect File");
            CompiledEffect compiledEffect = 
                Effect.CompileEffectFromFile(
                    "@/../../../../effect.fx", 
                    null, null, 
                    CompilerOptions.None, 
                    TargetPlatform.Windows
                );

            _effect = new Effect(this.GraphicsDevice, compiledEffect.GetEffectCode(), CompilerOptions.None, null);
            //_effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            
            //_effect = new BasicEffect(this.GraphicsDevice, null);
            //_effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);

            UpdatePatches();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Tab))
                _wireFrame = !_wireFrame;
            
            if (keyboardState.IsKeyDown(Keys.Space))
            {
                int count = 0;
                Trace.WriteLine(String.Format("MaxDistance: {0:0.00}  MaxPatch: {1}", MaxDistance, MaxPatchDepth));
                Trace.Indent();
                for (int y = 0; y < _patchesPerRow; y++)
                    for (int x = 0; x < _patchesPerRow; x++)
                        Trace.WriteLine((++count).ToString() + " " + _patches[x, y].ToString());
                Trace.Unindent();
            }

            if (_camera.Moved)
                UpdatePatches();

            base.Update(gameTime);
        }

        private void UpdatePatches()
        {
            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    _patches[x, y].Update(_camera);
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {

            if (_wireFrame)
                _device.RenderState.FillMode = FillMode.WireFrame;
            else
                _device.RenderState.FillMode = FillMode.Solid;

            _device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            _device.Clear(Color.DarkSlateBlue);

            _effect.CurrentTechnique = _effect.Techniques["Colored"];

            Matrix worldMatrix = Matrix.Identity;
            int offset = -1 * (_height / 2);
            worldMatrix = Matrix.CreateTranslation(new Vector3(offset, offset, 1));

//                worldMatrix *= Matrix.CreateRotationX(1f);
            worldMatrix *= Matrix.CreateScale(new Vector3(_scale, _scale, _scale));

            _effect.Parameters["xView"].SetValue(_camera.View);
            _effect.Parameters["xProjection"].SetValue(_camera.Projection);
            _effect.Parameters["xWorld"].SetValue(worldMatrix);

            //_effect.View = _camera.View;
            //_effect.Projection = _camera.Projection;
            //_effect.World = worldMatrix;
            
            _effect.Begin();
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                // generate index buffer for each patch
                for (int y = 0; y < _patchesPerRow; y++)
                {
                    for (int x = 0; x < _patchesPerRow; x++)
                    {
                        Patch p = _patches[x, y];
                        _device.VertexDeclaration = new VertexDeclaration(_device, VertexPositionColor.VertexElements);

                        VertexBuffer buffer = new VertexBuffer(
                            _device,
                            sizeof(float) * 4 * p.VertexBuffer.Length,
                            ResourceUsage.WriteOnly,
                            ResourceManagementMode.Automatic
                        );
                        buffer.SetData<VertexPositionColor>(p.VertexBuffer);

                        _device.Vertices[0].SetSource(
                            buffer, 
                            0, 
                            VertexPositionColor.SizeInBytes
                        );

                        IndexBuffer ib = new IndexBuffer(
                            _device, 
                            typeof(int), 
                            p.IndexBuffer.Length, 
                            ResourceUsage.WriteOnly, 
                            ResourceManagementMode.Automatic
                        ); 

                        ib.SetData<int>(p.IndexBuffer);
                        _device.Indices = ib;

                        try
                        {
                            _device.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                0, 0, p.VerticesCount,
                                0, p.IndexBuffer.Length / 3
                            );
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                pass.End();
            }
            _effect.End();

            base.Draw(gameTime);
        }
    }
}


