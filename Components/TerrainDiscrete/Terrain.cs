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
using System.Linq;
using System.Collections;
using System.Threading;
using DLOD31.Properties;

namespace Laan.DLOD.Discrete
{

    internal partial class Terrain : DrawableGameComponent, ITerrain
    {
        private const float SPLIT_TOLERANCE = 0.07f;
        private short[] _boundary;
        private int _mapSize;
        private int _waterLevel = 90;
        private BitArray _active;

        internal GraphicsDevice _device;
        internal Patch[,] _patches;
        internal int _scale;

        private Effect _effect;
        private System.Drawing.Bitmap _heightMap;
        private bool _wireFrame = false;
        private float[,] _HeightField;
        private float _scaleHeights;

        private int indexCount;
        private int vertexCount;

        private Texture2D forestTexture;
        private Texture2D waterTexture;
        private Texture2D stoneTexture;
        private Texture2D grassTexture;

        public Terrain(Game game, string heightMapName, int patchWidth) : base(game)
        {
            PatchWidth = patchWidth;

            _scale = 7;
            _scaleHeights = Settings.Default.HeightMultiplier; //  (Height / 5000.0f)
            _heightMap = new System.Drawing.Bitmap("Maps\\" + heightMapName);
            _mapSize = _heightMap.Height;
            _active = new BitArray(_mapSize * _mapSize);

            // Map must be a square
            if (_mapSize != _heightMap.Width)
                throw new ArgumentException("Height map must be a square");

            // must be a power of 2 plus 1
            Height = _mapSize - 1;
            double log2 = Math.Log(Height, 2);

            // check by ensuring that converting to int has the same value as the
            // raw double
            if (!IsWholeNumber(log2))
                throw new ArgumentException("Height map must be a logarithm of 2 plus one. eg. 2^n+1 (ie 5, 9, 17, 65, 257, etc.");

            MaxPatchDepth = 2 + (int)Math.Log(Height / PatchWidth, 2);

            double patchCount = Height / PatchWidth;

            if (!IsWholeNumber(patchCount))
                throw new ArgumentException(
                    "PatchLevel incompatable with heightMap - must allow an NxN number of patches");

            PatchesPerRow = (int)patchCount;

            int half = Height / 2;
            MaxDistance = Distance(
                new Point(-half, -half),
                new Point(half, half)
            ) * _scale / 2;
        }

        private void BuildBoundaryCache()
        {
            _boundary = new short[_mapSize];

            for (int n = 0; n < _mapSize; n++)
            {
                _boundary[n] = -1;
                if (n == 0)
                    _boundary[n] = (short)_mapSize;
                else
                {
                    for (short level = ( short )_mapSize; level > 1; level /= 2)
                    {
                        if (n % level != 0)
                            continue;

                        _boundary[n] = level;
                        break;
                    }
                    if (_boundary[n] == -1)
                        _boundary[n] = 1;
                }
            }
        }

        private void GenerateHeightField()
        {
            Trace.WriteLine("Loading HeightMap");

            using (var fast = new Laan.Drawing.FastBitmap(_heightMap))
            {
                _HeightField = new float[Height + 1, Height + 1];

                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Height; x++)
                        _HeightField[x, y] = fast.GetPixel(x, Height - y).R;
            }
        }
        
        private void GenerateNormalMap()
        {
            Trace.WriteLine("Generating Normal Map");

            for (int y = 0; y < PatchesPerRow; y++)
                for (int x = 0; x < PatchesPerRow; x++)
                    _patches[x, y].GenerateNormalMap();
        }

        private double Distance(Point a, Point b)
        {
            int dx = (a.X - b.X);
            int dy = (a.Y - b.Y);
            return Math.Sqrt(Math.Pow(dy, 2) + Math.Pow(dx, 2));
        }

        private void ProcessQuads()
        {
            Trace.WriteLine("Processing Quads");

            for (int y = 0; y < _mapSize; y++)
                for (int x = 0; x < _mapSize; x++)
                    CheckQuadForSplit(x, y);
        }

        private void InitializeBuffers()
        {
            Trace.WriteLine("Initialize Buffers");
            for (int y = 0; y < PatchesPerRow; y++)
                for (int x = 0; x < PatchesPerRow; x++)
                    _patches[x, y].InitialiseBuffer(_device);
        }

        private void UpdatePatches()
        {
            indexCount = 0;
            vertexCount = 0;
            for (int y = 0; y < PatchesPerRow; y++)
                for (int x = 0; x < PatchesPerRow; x++)
                {
                    Patch patch = _patches[x, y];
                    if (patch.Visible)
                    {
                        patch.Recalculate();
                        indexCount += patch.IndexBufferLength;
                        vertexCount += patch.VerticesCount;
                    }
                }
        }

        private float UnscaledHeightAt(int x, int y)
        {
            float h = _HeightField[x, y];
            if (h < _waterLevel)
                h = _waterLevel;
            return h;
        }
        
        private float HeightAt(int x, int y)
        {
            float h = _HeightField[x, y];
            if (h < _waterLevel)
                h = _waterLevel;
            return h * _scaleHeights;
        }

        private bool IsWholeNumber(double value)
        {
            return (value == (double)((int)(value)));
        }

        private void EnablePoint(int x, int y)
        {
            if (x < 0 || x > (int)_mapSize || y < 0 || y > (int)_mapSize)
                return;
                
            if(IsPointActive(x, y))
                return;

            ActivatePoint(x, y);
            int xl = _boundary[x];
            int yl = _boundary[y];
            int level = Math.Min(xl, yl);

            if (xl > yl)
            {
                EnablePoint(x - level, y);
                EnablePoint(x + level, y);
            }
            else if (xl < yl)
            {
                EnablePoint(x, y + level);
                EnablePoint(x, y - level);
            }
            else
            {
                int x2 = x & (level * 2);
                int y2 = y & (level * 2);

                if (x2 == y2)
                {
                    EnablePoint(x - level, y + level);
                    EnablePoint(x + level, y - level);
                }
                else
                {
                    EnablePoint(x + level, y + level);
                    EnablePoint(x - level, y - level);
                }
            }
        }

        private void CheckQuadForSplit(int x1, int y1)
        {
            int half = PatchWidth / 2;
            int xc = x1 + half;
            int x2 = x1 + PatchWidth;
            int yc = y1 + half;
            int y2 = y1 + PatchWidth;

            if (x2 >= _mapSize || y2 >= _mapSize || x1 < 0 || y1 < 0)
                return;

            float ul = UnscaledHeightAt(x1, y1);
            float ur = UnscaledHeightAt(x2, y1);
            float ll = UnscaledHeightAt(x1, y2);
            float lr = UnscaledHeightAt(x2, y2);
            float center = HeightAt(xc, yc);
            float average = (ul + lr + ll + ur) / 4.0f;

            //look for a delta between the center point and the average elevation
            float delta = Math.Abs((average - center)) * 5.0f;
            //scale the delta based on the size of the quad we are dealing with
            delta /= (float)PatchWidth;

/*
          //scale based on distance 
          delta *= (1.0f - (dist * 0.85f));
          //if the distance is very close, then we want a lot more detail
          if (dist < 0.15f) 
            delta *= 10.0f;
*/
            //smaller quads are much less imporant
            delta *= (float)(_mapSize + PatchWidth) / (float)(_mapSize * 2);
            if (delta > Settings.Default.Threshold)
                EnablePoint(xc, yc);
        }

        private void CompileEffectFile()
        {
            Trace.WriteLine("Compiling Effect File");
            CompiledEffect compiledEffect =
                Effect.CompileEffectFromFile(
                   @"../../../Content/Effects/Splatting.fx",
                    null, null,
                    CompilerOptions.None,
                    TargetPlatform.Windows
                );

            _effect = new Effect(_device, compiledEffect.GetEffectCode(), CompilerOptions.None, null);
            _effect.CurrentTechnique = _effect.Techniques["TextureSplatting"];
            _effect.Parameters["waterTexture"].SetValue(waterTexture);
            _effect.Parameters["grassTexture"].SetValue(grassTexture);
            _effect.Parameters["forestTexture"].SetValue(forestTexture);
            _effect.Parameters["stoneTexture"].SetValue(stoneTexture);
        }

        private void InitialisePatches()
        {
            // generate a matrix of patches, giving each an offset so it knows it's
            // position within the matrix
            Patch.Count = 0;

            Trace.WriteLine("Building Patch Array");
            _patches = new Patch[PatchesPerRow, PatchesPerRow];

            for (int y = 0; y < PatchesPerRow; y++)
                for (int x = 0; x < PatchesPerRow; x++)
                    _patches[x, y] = new Patch(this, PatchWidth, new Point(x, y));
        }

        private void ActivatePoint(float x, float y)
        {
            _active[(int)(x + y * _mapSize)] = true;
        }

        internal float UnscaledHeightAt(System.Drawing.Point offset)
        {
            return UnscaledHeightAt(offset.X, offset.Y);
        }

        internal float HeightAt(System.Drawing.Point offset)
        {
            return HeightAt(offset.X, offset.Y);
        }

        internal bool IsPointActive(float x, float y)
        {   
            return _active[(int)(x + y * _mapSize)];
        }

        public override string ToString()
        {
            return String.Format("Indices:  {0}\nVertices: {1}", indexCount, vertexCount);
        }

        public override void Initialize()
        {
            base.Initialize();

            _device = this.GraphicsDevice;

            ContentManager content = new ContentManager(this.Game.Services);

            waterTexture = content.Load<Texture2D>(@"Content\Textures\water");
            grassTexture = content.Load<Texture2D>(@"Content\Textures\grass");
            forestTexture = content.Load<Texture2D>(@"Content\Textures\forest");
            stoneTexture = content.Load<Texture2D>(@"Content\Textures\stone");

            GenerateHeightField();
            InitialisePatches();
            CompileEffectFile();
            BuildBoundaryCache();
            GenerateNormalMap();
            ProcessQuads();
            InitializeBuffers();
            UpdatePatches();
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Tab))
            {
                _wireFrame = !_wireFrame;
                Thread.Sleep(100);
            }

            if (keyboardState.IsKeyDown(Keys.Space))
            {
                int count = 0;
                Trace.WriteLine(String.Format("MaxDistance: {0:0.00}  MaxPatch: {1}", MaxDistance, MaxPatchDepth));
                Trace.Indent();
                for (int y = 0; y < PatchesPerRow; y++)
                    for (int x = 0; x < PatchesPerRow; x++)
                        Trace.WriteLine((++count).ToString() + " " + _patches[x, y].ToString());
                Trace.Unindent();
            }

            //if (Camera.Moved)
            //    UpdatePatches();

            base.Update(gameTime);
        }

        /// <summary>
        /// Allows the game component to draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {

            _device.RenderState.FillMode = _wireFrame ? FillMode.WireFrame : FillMode.Solid;
            _device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            _effect.CurrentTechnique = _effect.Techniques[_wireFrame ? "WireFrame" : "TextureSplatting"];

            Matrix worldMatrix = Matrix.Identity;
            int offset = -1 * (Height / 2);
            worldMatrix = Matrix.CreateTranslation(new Vector3(offset, offset, 1));
            worldMatrix *= Matrix.CreateScale(new Vector3(_scale, _scale, _scale));

            _effect.Parameters["ViewProjection"].SetValue(worldMatrix * Camera.View * Camera.Projection);

            _effect.Begin();
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                _device.VertexDeclaration = new VertexDeclaration(
                    _device, VertexMultiTextured.VertexElements
                );

                for (int y = 0; y < PatchesPerRow; y++)
                {
                    for (int x = 0; x < PatchesPerRow; x++)
                    {
                        Patch p = _patches[x, y];
                        if (p.Visible)
                        {
                            _device.Vertices[0].SetSource(p.Buffer, 0, VertexMultiTextured.SizeInBytes);
                            _device.Indices = p.IndexBuffer;
                            try
                            {
                                _device.DrawIndexedPrimitives(
                                    PrimitiveType.TriangleList,
                                    0, 0, p.VerticesCount,
                                    0, p.IndexBufferLength / 3
                                );
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }
                }

                pass.End();
            }
            _effect.End();

            base.Draw(gameTime);
        }

        #region ITerrain Members

        public int Scale
        {
            get { return _scale; }
        }

        #endregion

        public int MaxPatchDepth { get; set; }
        public int PatchesPerRow { get; set; }
        public int PatchWidth { get; set; }
        public int Height { get; private set; }
        public Camera Camera { get; set; }
        public double MaxDistance { get; private set; }

    }
}


