//extern alias MonoGameFramework;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using xInput = Microsoft.Xna.Framework.Input;
using ScreenControlsSample;
using Audacious.GameModel;
using System.Collections.Generic;
//using Microsoft.Phone.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace Audacious
{
    public class BaseGame : Game
    {
        public static Vector2 ScreenSize = new Vector2(800f, 480f);
        public static Vector2 GameScreenSize = new Vector2(512, 384);
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class AudaciousGame : BaseGame
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        ScreenPad screenPad;
        View currentView;
        int levelIndex = -1;
        Texture2D gameFrameTexture;
        bool buttonBPressed = false;

        public AudaciousGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft;
            graphics.PreferredBackBufferHeight = 480;
            graphics.PreferredBackBufferWidth = 800;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            ContentHelper.Setup(Content);

            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>
                (graphics_PreparingDeviceSettings);

            // Frame rate is 60 fps
            TargetElapsedTime = TimeSpan.FromTicks(166667);

            // Extend battery life under lock.
            InactiveSleepTime = TimeSpan.FromSeconds(1);

            Messenger.Default.Register<PassedLevelMessage>(this, (message) =>
            {
                if (message.LevelPassed == 8)
                    levelIndex = -1;

                PassLevel();
            });
        }

        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.PresentationInterval = PresentInterval.One;
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

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            gameFrameTexture = ContentHelper.Instance.GetContent<Texture2D>("GameFrame");
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            screenPad = new ScreenPad
            (
                this,
                ContentHelper.Instance.GetContent<Texture2D>("ThumbBase"),
                ContentHelper.Instance.GetContent<Texture2D>("ThumbStick"),
                ContentHelper.Instance.GetContent<Texture2D>("ABXY_buttons")
            );

            PassLevel();
        }

        private void PassLevel()
        {
            levelIndex++;
            BossMovement[] bossMovements = new BossMovement[] { 
                BossMovement.WalkHorizontal,
                BossMovement.FloatSenoidHorizontal,
                BossMovement.WalkHorizontal,
                BossMovement.FloatSenoidHorizontal,
                BossMovement.WalkHorizontal,
                BossMovement.WalkHorizontal,
                BossMovement.WalkHorizontal,
                BossMovement.WalkHorizontal
            };

            string[] levelNames = new string[] {
                "MEDUSA",
                "THANATOS",
                "HYDRA",
                "CYCLOPS",
                "AUTOMATON",
                "GIANT",
                "MINOTAUR",
                "ARGUS"
            };

            if (currentView != null)
            {
                currentView.UnregisterActions();
                currentView.UnloadContent();

                Content.RootDirectory = "Content";
                ContentHelper.Setup(Content);

                gameFrameTexture = ContentHelper.Instance.GetContent<Texture2D>("GameFrame");
                screenPad = new ScreenPad
                (
                    this,
                    ContentHelper.Instance.GetContent<Texture2D>("ThumbBase"),
                    ContentHelper.Instance.GetContent<Texture2D>("ThumbStick"),
                    ContentHelper.Instance.GetContent<Texture2D>("ABXY_buttons")
                );
            }

            currentView = new View(graphics, spriteBatch, Content, screenPad, bossMovements[levelIndex], levelIndex + 1, levelNames[levelIndex]);
            currentView.RegisterActions();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (xInput.GamePad.GetState(PlayerIndex.One).Buttons.Back == xInput.ButtonState.Pressed)
                this.Exit();

            if (screenPad.GetState().Buttons.B == ButtonState.Pressed)
            {
                buttonBPressed = true;
            }
            else if (screenPad.GetState().Buttons.B == ButtonState.Released && buttonBPressed)
            {
                buttonBPressed = false;
                //MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                //marketplaceReviewTask.Show();
            }

            screenPad.Update();
            currentView.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            var matrix = Matrix.CreateScale(1f, 1f, 1f);
            //GraphicsDevice.Viewport = new Viewport(0, 0, 800, 480);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix);
            currentView.Draw(gameTime);
            spriteBatch.Draw(gameFrameTexture, new Vector2(0, 0), Color.White);
            DrawScreenPad(gameTime);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawScreenPad(GameTime gameTime)
        {
            screenPad.Draw(gameTime, spriteBatch);
        }

        private void DrawPlayer(Vector2 deviceScreenSize)
        {
        }

    }
}
