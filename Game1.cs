using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BlockGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    enum GameState
    {
        Logo,
        PressKey,
        Menu,
        Playing,
        Paused
    }

    GameState currentState = GameState.Logo;
    Texture2D logoTexture;
    float logoAlpha = 0f;
    float logoTimer = 0f;

    float blinkTimer = 0f;
    bool showPressText = true;

    SpriteFont font;
    int selectedIndex = 0;

    string[] menuItems = { "Start Game", "Quit" };
    // Player
    Vector2 playerPosition;
    float playerSpeed = 200f;   // ความเร็ว (pixel ต่อวินาที)
    int playerSize = 16;
    
    Vector2 targetPosition;
    Random random = new Random();

    Texture2D playerTexture;
    Texture2D targetTexture;
    KeyboardState previousKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    private void SpawnTarget()
{
    int x = random.Next(0, GraphicsDevice.Viewport.Width - targetTexture.Width);
    int y = random.Next(0, GraphicsDevice.Viewport.Height - targetTexture.Height);

    targetPosition = new Vector2(x, y);
}

    protected override void Initialize()
    {
        playerPosition = new Vector2(
            GraphicsDevice.Viewport.Width / 2 - playerSize / 2,
            GraphicsDevice.Viewport.Height / 2 - playerSize / 2
        );
        base.Initialize();
    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        font = Content.Load<SpriteFont>("DefaultFont");
        logoTexture = Content.Load<Texture2D>("logo");
        playerTexture = Content.Load<Texture2D>("player");
        targetTexture = Content.Load<Texture2D>("target");
        SpawnTarget();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();

        // ---------- LOGO ----------
        if (currentState == GameState.Logo)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            logoTimer += deltaTime;

            // 0 - 2 วิ : Fade In
            if (logoTimer <= 2f)
            {
                logoAlpha = logoTimer / 2f;
            }
            // 2 - 4 วิ : ค้างไว้
            else if (logoTimer <= 4f)
            {
                logoAlpha = 1f;
            }
            // 4 - 6 วิ : Fade Out
            else if (logoTimer <= 6f)
            {
                logoAlpha = 1f - ((logoTimer - 4f) / 2f);
            }
            else
            {
                logoTimer = 0f;
                logoAlpha = 0f;
                currentState = GameState.PressKey;
            }
        }

        // ---------- PRESS ANY KEY ----------
        else if (currentState == GameState.PressKey)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            blinkTimer += deltaTime;

            if (blinkTimer >= 0.5f)
            {
                showPressText = !showPressText;
                blinkTimer = 0f;
            }

            if (Keyboard.GetState().GetPressedKeys().Length > 0 &&
                previousKeyboard.GetPressedKeys().Length == 0)
            {
                currentState = GameState.Menu;
            }
        }

        // ---------- MENU ----------
        else if (currentState == GameState.Menu)
        {
            if (keyboard.IsKeyDown(Keys.Up) && previousKeyboard.IsKeyUp(Keys.Up))
            {
                selectedIndex--;
                if (selectedIndex < 0)
                    selectedIndex = menuItems.Length - 1;
            }

            if (keyboard.IsKeyDown(Keys.Down) && previousKeyboard.IsKeyUp(Keys.Down))
            {
                selectedIndex++;
                if (selectedIndex >= menuItems.Length)
                    selectedIndex = 0;
            }

            if (keyboard.IsKeyDown(Keys.Enter) && previousKeyboard.IsKeyUp(Keys.Enter))
            {
                if (selectedIndex == 0)
                {
                    currentState = GameState.Playing;
                }
                else if (selectedIndex == 1)
                {
                    Exit();
                }
            }
        }

        // ---------- PLAYING ----------
        else if (currentState == GameState.Playing)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // กด ESC เพื่อ Pause
            if (keyboard.IsKeyDown(Keys.Escape) && previousKeyboard.IsKeyUp(Keys.Escape))
            {
                currentState = GameState.Paused;
            }

            // --------------------
            // PLAYER MOVEMENT
            // --------------------

            Vector2 movement = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
                movement.Y -= 1;

            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
                movement.Y += 1;

            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                movement.X -= 1;

            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                movement.X += 1;

            if (movement != Vector2.Zero)
            {
                movement.Normalize(); // กันเดินเฉียงเร็วกว่า
                playerPosition += movement * playerSpeed * deltaTime;
            }

            // --------------------
            // CLAMP ไม่ให้ออกนอกจอ
            // --------------------

            playerPosition.X = MathHelper.Clamp(
                playerPosition.X,
                0,
                GraphicsDevice.Viewport.Width - playerSize
            );

            playerPosition.Y = MathHelper.Clamp(
                playerPosition.Y,
                0,
                GraphicsDevice.Viewport.Height - playerSize
            );
        }


        // ---------- PAUSED ----------
        else if (currentState == GameState.Paused)
        {
            if (keyboard.IsKeyDown(Keys.Escape) && previousKeyboard.IsKeyUp(Keys.Escape))
            {
                currentState = GameState.Playing;
            }
        }

        previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        

        _spriteBatch.Begin();
        

        if (currentState == GameState.Logo)
        {
            Vector2 position = new Vector2(
                (GraphicsDevice.Viewport.Width - logoTexture.Width) / 2,
                (GraphicsDevice.Viewport.Height - logoTexture.Height) / 2
            );

            _spriteBatch.Draw(logoTexture, position, Color.White * logoAlpha);
        }
        else if (currentState == GameState.PressKey)
        {
            if (showPressText)
            {
                string text = "Press Any Key";
                Vector2 textSize = font.MeasureString(text);

                Vector2 position = new Vector2(
                    (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    (GraphicsDevice.Viewport.Height - textSize.Y) / 2
                );

                _spriteBatch.DrawString(font, text, position, Color.White);
            }
        }
        else if (currentState == GameState.Menu)
        {
            for (int i = 0; i < menuItems.Length; i++)
            {
                Color color = (i == selectedIndex) ? Color.Yellow : Color.White;

                Vector2 textSize = font.MeasureString(menuItems[i]);

                float x = (GraphicsDevice.Viewport.Width - textSize.X) / 2;
                float y = 200 + i * 50;

                Vector2 position = new Vector2(x, y);

                _spriteBatch.DrawString(font, menuItems[i], position, color);
            }
        }
        else if (currentState == GameState.Playing || currentState == GameState.Paused)
        {
            // วาด Player
            Rectangle playerRect = new Rectangle(
                (int)playerPosition.X,
                (int)playerPosition.Y,
                playerSize,
                playerSize
            );

            _spriteBatch.Draw(playerTexture, playerPosition, Color.White);
            _spriteBatch.Draw(targetTexture, targetPosition, Color.White);

            if (currentState == GameState.Playing)
            {
                _spriteBatch.DrawString(font, "Game Started!",
                    new Vector2(300, 200), Color.White);
            }
            else if (currentState == GameState.Paused)
            {
                string text = "Game Paused\nPress ESC to Resume";
                Vector2 textSize = font.MeasureString(text);

                Vector2 position = new Vector2(
                    (GraphicsDevice.Viewport.Width - textSize.X) / 2,
                    (GraphicsDevice.Viewport.Height - textSize.Y) / 2
                );

                _spriteBatch.Draw(playerTexture, playerPosition, Color.White);
            }
            
        }
        
        _spriteBatch.End();
        

        base.Draw(gameTime);
    }
}

