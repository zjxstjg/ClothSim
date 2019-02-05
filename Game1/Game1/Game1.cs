using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace Game1
{
    public class Assets
    {   //simulation fields
        public static float gravity = 9.8f;
        public static float vertletMulti = 0.5f;
        public static float friction = 0.98f;
        public static float massOfPoints = 0.9f;
        public static float restingDistances = 15;
        public static float stiffnesses = 0.9f;
        public static byte relaxIterations = 3;
        //program assets
        public static GraphicsDeviceManager graphics;
        public static GraphicsDevice graphicsDevice;
        public static SpriteBatch spriteBatch;
        public static Texture2D lineTexture;
        public static int gameWindowWidth = 800;
        public static int gameWindowHeight = 400;
        public static void Setup()
        {
            lineTexture = new Texture2D(graphicsDevice, 1, 1);
            lineTexture.SetData<Color>(new Color[] { Color.White });
        }
    }


    #region Data

    public class Line
    {
        public PointMass P1;
        public PointMass P2;
        public bool visible = true;
        public float angle = 0.0f;
        public int length = 0;
        public Rectangle rec = new Rectangle(0, 0, 1, 1); //this is drawn
        public Texture2D texture = Assets.lineTexture; //never changes
        public Rectangle texRec = new Rectangle(0, 0, 1, 1); //tex rec
        public Vector2 texOrigin = new Vector2(0, 0);
        public Line(PointMass p1, PointMass p2) { P1 = p1; P2 = p2; }
    }

    public class PointMass
    {
        public float X, Y;
        public float lastX, lastY;
        public float accX, accY = 0;
        public float mass = Assets.massOfPoints;
        public bool pinned = false;
        public List<PointMass> neighbors = new List<PointMass>(); //0=up, 1=right, 2=down, 3=left
        //constructors
        public PointMass(int xPos, int yPos)
        {
            X = xPos; Y = yPos;
            //add 4 nulls for neighbors
            neighbors.Add(null);
            neighbors.Add(null);
            neighbors.Add(null);
            neighbors.Add(null);
        }
    }

    public enum MouseButtons { LeftButton, RightButton }
    public static class InputData
    {
        public static KeyboardState currentKeyboardState = new KeyboardState();
        public static MouseState currentMouseState = new MouseState();
        public static KeyboardState lastKeyboardState = new KeyboardState();
        public static MouseState lastMouseState = new MouseState();
        public static void Update()
        {   //store the last input state
            lastKeyboardState = currentKeyboardState;
            lastMouseState = currentMouseState;
            //get the current input states + cursor position
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
        }
        public static bool IsLeftMouseBtnPress()
        {   //check to see if L mouse button was pressed
            return (currentMouseState.LeftButton == ButtonState.Pressed &&
                    lastMouseState.LeftButton == ButtonState.Released);
        }
        public static bool IsRightMouseBtnPress()
        {   //check to see if L mouse button was pressed
            return (currentMouseState.RightButton == ButtonState.Pressed &&
                    lastMouseState.RightButton == ButtonState.Released);
        }
    }

    #endregion


    #region Functionality

    public static class Functions_Simulate
    {
        public static void Constrain(PointMass Pm)
        {
            if (Pm.pinned) { return; }

            //keep the PointMasss within the screen
            if (Pm.Y < 1)
            { Pm.Y = 2 * (1) - Pm.Y; }
            else if (Pm.Y > Assets.gameWindowHeight - 1)
            { Pm.Y = 2 * (Assets.gameWindowHeight - 1) - Pm.Y; }

            if (Pm.X > Assets.gameWindowWidth - 1)
            { Pm.X = 2 * (Assets.gameWindowWidth - 1) - Pm.X; }
            else if (Pm.X < 1)
            { Pm.X = 2 * (1) - Pm.X; }
        }

        public static void RelaxPM(PointMass Pm)
        {   //relaxIterations
            for (int x = 0; x < Assets.relaxIterations; x++)
            {
                for (int g = 0; g < Pm.neighbors.Count; g++)
                {
                    if (Pm.neighbors[g] != null)
                    {
                        //calculate the distance between the two PointMassses
                        float diffX = Pm.X - Pm.neighbors[g].X;
                        float diffY = Pm.Y - Pm.neighbors[g].Y;
                        float d = (float)Math.Sqrt(diffX * diffX + diffY * diffY);

                        //find the difference, or the ratio of how far along the restingDistance the actual distance is.
                        float difference = (Assets.restingDistances - d) / d;

                        //inverse the mass quantities
                        float im1 = 1 / Pm.mass;
                        float im2 = 1 / Pm.neighbors[g].mass;
                        float scalarP1 = (im1 / (im1 + im2)) * Assets.stiffnesses;
                        float scalarP2 = Assets.stiffnesses - scalarP1;

                        //push/pull based on mass
                        //heavier objects will be pushed/pulled less than attached light objects
                        if (Pm.pinned == false)
                        {
                            Pm.X += diffX * scalarP1 * difference;
                            Pm.Y += diffY * scalarP1 * difference;
                        }
                        if (Pm.neighbors[g].pinned == false)
                        {
                            Pm.neighbors[g].X -= diffX * scalarP2 * difference;
                            Pm.neighbors[g].Y -= diffY * scalarP2 * difference;
                        }
                    }
                }
            }
        }

        public static double velocityX, velocityY;
        public static float nextX, nextY;

        public static void ApplyPhysics(PointMass Pm)
        {
            if (Pm.pinned) { return; }
            //apply force
            Pm.accX += 0 / Pm.mass;
            Pm.accY += Pm.mass * Assets.gravity;
            //calc velocity
            velocityX = Pm.X - Pm.lastX;
            velocityY = Pm.Y - Pm.lastY;
            //dampen velocity
            velocityX *= Assets.friction;
            velocityY *= Assets.friction;
            //calculate the next position using Verlet Integration
            nextX = (float)(Pm.X + velocityX + Assets.vertletMulti * Pm.accX);
            nextY = (float)(Pm.Y + velocityY + Assets.vertletMulti * Pm.accY);
            //reset variables
            Pm.lastX = Pm.X;
            Pm.lastY = Pm.Y;
            Pm.X = (int)nextX;
            Pm.Y = (int)nextY;
            Pm.accX = 0;
            Pm.accY = 0;
        }

        public static void UpdateLine(Line Line)
        {   //set angle
            Line.angle = (float)Math.Atan2(
                (Line.P1.Y - Line.P2.Y),
                (Line.P1.X - Line.P2.X));
            //set length
            Line.length = (int)Vector2.Distance(
                new Vector2(Line.P2.X, Line.P2.Y),
                new Vector2(Line.P1.X, Line.P1.Y));
            //update the rectangle parameters based on length, angle
            Line.rec.X = (int)Line.P2.X;
            Line.rec.Y = (int)Line.P2.Y;
            Line.rec.Width = Line.length;
            Line.rec.Height = 1;
        }

    }

    public static class Functions_Draw
    {
        public static void DrawCursor(Rectangle Rec)
        {
            Assets.spriteBatch.Draw(Assets.lineTexture, Rec, Color.Blue);
        }

        public static void Draw(Line Line)
        {
            Assets.spriteBatch.Draw(
                Line.texture,
                Line.rec, //draw rec
                Line.texRec, //texture rec
                Color.MonoGameOrange, //at 100% alpha
                Line.angle,
                Line.texOrigin, //wat is this Vector2 origin parameter?
                SpriteEffects.None,
                0.000001f //'layer'
            );
        }
    }

    #endregion


    #region Game (update + draw)

    public class Game1 : Game
    {
        public Rectangle cursorRec = new Rectangle(0, 0, 34, 34);
        List<PointMass> PointMasses;
        List<Line> Lines;
        int i;
        PointMass grabbedPM;

        public Game1()
        {
            Assets.graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.IsMouseVisible = true;
            Assets.graphics.PreferredBackBufferWidth = Assets.gameWindowWidth;
            Assets.graphics.PreferredBackBufferHeight = Assets.gameWindowHeight;
            Assets.graphics.ApplyChanges();
        }

        protected override void LoadContent()
        {
            Assets.graphicsDevice = GraphicsDevice;
            Assets.spriteBatch = new SpriteBatch(GraphicsDevice);
            Assets.Setup();

            byte curtainWidth = 43;
            byte curtainHeight = 14;
            byte spacing = 16;

            PointMasses = new List<PointMass>();
            Lines = new List<Line>();
            int counter;

            //create the point masses
            for (int y = 0; y < curtainHeight; y++)
            {
                for (int x = 0; x < curtainWidth; x++)
                {   //spread pms out
                    PointMass pm = new PointMass(64 + x * spacing, 16 + y * spacing);
                    if (y == 0)
                    {
                        //pm.pinned = true; //pin entire top row

                        //pin some of top row curtain
                        if (x == 0 || x == curtainWidth - 1) { pm.pinned = true; }
                        if (x % 6 == 0) { pm.pinned = true; }
                    }
                    PointMasses.Add(pm); //ad pm to game list
                }
            }

            //setup point masses neighbors
            counter = 0;
            for (int y = 0; y < curtainHeight; y++)
            {
                for (int x = 0; x < curtainWidth; x++)
                {
                    if (x < (curtainWidth - 1)) //connect left to right point, and vice versa
                    {   //neighbors ref ////0=up, 1=right, 2=down, 3=left
                        PointMasses[counter].neighbors[1] = PointMasses[counter + 1];
                        PointMasses[counter + 1].neighbors[3] = PointMasses[counter];
                    }
                    if (y < (curtainHeight - 1)) //connect top to bottom, and vice versa
                    {
                        PointMasses[counter].neighbors[2] = PointMasses[counter + curtainWidth];
                        PointMasses[counter + curtainWidth].neighbors[0] = PointMasses[counter];
                    }
                    //track which pm index we are at
                    counter++;
                }
            }

            //assign point masses to lines
            counter = 0;
            for (int y = 0; y < curtainHeight; y++)
            {
                for (int x = 0; x < curtainWidth; x++)
                {
                    if (x < (curtainWidth - 1)) //connect left to right point
                    { Lines.Add(new Line(PointMasses[counter], PointMasses[counter + 1])); }
                    if (y < (curtainHeight - 1)) //connect top to bottom
                    { Lines.Add(new Line(PointMasses[counter], PointMasses[counter + curtainWidth])); }
                    //track which pm index we are at
                    counter++;
                }
            }
        }

        protected override void UnloadContent() { }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);


            #region Process Curtain Simulation

            //update point masses
            for (i = 0; i < PointMasses.Count; i++)
            {
                Functions_Simulate.Constrain(PointMasses[i]);
                Functions_Simulate.ApplyPhysics(PointMasses[i]);
                //relax neighbors x3
                Functions_Simulate.RelaxPM(PointMasses[i]);
                Functions_Simulate.RelaxPM(PointMasses[i]);
                Functions_Simulate.RelaxPM(PointMasses[i]);
            }

            #endregion


            #region Pass input into curtain

            //get this frame of user input
            InputData.Update();
            //place cursor rec and draw game cursor
            MouseState currentMouseState = Mouse.GetState();
            cursorRec.X = (int)currentMouseState.X - (int)(cursorRec.Width / 2);
            cursorRec.Y = (int)currentMouseState.Y - (int)(cursorRec.Height / 2);

            if (InputData.IsLeftMouseBtnPress())
            {   //grab one PM colliding with cursor rec
                for (int g = 0; g < PointMasses.Count; g++)
                {   //do not allow input to be passed to pinned point masses
                    if (PointMasses[g].pinned == false)
                    {
                        if (cursorRec.Contains(PointMasses[g].X, PointMasses[g].Y))
                        {   //grab PM, bail
                            grabbedPM = PointMasses[g];
                            g = PointMasses.Count;
                        }
                    }
                }
            }
            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {   //handle dragging state (left mouse button down)
                if (grabbedPM != null)
                {   //relax the point and her neighbors while dragging
                    Functions_Simulate.RelaxPM(grabbedPM);
                    Functions_Simulate.RelaxPM(grabbedPM);
                    //drag the point until released
                    grabbedPM.X = currentMouseState.X;
                    grabbedPM.Y = currentMouseState.Y;
                }
            }
            else { grabbedPM = null; } //release grabbed pm

            #endregion


            //update how the lines connect to their pointMasses
            for (i = 0; i < Lines.Count; i++)
            { Functions_Simulate.UpdateLine(Lines[i]); }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Assets.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            //draw curtain lines + cursor
            for (i = 0; i < Lines.Count; i++) { Functions_Draw.Draw(Lines[i]); }
            //Functions_Draw.DrawCursor(cursorRec);
            Assets.spriteBatch.End();
            base.Draw(gameTime);
        }
    }

    #endregion

}