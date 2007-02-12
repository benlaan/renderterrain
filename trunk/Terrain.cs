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

        internal GraphicsDevice       _device;
        internal Patch[,]             _patches;
        internal int                  _scale;

        private Effect                _effect;
        private System.Drawing.Bitmap   _heightMap;
        private int                   _maxPatchDepth;
        private int                   _patchesPerRow;
        private int                   _patchWidth;
        private int                   _height;
        private Camera                _camera;
        private double                _maxDistance;
        private bool                  _wireFrame = false;
        private float[,]              _heightField;
        private float                 _scaleHeights;

        private int                   indexCount;
        private int                   vertexCount;

        private Texture2D             dirtTexture;
        private Texture2D             waterTexture;
        private Texture2D             stoneTexture;
        private Texture2D             grassTexture;
        private Texture2D             alphaTexture;

		public Terrain(Game game, string heightMapName, int patchWidth) : base(game)
        {
            _scale = 7;
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
            ) * _scale / 2;

            GenerateHeightField();
        }

        private int[] heightThreshold = new int[] { 145, 160, 200, 250 };

        internal int Threshold(float height)
        {
            int h = 0;
            while (h < 3 && height > heightThreshold[h])
                h++;

            return h;
        }

        private System.Drawing.Color SampleRange(int x, int y)
        {
            int[] textureCount = new int[4] { 0, 0, 0, 0 };

            int passes = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int dxx = dx + x;
                    int dyy = dy + y;
                    if (dxx >= 0 && dxx < _height && dyy >= 0 && dyy < _height)
                    {
                        float h = _heightField[dxx, dyy];
                        int t = Threshold(h);
                        textureCount[t]++;
                        passes++;
                    }
                }

            for (int i = 0; i < 4; i++)
                textureCount[i] = (int)(textureCount[i] * 1.0f / passes * 255.0f);

            return System.Drawing.Color.FromArgb(textureCount[0], textureCount[1], textureCount[2], textureCount[3]);
        }

        private Texture2D GenerateTexture()
        {
            Trace.WriteLine("Generating Texture");
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(_height + 1, _height + 1);

            using (Laan.Drawing.FastBitmap fast = new Laan.Drawing.FastBitmap(bitmap))
            {
                fast.LockBitmap();
                try
                {

                    for (int y = 0; y < _height; y++)
                        for (int x = 0; x < _height; x++)
                        {

                            Laan.Drawing.PixelData pixel = new Laan.Drawing.PixelData(SampleRange(x, y));
                            fast.SetPixel(x, y, pixel);
                        }
                }
                finally
                {
                    fast.UnlockBitmap();
                }
            }

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                bitmap.Save(@"D:\test.png", ImageFormat.Png);
                bitmap.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;
                return Texture2D.FromFile(GraphicsDevice, stream);
            };
        }

        private void GenerateHeightField()
        {
            Trace.WriteLine("Loading HeightMap");
            using (Laan.Drawing.FastBitmap fast = new Laan.Drawing.FastBitmap(_heightMap))
            {
                fast.LockBitmap();
                try
                {
                    _scaleHeights = (_height / 5f) / 255.0f;
                    _heightField = new float[_height + 1, _height + 1];

                    for (int y = 0; y < _height; y++)
                        for (int x = 0; x < _height; x++)
                        {
                            Laan.Drawing.PixelData pixel = fast.GetPixel(x, y);
                            _heightField[x, y] = pixel.Red;
                        }
                }
                finally
                {
                    fast.UnlockBitmap();
                }
            }
        }

        private void GenerateNormalMap()
        {
            Trace.WriteLine("Generating Texture");

            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    _patches[x, y].GenerateNormalMap();
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
            
            ContentManager content = new ContentManager(this.Game.Services);

            dirtTexture = content.Load<Texture2D>(@"Content\dirt");
            waterTexture = content.Load<Texture2D>(@"Content\water");
            stoneTexture = content.Load<Texture2D>(@"Content\stone");
            grassTexture = content.Load<Texture2D>(@"Content\grass");
            alphaTexture = GenerateTexture();

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
                   @"../../../Content/Splatting.fx", 
                    null, null, 
                    CompilerOptions.None, 
                    TargetPlatform.Windows
                );

            _effect = new Effect(_device, compiledEffect.GetEffectCode(), CompilerOptions.None, null);
            _effect.CurrentTechnique = _effect.Techniques["TextureSplatting"];
            
            _effect.Parameters["alphaTexture"].SetValue(alphaTexture);
            _effect.Parameters["dirtTexture"].SetValue(dirtTexture);
            _effect.Parameters["grassTexture"].SetValue(grassTexture);
            _effect.Parameters["waterTexture"].SetValue(waterTexture);
            _effect.Parameters["stoneTexture"].SetValue(stoneTexture);

            UpdatePatches();

            //GenerateNormalMap();

            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    _patches[x, y].InitialiseBuffer(_device);

            RecalculatePatches();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Tab))
            {
                _wireFrame = !_wireFrame;
            }

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
            {
                UpdatePatches();
                RecalculatePatches();
            }
            base.Update(gameTime);
        }

        private void UpdatePatches()
        {
            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    _patches[x, y].Update(_camera);

            indexCount = 0;
            vertexCount = 0;
            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                {
                    _patches[x, y].Recalculate();
                    indexCount += _patches[x, y].IndexBufferLength;
                    vertexCount += _patches[x, y].VerticesCount;
                }
        }

        private void RecalculatePatches()
        {
            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    _patches[x, y].Recalculate();
        }

        /// <summary>
        /// Allows the game component to draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {

            if (_wireFrame)
                _device.RenderState.FillMode = FillMode.WireFrame;
            else
                _device.RenderState.FillMode = FillMode.Solid;

            string technique = _wireFrame ? "WireFrame" : "TextureSplatting";
            _effect.CurrentTechnique = _effect.Techniques[technique];

            _device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            Matrix worldMatrix = Matrix.Identity;
            int offset = -1 * (_height / 2);
            worldMatrix = Matrix.CreateTranslation(new Vector3(offset, offset, 1));
            worldMatrix *= Matrix.CreateScale(new Vector3(_scale, _scale, _scale));

            _effect.Parameters["ViewProjection"].SetValue(worldMatrix * _camera.View * _camera.Projection);

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
                        if (p.Visible)
                        {
                            _device.VertexDeclaration = new VertexDeclaration(
                                _device, SplattingVertex.VertexElements
                            );
                            _device.Vertices[0].SetSource(p.Buffer, 0, SplattingVertex.SizeInBytes);
                            _device.Indices = p.IndexBuffer;
                            try
                            {
                                _device.DrawIndexedPrimitives(
                                    PrimitiveType.TriangleList,
                                    0, 0, p.VerticesCount,
                                    0, p.IndexBufferLength / 3
                                );
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                pass.End();
            }
            _effect.End();

            base.Draw(gameTime);
        }

        internal float HeightAt(System.Drawing.Point offset)
        {
            float h = _heightField[offset.X, offset.Y];
            if (h < heightThreshold[0])
                h = heightThreshold[0];
            return h *_scaleHeights;
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

        public Camera Camera
        {
            get { return _camera; }
            set { _camera = value; }
        }

        public double MaxDistance
        {
            get { return _maxDistance; }
        }

        public override string ToString()
        {
            return String.Format("Indices:  {0}\nVertices: {1}", indexCount, vertexCount);
        }

    }
}


