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
    public partial class TerrainCamera : Microsoft.Xna.Framework.GameComponent
    {

        Vector3 _cameraPosition;
        Vector3 _lookAt;
        Vector3 _cameraUpVector;

        Game _game;

        int _step;
        int _size;

        public TerrainCamera(Game game, int size) : base(game)
        {
            _size = size;
            _step = _size / 10;
            _game = game;

            _cameraPosition = new Vector3(0, _size * 3, 1);
            _lookAt = new Vector3(0, 0, 0);
            _cameraUpVector = new Vector3(0, 1, 0);

        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Left))
            {
                _cameraPosition.X += _step;
                _lookAt.X += _step;
            }
            if (keyboardState.IsKeyDown(Keys.Right))
            {
                _cameraPosition.X -= _step;
                _lookAt.X -= _step;
            }
            if (keyboardState.IsKeyDown(Keys.PageUp))
            {
                _cameraPosition.Z -= _step;
                _lookAt.Z -= _step;
            }
            if (keyboardState.IsKeyDown(Keys.PageDown))
            {
                _cameraPosition.Z += _step;
                _lookAt.Z += _step;
            }
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                _cameraPosition.Y -= _step;
                _lookAt.Y -= _step;
            }
            if (keyboardState.IsKeyDown(Keys.Down))
            {
                _cameraPosition.Y += _step;
                _lookAt.Y += _step;
            }

            base.Update(gameTime);
        }

        public Vector3 LookAt
        {
            get { return _lookAt; }
        }


        public Matrix View
        {
            get { return Matrix.CreateLookAt(_cameraPosition, _lookAt, _cameraUpVector); }
        }

        public Matrix Projection
        {
            get {
                Rectangle windowSize = Game.Window.ClientBounds;

                return Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.PiOver4,
                    windowSize.Height * 1.0f /
                    windowSize.Width, 
                    1.0f, 
                    5000.0f
                ); 
            }
        }

    }
}


