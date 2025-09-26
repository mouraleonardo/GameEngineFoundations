using System;                          // Basic system types like Exception, Console, etc.
using System.Drawing;                  // For Bitmap, Color, Rectangle (used for textures)
using System.Drawing.Imaging;          // For LockBits/PixelFormat to read bitmap data
using System.IO;                       // For File.Exists to check texture file
using OpenTK.Graphics.OpenGL4;         // OpenGL 4 functions (even though we target 3.3)
using OpenTK.Windowing.Common;         // For windowing events, FrameEventArgs, ResizeEventArgs
using OpenTK.Windowing.Desktop;        // For GameWindow and settings
using OpenTK.Mathematics;              // For Vector types (Vector2i, etc.)

namespace WindowEngine
{
    // Game class handles the OpenTK window and OpenGL rendering
    public class Game : IDisposable
    {
        // Fields for window, OpenGL objects
        private GameWindow _window;       // The main window
        private int _shaderProgram;       // OpenGL shader program ID
        private int _vao;                 // Vertex Array Object ID
        private int _vbo;                 // Vertex Buffer Object ID
        private int _texture;             // Texture ID

        // Vertex Shader (GLSL) - runs once per vertex
        private readonly string VertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPos;      // vertex position (x,y,z)
layout (location = 1) in vec2 aTexCoord; // texture coordinate (u,v)

out vec2 TexCoord;                       // pass UV to fragment shader

void main()
{
    gl_Position = vec4(aPos, 1.0);       // output clip-space position
    TexCoord = aTexCoord;                // pass texture coordinates
}";

        // Fragment Shader (GLSL) - runs once per pixel
        private readonly string FragmentShaderSource = @"
#version 330 core
out vec4 FragColor;       // output color
in vec2 TexCoord;         // incoming texture coordinate

uniform sampler2D ourTexture; // our 2D texture

void main()
{
    FragColor = texture(ourTexture, TexCoord); // sample texture at TexCoord
}";

        // Constructor - sets up the window and hooks events
        public Game()
        {
            // Settings for the OpenTK window
            var nativeSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(800, 600),          // window size
                Title = "OpenGL 3.3 Textured Triangle", // window title
                Flags = ContextFlags.ForwardCompatible  // required for core profile
            };

            // Settings for update loop
            var gameSettings = new GameWindowSettings()
            {
                UpdateFrequency = 60.0 // update ~60 times per second
            };

            // Create the GameWindow
            _window = new GameWindow(gameSettings, nativeSettings);

            // Hook events to our methods
            _window.Load += OnLoad;               // called once when window opens
            _window.RenderFrame += OnRenderFrame; // called every frame to render
            _window.UpdateFrame += OnUpdateFrame; // called every frame to update
            _window.Resize += OnResize;           // called when window size changes
        }

        // Called once when window loads
        private void OnLoad()
        {
            GL.ClearColor(Color.Black); // background color

            // Compile shaders and link them into a program
            int vert = CompileShader(ShaderType.VertexShader, VertexShaderSource);
            int frag = CompileShader(ShaderType.FragmentShader, FragmentShaderSource);

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vert);
            GL.AttachShader(_shaderProgram, frag);
            GL.LinkProgram(_shaderProgram);
            CheckProgram(_shaderProgram); // check linking errors

            // We can delete shader objects after linking
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            // Define a triangle (x,y,z + u,v)
            float[] vertices =
            {
                 0.0f,  0.8f, 0.0f,  0.5f, 1.0f, // top vertex
                -0.8f, -0.8f, 0.0f,  0.0f, 0.0f, // bottom left
                 0.8f, -0.8f, 0.0f,  1.0f, 0.0f  // bottom right
            };

            // Generate VAO and VBO
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            // Bind VAO first
            GL.BindVertexArray(_vao);

            // Bind VBO and upload vertex data
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Tell OpenGL how to interpret position data (location 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Tell OpenGL how to interpret UV data (location 1)
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Unbind buffers for safety
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            // Build path to texture dynamically (portable)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string texturePath = Path.Combine(baseDir, "Assets", "wall.jpg");
            _texture = LoadTexture(texturePath);

            // Set the shader uniform for the texture
            GL.UseProgram(_shaderProgram);
            int texLoc = GL.GetUniformLocation(_shaderProgram, "ourTexture");
            GL.Uniform1(texLoc, 0); // texture unit 0
        }

        // Called every frame to handle updates (input, logic)
        private void OnUpdateFrame(FrameEventArgs e)
        {
            if (_window.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                _window.Close(); // close window if ESC pressed
        }

        // Called every frame to render
        private void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit); // clear screen

            GL.UseProgram(_shaderProgram);            // use shader program
            GL.ActiveTexture(TextureUnit.Texture0);   // activate texture unit 0
            GL.BindTexture(TextureTarget.Texture2D, _texture); // bind our texture

            GL.BindVertexArray(_vao);                 // bind triangle VAO
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3); // draw the triangle

            _window.SwapBuffers();                    // show the rendered frame
        }

        // Called when window is resized
        private void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height); // update viewport to new window size
        }

        // Run the game loop
        public void Run() => _window.Run();

        // Helper method to compile a shader
        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
                throw new Exception(GL.GetShaderInfoLog(shader)); // throw error if failed

            return shader;
        }

        // Helper method to check program linking
        private void CheckProgram(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
                throw new Exception(GL.GetProgramInfoLog(program));
        }

        // Load a texture from file into OpenGL
        private int LoadTexture(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Could not find texture file: {path}");

            int texId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texId);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Load the image
            using (Bitmap bmp = new Bitmap(path))
            {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY); // flip vertically

                var data = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.Rgba,
                              data.Width,
                              data.Height,
                              0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                              PixelType.UnsignedByte,
                              data.Scan0);

                bmp.UnlockBits(data);
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D); // generate mipmaps
            return texId;
        }

        // Clean up resources
        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GL.DeleteTexture(_texture);
            GL.DeleteProgram(_shaderProgram);
            _window.Dispose(); // dispose window
        }
    }
}