
using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace WindowEngine
{
    public class Game : GameWindow
    {
        private int vboHandle; // Vertex Buffer Object handle
        private int vaoHandle; // Vertex Array Object handle
        private int shaderProgramHandle; // Shader program handle

        private int modelLoc, viewLoc, projLoc; // Uniform locations

        public Game()
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            // Set window size and center it on the screen
            this.Size = new Vector2i(1280, 768);
            this.CenterWindow(this.Size);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            // Set background color (clear color)
            GL.ClearColor(0.5f, 0.7f, 0.8f, 1f);

            // Define 6 vertices (two triangles) for a square
            // Format: position(x,y,z) + color(r,g,b)
            float[] vertices = new float[]
            {
                // Triangle 1
                -0.5f,  0.5f, 0f,   1f, 0f, 0f,   // top-left, red
                -0.5f, -0.5f, 0f,   0f, 1f, 0f,   // bottom-left, green
                 0.5f,  0.5f, 0f,   0f, 0f, 1f,   // top-right, blue

                // Triangle 2
                -0.5f, -0.5f, 0f,   0f, 1f, 0f,   // bottom-left, green
                 0.5f, -0.5f, 0f,   1f, 1f, 0f,   // bottom-right, yellow
                 0.5f,  0.5f, 0f,   0f, 0f, 1f    // top-right, blue
            };

            // Create Vertex Buffer Object (VBO)
            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Create Vertex Array Object (VAO)
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);

            // Position attribute (location = 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute (location = 1)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Unbind buffers for safety
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            // Vertex shader: applies model-view-projection transformations and passes color to fragment shader
            string vertexShaderCode = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;
                layout(location = 1) in vec3 aColor;

                out vec3 vertexColor;

                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProj;

                void main()
                {
                    gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0);
                    vertexColor = aColor; // pass vertex color to fragment shader
                }
            ";

            // Fragment shader: outputs interpolated color
            string fragmentShaderCode = @"
                #version 330 core
                in vec3 vertexColor;
                out vec4 FragColor;

                void main()
                {
                    FragColor = vec4(vertexColor, 1.0);
                }
            ";

            // Compile shaders and check for errors
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderCode);
            GL.CompileShader(vertexShader);
            CheckShaderCompile(vertexShader, "Vertex Shader");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderCode);
            GL.CompileShader(fragmentShader);
            CheckShaderCompile(fragmentShader, "Fragment Shader");

            // Link shaders into a program
            shaderProgramHandle = GL.CreateProgram();
            GL.AttachShader(shaderProgramHandle, vertexShader);
            GL.AttachShader(shaderProgramHandle, fragmentShader);
            GL.LinkProgram(shaderProgramHandle);

            GL.DetachShader(shaderProgramHandle, vertexShader);
            GL.DetachShader(shaderProgramHandle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Get uniform locations for model, view, projection matrices
            modelLoc = GL.GetUniformLocation(shaderProgramHandle, "uModel");
            viewLoc = GL.GetUniformLocation(shaderProgramHandle, "uView");
            projLoc = GL.GetUniformLocation(shaderProgramHandle, "uProj");
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shaderProgramHandle);

            // Model matrix: identity (no translation, so square is centered)
            Matrix4 model = Matrix4.Identity;

            // View matrix: camera looks at origin from z = 3
            Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);

            // Projection matrix: perspective projection
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                (float)Size.X / Size.Y,
                0.1f,
                100f
            );

            // Send matrices to shader
            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref proj);

            // Bind VAO and draw the two triangles (6 vertices)
            GL.BindVertexArray(vaoHandle);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            // Clean up GPU resources
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vboHandle);
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteProgram(shaderProgramHandle);

            base.OnUnload();
        }

        // Check shader compilation errors
        private void CheckShaderCompile(int shader, string name)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                Console.WriteLine($"Error compiling {name}: {GL.GetShaderInfoLog(shader)}");
            }
        }
    }
}
