using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace WindowEngine
{
    public class Game : GameWindow
    {
        private int vboHandle;
        private int vaoHandle;
        private int eboHandle;
        private int shaderProgramHandle;

        private int modelLoc, viewLoc, projLoc;

        private float rotationAngle = 0f; // Angle for rotation animation
        private float scaleFactor = 1f;   // Scaling factor

        public Game()
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.Size = new Vector2i(1280, 768);
            this.CenterWindow(this.Size);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.5f, 0.7f, 0.8f, 1f);

            // 4 unique vertices for the square (position + color)
            float[] vertices = new float[]
            {
                -0.5f,  0.5f, 0f,   1f, 0f, 0f,   // top-left, red
                -0.5f, -0.5f, 0f,   0f, 1f, 0f,   // bottom-left, green
                 0.5f, -0.5f, 0f,   0f, 0f, 1f,   // bottom-right, blue
                 0.5f,  0.5f, 0f,   1f, 1f, 0f    // top-right, yellow
            };

            uint[] indices = new uint[]
            {
                0, 1, 2, 0, 2, 3
            };

            // Generate buffers
            vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            // Position attribute
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            // Vertex shader with model transformation
            string vertexShaderCode = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;
                layout(location = 1) in vec3 aColor;

                uniform mat4 uModel;
                uniform mat4 uView;
                uniform mat4 uProj;

                out vec3 vertexColor;

                void main()
                {
                    gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0);
                    vertexColor = aColor;
                }
            ";

            string fragmentShaderCode = @"
                #version 330 core
                in vec3 vertexColor;
                out vec4 FragColor;

                void main()
                {
                    FragColor = vec4(vertexColor, 1.0);
                }
            ";

            // Compile shaders
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderCode);
            GL.CompileShader(vertexShader);
            CheckShaderCompile(vertexShader, "Vertex Shader");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderCode);
            GL.CompileShader(fragmentShader);
            CheckShaderCompile(fragmentShader, "Fragment Shader");

            shaderProgramHandle = GL.CreateProgram();
            GL.AttachShader(shaderProgramHandle, vertexShader);
            GL.AttachShader(shaderProgramHandle, fragmentShader);
            GL.LinkProgram(shaderProgramHandle);

            GL.DetachShader(shaderProgramHandle, vertexShader);
            GL.DetachShader(shaderProgramHandle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            modelLoc = GL.GetUniformLocation(shaderProgramHandle, "uModel");
            viewLoc = GL.GetUniformLocation(shaderProgramHandle, "uView");
            projLoc = GL.GetUniformLocation(shaderProgramHandle, "uProj");
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            // Update rotation angle and scaling factor over time
            rotationAngle += (float)args.Time;          // rotate continuously
            scaleFactor = 1.0f + 0.5f * (float)Math.Sin(rotationAngle); // oscillating scale
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shaderProgramHandle);

            // Model matrix: apply scaling and rotation
            Matrix4 model = Matrix4.CreateScale(scaleFactor) * Matrix4.CreateRotationZ(rotationAngle);

            // View matrix: camera looking at origin
            Matrix4 view = Matrix4.LookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);

            // Projection matrix: perspective
            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), (float)Size.X / Size.Y, 0.1f, 100f);

            // Send matrices to shader
            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref proj);

            // Draw square using EBO
            GL.BindVertexArray(vaoHandle);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vboHandle);
            GL.DeleteBuffer(eboHandle);
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vaoHandle);
            GL.DeleteProgram(shaderProgramHandle);
            base.OnUnload();
        }

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
