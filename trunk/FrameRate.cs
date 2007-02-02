using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


namespace Laan.DLOD
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public partial class FrameRate : Microsoft.Xna.Framework.DrawableGameComponent
    {
        private float deltaFPSTime;
        private double currentFramerate;
        private string windowTitle, displayFormat;
        private bool canDraw;
        private bool showDecimals;
        
        public FrameRate(Game game) : base(game)
        {
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            this.canDraw = false;
            this.currentFramerate = 0;
            this.windowTitle = this.Game != null ? this.Game.Window.Title : String.Empty;

            base.Initialize();
        }

        public double Current
        {
            get { return this.currentFramerate; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the time step must be fixed or not.
        /// </summary>
        /// <remarks>
        /// If set to true, the game will target the desired constant framerate set in your main class ('Game1', by default).
        /// </remarks>
        public bool IsFixedTimeStep
        {
            get { return this.Game.IsFixedTimeStep; }
            set
            {
                if (this.Game != null)
                    this.Game.IsFixedTimeStep = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the framerate will display decimals on screen or not.
        /// </summary>
        public bool ShowDecimals
        {
            get { return this.showDecimals; }
            set { this.showDecimals = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the decimal part of the framerate value must be display as fixed format (or as double format, otherwise).
        /// </summary>
        /// <remarks>
        /// The 'ShowDecimals' property must be set to true in order to set the proper format.
        /// </remarks>
        public bool FixedFormatDisplay
        {
            get { return this.displayFormat == "F"; }
            set { this.displayFormat = value == true ? "F" : "R"; }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // The time since Update() method was last called.
            float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Ads the elapsed time to the cumulative delta time.
            this.deltaFPSTime += elapsed;

            // If delta time is greater than a second: (a) the framerate is calculated, (b) it is marked to be drawn, and (c) the delta time is adjusted, accordingly.
            if (this.deltaFPSTime > 1000)
            {
                this.currentFramerate = 1000 / elapsed;
                this.deltaFPSTime -= 1000;
                this.canDraw = true;
            }
            
            base.Update(gameTime);
        }

        /// <summary>
        /// Called when the gamecomponent needs to be drawn.
        /// </summary>
        /// <remarks>
        /// Currently, the framerate is shown in the window's title of the game.
        /// </remarks>
        public override void Draw(GameTime gameTime)
        {
            // If the framerate can be drawn, it is shown in the window's title of the game.
            if (this.canDraw)
            {
                string currentFramerateString = this.showDecimals ? this.currentFramerate.ToString(this.displayFormat) : ((int)this.currentFramerate).ToString("D");

                this.Game.Window.Title = "FPS: " + currentFramerateString;

                this.canDraw = false;
            }
        }
    }
}


