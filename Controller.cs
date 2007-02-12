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

namespace Laan.DLOD
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class GameController : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        public ContentManager content;

        BitmapFont fontCourierNew;
        Terrain _terrain;
        Camera _camera;

        public GameController()
        {
            graphics = new GraphicsDeviceManager(this);
            content = new ContentManager(Services);

            this.IsMouseVisible = true;
            this.IsFixedTimeStep = false;

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
            int patchWidth = Int32.Parse(ConfigurationSettings.AppSettings["patchWidth"]);
            string heightMap = ConfigurationSettings.AppSettings["heightMap"];

            Point fpsPosition = new Point(700, 10);

            _terrain = new Terrain(this, heightMap, patchWidth);
            _camera = new Camera(_terrain, this, _terrain.Height);
            _terrain.Camera = _camera;
            fontCourierNew = new BitmapFont(@"..\..\..\Content\courier12.xml", this);

            this.Components.Add(fontCourierNew);
            this.Components.Add(_camera);
            this.Components.Add(_terrain);
            this.Components.Add(new FrameRate(this, fpsPosition));
            this.Components.Add(new Grid(this, _camera));

            base.Initialize();
        }

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        protected override void LoadGraphicsContent(bool loadAllContent)
        {
            if (loadAllContent)
            {
                // TODO: Load any ResourceManagementMode.Automatic content
            }

            // TODO: Load any ResourceManagementMode.Manual content
        }


        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        protected override void UnloadGraphicsContent(bool unloadAllContent)
        {
            if (unloadAllContent == true)
            {
                content.Unload();
            }
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
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            base.Draw(gameTime);
            string output = _camera.ToString() + "\n" + _terrain.ToString();
            fontCourierNew.TextBox(new Rectangle(0, 0, 500, 100), Color.White, output);
        }
    }
}