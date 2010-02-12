using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using System.Configuration;
using XNAExtras;
using DLOD31.Properties;

#if DISCRETE
    using Component = Laan.DLOD.Discrete;
#else
    using Component = Laan.DLOD.Levelled;
#endif

namespace Laan.DLOD
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class GameController : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private ContentManager _content;
        private BitmapFont _fontCourierNew;
        private Component.Terrain _terrain;
        private Camera _camera;

        public GameController()
        {
            _graphics = new GraphicsDeviceManager(this);
            _content = new ContentManager(Services);

            IsMouseVisible = true;
            IsFixedTimeStep = false;

            _graphics.PreferredBackBufferWidth = 1680;
            _graphics.PreferredBackBufferHeight = 960;
            _graphics.ApplyChanges();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            int patchWidth = Settings.Default.PatchWidth;
            string heightMap = Settings.Default.HeightMap;

            _terrain = new Component.Terrain(this, heightMap, patchWidth);
            _camera = new Camera(_terrain, this, _terrain.Height);
            _terrain.Camera = _camera;
            _fontCourierNew = new BitmapFont(@"..\..\..\Content\courier12.xml", this);

            this.Components.Add(_fontCourierNew);
            this.Components.Add(_camera);
            this.Components.Add(_terrain);
            this.Components.Add(new FrameRate(this, new Point(700, 10)));
            //this.Components.Add(new Grid(this, _camera));

            base.Initialize();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the default game to exit on Xbox 360 and Windows
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            _graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            base.Draw(gameTime);
            string output = _camera.ToString() + "\n" + _terrain.ToString();
            _fontCourierNew.TextBox(new Rectangle(0, 0, 500, 100), Color.White, output);
        }
    }
}