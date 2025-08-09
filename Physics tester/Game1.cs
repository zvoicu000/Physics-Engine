using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
namespace Physics_tester
{
    public class PhysicsTester : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private FlatPhysics.FlatWorld _world;
        private Texture2D _pixel;
        private float _accumulator;
        private const float TimeStep = 1f / 60f;
        private KeyboardState _previousKeyboardState;

        public PhysicsTester()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _world = new FlatPhysics.FlatWorld();

            // Set gravity (downward)
            var gravityField = typeof(FlatPhysics.FlatWorld).GetField("gravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gravityField?.SetValue(_world, new FlatPhysics.FlatVector(0f, 9.81f));

            // --- PLATFORM ---
            // A wide, thin platform at the bottom center
            // Change the width parameter from 50f to a larger value, e.g., 200f
            AddStaticBox(400f, 450f, 800f, 20f); // x, y, width, height

            // --- OBSTACLES ---
            // A few static boxes as obstacles above the platform
            AddStaticBox(250f, 400f, 100f, 20f); // left obstacle
            AddStaticBox(550f, 350f, 100f, 20f); // right obstacle
            AddStaticBox(400f, 250f, 60f, 20f);  // center obstacle

            // Optionally, add left/right walls to keep objects in view
            AddStaticBox(10f, 300f, 20f, 600f);   // left wall
            AddStaticBox(790f, 300f, 20f, 600f);  // right wall

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            HandleInput();

            _accumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
            while (_accumulator >= TimeStep)
            {
                _world.Step(TimeStep, 8);
                _accumulator -= TimeStep;
            }

            base.Update(gameTime);
        }

        private void AddStaticBox(float x, float y, float width, float height)
        {
            if (FlatPhysics.FlatBody.CreateBoxBody(width, height, 1f, true, 0.2f, out var body, out string error))
            {
                body.MoveTo(new FlatPhysics.FlatVector(x, y));
                _world.AddBody(body);
            }
        }

        private void HandleInput()
        {
            KeyboardState kb = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            // Spawn a box on B press
            if (kb.IsKeyDown(Keys.B) && !_previousKeyboardState.IsKeyDown(Keys.B))
            {
                if (FlatPhysics.FlatBody.CreateBoxBody(50, 50, 1f, false, 0.2f, out var body, out string error))
                {
                    body.MoveTo(new FlatPhysics.FlatVector(mouse.X, mouse.Y));
                    _world.AddBody(body);
                }
            }

            // Spawn a circle on C press
            if (kb.IsKeyDown(Keys.C) && !_previousKeyboardState.IsKeyDown(Keys.C))
            {
                if (FlatPhysics.FlatBody.CreateCircleBody(25, 1f, false, 0.2f, out var body, out string error))
                {
                    body.MoveTo(new FlatPhysics.FlatVector(mouse.X, mouse.Y));
                    _world.AddBody(body);
                }
            }

            _previousKeyboardState = kb;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            foreach (var body in EnumerateBodies())
            {
                bool isStatic = body.IsStatic;

                // Platform: y == 450, width == 800, height == 20
                // Walls: (x == 10 or x == 790), width == 20, height == 600
                bool isPlatform = isStatic && body.ShapeType == FlatPhysics.ShapeType.Box &&
                                  body.Width == 800f && body.Height == 20f && body.Position.Y == 450f;
                bool isLeftWall = isStatic && body.ShapeType == FlatPhysics.ShapeType.Box &&
                                  body.Width == 20f && body.Height == 600f && body.Position.X == 10f;
                bool isRightWall = isStatic && body.ShapeType == FlatPhysics.ShapeType.Box &&
                                   body.Width == 20f && body.Height == 600f && body.Position.X == 790f;

                if (isPlatform || isLeftWall || isRightWall)
                {
                    // Fill rectangle
                    var rect = new Rectangle(
                        (int)(body.Position.X - body.Width / 2f),
                        (int)(body.Position.Y - body.Height / 2f),
                        (int)body.Width,
                        (int)body.Height
                    );
                    _spriteBatch.Draw(_pixel, rect, Color.Gray);
                }

                Color color = (isPlatform || isLeftWall || isRightWall)
                    ? Color.Gray
                    : isStatic ? Color.Yellow : (body.ShapeType == FlatPhysics.ShapeType.Circle ? Color.Red : Color.Green);

                int thickness = isStatic ? 4 : 2;

                if (body.ShapeType == FlatPhysics.ShapeType.Circle)
                    DrawCircle(body.Position, body.Radius, color, thickness);
                else if (body.ShapeType == FlatPhysics.ShapeType.Box)
                    DrawPolygon(body.GetTransformedVertices(), color, thickness);

                // Draw a rotation marker for dynamic boxes
                if (!isStatic && body.ShapeType == FlatPhysics.ShapeType.Box)
                {
                    var verts = body.GetTransformedVertices();
                    var center = new Vector2(body.Position.X, body.Position.Y);
                    var corner = new Vector2(verts[0].X, verts[0].Y);
                    DrawLine(center, corner, Color.White, 2);
                }
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private System.Collections.Generic.IEnumerable<FlatPhysics.FlatBody> EnumerateBodies()
        {
            var field = typeof(FlatPhysics.FlatWorld).GetField("bodyList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(_world) as System.Collections.Generic.List<FlatPhysics.FlatBody>;
                if (list != null)
                    foreach (var b in list) yield return b;
            }
        }

        private void DrawCircle(FlatPhysics.FlatVector center, float radius, Color color, int thickness = 2)
        {
            int segments = 24;
            float increment = MathHelper.TwoPi / segments;

            Vector2[] points = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = increment * i;
                points[i] = new Vector2(center.X + radius * (float)Math.Cos(angle), center.Y + radius * (float)Math.Sin(angle));
            }

            for (int i = 0; i < segments; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % segments];
                DrawLine(p1, p2, color, thickness);
            }
        }

        private void DrawPolygon(FlatPhysics.FlatVector[] vertices, Color color, int thickness = 2)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 p1 = new Vector2(vertices[i].X, vertices[i].Y);
                Vector2 p2 = new Vector2(vertices[(i + 1) % vertices.Length].X, vertices[(i + 1) % vertices.Length].Y);
                DrawLine(p1, p2, color, thickness);
            }
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 2)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();

            _spriteBatch.Draw(_pixel, start, null, color,
                angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0);
        }
    }
}
