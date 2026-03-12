using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class Game : Form
{
    Bitmap frame;
    int[] buffer;
    int renderW, renderH;


Bitmap texture;
    int[] texPixels;
    int texW, texH;

    float pX = 3.5f, pY = 3.5f;
    float pDir = 0f;
    float fov = 0.66f;

    Timer timer = new Timer();
    HashSet<Keys> keys = new HashSet<Keys>();

    int fps = 0;
    int frameCount = 0;
    DateTime lastFps = DateTime.Now;

    int[,] map = {


{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
{1,0,1,1,1,1,1,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1},
{1,0,1,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1},
{1,0,1,0,1,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1,0,1},
{1,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1},
{1,0,1,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1,0,1,0,1},
{1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,1},
{1,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1,0,1,0,1,0,1},
{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,1,0,1},
{1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,1,0,1,0,1,0,1,0,1},
{1,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,1,0,1,0,1,0,1},
{1,0,1,1,1,1,1,1,1,1,1,1,0,1,0,1,0,1,0,1,0,1,0,1},
{1,0,1,0,0,0,0,0,0,0,0,1,0,1,0,1,0,1,0,1,0,1,0,1},
{1,0,1,0,1,1,1,1,1,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1},
{1,0,1,0,1,0,0,0,0,1,0,1,0,1,0,1,0,1,0,1,0,1,0,1},
{1,0,1,0,1,0,1,1,0,1,0,1,0,1,0,1,0,1,1,1,0,1,0,1},
{1,0,1,0,1,0,1,1,0,1,0,1,0,0,0,1,0,0,0,0,0,0,0,1},
{1,0,1,0,0,0,1,1,0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,1},
{1,0,1,1,1,1,1,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
{1,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
{1,0,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
{1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
};


public Game()
    {
        Text = "3D Maze Game - Cyber Slot";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;

        try { texture = new Bitmap("texture.png"); }
        catch
        {
            texture = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(texture))
            {
                g.Clear(Color.DarkRed);
                g.DrawRectangle(Pens.White, 0, 0, 63, 63);
            }
        }

        texW = texture.Width;
        texH = texture.Height;
        texPixels = new int[texW * texH];

        BitmapData tdata = texture.LockBits(new Rectangle(0, 0, texW, texH),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        Marshal.Copy(tdata.Scan0, texPixels, 0, texPixels.Length);
        texture.UnlockBits(tdata);

        InitBuffers();

        KeyDown += (s, e) => keys.Add(e.KeyCode);
        KeyUp += (s, e) => keys.Remove(e.KeyCode);
        Resize += (s, e) => InitBuffers();

        timer.Interval = 16;
        timer.Tick += (s, e) => {
            UpdateGame();
            Render();
            Invalidate();
        };
        timer.Start();
    }

    void InitBuffers()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

        renderW = ClientSize.Width;
        renderH = ClientSize.Height;

        frame = new Bitmap(renderW, renderH, PixelFormat.Format32bppArgb);
        buffer = new int[renderW * renderH];
    }

    void UpdateGame()
    {
        float speed = 0.08f;
        float rot = 0.06f;
        float padding = 0.3f;

        if (keys.Contains(Keys.Left) || keys.Contains(Keys.Q)) pDir -= rot;
        if (keys.Contains(Keys.Right) || keys.Contains(Keys.E)) pDir += rot;

        float dirX = (float)Math.Cos(pDir);
        float dirY = (float)Math.Sin(pDir);

        float strafeX = (float)Math.Cos(pDir + Math.PI / 2);
        float strafeY = (float)Math.Sin(pDir + Math.PI / 2);

        float moveX = 0, moveY = 0;

        if (keys.Contains(Keys.W) || keys.Contains(Keys.Up)) { moveX += dirX * speed; moveY += dirY * speed; }
        if (keys.Contains(Keys.S) || keys.Contains(Keys.Down)) { moveX -= dirX * speed; moveY -= dirY * speed; }
        if (keys.Contains(Keys.A)) { moveX -= strafeX * speed; moveY -= strafeY * speed; }
        if (keys.Contains(Keys.D)) { moveX += strafeX * speed; moveY += strafeY * speed; }

        float checkX = moveX > 0 ? moveX + padding : moveX - padding;
        if (map[(int)pY, (int)(pX + checkX)] == 0) pX += moveX;

        float checkY = moveY > 0 ? moveY + padding : moveY - padding;
        if (map[(int)(pY + checkY), (int)pX] == 0) pY += moveY;
    }

    void Render()
    {
        if (buffer == null) return;

        int sky = unchecked((int)0xFF101010);
        int floor = unchecked((int)0xFF202020);

        for (int i = 0; i < buffer.Length / 2; i++) buffer[i] = sky;
        for (int i = buffer.Length / 2; i < buffer.Length; i++) buffer[i] = floor;

        float dirX = (float)Math.Cos(pDir);
        float dirY = (float)Math.Sin(pDir);

        float planeX = -dirY * fov;
        float planeY = dirX * fov;

        for (int x = 0; x < renderW; x++)
        {
            float camX = 2f * x / renderW - 1f;

            float rDirX = dirX + planeX * camX;
            float rDirY = dirY + planeY * camX;

            int mX = (int)pX;
            int mY = (int)pY;

            float dDistX = Math.Abs(1 / rDirX);
            float dDistY = Math.Abs(1 / rDirY);

            int sX, sY, side = 0;
            float sDistX, sDistY;

            if (rDirX < 0) { sX = -1; sDistX = (pX - mX) * dDistX; }
            else { sX = 1; sDistX = (mX + 1f - pX) * dDistX; }

            if (rDirY < 0) { sY = -1; sDistY = (pY - mY) * dDistY; }
            else { sY = 1; sDistY = (mY + 1f - pY) * dDistY; }

            bool hit = false;

            while (!hit)
            {
                if (sDistX < sDistY) { sDistX += dDistX; mX += sX; side = 0; }
                else { sDistY += dDistY; mY += sY; side = 1; }

                if (mX < 0 || mY < 0 || mX >= map.GetLength(1) || mY >= map.GetLength(0)) break;

                if (map[mY, mX] > 0) hit = true;
            }

            float dist = (side == 0) ? sDistX - dDistX : sDistY - dDistY;
            if (dist < 0.1f) dist = 0.1f;

            int lineH = (int)(renderH / dist);

            int drawStart = Math.Max(0, -lineH / 2 + renderH / 2);
            int drawEnd = Math.Min(renderH - 1, lineH / 2 + renderH / 2);

            float wallX = (side == 0) ? pY + dist * rDirY : pX + dist * rDirX;
            wallX -= (float)Math.Floor(wallX);

            int txX = (int)(wallX * texW);
            if (txX < 0) txX = 0;
            if (txX >= texW) txX = texW - 1;

            if (side == 0 && rDirX > 0) txX = texW - txX - 1;
            if (side == 1 && rDirY < 0) txX = texW - txX - 1;

            float step = 1f * texH / lineH;
            float texPos = (drawStart - renderH / 2 + lineH / 2) * step;

            for (int y = drawStart; y < drawEnd; y++)
            {
                int txY = (int)texPos % texH;
                if (txY < 0) txY += texH;
                texPos += step;

                int color = texPixels[txY * texW + txX];

                if (side == 1)
                    color = (color & unchecked((int)0xFF000000)) |
                           (((color & 0xFEFEFE) >> 1) & 0x7F7F7F);

                buffer[y * renderW + x] = color;
            }
        }

        BitmapData data = frame.LockBits(new Rectangle(0, 0, renderW, renderH),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
        frame.UnlockBits(data);

        frameCount++;

        if ((DateTime.Now - lastFps).TotalSeconds >= 1)
        {
            fps = frameCount;
            frameCount = 0;
            lastFps = DateTime.Now;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.DrawImage(frame, 0, 0);
        e.Graphics.DrawString("FPS: " + fps, Font, Brushes.Lime, 10, 10);
        DrawMiniMap(e.Graphics);
    }

    void DrawMiniMap(Graphics g)
    {
        int mapScale = 8;
        int mapW = map.GetLength(1);
        int mapH = map.GetLength(0);

        int offsetX = renderW - mapW * mapScale - 10;
        int offsetY = 10;

        g.FillRectangle(Brushes.Black, offsetX - 2, offsetY - 2, mapW * mapScale + 4, mapH * mapScale + 4);

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                Brush brush = map[y, x] > 0 ? Brushes.Gray : Brushes.DarkGray;
                g.FillRectangle(brush, offsetX + x * mapScale, offsetY + y * mapScale, mapScale, mapScale);
            }
        }

        float playerX = pX * mapScale + offsetX;
        float playerY = pY * mapScale + offsetY;

        float size = 5;

        
        PointF[] points = new PointF[]
        {
        new PointF(0, -size),
        new PointF(size / 2, size),
        new PointF(-size / 2, size)
        };

        
        float angle = pDir - (float)Math.PI / 2;

        for (int i = 0; i < points.Length; i++)
        {
            float x = points[i].X;
            float y = points[i].Y;
            points[i].X = x * (float)Math.Cos(angle) + y * (float)Math.Sin(angle) + playerX;
            points[i].Y = x * (float)Math.Sin(angle) - y * (float)Math.Cos(angle) + playerY;
        }

        g.FillPolygon(Brushes.Lime, points);
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new Game());
    }


}
