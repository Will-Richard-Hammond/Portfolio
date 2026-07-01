using MGGameLibrary;
using MGGameLibrary.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.CompilerServices;
namespace MainQuest2_SuperStroop
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private StroopShape[] _shapes;
        private Color[] colours;
        private Texture2D circleTexture;
        private Texture2D triangleTexture;
        private Texture2D squareTexture;
        private SpriteFont _displayFont;
        private string _displayText = "SuperStroop";
        private Color _displayColour = Color.White;
        private ShapeRequester shapeRequester;
        private MouseState _previousMouseState;
        private bool _showFeedback = false;
        private string _feedbackText = "";
        private Color _feedbackColor = Color.White;
        private int _lives = 3;
        private string _livesText = "Lives: ";
        private int _score = 0;
        private string _scoreText = "Score: ";
        private float _timer = 2f;
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

            circleTexture = Content.Load<Texture2D>("circle");
            triangleTexture = Content.Load<Texture2D>("triangle");
            squareTexture = new Texture2D(GraphicsDevice, 1, 1);
            squareTexture.SetData(new[] { Color.White });
            _displayFont = Content.Load<SpriteFont>("DisplayFont");

            colours = new Color[]
            {
                Color.Red,
                Color.Yellow,
                Color.Green,
            };
            _shapes = new StroopShape[] {
                new StroopCircle(this,  "circle", Color.Red, circleTexture),
                new StroopCircle(this,  "circle", Color.Green, circleTexture),
                new StroopCircle(this,  "circle", Color.Blue, circleTexture),
                new StroopTriangle(this, "triangle", Color.Green,  triangleTexture),
                new StroopTriangle(this, "triangle", Color.Red,  triangleTexture),
                new StroopTriangle(this, "triangle", Color.Blue,  triangleTexture),
                new StroopSquare(this, "square",  Color.Blue, squareTexture),
                new StroopSquare(this, "square",  Color.Green, squareTexture),
                new StroopSquare(this, "square",  Color.Red, squareTexture),
                };
            foreach (StroopShape shape in _shapes)
            {
                Components.Add(shape);
            }
            shapeRequester = new ShapeRequester(_shapes, colours);
            shapeRequester.GetNewRequest();
        }

        protected override void Update(GameTime gameTime)
        {
            var mouseState = Mouse.GetState();
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            //default display text
            _displayColour = Color.White;
            _displayText = $"Click the {shapeRequester.stroopShape.Name}";
            float secondsPassed = gameTime.ElapsedGameTime.Milliseconds / 1000f;
            DecreaseTimer(secondsPassed);
            if (_timer <= 0)
            {
                _lives--;
                shapeRequester.GetNewRequest();
                ResetTimer(2f);
                
            }
            
            // Mouse over logic
            foreach (StroopShape shape in _shapes)
            {
                if (shape.IsInside(mouseState.Position) && mouseState.LeftButton != ButtonState.Pressed)
                {
                    _displayColour = shape.Colour;
                    _displayText = $"Mouse over the {shape.Name}";
                }
            }
            //check for mouse click on shapes
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                foreach (StroopShape shape in _shapes)
                {
                    if (shape.IsInside(mouseState.Position))
                    {
                        if (shape == shapeRequester.stroopShape)
                        {
                            mouseState = Mouse.GetState();
                            _showFeedback = true;
                            _feedbackText = $"Correct! It was the {shapeRequester.stroopShape.Name}";
                            _feedbackColor = shapeRequester.stroopShape.Colour;

                        }
                        else
                        {
                            mouseState = Mouse.GetState();
                            _showFeedback = true;
                            _feedbackText = $"Wrong! That was the {shape.Name}";
                            _feedbackColor = shapeRequester.stroopShape.Colour;

                        }
                    }
                }
            }
            mouseState = Mouse.GetState();
            if (_previousMouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton == ButtonState.Released)
            {
                if (_showFeedback && !_feedbackText.StartsWith("Correct!"))
                {
                    _lives--;
                }
                else if (_showFeedback && _feedbackText.StartsWith("Correct!"))
                {
                    _score = _score + 100;
                }
                ResetTimer();
                shapeRequester.GetNewRequest();
                _showFeedback = false;
                _feedbackText = "";
            }
            _previousMouseState = mouseState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_displayFont, $"{_livesText} {_lives}", new Vector2(10, 10), Color.White);
            _spriteBatch.DrawString(_displayFont, $"{_scoreText} {_score}", new Vector2(600, 10), Color.White);

            Vector2 timerPosition = new Vector2(_graphics.GraphicsDevice.Viewport.Width - _displayFont.MeasureString(_timer.ToString("0.0")).X - 40, 50);

            _spriteBatch.DrawString(_displayFont, _timer.ToString("0.0"), timerPosition + new Vector2(2, 2), new Color(242f / 255, 70f / 255, 80f / 255, 1f));
            _spriteBatch.DrawString(_displayFont, _timer.ToString("0.0"), timerPosition, new Color(252f / 255, 234f / 255, 51f / 255, 1f));
            if (_showFeedback && !string.IsNullOrEmpty(_feedbackText))
            {
                _spriteBatch.DrawString(_displayFont, _feedbackText, new Vector2(200, 10), _feedbackColor);
            }
            else
            {
                _spriteBatch.DrawString(_displayFont, _displayText, new Vector2(200, 10), _displayColour);
            }
            foreach (StroopShape shape in _shapes)
            {
                shape.Draw(_spriteBatch);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
        private void ResetTimer()// overload for reset timer
        {
            _timer = 2f;
        }
        private void ResetTimer(float newTime)
        {
            if (_timer <= 0)
            {
                _timer = newTime;
            }
        }
        private void DecreaseTimer(float amount)// check to see if this works
        {

            _timer -= amount;
            if (_timer < 0)
            {
                _timer = 0;
            }
        }
    }

    abstract public class StroopShape : GameComponent
    {
        protected MGGameLibrary.Shapes.Shape _shape; // Reference to library shape
        private Color _colour;
        public Color Colour => _colour;
        public string Name => _name;
        protected Rectangle _rectangle;
        private Texture2D _texture;
        private string _name;
        public Vector2 _startPosition;
        public Vector2 _endPosition;
        public float _movementDuration;// Duration of the movement in seconds
        public float _elapsedTime;
        static Random random = new Random();
        public StroopShape(Game game, string name, Color colour, Texture2D texture) : base(game)
        {
            int size = random.Next(50, 100);
            _colour = colour;
            _startPosition = new Vector2(random.Next(size, game.GraphicsDevice.Viewport.Width - size), random.Next(size, game.GraphicsDevice.Viewport.Height - size));
            _endPosition = new Vector2(random.Next(size, game.GraphicsDevice.Viewport.Width - size), random.Next(size, game.GraphicsDevice.Viewport.Height - size));
            _rectangle = new Rectangle((int)_startPosition.X, (int)_startPosition.Y, size, size);
            _texture = texture;
            _name = $"{GetColorName(colour)} {name}";
            _elapsedTime = 0f;
            _movementDuration = 2f + 3 * random.NextSingle();
        }



        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, _rectangle, _colour);
        }
        public bool IsInside(Point point)
        {
            return _shape.IsInside(point);
        }


        public override string ToString()
        {
            return $"{_colour} {_texture}";
        }
        public override void Update(GameTime gameTime)
        {
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            float t = MathHelper.Clamp(_elapsedTime / _movementDuration, 0f, 1f);

            Vector2 newPosition = (1 - t) * _startPosition + t * _endPosition;
            _rectangle.X = (int)newPosition.X;
            _rectangle.Y = (int)newPosition.Y;
            if (t >= 1f)
            {
                (_startPosition, _endPosition) = (_endPosition, _startPosition);
                _elapsedTime = 0f;
            }
            base.Update(gameTime);
        }
        private static string GetColorName(Color color)
        {
            if (color == Color.Red) return "Red";
            if (color == Color.Green) return "Green";
            if (color == Color.Blue) return "Blue";
            if (color == Color.Yellow) return "Yellow";
            // Add more as needed
            return $"R{color.R}G{color.G}B{color.B}";
        }

    }

    public class StroopCircle : StroopShape
    {

        public StroopCircle(Game game, string name, Color colour, Texture2D texture)
            : base(game, name, colour, texture)
            
        {
            _shape = new MGGameLibrary.Shapes.Circle(
            new Vector2(_rectangle.X + _rectangle.Width / 2, _rectangle.Y + _rectangle.Height / 2),
            _rectangle.Width / 2f);

        }

        //public override bool IsInside(Point point)
        //{
        //    // Calculate the center of the circle
        //    float centerX = _rectangle.X + _rectangle.Width / 2f;
        //    float centerY = _rectangle.Y + _rectangle.Height / 2f;
        //    float radius = _rectangle.Width / 2f;

        //    // Calculate the distance from the point to the center
        //    float dx = point.X - centerX;
        //    float dy = point.Y - centerY;
        //    float distanceSquared = dx * dx + dy * dy;

        //    // Check if the distance is within the radius
        //    return distanceSquared <= radius * radius;
        //}
    }

    public class StroopSquare : StroopShape
    {
        public StroopSquare(Game game, string name, Color colour, Texture2D texture)
            : base(game, name, colour, texture)
        {
            _shape = new MGGameLibrary.Shapes.Square(
            new Vector2(_rectangle.X, _rectangle.Y),
            _rectangle.Width);
        }
        //public override bool IsInside(Point point)
        //{
        //    return _rectangle.Contains(point);
        //}
    }
    public class StroopTriangle : StroopShape
    {
        public StroopTriangle(Game game, string name, Color colour, Texture2D texture)
            : base(game, name, colour, texture)
        {
            _shape = new MGGameLibrary.Shapes.Triangle(
            new Vector2(_rectangle.X + _rectangle.Width / 2, _rectangle.Y),
            _rectangle.Width);
        }
        //public override bool IsInside(Point point)
        //{
        //    // Barycentric technique
        //    //p1 = top middle, p2 = bottom left, p3 = bottom right
        //    Point p1 = new Point(_rectangle.X + _rectangle.Width / 2, _rectangle.Y);
        //    Point p2 = new Point(_rectangle.X, _rectangle.Y + _rectangle.Height);
        //    Point p3 = new Point(_rectangle.X + _rectangle.Width, _rectangle.Y + _rectangle.Height);

        //    float denom = (float)((p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y));
        //    float w1 = ((p2.Y - p3.Y) * (point.X - p3.X) + (p3.X - p2.X) * (point.Y - p3.Y)) / denom;
        //    float w2 = ((p3.Y - p1.Y) * (point.X - p3.X) + (p1.X - p3.X) * (point.Y - p3.Y)) / denom;
        //    float w3 = 1 - w1 - w2;

        //    return w1 >= 0 && w2 >= 0 && w3 >= 0;
        //}
    }

    public class ShapeRequester
    {
        private Color[] _colours;
        private StroopShape[] _shapes;
        private Random _random;
        private int _shapeIndex;
        public StroopShape stroopShape;
        public Color Colour;

        public ShapeRequester(StroopShape[] shapes, Color[] colours)
        {
            _shapes = shapes;
            _colours = colours;
            _random = new Random();
        }
        public void GetNewRequest()
        {
            _shapeIndex = _random.Next(0, _shapes.Length);
            stroopShape = _shapes[_shapeIndex];
            int colourIndex;
            do
            {
                colourIndex = _random.Next(0, _colours.Length);
                Colour = _colours[colourIndex];
            } while (Colour == stroopShape.Colour);
        }

    }
}