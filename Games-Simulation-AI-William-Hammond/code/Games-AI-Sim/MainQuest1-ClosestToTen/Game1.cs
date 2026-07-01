using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq.Expressions;

namespace MainQuest1_ClosestToTen
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Rectangle _rectangle;
        private Rectangle _timerRectangle, _button;
        private Texture2D _whitePixelTexure;
        private Texture2D _studioLogoTexture;
        private float _timeRemaining;
        private float _stopwatch;
        private float _score;
        private SpriteFont _timerFont;
        private MouseState _prevMouse, _currMouse;
        enum Screen { FlashScreen, TitleScreen, CreditsScreen, GameScreen, PauseScreen, GameOverScreen };
        private Screen _screen;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _timerFont = Content.Load<SpriteFont>("Timer");

            _studioLogoTexture = Content.Load<Texture2D>("GGG_TypeLogo2");
            int rectangleWidth = _studioLogoTexture.Width;
            int rectangleHeight = _studioLogoTexture.Height;

            int rectangleX = (GraphicsDevice.Viewport.Width - rectangleWidth) / 2;
            int rectangleY = (GraphicsDevice.Viewport.Height - rectangleHeight) / 2;

            int timerRectangleWidth = (int)_timerFont.MeasureString("00.00").X + 20;
            int timerRectangleHeight = (int)_timerFont.MeasureString("00.00").Y + 20;





            _rectangle = new Rectangle(rectangleX, rectangleY, rectangleWidth, rectangleHeight);
            _timerRectangle = new Rectangle((GraphicsDevice.Viewport.Width - timerRectangleWidth) / 2, (GraphicsDevice.Viewport.Height - timerRectangleHeight) / 2, timerRectangleWidth, timerRectangleHeight);
            _button = _timerRectangle;
            

            _whitePixelTexure = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixelTexure.SetData(new[] { Color.White });

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            _prevMouse = _currMouse;
            _currMouse = Mouse.GetState();
            switch (_screen)
            {
                case Screen.FlashScreen:

                    ResetTimer(1f);
                    
                    float secondsPassed = gameTime.ElapsedGameTime.Milliseconds / 1000f;

                    DecreaseTimer(secondsPassed);

                    if (_timeRemaining <= 0)
                    {
                        _screen = Screen.TitleScreen;
                    }

                    break;
                case Screen.TitleScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.C))
                    {
                        _screen = Screen.CreditsScreen;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Space))
                    {
                        _screen = Screen.GameScreen;
                    }
                    break;
                case Screen.CreditsScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.T))
                    {
                        _screen = Screen.TitleScreen;
                    }
                    break;
                case Screen.GameScreen:
                    ResetTimer(10f);
                    ResetStopwatch(10f);
                    secondsPassed = gameTime.ElapsedGameTime.Milliseconds / 1000f;
                    DecreaseTimer(secondsPassed);
                    _stopwatch += secondsPassed;
                    if (Keyboard.GetState().IsKeyDown(Keys.U))
                    {
                        _screen = Screen.PauseScreen;
                    }
                    else if (_button.Contains(_currMouse.Position)
                     && _currMouse.LeftButton == ButtonState.Pressed
                     && _prevMouse.LeftButton == ButtonState.Released)
                    {
                        _score = (int)System.Math.Round(100f * System.MathF.Min(_stopwatch, 10f));
                        if (_score > 1000) _score = 1000;
                        ResetTimer();
                        _screen = Screen.GameOverScreen;
                        
                    }
                    break;
                case Screen.PauseScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.I))
                    {
                        _screen = Screen.GameScreen;
                    }
                    break;
                case Screen.GameOverScreen:// need to add stuff here too
                    
                    if (_timeRemaining > -1f)
                    {
                        secondsPassed = gameTime.ElapsedGameTime.Milliseconds / 1000f;
                        DecreaseTimer(secondsPassed);

                        if (_timeRemaining <= 0)
                        {
                            _screen = Screen.TitleScreen;
                        }
                        break;
                    }
                    break;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            Vector2 timerSize = _timerFont.MeasureString(_timeRemaining.ToString());

            // TODO: Add your drawing code here
            switch (_screen)
            {
                case Screen.FlashScreen:
                    _spriteBatch.Begin();

                    _spriteBatch.Draw(_studioLogoTexture, _rectangle, Color.White);

                    Vector2 timerPosition = new Vector2(_graphics.GraphicsDevice.Viewport.Width - _timerFont.MeasureString(_timeRemaining.ToString("0.0")).X - 10, 10);

                    _spriteBatch.DrawString(_timerFont, _timeRemaining.ToString("0.0"), timerPosition + new Vector2(2, 2), new Color(242f / 255, 70f / 255, 80f / 255, 1f));
                    _spriteBatch.DrawString(_timerFont, _timeRemaining.ToString("0.0"), timerPosition, new Color(252f / 255, 234f / 255, 51f / 255, 1f));

                    _spriteBatch.End();
                    break;
                case Screen.TitleScreen:
                    _spriteBatch.Begin();
                    GraphicsDevice.Clear(Color.GreenYellow);
                    _spriteBatch.DrawString(_timerFont, "Press C for Credits\nPress Space to Start Game", new Vector2(100, 100), Color.Black);
                    _spriteBatch.End();
                    break;
                case Screen.CreditsScreen:
                    _spriteBatch.Begin();
                    GraphicsDevice.Clear(Color.Black);
                    _spriteBatch.DrawString(_timerFont, "Credits\nMostly Simon's work\n Refactored by Will", new Vector2(100, 100), Color.White);
                    _spriteBatch.End();
                    break;
                case Screen.GameScreen:
                    _spriteBatch.Begin();
                    GraphicsDevice.Clear(Color.White);
                    _spriteBatch.DrawString(_timerFont, "Press the button\n as close to 10\n as possible", new Vector2(100, 20), Color.Black);
                    _spriteBatch.Draw(_whitePixelTexure, _timerRectangle, Color.Red);
                    _spriteBatch.DrawString(_timerFont, _stopwatch.ToString("00.00"), new Vector2(_timerRectangle.X + 10, _timerRectangle.Y + 10) + new Vector2(2, 2), Color.Blue);
                    _spriteBatch.DrawString(_timerFont, _stopwatch.ToString("00.00"), new Vector2(_timerRectangle.X + 10, _timerRectangle.Y + 10), new Color(252f / 255, 234f / 255, 51f / 255, 1f));
                    // figure out how to center this and stuff (member variables)

                    _spriteBatch.End();
                    break;
                case Screen.PauseScreen:
                    _spriteBatch.Begin();
                    GraphicsDevice.Clear(Color.Gray);
                    _spriteBatch.DrawString(_timerFont, "Paused\n Press I to unpause", new Vector2(100, 100), Color.Black);
                    _spriteBatch.End();
                    break;
                case Screen.GameOverScreen:
                    _spriteBatch.Begin();
                    GraphicsDevice.Clear(Color.Red);
                    _spriteBatch.DrawString(_timerFont, "Game Over", new Vector2(100, 100), Color.Black);
                    _spriteBatch.DrawString(_timerFont, $"Score: {_score}", new Vector2(100, 160), Color.Black);
                    _spriteBatch.End();
                    break;
            }
            //_spriteBatch.End();
            base.Draw(gameTime);
        }
        private void ResetTimer()// overload for reset timer
        {
            _timeRemaining = 2f;
        }
        private void ResetTimer(float newTime)
        {
            if (_timeRemaining <= 0)
            {
                _timeRemaining = newTime;
            }
        }
       
        private void ResetStopwatch(float max)
        {
            if (_stopwatch > max)
                _stopwatch = 0f;
        }
        private void DecreaseTimer(float amount)// check to see if this works
        {
            
            _timeRemaining -= amount;
            if (_timeRemaining < 0)
            {
                _timeRemaining = 0;
            }
        }

    }
}
