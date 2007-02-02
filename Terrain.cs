using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace Laan.DLOD
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public partial class Terrain : Microsoft.Xna.Framework.DrawableGameComponent
    {
        //VertexPositionColor[] vertices;

        Effect effect;
        //GraphicsDeviceManager graphics;
        internal GraphicsDevice _device;
        private int             _height;
        private TerrainCamera   _camera;

		public Terrain(Game game, string heightMapName, int patchWidth) : base(game)
        {
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

			_maxPatchDepth = 2 + (int)log2;

			double patchCount = _height / _patchWidth;
                                                  
			if(!IsWholeNumber(patchCount))
				throw new ArgumentException(
					"PatchLevel incompatable with heightMap - must allow an NxN number of patches");

			_patchesPerRow = (int)patchCount;
		}

        internal float HeightAt(System.Drawing.Point offset)
        {
            return 0; // (_heightMap.GetPixel(offset.X, offset.Y).R / 255.0f) * 2.0f;
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

        internal Patch[,] patches;

		private System.Drawing.Bitmap _heightMap;
		private int _maxPatchDepth;
		private int _patchesPerRow;
		private int _patchWidth;

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
            patches = new Patch[_patchesPerRow, _patchesPerRow];
            for (int y = 0; y < _patchesPerRow; y++)
                for (int x = 0; x < _patchesPerRow; x++)
                    patches[x, y] = new Patch(this, _patchWidth, new Point(x, y)); 
            
            
            CompiledEffect compiledEffect = Effect.CompileEffectFromFile("@/../../../../effect.fx", null, null, CompilerOptions.None, TargetPlatform.Windows);
            effect = new Effect(this.GraphicsDevice, compiledEffect.GetEffectCode(), CompilerOptions.None, null);

            SetUpCamera();
        }

        private void SetUpCamera()
        {
            //Matrix viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 40), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            //Matrix projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 800 / 500, 1.0f, 50.0f);

            //effect.Parameters["xView"].SetValue(viewMatrix);
            //effect.Parameters["xProjection"].SetValue(projectionMatrix);

            //effect.Parameters["xView"].SetValue(_camera.View);
            //effect.Parameters["xProjection"].SetValue(_camera.Projection);
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
        }
        
        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            //if (keyboardState.IsKeyDown(Keys.Space))
            {
                for (int y = 0; y < _patchesPerRow; y++)
                    for (int x = 0; x < _patchesPerRow; x++)
                        patches[x, y].Update(_camera);
            }
            base.Update(gameTime);
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {

            _device.RenderState.FillMode = FillMode.WireFrame;

            _device.RenderState.CullMode = CullMode.CullClockwiseFace;
            _device.Clear(Color.DarkSlateBlue);

            effect.CurrentTechnique = effect.Techniques["Colored"];

            Matrix worldMatrix = Matrix.Identity;
            int offset = -1 * (_height / 2);
            worldMatrix = Matrix.CreateTranslation(new Vector3(offset, offset, 1));
            worldMatrix *= Matrix.CreateRotationX(-0.9f);
            //worldMatrix *= Matrix.CreateScale(new Vector3(2, 1, 2));

            effect.Parameters["xView"].SetValue(_camera.View);
            effect.Parameters["xProjection"].SetValue(_camera.Projection);
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                // generate index buffer for each patch
                for (int y = 0; y < _patchesPerRow; y++)
                {
                    for (int x = 0; x < _patchesPerRow; x++)
                    {
                        Patch p = patches[x, y];
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

                        _device.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0, 0, p.VerticesCount,
                            0, p.IndexBuffer.Length / 3
                        );
                    }
                }

                //_device.VertexDeclaration =
                //_device.DrawUserPrimitives

                pass.End();
            }
            effect.End();

            base.Draw(gameTime);
        }
    }
}


