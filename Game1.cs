using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BlockGame;

public class Game1 : Game
{

    // Core graphics components
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Game state management (Finite State Machine)
    enum GameState { Logo, PressKey, Menu, Playing, Paused }
    GameState currentState = GameState.Logo;

    // UI & visual assets
    Texture2D logoTexture;
    Texture2D pixel; // 1x1 texture used for drawing overlays

    // Logo animation control
    float logoAlpha = 0f;     // Transparency value for fade-in / fade-out
    float logoTimer = 0f;     // Timer controlling logo animation duration

    // Gameplay data
    int score = 0;
    float playTime = 0f;      // Total play time in seconds

    // Press key screen animation
    float blinkTimer = 0f;
    bool showPressText = true;

    // UI font
    SpriteFont font;

    // Menu navigation
    int selectedIndex = 0;
    int pauseSelectedIndex = 0;

    // Menu definitions
    string[] menuItems = { "Start Game", "Quit" };
    string[] pauseMenuItems = { "Resume", "Exit to Main Menu" };

    // Player configuration
    Vector2 playerPosition;
    float playerSpeed = 200f;   // Movement speed in pixels per second

    // Target configuration
    Vector2 targetPosition;
    Random random = new Random();

    // Game textures
    Texture2D playerTexture;
    Texture2D targetTexture;

    // Used to detect key press transitions (KeyDown once)
    KeyboardState previousKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    /// <summary>
    /// Generates a new random position for the target
    /// within the visible screen bounds.
    /// </summary>
    private void SpawnTarget()
    {
        int x = random.Next(0, GraphicsDevice.Viewport.Width - targetTexture.Width);
        int y = random.Next(0, GraphicsDevice.Viewport.Height - targetTexture.Height);

        targetPosition = new Vector2(x, y);
    }

    private void ResetPlayer()
    {
        playerPosition = new Vector2(
            GraphicsDevice.Viewport.Width / 2 - playerTexture.Width / 2,
            GraphicsDevice.Viewport.Height / 2 - playerTexture.Height / 2
        );
    }

    /// <summary>
    /// Resets gameplay state including score,
    /// timer, player position, and target position.
    /// </summary>
    private void ResetGame()
    {
        score = 0;
        playTime = 0f;

        ResetPlayer();
        SpawnTarget();
    }

    protected override void Initialize()
    {

        base.Initialize();
    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        font = Content.Load<SpriteFont>("DefaultFont");
        logoTexture = Content.Load<Texture2D>("logo");
        playerTexture = Content.Load<Texture2D>("player");
        targetTexture = Content.Load<Texture2D>("target");
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        SpawnTarget();
        ResetPlayer();

        // ===== ✅ Position the texture after it has been loaded. =====
        playerPosition = new Vector2(
            GraphicsDevice.Viewport.Width / 2 - playerTexture.Width / 2,
            GraphicsDevice.Viewport.Height / 2 - playerTexture.Height / 2
        );
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();

        switch (currentState)
        {
            case GameState.Logo:
                UpdateLogo(gameTime);
                break;

            case GameState.PressKey:
                UpdatePressKey(gameTime, keyboard);
                break;

            case GameState.Menu:
                UpdateMenu(keyboard);
                break;

            case GameState.Playing:
                UpdatePlaying(gameTime, keyboard);
                break;

            case GameState.Paused:
                UpdatePaused(keyboard);
                break;
        }

        previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    /// <summary>
    /// Controls logo fade-in, hold, and fade-out animation.
    /// Total duration = 6 seconds.
    /// </summary>
    private void UpdateLogo(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        logoTimer += deltaTime;

        if (logoTimer <= 2f)
            logoAlpha = logoTimer / 2f;
        else if (logoTimer <= 4f)
            logoAlpha = 1f;
        else if (logoTimer <= 6f)
            logoAlpha = 1f - ((logoTimer - 4f) / 2f);
        else
        {
            logoTimer = 0f;
            logoAlpha = 0f;
            currentState = GameState.PressKey;
        }
    }

    private void UpdatePressKey(GameTime gameTime, KeyboardState keyboard)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        blinkTimer += deltaTime;

        if (blinkTimer >= 0.5f)
        {
            showPressText = !showPressText;
            blinkTimer = 0f;
        }

        if (keyboard.GetPressedKeys().Length > 0 &&
            previousKeyboard.GetPressedKeys().Length == 0)
        {
            currentState = GameState.Menu;
        }
    }

    private void UpdateMenu(KeyboardState keyboard)
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
                ResetGame();
                currentState = GameState.Playing;
            }
            else if (selectedIndex == 1)
            {
                Exit();
            }
        }
    }

    /// <summary>
    /// Handles active gameplay logic including:
    /// - Timer update
    /// - Pause input
    /// - Player movement
    /// - Boundary clamp
    /// - Collision detection
    /// </summary>
    private void UpdatePlaying(GameTime gameTime, KeyboardState keyboard)
    {
        playTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyboard.IsKeyDown(Keys.Escape) && previousKeyboard.IsKeyUp(Keys.Escape))
        {
            pauseSelectedIndex = 0;
            currentState = GameState.Paused;
            return;
        }

        HandlePlayerMovement(deltaTime, keyboard);
        ClampPlayer();
        CheckCollision();
    }

    /// <summary>
    /// Checks collision between player and target.
    /// Increments score and respawns target on hit.
    /// </summary>
    private void CheckCollision()
    {
        Rectangle playerRect = new Rectangle(
            (int)playerPosition.X,
            (int)playerPosition.Y,
            playerTexture.Width,
            playerTexture.Height
        );

        Rectangle targetRect = new Rectangle(
            (int)targetPosition.X,
            (int)targetPosition.Y,
            targetTexture.Width,
            targetTexture.Height
        );

        if (playerRect.Intersects(targetRect))
        {
            score++;        // +1 Score
            SpawnTarget();
        }
    }

    private void UpdatePaused(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.Up) && previousKeyboard.IsKeyUp(Keys.Up))
        {
            pauseSelectedIndex--;
            if (pauseSelectedIndex < 0)
                pauseSelectedIndex = pauseMenuItems.Length - 1;
        }

        if (keyboard.IsKeyDown(Keys.Down) && previousKeyboard.IsKeyUp(Keys.Down))
        {
            pauseSelectedIndex++;
            if (pauseSelectedIndex >= pauseMenuItems.Length)
                pauseSelectedIndex = 0;
        }

        // ===== ESC = Resume shortcut =====
        if (keyboard.IsKeyDown(Keys.Escape) && previousKeyboard.IsKeyUp(Keys.Escape))
        {
            currentState = GameState.Playing;
            return;
        }

        if (keyboard.IsKeyDown(Keys.Enter) && previousKeyboard.IsKeyUp(Keys.Enter))
        {
            if (pauseSelectedIndex == 0) // Resume
            {
                currentState = GameState.Playing;
            }
            else if (pauseSelectedIndex == 1) // Exit to Main Menu
            {
                currentState = GameState.Menu;
            }
        }
    }

    /// <summary>
    /// Handles player movement using WASD and Arrow keys.
    /// Movement is normalized to prevent faster diagonal speed.
    /// </summary>
    private void HandlePlayerMovement(float deltaTime, KeyboardState keyboard)
    {
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
            movement.Normalize();
            playerPosition += movement * playerSpeed * deltaTime;
        }
    }

    /// <summary>
    /// Prevents player from moving outside screen boundaries.
    /// </summary>
    private void ClampPlayer()
    {
        playerPosition.X = MathHelper.Clamp(
            playerPosition.X,
            0,
            GraphicsDevice.Viewport.Width - playerTexture.Width
        );

        playerPosition.Y = MathHelper.Clamp(
            playerPosition.Y,
            0,
            GraphicsDevice.Viewport.Height - playerTexture.Height
        );
    }

    /// <summary>
    /// Main draw loop.
    /// Renders content based on current game state.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {

        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        switch (currentState)
        {
            case GameState.Logo:
                DrawLogo();
                break;

            case GameState.PressKey:
                DrawPressKey();
                break;

            case GameState.Menu:
                DrawMenu();
                break;

            case GameState.Playing:
                DrawPlaying();
                break;

            case GameState.Paused:
                DrawPlaying();
                DrawPauseOverlay();
                break;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawLogo()
    {
        Vector2 position = new Vector2(
            (GraphicsDevice.Viewport.Width - logoTexture.Width) / 2,
            (GraphicsDevice.Viewport.Height - logoTexture.Height) / 2
        );

        _spriteBatch.Draw(logoTexture, position, Color.White * logoAlpha);
    }

    private void DrawPressKey()
    {
        if (!showPressText) return;

        string text = "Press Any Key";
        Vector2 textSize = font.MeasureString(text);

        Vector2 position = new Vector2(
            (GraphicsDevice.Viewport.Width - textSize.X) / 2,
            (GraphicsDevice.Viewport.Height - textSize.Y) / 2
        );

        _spriteBatch.DrawString(font, text, position, Color.White);
    }

    private void DrawMenu()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            Color color = (i == selectedIndex) ? Color.Yellow : Color.White;

            Vector2 textSize = font.MeasureString(menuItems[i]);

            float x = (GraphicsDevice.Viewport.Width - textSize.X) / 2;
            float y = 200 + i * 50;

            _spriteBatch.DrawString(font, menuItems[i], new Vector2(x, y), color);
        }
    }

    private void DrawPlaying()
    {
        _spriteBatch.Draw(playerTexture, playerPosition, Color.White);
        _spriteBatch.Draw(targetTexture, targetPosition, Color.White);

        DrawHUD();
    }

    /// <summary>
    /// Renders semi-transparent overlay and pause menu.
    /// Does not stop drawing gameplay behind it.
    /// </summary>
    private void DrawPauseOverlay()
    {
        // ===== Draw a dark, transparent background. =====
        _spriteBatch.Draw(
            pixel,
            new Rectangle(
                0,
                0,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height
            ),
            Color.Black * 0.6f   // 0.6 = Opacity 60%
        );

        string title = "Game Paused";
        Vector2 titleSize = font.MeasureString(title);

        Vector2 titlePos = new Vector2(
            (GraphicsDevice.Viewport.Width - titleSize.X) / 2,
            150
        );

        _spriteBatch.DrawString(font, title, titlePos, Color.Yellow);

        for (int i = 0; i < pauseMenuItems.Length; i++)
        {
            Color color = (i == pauseSelectedIndex) ? Color.Yellow : Color.White;

            Vector2 textSize = font.MeasureString(pauseMenuItems[i]);

            float x = (GraphicsDevice.Viewport.Width - textSize.X) / 2;
            float y = 250 + i * 50;

            _spriteBatch.DrawString(font, pauseMenuItems[i], new Vector2(x, y), color);
        }

        // Hint
        string hint = "ESC = Resume";
        Vector2 hintSize = font.MeasureString(hint);

        Vector2 hintPos = new Vector2(
            (GraphicsDevice.Viewport.Width - hintSize.X) / 2,
            400
        );

        _spriteBatch.DrawString(font, hint, hintPos, Color.Gray);
    }

    /// <summary>
    /// Displays gameplay HUD including:
    /// - Elapsed time (HH:mm:ss)
    /// - Current score
    /// </summary>
    private void DrawHUD()
    {
        // ===== Time =====
        TimeSpan time = TimeSpan.FromSeconds(playTime);
        string timeText = time.ToString(@"hh\:mm\:ss");

        _spriteBatch.DrawString(
            font,
            timeText,
            new Vector2(10, 10),
            Color.White
        );

        // ===== Score =====
        string scoreText = "Score: " + score;

        Vector2 textSize = font.MeasureString(scoreText);

        _spriteBatch.DrawString(
            font,
            scoreText,
            new Vector2(
                GraphicsDevice.Viewport.Width - textSize.X - 10,
                10
            ),
            Color.White
        );
    }

}

