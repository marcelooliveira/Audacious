using Audacious.GameModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using ScreenControlsSample;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xInput = Microsoft.Xna.Framework.Input;

namespace Audacious.GameModel
{
    public class View
    {
        enum ViewState
        {
            Menu,
            Intro,
            Playing,
            ShowLevel,
            PassedLevel,
            GameOver,
            TheEnd
        }

        ViewState viewState = ViewState.Intro;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        ContentManager content;
        ScreenPad screenPad;
        BossMovement bossMovement;
        int levelNumber;
        string levelName;
        bool buttonAPressed = false;
        bool buttonBPressed = false;

        Vector2 screenSize = new Vector2(800f, 480f);
        Vector2 gameScreenSize = new Vector2(512, 480);
        protected Vector2 windowTilesSize = new Vector2(32, 30);
        SpriteFont font;
        int tickInMS = 125;
        int tickCount = 0;
        int tileWidth = 16;

        int score = 0;
        int hiScore = 0;
        int rest = 2;
        int stage = 1;
        float lastScrollRows = -1;
        bool showLove = false;

        TimeSpan accumElapsedGameTime = TimeSpan.FromSeconds(0);
        int collisionMS = 5;
        TimeSpan accumCollisionTime = TimeSpan.FromSeconds(0);
        int soundCheckMS = 50;
        TimeSpan accumSoundCheckTime = TimeSpan.FromSeconds(0);
        Song currentSong;
        Song level1Song;
        Song level2Song;
        Song bossSong;
        Song introSong;
        Song dyingSong;
        Song gameOverSong;
        Song finishLevelSong;
        Song theEndSong;
        EventHandler<EventArgs> lastMediaStateChanged;
        SoundEffectInstance bonusSoundEffectInstance;
        SoundEffectInstance weaponSoundEffectInstance;
        SoundEffectInstance powerUpSoundEffectInstance;
        SoundState lastPowerUpSoundState = SoundState.Stopped;
        SoundEffectInstance comboSoundEffectInstance;
        SoundEffectInstance pauseSoundEffectInstance;
        SoundEffectInstance unpauseSoundEffectInstance;
        Map gameMap;
        Player player;
        Princess princess;
        Boss boss;
        Bullet bossBullet;
        Weapon currentWeapon;
        PowerUp currentPowerUp;
        List<Bullet> playerBullets = new List<Bullet>();
        List<PhysicalObject> bonuses = new List<PhysicalObject>();
        List<Weapon> weapons = new List<Weapon>();
        List<PowerUp> powerUps = new List<PowerUp>();
        List<PhysicalObject> onScreenBonuses = new List<PhysicalObject>();
        List<Weapon> onScreenWeapons = new List<Weapon>();
        List<PowerUp> onScreenPowerUps = new List<PowerUp>();
        List<Enemy> enemies = new List<Enemy>();
        Dictionary<int, int> enemyGroupCount = new Dictionary<int, int>();
        List<Enemy> onScreenEnemies = new List<Enemy>();
        public Vector2 topLeftCorner;
        Texture2D titleTexture;
        Texture2D loveTexture;
        Action<EnemyDeathMessage> EnemyDeathMessageAction;
        Action<PlayerDeathMessage> PlayerDeathMessageAction;
        Action<BossDeathMessage> BossDeathMessageAction;
        Action<BonusStateChangedMessage> BonusStateChangedMessageAction;
        Action<WeaponStateChangedMessage> WeaponStateChangedMessageAction;
        Action<ComboMessage> ComboMessageAction;
        Action<EnemyShotMessage> EnemyShotMessageAction;
        bool levelFinished = false;

        public View(GraphicsDeviceManager graphics, SpriteBatch spriteBatch, ContentManager content, ScreenPad screenPad, BossMovement bossMovement, int levelNumber, string levelName)
        {
            this.content = content;
            this.graphics = graphics;
            this.spriteBatch = spriteBatch;
            this.content = content;
            this.screenPad = screenPad;
            this.bossMovement = bossMovement;
            this.levelNumber = levelNumber;
            this.levelName = levelName;
            this.topLeftCorner = new Vector2((screenSize.X - gameScreenSize.X) / 2, (screenSize.Y - gameScreenSize.Y) / 2);

            DefineActions();

            LoadContent();
        }

        public void RegisterActions()
        {
            Messenger.Default.Register<EnemyDeathMessage>(this, EnemyDeathMessageAction);
            Messenger.Default.Register<PlayerDeathMessage>(this, PlayerDeathMessageAction);
            NewMessenger.Default.Register<BossDeathMessage>(this, BossDeathMessageAction);
            Messenger.Default.Register<BonusStateChangedMessage>(this, BonusStateChangedMessageAction);
            Messenger.Default.Register<WeaponStateChangedMessage>(this, WeaponStateChangedMessageAction);
            Messenger.Default.Register<ComboMessage>(this, ComboMessageAction);
            Messenger.Default.Register<EnemyShotMessage>(this, EnemyShotMessageAction);
        }

        public void UnregisterActions()
        {
            Messenger.Default.Unregister<EnemyDeathMessage>(this, EnemyDeathMessageAction);
            Messenger.Default.Unregister<PlayerDeathMessage>(this, PlayerDeathMessageAction);
            NewMessenger.Default.Unregister<BossDeathMessage>(this, BossDeathMessageAction);
            Messenger.Default.Unregister<BonusStateChangedMessage>(this, BonusStateChangedMessageAction);
            Messenger.Default.Unregister<WeaponStateChangedMessage>(this, WeaponStateChangedMessageAction);
            Messenger.Default.Unregister<ComboMessage>(this, ComboMessageAction);
            Messenger.Default.Unregister<EnemyShotMessage>(this, EnemyShotMessageAction);
        }

        private void DefineActions()
        {
            EnemyDeathMessageAction = (message) =>
            {
                var wasCombo = false;
                var enemyGroupId = message.Enemy.GroupId;
                if (enemyGroupCount.ContainsKey(enemyGroupId))
                {
                    enemyGroupCount[enemyGroupId]--;

                    if (enemyGroupCount[enemyGroupId] == 0)
                    {
                        message.Enemy.State = PlayerState.Combo;
                        comboSoundEffectInstance.Play();
                        AddToScore((int)Points.Combo);

                        wasCombo = true;
                        Messenger.Default.Send(new ComboMessage { Enemy = message.Enemy });
                    }
                }

                if (!wasCombo)
                {
                    onScreenEnemies.Remove(message.Enemy);
                    if (!message.Enemy.IsBullet)
                        enemies.Add(message.Enemy);
                }
            };

            PlayerDeathMessageAction = (message) =>
            {
                lastPowerUpSoundState = SoundState.Stopped;
                bonusSoundEffectInstance.Stop();
                weaponSoundEffectInstance.Stop();
                powerUpSoundEffectInstance.Stop();
                comboSoundEffectInstance.Stop();
                pauseSoundEffectInstance.Stop();
                unpauseSoundEffectInstance.Stop();
                MediaPlayer.Stop();
                currentWeapon.State = WeaponState.Arrow;

                PlaySong(dyingSong, (s, e) =>
                {
                    if (currentSong == dyingSong)
                    {
                        if (MediaPlayer.State == MediaState.Stopped)
                        {
                            if (player.Lives <= 0)
                            {
                                viewState = ViewState.GameOver;
                            }
                            else
                            {
                                viewState = ViewState.ShowLevel;
                                RespawnPlayerAndEnemies();
                            }
                        }
                    }
                });
            };

            BossDeathMessageAction = (message) =>
            {
                bossBullet.Position = new Vector2(-10, -10);
                if (this.levelNumber == 8)
                {
                    PlaySong(finishLevelSong, (s, e) =>
                    {
                        if (currentSong == finishLevelSong
                            && MediaPlayer.State == MediaState.Stopped)
                        {
                            viewState = ViewState.TheEnd;
                            PlaySong(theEndSong, (s2, e2) =>
                            {
                                if (currentSong == theEndSong
                                    && MediaPlayer.State == MediaState.Stopped)
                                {
                                    StopSong(() =>
                                        {
                                            viewState = ViewState.Menu;
                                            Messenger.Default.Send(new PassedLevelMessage { LevelPassed = this.levelNumber });
                                        });
                                }
                            });
                        }
                    });
                }
                else
                {
                    PlaySong(finishLevelSong, (s, e) =>
                    {
                        if (currentSong == finishLevelSong
                            && MediaPlayer.State == MediaState.Stopped)
                        {
                            viewState = ViewState.PassedLevel;
                        }
                    });

                    levelFinished = true;
                    Messenger.Default.Send(new PassedLevelMessage { LevelPassed = this.levelNumber });
                }
            };

            BonusStateChangedMessageAction = (message) =>
            {
                //onScreenBonuses.Remove(message.Bonus);
            };

            WeaponStateChangedMessageAction = (message) =>
            {
                //weaponState = message.Weapon.State;
            };

            ComboMessageAction = (message) =>
            {
            };

            EnemyShotMessageAction = (message) =>
            {
            };
        }

        private void RespawnPlayerAndEnemies()
        {
            //var segmentRowCount = (gameMap.MapLines.Count() * 2) / gameMap.SegmentCount;
            //var spawnRow = (int)(Math.Ceiling(((float)gameMap.ScrollRows / segmentRowCount))) * segmentRowCount - (int)windowTilesSize.Y;
            //spawnRow = Math.Max(0, spawnRow);
            //gameMap.ScrollRows = spawnRow;
            gameMap.RestoreScrollStartRow();
            onScreenEnemies.Where(enemy => !enemy.IsBullet).ToList().ForEach(enemy =>
            {
                enemy.RestorePosition();
                enemies.Add(enemy);
            });
            enemies.ForEach(enemy =>
            {
                enemy.State = PlayerState.Alive;
                enemy.RestorePosition();
            });
            onScreenEnemies.Clear();
            bossBullet.Position = new Vector2(-10, -10);
            player.Respawn();
        }

        private void PlaySong(Song song, EventHandler<EventArgs> mediaStateChanged, bool loop = false)
        {
            MediaPlayer.Stop();
            if (lastMediaStateChanged != null)
                MediaPlayer.MediaStateChanged -= lastMediaStateChanged;

            if (mediaStateChanged != null)
                MediaPlayer.MediaStateChanged += mediaStateChanged;

            currentSong = null;
            lastMediaStateChanged = mediaStateChanged;
            MediaPlayer.IsRepeating = loop;
            currentSong = song;
            MediaPlayer.Play(song);
        }

        private void StopSong(Action afterSongStopped)
        {
            currentSong = null;
            if (lastMediaStateChanged != null)
                MediaPlayer.MediaStateChanged -= lastMediaStateChanged;

            lastMediaStateChanged = null;
            afterSongStopped();
            MediaPlayer.Stop();
        }

        void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            introSong = ContentHelper.Instance.GetContent<Song>("Intro");
            level1Song = ContentHelper.Instance.GetContent<Song>(string.Format("Level1", levelNumber));
            level2Song = ContentHelper.Instance.GetContent<Song>(string.Format("Level2", levelNumber));
            bossSong = ContentHelper.Instance.GetContent<Song>("BossTheme");
            dyingSong = ContentHelper.Instance.GetContent<Song>("Death");
            gameOverSong = ContentHelper.Instance.GetContent<Song>("GameOver");
            finishLevelSong = ContentHelper.Instance.GetContent<Song>("FinishLevel");
            theEndSong = ContentHelper.Instance.GetContent<Song>("TheEnd");
            font = ContentHelper.Instance.GetContent<SpriteFont>("Super-Contra-NES");
            titleTexture = ContentHelper.Instance.GetContent<Texture2D>("Title");
            loveTexture = ContentHelper.Instance.GetContent<Texture2D>("LoveSpriteSheet");
            bonusSoundEffectInstance = ContentHelper.Instance.GetSoundEffectInstance("Bonus");
            weaponSoundEffectInstance = ContentHelper.Instance.GetSoundEffectInstance("Weapon");
            powerUpSoundEffectInstance = ContentHelper.Instance.GetSoundEffectInstance("PowerUp");
            comboSoundEffectInstance = ContentHelper.Instance.GetSoundEffectInstance("Combo");
            pauseSoundEffectInstance = ContentHelper.Instance.GetSoundEffectInstance("pause");
            unpauseSoundEffectInstance = ContentHelper.Instance.GetSoundEffectInstance("unpause");

            gameMap = new Map(content, spriteBatch, screenSize, screenPad, levelNumber);
            gameMap.Scrolled += gameMap_Scrolled;
            player = new Player(content, spriteBatch, screenSize, screenPad);
            princess = new Princess(content, spriteBatch, screenSize, screenPad);
            boss = new Boss(content, spriteBatch, screenSize, screenPad, windowTilesSize, levelNumber, bossMovement);
            bossBullet = new Bullet(content, spriteBatch, screenSize, screenPad, 1);
            bossBullet.Position = boss.Position + (boss.Size) / 2 - new Vector2(0, gameMap.ScrollRows);
            bossBullet.OffScreen += (sb, eb) =>
            {
                if (boss.State == PlayerState.Alive)
                {
                    bossBullet.Position = boss.Position + (boss.Size) / 2 - new Vector2(0, gameMap.ScrollRows);
                    var newDirection = new Vector2(player.Position.X - bossBullet.Position.X, player.Position.Y - bossBullet.Position.Y) + player.Size / 2;
                    newDirection.Normalize();
                    bossBullet.Direction = newDirection;
                    if (!bossBullet.IsOffScreen())
                        bossBullet.Shoot();
                }
            };

            currentWeapon = new Weapon(content, spriteBatch, screenSize, screenPad, new Vector2(-15, -15), player);

            InitializeLevel();
        }

        public void UnloadContent()
        {
            gameMap.UnloadContent();
        }

        private void InitializeLevel()
        {
            showLove = false;
            MediaPlayer.Volume = 1f;
            tickCount = 0;
            player.Initialize();
            score = 0;
            viewState = (levelNumber == 1 ? ViewState.Menu : ViewState.Intro);
            gameMap.Initialize();
            playerBullets.Clear();
            onScreenBonuses.Clear();
            onScreenWeapons.Clear();
            onScreenPowerUps.Clear();
            onScreenEnemies.Clear();
            InitializeEnemies();
        }

        private void InitializeEnemies()
        {
            enemies.Clear();
            var mapLines = gameMap.MapLines;
            var y = 0;
            foreach (var line in mapLines)
            {
                var x = 0;
                foreach (var c in line)
                {
                    var pos = new Vector2(x * 2, y * 2);

                    if ("ABCDE".Contains(c))
                    {
                        var bonus = new Bonus(content, spriteBatch, screenSize, screenPad, pos, c);
                        bonuses.Add(bonus);
                    }
                    else if (c == 'W')
                    {
                        var weapon = new Weapon(content, spriteBatch, screenSize, screenPad, pos, player);
                        weapons.Add(weapon);
                    }
                    else if (c == 'P')
                    {
                        //var powerUp = new PowerUp(content, spriteBatch, screenSize, screenPad, pos, player);
                        //powerUps.Add(powerUp);
                    }
                    else if (c == 'a')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy));
                        enemies.Add(new Enemy(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'b')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy2));
                        enemies.Add(new Enemy2(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'c')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy3));
                        enemies.Add(new Enemy3(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'd')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy4));
                        enemies.Add(new Enemy4(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'e')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy5));
                        enemies.Add(new Enemy5(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'f')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy6));
                        enemies.Add(new Enemy6(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'g')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy7));
                        enemies.Add(new Enemy7(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'h')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy8));
                        enemies.Add(new Enemy8(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'i')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy9));
                        enemies.Add(new Enemy9(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'j')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy10));
                        enemies.Add(new Enemy10(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'k')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy11));
                        enemies.Add(new Enemy11(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'l')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy12));
                        enemies.Add(new Enemy12(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    else if (c == 'm')
                    {
                        var enemyGroupId = GetEnemyGroupId(y, x, typeof(Enemy13));
                        enemies.Add(new Enemy13(content, spriteBatch, screenSize, screenPad, pos, enemyGroupId));
                    }
                    x++;
                }
                y++;
            }

            var groupsToRemoveIdList = new List<int>();
            foreach (var enemyGroup in enemyGroupCount)
            {
                if (enemyGroup.Value < 3)
                {
                    groupsToRemoveIdList.Add(enemyGroup.Key);
                }
            }

            foreach (var id in groupsToRemoveIdList)
            {
                enemyGroupCount.Remove(id);
                foreach (var enemy in enemies.Where(e => e.GroupId == id))
                {
                    enemy.GroupId = 0;
                }
            }
        }

        private int GetEnemyGroupId(int y, int x, Type enemyType)
        {
            var enemyGroupId = 0;
            enemies.Where(e =>
                (
                    (int)e.Position.Y == (y - 1) * 2
                    && (
                        ((int)e.Position.X >= (x - 1) * 2)
                        && ((int)e.Position.X <= (x + 1) * 2)
                        )
                ) ||
                (
                    (int)e.Position.Y == y * 2
                    && ((int)e.Position.X == (x - 1) * 2)
                )
                ).ToList().ForEach(e =>
                {
                    if (enemyGroupId == 0 && e.GetType() == enemyType)
                    {
                        enemyGroupId = e.GroupId;
                    }
                });

            if (enemyGroupId == 0)
                enemyGroupId = enemyGroupCount.LastOrDefault().Key + 1;

            if (enemyGroupCount.ContainsKey(enemyGroupId))
            {
                enemyGroupCount[enemyGroupId]++;
            }
            else
            {
                enemyGroupCount.Add(enemyGroupId, 1);
            }

            return enemyGroupId;
        }

        void gameMap_Scrolled(object sender, EventArgs e)
        {
            var collisionResult = gameMap.TestCollision(player, player.Position, gameMap.ScrollRows);
            if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                player.Position = player.Position + new Vector2(0, .0625f);

            collisionResult = gameMap.TestCollision(player, player.Position, gameMap.ScrollRows);
            if (player.State == PlayerState.Alive && collisionResult.CollisionType != CollisionType.None)
            {
                player.ProcessDeath(tickCount);
            }

            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];

                if (enemy.Position.Y == gameMap.ScrollRows)
                {
                    if (enemies.Contains(enemy))
                    {
                        enemies.Remove(enemy);
                        onScreenEnemies.Add(enemy);
                    }
                }
            }

            for (var i = onScreenEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = onScreenEnemies[i];
                if (!enemy.IsBullet && enemy.OnWindowPosition.HasValue && !enemy.IsFlying && !enemy.IsPassingBy)
                {
                    if (enemy.OnWindowPosition.Value.Y > 0)
                    {
                        var enemyPosition = (enemy.OnWindowPosition.Value / tileWidth);
                        var mapCollision = gameMap.TestCollision(enemy, enemyPosition, gameMap.ScrollRows);
                        if (mapCollision.CollisionType == CollisionType.Blocked)
                        {
                            enemy.ProcessDeath(tickCount);
                        }
                    }
                }
            }
            
            for (var i = bonuses.Count - 1; i >= 0; i--)
            {
                var bonus = bonuses[i];
                if (bonus.Position.Y + bonus.Size.Y >= gameMap.ScrollRows)
                {
                    bonuses.Remove(bonus);
                    onScreenBonuses.Add(bonus);
                }
            }

            for (var i = weapons.Count - 1; i >= 0; i--)
            {
                var weapon = weapons[i];
                if (weapon.Position.Y + weapon.Size.Y >= gameMap.ScrollRows - windowTilesSize.Y)
                {
                    weapons.Remove(weapon);
                    onScreenWeapons.Add(weapon);
                }
            }

            for (var i = powerUps.Count - 1; i >= 0; i--)
            {
                var powerUp = powerUps[i];
                if (powerUp.Position.Y + powerUp.Size.Y >= gameMap.ScrollRows - windowTilesSize.Y)
                {
                    powerUps.Remove(powerUp);
                    onScreenPowerUps.Add(powerUp);
                }
            }

            if (gameMap.ScrollRows == windowTilesSize.Y && lastScrollRows != gameMap.ScrollRows && viewState == ViewState.Playing)
            {
                MediaPlayer.Volume = 1f;
                PlaySong(bossSong, null, true);
            }

            lastScrollRows = gameMap.ScrollRows;
        }

        void PlayBossSong(object sender, EventArgs e)
        {
            if (boss.State == PlayerState.Alive && viewState == ViewState.Playing && MediaPlayer.State == MediaState.Stopped)
                PlaySong(bossSong, null, true);
        }

        public void Update(GameTime gameTime)
        {
            if (levelFinished)
                return;

            switch (viewState)
            {
                case ViewState.Menu:
                    UpdateViewStateMenu(gameTime);
                    break;
                case ViewState.Intro:
                    UpdateViewStateIntro(gameTime);
                    break;
                case ViewState.ShowLevel:
                    UpdateViewStateShowLevel(gameTime);
                    break;
                case ViewState.Playing:
                    UpdateViewStatePlaying(gameTime);
                    break;
                case ViewState.GameOver:
                    UpdateViewStateGameOver(gameTime);
                    break;
                case ViewState.TheEnd:
                    UpdateViewStateTheEnd(gameTime);
                    break;
            }
        }

        void DrawStringCentralized(params string[] lines)
        {
            var offsetY = 0f;
            foreach (var text in lines)
            {
                var stringSize = font.MeasureString(string.IsNullOrEmpty(text) ? "A" : text);
                var position = topLeftCorner + new Vector2((windowTilesSize.X - text.Length) / 2, (windowTilesSize.Y - lines.Length) / 2) * tileWidth + new Vector2(0, offsetY);

                for (var y = -2; y <= 2; y++)
                {
                    for (var x = -2; x <= 2; x++)
                    {
                        spriteBatch.DrawString(font, string.Format(text), position + new Vector2(x, y), Color.Black);
                    }
                }
                spriteBatch.DrawString(font, string.Format(text), position, Color.White);
                offsetY += 64;
            }
        }

        void UpdateViewStateMenu(GameTime gameTime)
        {
            if (screenPad.GetState().Buttons.X == ButtonState.Pressed)
            {
                viewState = ViewState.Intro;
            }
        }

        void UpdateViewStateIntro(GameTime gameTime)
        {
            if (MediaPlayer.State == MediaState.Stopped && currentSong != introSong)
            {
                PlaySong(introSong, (s, e) =>
                {
                    if (currentSong == introSong)
                    {
                        if (MediaPlayer.State == MediaState.Stopped)
                            viewState = ViewState.Playing;
                    }
                });
            }
        }

        void UpdateViewStateShowLevel(GameTime gameTime)
        {
            if (MediaPlayer.State == MediaState.Stopped)
            {
                MediaPlayer.Volume = 0;
                PlaySong(dyingSong, (s, e) =>
                {
                    if (currentSong == dyingSong && viewState == ViewState.ShowLevel)
                    {
                        if (MediaPlayer.State == MediaState.Stopped)
                        {
                            MediaPlayer.Volume = 1f;
                            viewState = ViewState.Playing;
                        }
                    }
                });
            }

            if (viewState == ViewState.ShowLevel && currentSong == GetLevelSong() && MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Stop();
            }
        }

        private void UpdateViewStateGameOver(GameTime gameTime)
        {
            if (MediaPlayer.State == MediaState.Stopped && currentSong != gameOverSong)
            {
                PlaySong(gameOverSong, (s, e) =>
                {
                    if (currentSong == gameOverSong && MediaPlayer.State == MediaState.Stopped)
                    {
                        InitializeLevel();
                    }
                });
            }
        }

        private void UpdateViewStatePlaying(GameTime gameTime)
        {
            var previousRemainingLives = player.Lives;

            if (MediaPlayer.State == MediaState.Stopped)
            {
                PlaySong(gameMap.ScrollRows < windowTilesSize.Y ? bossSong : GetLevelSong(),
                        (s, e) =>
                        {
                            if (currentSong == bossSong && previousRemainingLives == player.Lives && viewState == ViewState.Playing && boss.State == PlayerState.Alive)
                            {
                                if (MediaPlayer.State == MediaState.Stopped && viewState == ViewState.Playing)
                                    PlaySong(bossSong, null, true);
                            }
                        }                    
                    );
            }

            gameMap.Update(gameTime, tickCount, gameMap.ScrollRows);

            bool isPlayerBlocked = CheckCollisions(gameTime);

            player.Update(gameTime, tickCount, gameMap.ScrollRows);

            for (var i = 0; i < playerBullets.Count; i++)
            {
                var playerBullet = playerBullets[i];
                playerBullet.Update(gameTime, tickCount, gameMap.ScrollRows);
            }

            if (gameMap.State == MapState.Scrolling)
            {
                for (var i = 0; i < onScreenBonuses.Count; i++)
                {
                    var bonus = onScreenBonuses[i];
                    if (bonus.Position.Y > gameMap.ScrollRows + windowTilesSize.Y)
                    {
                        onScreenBonuses.Remove(bonus);
                    }
                    else
                    {
                        bonus.Update(gameTime, tickCount, gameMap.ScrollRows);
                    }
                } 
                
                for (var i = 0; i < onScreenWeapons.Count; i++)
                {
                    var weapon = onScreenWeapons[i];
                    weapon.Update(gameTime, tickCount, gameMap.ScrollRows);
                }

                for (var i = 0; i < onScreenPowerUps.Count; i++)
                {
                    var powerUp = onScreenPowerUps[i];
                    powerUp.Update(gameTime, tickCount, gameMap.ScrollRows);
                }

                boss.Update(gameTime, tickCount, gameMap.ScrollRows);
                if (boss.State == PlayerState.Alive)
                    bossBullet.Update(gameTime, tickCount, gameMap.ScrollRows);
            }

            for (var i = 0; i < onScreenEnemies.Count; i++)
            {
                var enemy = onScreenEnemies[i];

                if (enemy.Position.X < -windowTilesSize.X
                    || enemy.Position.X > windowTilesSize.X * 2
                    || enemy.Position.Y > gameMap.ScrollRows + windowTilesSize.Y)
                {
                    onScreenEnemies.Remove(enemy);
                    if (!enemy.IsBullet)
                        enemies.Add(enemy);
                }
                else
                {
                    enemy.UpdateDirection(player, gameMap);
                    enemy.Update(gameTime, tickCount, gameMap.ScrollRows, onScreenEnemies, gameMap);

                    if (enemy.OnWindowPosition.HasValue && enemy.OnWindowPosition.Value.Y > 0)
                    {
                        if (enemy.GroupId == 0 && !enemy.IsBullet && enemy.Reloaded)
                        {
                            enemy.Reloaded = false;
                            var enemyBullet = enemy.GetBullet(player, gameMap);
                            onScreenEnemies.Add(enemyBullet);
                        }
                    }

                    foreach (var other in onScreenEnemies)
                    {
                        if (other.TestCollision(enemy, enemy.Position, gameMap.ScrollRows).CollisionType == CollisionType.Blocked)
                        {
                            //other.OnWindowPosition = other.OnWindowPosition + new Vector2(other.Size.X, 0);
                        }
                    }
                }
            }


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
            else if (screenPad.GetState().Buttons.A == ButtonState.Pressed)
            {
                buttonAPressed = true;
            }
            else if (screenPad.GetState().Buttons.A == ButtonState.Released && buttonAPressed)
            {
                buttonAPressed = false;
                if (gameMap.State == MapState.Paused)
                {
                    MediaPlayer.Volume = 1;
                    gameMap.State = MapState.Scrolling;
                    unpauseSoundEffectInstance.Play();
                }
                else
                {
                    MediaPlayer.Volume = 0;
                    pauseSoundEffectInstance.Play();
                    gameMap.State = MapState.Paused;
                }
            }
            else if (screenPad.GetState().Buttons.X == ButtonState.Pressed)
            {
                if (gameMap.State != MapState.Paused && player.CanShoot && player.State == PlayerState.Alive)
                {
                    List<PlayerBullet> weaponBullets = currentWeapon.Shoot();

                    foreach (var playerBullet in weaponBullets)
                    {
                        playerBullet.Position =
                            new Vector2(0, (gameMap.ScrollRows))
                            + player.Position - new Vector2(0, 1)
                            - new Vector2(0, gameMap.ScrollRows);

                        playerBullet.OffScreen += (spb, epb) =>
                        {
                            playerBullets.Remove(playerBullet);
                        };
                        playerBullets.Add(playerBullet);
                        playerBullet.Shoot();
                    }
                }
            }

            if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS))
            {
                accumElapsedGameTime = TimeSpan.FromSeconds(0);
                tickCount++;
            }
            accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);

            if (player.State == PlayerState.Alive)
            {
                if (!isPlayerBlocked && gameMap.State != MapState.Paused)
                    player.ProcessGamePad(screenPad, gameMap, gameMap.ScrollRows);
            }

            currentWeapon.Update(gameTime, tickCount, gameMap.ScrollRows);

            CheckSound(gameTime);
        }

        private void UpdateViewStateTheEnd(GameTime gameTime)
        {
            bool isPlayerBlocked = CheckCollisions(gameTime);

            player.Update(gameTime, tickCount, gameMap.ScrollRows);
            princess.Update(gameTime, tickCount, gameMap.ScrollRows);

            if (screenPad.GetState().Buttons.B == ButtonState.Pressed)
            {
                //MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                //marketplaceReviewTask.Show();
            }
            else if (screenPad.GetState().Buttons.A == ButtonState.Pressed)
            {
                buttonAPressed = true;
            }
            else if (screenPad.GetState().Buttons.A == ButtonState.Released && buttonAPressed)
            {
                buttonAPressed = false;
                if (gameMap.State == MapState.Paused)
                {
                    MediaPlayer.Volume = 1;
                    gameMap.State = MapState.Scrolling;
                    unpauseSoundEffectInstance.Play();
                }
                else
                {
                    MediaPlayer.Volume = 0;
                    pauseSoundEffectInstance.Play();
                    gameMap.State = MapState.Paused;
                }
            }

            if (accumElapsedGameTime >= TimeSpan.FromMilliseconds(tickInMS))
            {
                accumElapsedGameTime = TimeSpan.FromSeconds(0);
                tickCount++;
            }
            accumElapsedGameTime = accumElapsedGameTime.Add(gameTime.ElapsedGameTime);

            var awaitingPrincessPosition = new Vector2(windowTilesSize.X / 2 - player.Size.X / 2, windowTilesSize.Y / 2 + player.Size.Y / 2);
            var deltaPosition = awaitingPrincessPosition - player.Position;
            
            if (Math.Abs(deltaPosition.Y) > .1)
            {
                player.Position += new Vector2(0, deltaPosition.Y > 0 ? 1 : -1) / 8;
            }
            else if (Math.Abs(deltaPosition.X) > .1)
            {
                player.Position += new Vector2(deltaPosition.X > 0 ? 1 : -1, 0) / 8;
            }
            else
            {
                showLove = true;
            }

            CheckSound(gameTime);
        }

        private Song GetLevelSong()
        {
            Song levelSong;
            levelSong = (levelNumber % 2 == 1 ? level1Song : level2Song);
            return levelSong;
        }

        private void CheckSound(GameTime gameTime)
        {
            if (accumSoundCheckTime >= TimeSpan.FromMilliseconds(soundCheckMS))
            {
                accumSoundCheckTime = TimeSpan.FromSeconds(0);

                if (lastPowerUpSoundState == SoundState.Playing && powerUpSoundEffectInstance.State == SoundState.Stopped && viewState == ViewState.Playing)
                {
                    PlaySong(GetLevelSong(), null, true);
                    MediaPlayer.Volume = 1;
                }

                lastPowerUpSoundState = powerUpSoundEffectInstance.State;
            }
            accumSoundCheckTime = accumSoundCheckTime.Add(gameTime.ElapsedGameTime);
        }

        private bool CheckCollisions(GameTime gameTime)
        {
            bool isPlayerBlocked = false;
            if (accumCollisionTime >= TimeSpan.FromMilliseconds(collisionMS))
            {
                accumCollisionTime = TimeSpan.FromSeconds(0);

                var collisionResult = boss.TestCollision(player, player.Position + new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                {
                    player.ProcessDeath(tickCount);
                }

                collisionResult = bossBullet.TestCollision(player, player.Position, gameMap.ScrollRows);
                if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                {
                    player.ProcessDeath(tickCount);
                }

                for (var i = 0; i < onScreenEnemies.Count; i++)
                {
                    var enemy = (Enemy)onScreenEnemies[i];
                    if (enemy.State == PlayerState.Alive)
                    {
                        collisionResult = enemy.TestCollision(player, player.Position + new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                        if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                        {
                            player.ProcessDeath(tickCount);
                        }
                    }
                }

                for (var i = 0; i < onScreenBonuses.Count; i++)
                {
                    var bonus = (Bonus)onScreenBonuses[i];
                    collisionResult = bonus.TestCollision(player, player.Position + new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                    if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                    {
                        switch (bonus.State)
                        {
                            case BonusState.FiveHundredPoints:
                                bonus.State = BonusState.Used;
                                AddToScore(500);
                                bonusSoundEffectInstance.Play();
                                break;
                            case BonusState.ExtraLife:
                                bonus.State = BonusState.Used;
                                player.Lives++;
                                bonusSoundEffectInstance.Play();
                                gameMap.SaveScrollStartRow();
                                player.SavePosition();
                                break;
                            case BonusState.KillAllInScreen:
                                bonus.State = BonusState.Used;
                                foreach (var e in onScreenEnemies)
                                {
                                    AddToScore((int)Points.Minimum);
                                    e.State = PlayerState.Dead;
                                    e.ProcessDeath(tickCount);
                                }
                                bonusSoundEffectInstance.Play();
                                break;
                            case BonusState.Barrier:
                                isPlayerBlocked = true;
                                break;
                            case BonusState.Freeze:
                                bonus.State = BonusState.Used;
                                bonusSoundEffectInstance.Play();
                                gameMap.State = MapState.Timer;
                                break;
                        }
                    }
                }

                for (var i = 0; i < onScreenWeapons.Count; i++)
                {
                    var weapon = (Weapon)onScreenWeapons[i];
                    collisionResult = weapon.TestCollision(player, player.Position + new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                    if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                    {
                        if (weapon.State != WeaponState.OneThousandPoints)
                            currentWeapon = weapon;

                        onScreenWeapons.Remove(weapon);

                        weaponSoundEffectInstance.Play();
                    }
                }

                for (var i = 0; i < onScreenPowerUps.Count; i++)
                {
                    var powerUp = (PowerUp)onScreenPowerUps[i];
                    collisionResult = powerUp.TestCollision(player, player.Position + new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                    if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                    {
                        onScreenPowerUps.Remove(powerUp);
                        MediaPlayer.Volume = 0;
                        accumSoundCheckTime = TimeSpan.FromSeconds(0);
                        powerUpSoundEffectInstance.Play();
                    }
                }

                for (var i = 0; i < playerBullets.Count; i++)
                {
                    var playerBullet = playerBullets[i];
                    collisionResult = playerBullet.TestCollision(boss, boss.Position - new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                    if (boss.State == PlayerState.Alive && player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                    {
                        AddToScore((int)Points.BossHit);
                        playerBullets.Remove(playerBullet);
                        boss.Hit();
                    }

                    for (var j = 0; j < onScreenEnemies.Count; j++)
                    {
                        var enemy = (Enemy)onScreenEnemies[j];
                        if (enemy.State == PlayerState.Alive)
                        {
                            collisionResult = playerBullet.TestCollision(enemy, enemy.Position - new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                            if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                            {
                                enemy.Hit(playerBullet, tickCount);
                                if (enemy.State == PlayerState.Dead)
                                {
                                    AddToScore((int)Points.Minimum);
                                }
                                if (playerBullet.Damage <= 0)
                                {
                                    playerBullets.Remove(playerBullet);
                                }
                            }
                        }
                    }

                    for (var j = 0; j < onScreenBonuses.Count; j++)
                    {
                        var bonus = (Bonus)onScreenBonuses[j];
                        if (bonus.State == BonusState.Unknown || bonus.State == BonusState.Barrier)
                        {
                            collisionResult = playerBullet.TestCollision(bonus, bonus.Position - new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                            if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                            {
                                bonus.Hit(playerBullet);
                                if (bonus.State != BonusState.Unknown)
                                {
                                    AddToScore((int)Points.Minimum);
                                    playerBullets.Remove(playerBullet);
                                }
                                if (playerBullet.Damage <= 0)
                                {
                                    playerBullets.Remove(playerBullet);
                                }
                            }
                        }
                    }

                    for (var j = 0; j < onScreenWeapons.Count; j++)
                    {
                        var weapon = (Weapon)onScreenWeapons[j];
                        collisionResult = playerBullet.TestCollision(weapon, weapon.Position - new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                        if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                        {
                            weapon.Hit(playerBullet.Position);
                            playerBullets.Remove(playerBullet);
                        }
                    }

                    for (var j = 0; j < onScreenPowerUps.Count; j++)
                    {
                        var powerUp = (PowerUp)onScreenPowerUps[j];
                        collisionResult = playerBullet.TestCollision(powerUp, powerUp.Position - new Vector2(0, gameMap.ScrollRows), gameMap.ScrollRows);
                        if (player.State == PlayerState.Alive && collisionResult.CollisionType == CollisionType.Blocked)
                        {
                            powerUp.Hit(playerBullet.Position);
                            playerBullets.Remove(playerBullet);
                        }
                    }
                }
            }
            accumCollisionTime = accumCollisionTime.Add(gameTime.ElapsedGameTime);
            return isPlayerBlocked;
        }

        private void AddToScore(int points)
        {
            score += points;
            hiScore = Math.Max(score, hiScore);
        }

        public void Draw(GameTime gameTime)
        {
            if (levelFinished)
                return;

            switch (viewState)
            {
                case ViewState.Menu:
                    DrawViewStateMenu(gameTime);
                    break;
                case ViewState.Intro:
                    if (currentSong == finishLevelSong)
                        DrawViewStateFinishLevel(gameTime, levelName);
                    else
                        DrawViewStateIntro(gameTime, levelName);
                    break;
                case ViewState.ShowLevel:
                    DrawViewStateLevel(gameTime, levelName);
                    break;
                case ViewState.Playing:
                    DrawViewStatePlaying(gameTime);
                    break;
                case ViewState.GameOver:
                    DrawViewStateGameOver(gameTime);
                    break;
                case ViewState.TheEnd:
                    DrawViewStateTheEnd(gameTime);
                    break;
            }
        }

        void DrawViewStateMenu(GameTime gameTime)
        {
            spriteBatch.Draw(titleTexture, topLeftCorner, Color.White);
            DrawStringCentralized("", "PUSH BUTTON TO START");
        }

        void DrawViewStateIntro(GameTime gameTime, string levelName)
        {
            DrawStringCentralized(string.Format("LEVEL {0}", levelNumber), levelName);
        }

        void DrawViewStateFinishLevel(GameTime gameTime, string levelName)
        {
            DrawStringCentralized(string.Format("CONGRATULATIONS!", "YOU HAVE DEFEATED {0}!", levelName));
        }

        void DrawViewStateLevel(GameTime gameTime, string levelName)
        {
            DrawStringCentralized(string.Format("LEVEL {0}", levelNumber), levelName);
        }

        private void DrawViewStateGameOver(GameTime gameTime)
        {
            DrawStringCentralized("GAME OVER");
        }

        private void DrawViewStatePlaying(GameTime gameTime)
        {
            gameMap.Draw(gameTime, tickCount, gameMap.ScrollRows);

            for (var i = 0; i < onScreenBonuses.Count; i++)
            {
                var bonus = onScreenBonuses[i];
                bonus.Draw(gameTime, tickCount, gameMap.ScrollRows);
            }

            for (var i = 0; i < onScreenWeapons.Count; i++)
            {
                var weapon = onScreenWeapons[i];
                weapon.Draw(gameTime, tickCount, gameMap.ScrollRows);
            }

            for (var i = 0; i < onScreenPowerUps.Count; i++)
            {
                var powerUp = onScreenPowerUps[i];
                powerUp.Draw(gameTime, tickCount, gameMap.ScrollRows);
            }

            player.Draw(gameTime, tickCount, gameMap.ScrollRows);
            boss.Draw(gameTime, tickCount, gameMap.ScrollRows);
            if (boss.State == PlayerState.Alive)
                bossBullet.Draw(gameTime, tickCount, gameMap.ScrollRows);
            for (var i = 0; i < playerBullets.Count; i++)
            {
                var playerBullet = playerBullets[i];
                playerBullet.Draw(gameTime, tickCount, gameMap.ScrollRows);
            }

            for (var i = 0; i < onScreenEnemies.Count; i++)
            {
                var enemy = onScreenEnemies[i];
                if (enemy.Position.Y + enemy.Size.Y >= gameMap.ScrollRows)
                {
                    enemy.Draw(gameTime, tickCount, gameMap.ScrollRows);
                }
            }

            var textHeight = font.MeasureString("SCORE").Y;

            spriteBatch.DrawString(font, string.Format("   SCORE  HISCORE  LIVES LEVEL  "), new Vector2((screenSize.X - gameScreenSize.X) / 2, screenSize.Y - textHeight * 2), Color.White);
            spriteBatch.DrawString(font, string.Format("   {0:d5}    {1:d5}    {2:d2}     {3:d2}  ", score, hiScore, (int)(player.Lives), levelNumber), new Vector2((screenSize.X - gameScreenSize.X) / 2, screenSize.Y - textHeight), Color.White);

            //spriteBatch.DrawString(font, string.Format("   BONUS  ENEMIES  WEAP"), new Vector2((screenSize.X - gameScreenSize.X) / 2, 401), Color.White);
            //spriteBatch.DrawString(font, string.Format("   {0:d5}    {1:d5}    {2:d2}", onScreenBonuses.Count(), onScreenEnemies.Where(e => e.State == PlayerState.Alive).Count(), onScreenWeapons.Count()), new Vector2((screenSize.X - gameScreenSize.X) / 2, 417), Color.White);

            //if (currentSong != null)
            //{
            //    var m1 = MediaPlayer.PlayPosition.Minutes;
            //    var s1 = MediaPlayer.PlayPosition.Seconds;
            //    var m2 = currentSong.Duration.Minutes;
            //    var s2 = currentSong.Duration.Seconds;

            //    spriteBatch.DrawString(font, string.Format("TIME  DURATION"), new Vector2((screenSize.X - gameScreenSize.X) / 2, screenSize.Y - textHeight * 2), Color.White);
            //    spriteBatch.DrawString(font, string.Format("{0:d2} {1:d2} {2:d2} {3:d2}", m1, s1, m2, s2), new Vector2((screenSize.X - gameScreenSize.X) / 2, screenSize.Y - textHeight), Color.White);
            //}

            if (gameMap.State == MapState.Paused)
                DrawStringCentralized("PAUSED");
        }

        private void DrawViewStateTheEnd(GameTime gameTime)
        {
            gameMap.Draw(gameTime, tickCount, gameMap.ScrollRows);

            player.Draw(gameTime, tickCount, gameMap.ScrollRows);
            player.Draw(gameTime, tickCount, gameMap.ScrollRows);

            princess.Draw(gameTime, tickCount, gameMap.ScrollRows);

            if (showLove)
            {
                DrawLove();
            }

            var textHeight = font.MeasureString("SCORE").Y;
            spriteBatch.DrawString(font, string.Format("   SCORE  HISCORE  REST  LEVEL  "), new Vector2((screenSize.X - gameScreenSize.X) / 2, screenSize.Y - textHeight * 2), Color.White);
            spriteBatch.DrawString(font, string.Format("   {0:d5}    {1:d5}    {2:d2}     {3:d2}  ", score, hiScore, (int)(player.Lives), levelNumber), new Vector2((screenSize.X - gameScreenSize.X) / 2, screenSize.Y - textHeight), Color.White);

            if (gameMap.State == MapState.Paused)
                DrawStringCentralized("PAUSED");
        }

        private void DrawLove()
        {
            Rectangle loveRectangle = new Rectangle(
                loveTexture.Height * ((tickCount / 4) % (loveTexture.Width / loveTexture.Height)),
                0,
                loveTexture.Height,
                loveTexture.Height);
            var frameIndex = (tickCount / 2) % 2;
            this.topLeftCorner = new Vector2((screenSize.X - gameScreenSize.X) / 2, (screenSize.Y - gameScreenSize.Y) / 2);
            spriteBatch.Draw(
                loveTexture,
                topLeftCorner + (player.Position - new Vector2(0, player.Size.Y)) * tileWidth,
                loveRectangle,
                Color.White);

            DrawStringCentralized("", "", "LOVE IS FOREVER...");
        }

        private void DrawScreenPad(GameTime gameTime)
        {
            screenPad.Draw(gameTime, spriteBatch);
        }
    }

    public class PassedLevelMessage { public int LevelPassed { get; set; } }

    public class ComboMessage { public Enemy Enemy { get; set; } }

    public enum Points
    {
        Minimum = 10,
        BossHit = 15,
        Combo = 50
    }
}
