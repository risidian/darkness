using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch; 

namespace WalkingGame
{
	public class CharacterEntity
	{
		static Texture2D characterSheetTexture;

		Animation walkDown;
		Animation walkUp;
		Animation walkLeft;
		Animation walkRight;

		Animation standDown;
		Animation standUp;
		Animation standLeft;
		Animation standRight;

		Animation currentAnimation;

		public float X
		{
			get;
			set;
		}

		public float Y
		{
			get;
			set;
		}

		public CharacterEntity (GraphicsDevice graphicsDevice)
		{
			if (characterSheetTexture == null)
			{
				using (var stream = TitleContainer.OpenStream ("Content/Atriartous.png"))
				{
					characterSheetTexture = Texture2D.FromStream (graphicsDevice, stream);
				}
			}


            walkUp = new Animation();
            walkUp.AddFrame(new Rectangle(128, 518, 64, 64), TimeSpan.FromSeconds(.125));
            walkUp.AddFrame(new Rectangle(196, 518, 64, 64), TimeSpan.FromSeconds(.125));
            walkUp.AddFrame(new Rectangle(256, 518, 64, 64), TimeSpan.FromSeconds(.125));
            walkUp.AddFrame(new Rectangle(320, 518, 64, 64), TimeSpan.FromSeconds(.125));
            walkUp.AddFrame(new Rectangle(384, 518, 64, 64), TimeSpan.FromSeconds(.125));
            walkUp.AddFrame(new Rectangle(448, 518, 64, 64), TimeSpan.FromSeconds(.125));
            walkUp.AddFrame(new Rectangle(512, 518, 64, 64), TimeSpan.FromSeconds(.125));

            walkLeft = new Animation();
            walkLeft.AddFrame(new Rectangle(64, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(128, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(196, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(256, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(320, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(384, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(448, 582, 64, 64), TimeSpan.FromSeconds(.125));
            walkLeft.AddFrame(new Rectangle(512, 582, 64, 64), TimeSpan.FromSeconds(.125));

            walkDown = new Animation();
            walkDown.AddFrame(new Rectangle(64, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(128, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(192, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(256, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(320, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(384, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(448, 646, 64, 64), TimeSpan.FromSeconds(.125));
            walkDown.AddFrame(new Rectangle(512, 646, 64, 64), TimeSpan.FromSeconds(.125));

            walkRight = new Animation();
            walkRight.AddFrame(new Rectangle(64, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(128, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(196, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(256, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(320, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(384, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(448, 710, 64, 64), TimeSpan.FromSeconds(.125));
            walkRight.AddFrame(new Rectangle(512, 710, 64, 64), TimeSpan.FromSeconds(.125));

            // Standing animations only have a single frame of animation:

            standUp = new Animation ();
            standUp.AddFrame(new Rectangle(64, 518, 64, 64), TimeSpan.FromSeconds(.125));

            standLeft = new Animation ();
            standLeft.AddFrame(new Rectangle(0, 582, 64, 64), TimeSpan.FromSeconds(.125));

            standDown = new Animation();
            standDown.AddFrame(new Rectangle(0, 646, 64, 64), TimeSpan.FromSeconds(.125));

            standRight = new Animation ();
            standRight.AddFrame(new Rectangle(0, 710, 64, 64), TimeSpan.FromSeconds(.125));
        }

		public void Draw(SpriteBatch spriteBatch)
		{
			Vector2 topLeftOfSprite = new Vector2 (this.X, this.Y);
			Color tintColor = Color.White;
			var sourceRectangle = currentAnimation.CurrentRectangle;

			spriteBatch.Draw(characterSheetTexture, topLeftOfSprite, sourceRectangle, Color.White);
		}

		public void Update(GameTime gameTime)
		{
			var velocity = GetDesiredVelocityFromInput ();

			this.X += velocity.X * (float)gameTime.ElapsedGameTime.TotalSeconds;
			this.Y += velocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds;

			// We can use the velocity variable to determine if the 
			// character is moving or standing still
			bool isMoving = velocity != Vector2.Zero;
			if (isMoving)
			{
				// If the absolute value of the X component
				// is larger than the absolute value of the Y
				// component, then that means the character is
				// moving horizontally:
				bool isMovingHorizontally = Math.Abs (velocity.X) > Math.Abs (velocity.Y);
				if (isMovingHorizontally)
				{
					// No that we know the character is moving horizontally 
					// we can check if the velocity is positive (moving right)
					// or negative (moving left)
					if (velocity.X > 0)
					{
						currentAnimation = walkRight;
					}
					else
					{
						currentAnimation = walkLeft;
					}
				}
				else
				{
					// If the character is not moving horizontally
					// then it must be moving vertically. The SpriteBatch
					// class treats positive Y as down, so this defines the
					// coordinate system for our game. Therefore if
					// Y is positive then the character is moving down.
					// Otherwise, the character is moving up.
					if (velocity.Y > 0)
					{
						currentAnimation = walkDown;
					}
					else
					{
						currentAnimation = walkUp;
					}
				}
			}
			else
			{
				// This else statement contains logic for if the
				// character is standing still.
				// First we are going to check if the character
				// is currently playing any walking animations.
				// If so, then we want to switch to a standing animation.
				// We want to preserve the direction that the character
				// is facing so we'll set the corresponding standing
				// animation according to the walking animation being played.
				if (currentAnimation == walkRight)
				{
					currentAnimation = standRight;
				}
				else if (currentAnimation == walkLeft)
				{
					currentAnimation = standLeft;
				}
				else if (currentAnimation == walkUp)
				{
					currentAnimation = standUp;
				}
				else if (currentAnimation == walkDown)
				{
					currentAnimation = standDown;
				}
				// If the character is standing still but is not showing
				// any animation at all then we'll default to facing down.
				else if (currentAnimation == null)
				{
					currentAnimation = standDown;
				}
			}

			currentAnimation.Update (gameTime);
		}

		Vector2 GetDesiredVelocityFromInput()
		{
			Vector2 desiredVelocity = new Vector2 ();

			TouchCollection touchCollection = TouchPanel.GetState();

			if (touchCollection.Count > 0)
			{
				desiredVelocity.X = touchCollection [0].Position.X - this.X;
				desiredVelocity.Y = touchCollection [0].Position.Y - this.Y;

				if (desiredVelocity.X != 0 || desiredVelocity.Y != 0)
				{
					desiredVelocity.Normalize();
					const float desiredSpeed = 200;
					desiredVelocity *= desiredSpeed;
				}
			}

			return desiredVelocity;
		}
	}
}