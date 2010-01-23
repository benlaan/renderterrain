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
    public partial class Grid : Microsoft.Xna.Framework.DrawableGameComponent
    {
        BasicEffect _effect; 
        private Camera _camera;
        internal GraphicsDevice _device;
        VertexPositionNormalTexture[] pointList;

        public Grid(Game game, Camera camera) : base(game)
        {
            _camera = camera;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {

            Vector3[] points = new Vector3[6] {
                    new Vector3(-1, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, -1),
                    new Vector3(0, 0, 1)
            };

            base.Initialize();

            pointList = new VertexPositionNormalTexture[6];
            for(int i=0; i < 6; i++)
                pointList[i] = new VertexPositionNormalTexture(
                    points[i],
                    Vector3.Forward,
                    new Vector2()
                );

            _device = this.GraphicsDevice;

            _effect = new BasicEffect(this.GraphicsDevice, null);
            _effect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
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

            Matrix worldMatrix = Matrix.Identity;
            worldMatrix *= Matrix.CreateScale(10000);

            _effect.View = _camera.View;
            _effect.Projection = _camera.Projection;
            _effect.World = worldMatrix;

            _effect.Begin();
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Begin();

                _device.VertexDeclaration = new VertexDeclaration(
                        _device, 
                        VertexPositionNormalTexture.VertexElements
                );

                VertexBuffer buffer = new VertexBuffer(
                    _device, VertexPositionNormalTexture.SizeInBytes * (pointList.Length),
                    BufferUsage.None
                );

                buffer.SetData<VertexPositionNormalTexture>(pointList);

                _device.Vertices[0].SetSource(
                    buffer,
                    0,
                    VertexPositionNormalTexture.SizeInBytes
                );

                _device.DrawPrimitives(PrimitiveType.LineList,0, 3);

                pass.End();
            }
            _effect.End();
        }

    }
}


