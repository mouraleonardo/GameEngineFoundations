using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;

namespace WindowEngine
{
    public class Game : GameWindow
    {
        // Handles for GPU objects
        private int vertexBufferHandle;   // Vertex Buffer Object (stores vertices)
        private int shaderProgramHandle;  // Shader Program (vertex + fragment shaders)
        private int vertexArrayHandle;    // Vertex Array Object (stores vertex attribute layout)

        // Uniform locations for matrices
        private int modelLoc, viewLoc, projLoc;

        // Rotation angle for continuous rotation
        private float rotationAngle = 0f;

        public Game()
            : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            // Set the window size
            this.Size = new Vector2i(1280, 768);

            // Center the window on the screen
            this.CenterWindow(this.Size);
        }

        // Called automatically whenever the window is resized
        protected override void OnResize(ResizeEventArgs e)
        {
            // Update the OpenGL viewport to match the window size
            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }

        // Called once when the game starts (load resources here)
        protected override void OnLoad()
        {
            base.OnLoad();

            // Set background color (sky blue tone)
            GL.ClearColor(new Color4(0.5f, 0.7f, 0.8f, 1f));

            // Define a simple triangle (3 vertices in normalized device coordinates)
            float[] vertices = new float[]
            {
                0.0f,  0.5f, 0.0f,   // Top vertex
               -0.5f, -0.5f, 0.0f,   // Bottom-left vertex
                0.5f, -0.5f, 0.0f    // Bottom-right vertex
            };

            // Create a Vertex Buffer Object (VBO) and upload vertex data to GPU
            vertexBufferHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Create a Vertex Array Object (VAO) to store vertex attribute configuration
            vertexArrayHandle = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayHandle);

            // Configure how vertex data is interpreted by the shader
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            // Vertex Shader (handles vertex positions, now with transformation matrices)
            string vertexShaderCode = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;

                uniform mat4 uModel; // Model transformation
                uniform mat4 uView;  // Camera (view) transformation
                uniform mat4 uProj;  // Projection transformation

                void main()
                {
                    // Apply Model-View-Projection transform to the vertex
                    gl_Position = uProj * uView * uModel * vec4(aPosition, 1.0);
                }
            ";

            // Fragment Shader (defines pixel color)
            string fragmentShaderCode = @"
                #version 330 core
                out vec4 FragColor;

                void main()
                {
                    // Solid purple color
                    FragColor = vec4(0.6, 0.2, 0.8, 1.0);
                }
            ";

            // Compile vertex shader
            int vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShaderHandle, vertexShaderCode);
            GL.CompileShader(vertexShaderHandle);
            CheckShaderCompile(vertexShaderHandle, "Vertex Shader");

            // Compile fragment shader
            int fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShaderHandle, fragmentShaderCode);
            GL.CompileShader(fragmentShaderHandle);
            CheckShaderCompile(fragmentShaderHandle, "Fragment Shader");

            // Link shaders into a program
            shaderProgramHandle = GL.CreateProgram();
            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);
            GL.LinkProgram(shaderProgramHandle);

            // Clean up shaders (no longer needed after linking)
            GL.DetachShader(shaderProgramHandle, vertexShaderHandle);
            GL.DetachShader(shaderProgramHandle, fragmentShaderHandle);
            GL.DeleteShader(vertexShaderHandle);
            GL.DeleteShader(fragmentShaderHandle);

            // Get uniform variable locations in the shader program
            modelLoc = GL.GetUniformLocation(shaderProgramHandle, "uModel");
            viewLoc = GL.GetUniformLocation(shaderProgramHandle, "uView");
            projLoc = GL.GetUniformLocation(shaderProgramHandle, "uProj");
        }

        // Called every frame to update logic (input, animations, physics, etc.)
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            // Increase the rotation angle over time (rotate continuously)
            rotationAngle += (float)args.Time;
        }

        // Called every frame to render graphics
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // Clear screen and depth buffer
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(shaderProgramHandle);

            // Create rotation using a quaternion (rotate around Y axis)
            Quaternion q = Quaternion.FromAxisAngle(Vector3.UnitY, rotationAngle);
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(q);

            // Model matrix: rotation + translation
            Matrix4 model = rotationMatrix * Matrix4.CreateTranslation(0f, 0f, -2f);

            // View matrix: camera positioned at (0,0,3), looking at the origin
            Matrix4 view = Matrix4.LookAt(
                new Vector3(0, 0, 3),
                Vector3.Zero,
                Vector3.UnitY
            );

            // Projection matrix: perspective projection
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),   // Field of View
                (float)Size.X / Size.Y,             // Aspect ratio
                0.1f,                               // Near clipping plane
                100f                                // Far clipping plane
            );

            // Send matrices to the shader
            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);

            // Draw the triangle
            GL.BindVertexArray(vertexArrayHandle);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);

            // Swap buffers (display the frame)
            SwapBuffers();
        }

        // Called when the game closes (clean up GPU resources)
        protected override void OnUnload()
        {
            // Delete buffers and shader program
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vertexBufferHandle);

            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vertexArrayHandle);

            GL.UseProgram(0);
            GL.DeleteProgram(shaderProgramHandle);

            base.OnUnload();
        }

        // Helper function to check for shader compilation errors
        private void CheckShaderCompile(int shaderHandle, string shaderName)
        {
            GL.GetShader(shaderHandle, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shaderHandle);
                Console.WriteLine($"Error compiling {shaderName}: {infoLog}");
            }
        }
    }
}
