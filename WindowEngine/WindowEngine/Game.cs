using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace WindowEngine
{
    public class Game : IDisposable
    {
        private GameWindow _window;
        private int _shaderProgram;
        private int _vao;
        private int _vbo;
        private int _texture;

        // Default wrapping and filtering
        private TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        private TextureMinFilter minFilter = TextureMinFilter.LinearMipmapLinear;
        private TextureMagFilter magFilter = TextureMagFilter.Linear;

        private readonly string VertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos, 1.0);
    TexCoord = aTexCoord;
}";

        private readonly string FragmentShaderSource = @"
#version 330 core
out vec4 FragColor;
in vec2 TexCoord;

uniform sampler2D ourTexture;

void main()
{
    FragColor = texture(ourTexture, TexCoord);
}";

        public Game()
        {
            var nativeSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(800, 600),
                Title = "OpenGL 3.3 Textured Triangle",
                Flags = ContextFlags.ForwardCompatible
            };

            var gameSettings = new GameWindowSettings()
            {
                UpdateFrequency = 60.0
            };

            _window = new GameWindow(gameSettings, nativeSettings);
            _window.Load += OnLoad;
            _window.RenderFrame += OnRenderFrame;
            _window.UpdateFrame += OnUpdateFrame;
            _window.Resize += OnResize;
        }

        private void OnLoad()
        {
            GL.ClearColor(Color.Black);

            // Compile shaders
            int vert = CompileShader(ShaderType.VertexShader, VertexShaderSource);
            int frag = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vert);
            GL.AttachShader(_shaderProgram, frag);
            GL.LinkProgram(_shaderProgram);
            CheckProgram(_shaderProgram);

            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            // Triangle vertices (x,y,z + u,v)
            float[] vertices =
            {
                 0.0f,  0.8f, 0.0f,  0.5f, 1.0f, // top
                -0.8f, -0.8f, 0.0f,  0.0f, 0.0f, // bottom left
                 0.8f, -0.8f, 0.0f,  1.0f, 0.0f  // bottom right
            };

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            // Load texture
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string texturePath = Path.Combine(baseDir, "Assets", "wall.jpg");
            _texture = LoadTexture(texturePath);
        }

        private void OnUpdateFrame(FrameEventArgs e)
        {
            if (_window.IsKeyDown(Keys.Escape))
                _window.Close();

            // Switch wrapping modes
            if (_window.IsKeyPressed(Keys.D1)) SetWrapMode(TextureWrapMode.Repeat);
            if (_window.IsKeyPressed(Keys.D2)) SetWrapMode(TextureWrapMode.MirroredRepeat);
            if (_window.IsKeyPressed(Keys.D3)) SetWrapMode(TextureWrapMode.ClampToEdge);
            if (_window.IsKeyPressed(Keys.D4)) SetWrapMode(TextureWrapMode.ClampToBorder);

            // Switch filtering modes
            if (_window.IsKeyPressed(Keys.F1)) SetFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            if (_window.IsKeyPressed(Keys.F2)) SetFilter(TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear);
        }

        private void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(_shaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            _window.SwapBuffers();
        }

        private void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        public void Run() => _window.Run();

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0) throw new Exception(GL.GetShaderInfoLog(shader));
            return shader;
        }

        private void CheckProgram(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0) throw new Exception(GL.GetProgramInfoLog(program));
        }

        private int LoadTexture(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not find texture file: {path}");

            int texId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texId);

            // Initial wrap and filter
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

            using (Bitmap bmp = new Bitmap(path))
            {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var data = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb); // fully qualified

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bmp.UnlockBits(data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            return texId;
        }

        // Update wrap mode live
        private void SetWrapMode(TextureWrapMode mode)
        {
            wrapMode = mode;
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
        }

        // Update filter mode live
        private void SetFilter(TextureMinFilter min, TextureMagFilter mag)
        {
            minFilter = min;
            magFilter = mag;
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GL.DeleteTexture(_texture);
            GL.DeleteProgram(_shaderProgram);
            _window.Dispose();
        }
    }
}
