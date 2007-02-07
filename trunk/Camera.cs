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
        Terrain _terrain;

        Game _game;

        float _step;
        int _size;
        bool _moved;

        public TerrainCamera(Terrain terrain, Game game, int size) : base(game)
        {
            _terrain = terrain;
            _size = size;
            _step = _size / 20.0f;
            _game = game;

            //_cameraPosition = new Vector3(0, size / -2.0f, size);
            //_lookAt = new Vector3(0, 0, size / -4.0f);
            //_cameraUpVector = new Vector3(0, 0, 1);
            _cameraPosition = new Vector3(size / -2.0f, size / -2.0f, size);
            _lookAt = new Vector3(0, 0, size / -4.0f);
            _cameraUpVector = new Vector3(0, 0, 1);

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

            this.Game.Window.Title = String.Format("P: {0} L: {1}", _cameraPosition, _lookAt);

            Vector3 change = new Vector3();

            if (keyboardState.IsKeyDown(Keys.Left))
            {
                change.X -= _step;
                change.Y += _step;
            }
            if (keyboardState.IsKeyDown(Keys.Right))
            {
                change.X += _step;
                change.Y -= _step;
            }
            if (keyboardState.IsKeyDown(Keys.PageUp))
            {
                change.Z -= _step;
            }
            if (keyboardState.IsKeyDown(Keys.PageDown))
            {
                change.Z += _step;
            }
            if (keyboardState.IsKeyDown(Keys.Up))
            {
                change.X += _step;
                change.Y += _step;
            }
            if (keyboardState.IsKeyDown(Keys.Down))
            {
                change.X -= _step;
                change.Y -= _step;
            }

            change *= _terrain._scale;

            _cameraPosition += change;
            _lookAt += change;

            _moved = ((change.X != 0) || (change.Y != 0));

            base.Update(gameTime);
        }

        public bool Moved
        {
            get { return _moved; }
        }

        public Vector3 LookAt
        {
            get { return _lookAt; }
        }

        public Vector3 CameraPosition
        {
            get { return _cameraPosition; }
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
                    windowSize.Height * 1.0f / windowSize.Width, 
                    1.0f, 
                    50000.0f
                ); 
            }
        }

    }
}


