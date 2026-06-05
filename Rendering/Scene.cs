using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

using StudioCCS.FileFormat;
using StudioCCS.FileFormat.Animation;
using StudioCCS.FileFormat.Geometry;
using StudioCCS.FileFormat.Materials;
using StudioCCS.FileFormat.SceneObjects;
using StudioCCS.Logging;
using StudioCCS.Rendering.Gizmos;

namespace StudioCCS.Rendering;

public static class Scene
{

    public enum SceneMode { Preview, Scene, All };
    public enum KeyStatus { Up, Pressed, Repeated };
    //Portable camera-control keys, mapped from the UI's key events.
    public enum CameraKey { None, Forward, Backward, Left, Right, Up, Down, ZoomIn, ZoomOut };
    private const int AxisViewSize = 80;

    //Draw Mode Flags for rendering
    public static int SCENE_DRAW_LINES = 1;
    public static int SCENE_DRAW_VERTEX_COLORS = 2;
    public static int SCENE_DRAW_SMOOTH = 4;
    public static int SCENE_DRAW_TEXTURE = 8;
    public static int SCENE_DRAW_SELECTION = 16;
    public static int SCENE_DRAW_FLIP_TEXCOORDS = 32;



    //public static RenderMode DisplayMode = RenderMode.Wireframe;
    public static SceneMode SceneDisplay = SceneMode.Preview;

    public static bool BackfaceCull = false;

    //The GL context + buffer swap are owned by the Avalonia OpenGlControlBase.
    //The viewport's pixel size is pushed into ViewWidth/ViewHeight before each Render().
    public static bool WasInit = false;
    public static int ViewWidth = 1;
    public static int ViewHeight = 1;
    public static Stopwatch Timer = new Stopwatch();

    public static Matrix4 ProjectionMtx = Matrix4.Identity;
    public static Matrix4 AxisProjectionMtx = Matrix4.Identity;

    //Viewport clear + grid colours. Driven by the UI's active theme variant
    //(MainWindow.ApplyViewportTheme) and re-applied each frame so an OS
    //light/dark switch is reflected live. Defaults match the dark theme.
    public static Vector4 BackgroundColor = new Vector4(64 / 255.0f, 64 / 255.0f, 64 / 255.0f, 1.0f);
    public static Vector4 GridColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

    //Draw options
    public static bool DrawViewAxis = true;
    public static bool DrawViewGrid = true;
    public static bool DrawCollisionMeshes = true;
    public static bool DrawBoundingBoxes = true;
    public static bool DrawWorldCenter = true;
    public static bool DrawDummyHelpers = true;
    public static bool DrawLightHelpers = true;

    public static bool DrawWireframe = false;
    public static bool DrawVertexColors = false;
    public static bool DrawVertexNormals = false;
    public static bool DrawTextures = false;

    //Camera
    public static ArcBallCamera PreviewCamera = new ArcBallCamera();
    public static ArcBallCamera SceneCamera = new ArcBallCamera();
    public static ArcBallCamera AllCamera = new ArcBallCamera();
    public static bool DefaultToAxisMovement = false;

    //Input
    public static float LastMouseX = 0.0f;
    public static float LastMouseY = 0.0f;
    public static float MouseSensitivity = 0.2f;
    public static float MouseWheelSensitivity = 0.000050f;
    public static float MovementSpeed = 0.0025f;
    public static float DeltaTime = 1.0f;

    //Keys we handle
    //TODO: Make these remappable?
    private static KeyStatus MoveForward = KeyStatus.Up;
    private static KeyStatus MoveBackward = KeyStatus.Up;
    private static KeyStatus MoveLeft = KeyStatus.Up;
    private static KeyStatus MoveRight = KeyStatus.Up;
    private static KeyStatus MoveUp = KeyStatus.Up;
    private static KeyStatus MoveDown = KeyStatus.Up;
    private static KeyStatus ZoomIn = KeyStatus.Up;
    private static KeyStatus ZoomOut = KeyStatus.Up;
    private static KeyStatus ShiftModifier = KeyStatus.Up;
    private static KeyStatus ControlModifier = KeyStatus.Up;

    //Items We'll Render
    public static List<CCSFile> CCSFileList = new List<CCSFile>();
    public static List<CCSAnime> ActiveAnimes = new List<CCSAnime>();
    public static TreeNodeTag SelectedPreviewItemTag = null;

    public static int LoadProgram(string programName, bool hasGeometryShader = false)
    {
        bool result = true;

        int vShaderID = GL.CreateShader(ShaderType.VertexShader);
        int fShaderID = GL.CreateShader(ShaderType.FragmentShader);
        int gShaderID = 0;
        if (hasGeometryShader)
        {
            gShaderID = GL.CreateShader(ShaderType.GeometryShader);
        }

        if (!LoadShader("Rendering/Shaders/" + programName + ".vsh", vShaderID))
        {
            result = false;
        }

        if (!LoadShader("Rendering/Shaders/" + programName + ".fsh", fShaderID))
        {
            result = false;
        }

        if (hasGeometryShader)
        {
            if (!LoadShader("Rendering/Shaders/" + programName + ".gsh", gShaderID))
            {
                result = false;
            }
        }

        int programID = GL.CreateProgram();
        if (result)
        {
            GL.AttachShader(programID, vShaderID);
            GL.AttachShader(programID, fShaderID);
            if (hasGeometryShader)
            {
                GL.AttachShader(programID, gShaderID);
            }

            GL.LinkProgram(programID);
            int programLinkResult = 0;
            GL.GetProgram(programID, GetProgramParameterName.LinkStatus, out programLinkResult);
            if (programLinkResult == 0)
            {
                Log.Error(string.Format("Error linking program {0}:\n{1}\n", programName, GL.GetProgramInfoLog(programID)));
                result = false;
            }

        }

        GL.DeleteShader(vShaderID);
        GL.DeleteShader(fShaderID);
        if (hasGeometryShader)
        {
            GL.DeleteShader(gShaderID);
        }

        if (result)
        {
            return programID;
        }

        GL.DeleteProgram(programID);
        return -1;
    }

    private static bool LoadShader(string fileName, int shaderID)
    {
        string shaderCode;
        try
        {
            // AssetLoader.Open throws if the embedded resource is missing
            // (it never returns null), so a misnamed/absent shader would
            // otherwise propagate an uncaught exception instead of letting
            // LoadProgram fall through to its -1 result.
            using (StreamReader sr = new StreamReader(EmbeddedData.Open(fileName)))
            {
                shaderCode = sr.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            Log.Error(string.Format("Error opening shader {0}:\n{1}\n", fileName, ex.Message));
            return false;
        }

        GL.ShaderSource(shaderID, shaderCode);
        GL.CompileShader(shaderID);
        int compileResult = 0;
        GL.GetShader(shaderID, ShaderParameter.CompileStatus, out compileResult);
        if (compileResult == 0)
        {
            Log.Error(string.Format("Error compiling shader {0}:\n{1}\n", fileName, GL.GetShaderInfoLog(shaderID)));
            return false;
        }
        return true;
    }

    // Loading is split into two phases so the heavy CPU parse can run off the UI
    // thread while the GL upload stays on the render callback (the only place the
    // GL context is current):
    //   1. ReadCCSFile(path)  - parses the file (CPU only, safe on any thread).
    //   2. InitCCSFile(file)  - creates GL resources; MUST run with the context
    //                           current (i.e. inside GlViewport's render callback).
    public static CCSFile ReadCCSFile(string fileName)
    {
        CCSFile tmpCCS = new CCSFile();
        if (tmpCCS.Read(fileName))
        {
            return tmpCCS;
        }

        Debug.WriteLine("Failed to read {0}...", fileName);
        return null;
    }

    public static CCSTreeNode InitCCSFile(CCSFile file)
    {
        if (file == null)
        {
            return null;
        }

        if (!file.Init())
        {
            return null;
        }

        CCSFileList.Add(file);
        return file.ToNode();
    }

    public static bool UnloadCCSFile(CCSFile file)
    {
        file.DeInit();
        CCSFileList.Remove(file);
        return true;
    }

    public static CCSTreeNode ToNode()
    {
        CCSTreeNode retNode = new CCSTreeNode();
        foreach (var tmpCCS in CCSFileList)
        {
            retNode.Nodes.Add(tmpCCS.ToNode());
        }

        return retNode;
    }

    public static CCSTreeNode ToSceneNode()
    {

        CCSTreeNode tmpMainAnmNode = new CCSTreeNode("Animations");
        foreach (var tmpAnmNode in ActiveAnimes)
        {
            tmpMainAnmNode.Nodes.Add(tmpAnmNode.ToNode());
        }

        return tmpMainAnmNode;
    }

    public static void Init()
    {
        //The GL context is already current (we are called from the Avalonia
        //OpenGlControlBase render/init callback), and bindings are loaded.
        WasInit = true;

        GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, BackgroundColor.W);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        //NOTE: legacy fixed-function GL.Enable(EnableCap.AlphaTest) and
        //GL.Enable(EnableCap.Texture2D) are not valid in a core profile and have
        //been dropped - the shaders handle texturing and alpha themselves.

        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        //GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcColor);

        GL.Disable(EnableCap.CullFace);

        //Init other stuff here
        AxisMarker.Init();
        WireHelper.Init();
        GridRenderer.Init();
        TexturePreview.Init();

        //PreviewCamera.Target = new Vector3(-46.28392f, -0.38321028f, -1.108414f);
    }

    public static void DeInit()
    {

        foreach (var tmpCCS in CCSFileList)
        {
            tmpCCS.DeInit();
        }
        AxisMarker.DeInit();
        WireHelper.DeInit();
        GridRenderer.DeInit();
        TexturePreview.DeInit();
    }


    private static void HandleInput()
    {
        ArcBallCamera curCamera = CurrentCamera();
        Vector3 CamTarget = curCamera.Target;

        //Check for Movement
        bool shiftMod = (ShiftModifier != KeyStatus.Up);
        if (!DefaultToAxisMovement)
        {
            shiftMod = !shiftMod;
        }

        if (shiftMod)
        {
            Vector3 movement = new Vector3(0.0f, 0.0f, 0.0f);

            Matrix4 viewMtx = curCamera.GetMatrix();
            Vector3 forward = new Vector3(viewMtx[0, 2], viewMtx[1, 2], viewMtx[2, 2]).Normalized();
            Vector3 up = new Vector3(viewMtx[0, 1], viewMtx[1, 1], viewMtx[2, 1]).Normalized();
            Vector3 right = Vector3.Cross(forward, up).Normalized();

            if (MoveForward != KeyStatus.Up)
            {

                movement += forward;
            }

            if (MoveBackward != KeyStatus.Up)
            {
                movement -= forward;
            }

            if (MoveLeft != KeyStatus.Up)
            {
                movement -= right;

            }

            if (MoveRight != KeyStatus.Up)
            {
                movement += right;
            }

            if (ControlModifier == KeyStatus.Up)
            {
                movement *= new Vector3(1.0f, 0.0f, 1.0f);
            }

            if (MoveUp != KeyStatus.Up)
            {
                movement += Vector3.UnitY;
            }

            if (MoveDown != KeyStatus.Up)
            {
                movement -= Vector3.UnitY;
            }

            CamTarget += (movement * MovementSpeed * DeltaTime);
        }
        else
        {
            if (MoveForward != KeyStatus.Up)
            {
                CamTarget.Z -= DeltaTime * MovementSpeed;
            }
            if (MoveBackward != KeyStatus.Up)
            {
                CamTarget.Z += DeltaTime * MovementSpeed;
            }
            if (MoveLeft != KeyStatus.Up)
            {
                CamTarget.X -= DeltaTime * MovementSpeed;
            }
            if (MoveRight != KeyStatus.Up)
            {
                CamTarget.X += DeltaTime * MovementSpeed;
            }
            if (MoveUp != KeyStatus.Up)
            {
                CamTarget.Y -= DeltaTime * MovementSpeed;
            }
            if (MoveDown != KeyStatus.Up)
            {
                CamTarget.Y += DeltaTime * MovementSpeed;
            }
        }
        curCamera.Target = CamTarget;

        float keyZoom = 0.0075f;
        float distToZoom = DeltaTime * keyZoom;
        if (ShiftModifier != KeyStatus.Up)
        {
            distToZoom *= 0.25f;
        }

        if (ZoomIn != KeyStatus.Up)
        {
            curCamera.Distance -= distToZoom;
        }

        if (ZoomOut != KeyStatus.Up)
        {
            curCamera.Distance += distToZoom;
        }
    }

    public static void Render()
    {
        //Context is already current; Avalonia owns MakeCurrent + buffer swap.

        /*
        if(DisplayMode == RenderMode.Wireframe)
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        }
        else
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        }
        */

        if (BackfaceCull)
        {
            GL.Enable(EnableCap.CullFace);
        }
        else
        {
            GL.Disable(EnableCap.CullFace);
        }

        Timer.Stop();
        DeltaTime = (float)Timer.Elapsed.TotalMilliseconds;
        Timer.Reset();
        Timer.Start();

        //Handle Keyboard input
        HandleInput();
        ArcBallCamera curCamera = CurrentCamera();

        //Clear. The clear colour is re-set every frame so a live theme switch
        //(BackgroundColor updated off the UI thread) takes effect immediately.
        GL.ClearColor(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z, BackgroundColor.W);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

        SetMainViewport();
        Matrix4 CamMtx = curCamera.GetMatrix();
        Matrix4 ProjViewMtx = CamMtx * ProjectionMtx;

        //ProjViewMtx *= Matrix4.CreateRotationX(90.0f);
        Matrix4 CCSMatrix = Matrix4.CreateRotationX(-90.0f * (float)Math.PI / 180.0f) * ProjViewMtx;
        //Matrix4 CCSMatrix = ProjViewMtx;
        //GL.Enable(EnableCap.Blend);
        //GL.Enable(EnableCap.DepthTest);

        Matrix4 Helper1 = Matrix4.CreateTranslation(-4.0f, 0.0f, 0.0f) * ProjViewMtx;
        Matrix4 Helper2 = Matrix4.CreateTranslation(4.0f, 0.0f, 0.0f) * ProjViewMtx;
        //LightHelper.RenderOmniHelper(Helper2, 2.0f);
        //cmesh.Render(Helper1);
        //LightHelper.RenderDirectionalHelper(Helper1);

        /*
        if(IsPreviewTexture())
        {
            CCSTexture tex = SelectedPreviewItemTag.File.GetObject<CCSTexture>(SelectedPreviewItemTag.ObjectID);
            if(tex != null)
            {
                GL.Viewport(0, 0, ViewWidth, ViewHeight);
                Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI * (45.0f / 180.0f)), ViewWidth / (float)ViewHeight, 0.1f, 100000.0f);

                float texW = (float)tex.Width;
                float texH = (float)tex.Height;
                TexturePreview.Render(proj, tex.TextureID, texW, texH);
            }
        }
        else
        {
        */
        if (DrawWorldCenter)
        {
            //AxisMarker.Render(Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f) * ProjViewMtx, 1.0f);
            RenderViewAxisGizmo(10.0f, ProjectionMtx);
        }
        if (DrawViewAxis)
        {
            SetAxisViewport();
            RenderViewAxisGizmo(1.75f, AxisProjectionMtx, true);
            SetMainViewport();
        }
        if (DrawViewGrid)
        {
            GridRenderer.Render(Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f) * ProjViewMtx, 1.0f);
        }

        //LightHelper.RenderOmniHelper(Helper2, 1.0f);

        //fuck it, frame forward animes here...
        /*
        foreach(var tmpAnime in ActiveAnimes)
        {
            //tmpAnime.FrameForward();
        }
        */

        if (SceneDisplay == SceneMode.Preview)
        {
            PreviewRender(CCSMatrix);
        }
        else if (SceneDisplay == SceneMode.Scene)
        {
            SceneRender(CCSMatrix);
        }
        else
        {
            AllRender(CCSMatrix);
        }
        //}
    }

    private static bool IsPreviewTexture()
    {
        if (SelectedPreviewItemTag == null)
        {
            return false;
        }

        if (SelectedPreviewItemTag.ObjectType != CCSFile.SECTION_TEXTURE)
        {
            return false;
        }

        return true;
    }

    public static int GetRenderMode()
    {
        int retVal = 0;
        /*
        if(DrawWireframeOverlay || (DisplayMode == RenderMode.Wireframe )) retVal |= SCENE_DRAW_LINES;
        if(DisplayMode == RenderMode.Smooth) retVal |= SCENE_DRAW_FILL;
        if(DisplayMode == RenderMode.Textured) retVal |= (SCENE_DRAW_TEXTURE | SCENE_DRAW_FILL);
        */
        if (DrawWireframe)
        {
            retVal |= SCENE_DRAW_LINES;
        }

        if (DrawVertexColors)
        {
            retVal |= SCENE_DRAW_VERTEX_COLORS;
        }

        if (DrawVertexNormals)
        {
            retVal |= SCENE_DRAW_SMOOTH;
        }

        if (DrawTextures)
        {
            retVal |= SCENE_DRAW_TEXTURE;
        }

        return retVal;
    }

    private static void AllRender(Matrix4 ProjViewMtx)
    {
        RenderAllCCS(ProjViewMtx);
    }

    private static void SceneRender(Matrix4 ProjViewMtx)
    {

        foreach (var tmpAnime in ActiveAnimes)
        {
            tmpAnime.Render(ProjViewMtx, GetRenderMode());
        }

        //RenderAllCCS(ProjViewMtx);
    }

    private static void PreviewRender(Matrix4 ProjViewMtx)
    {
        int extraOptions = GetRenderMode();

        if (SelectedPreviewItemTag != null)
        {
            if (SelectedPreviewItemTag.ObjectType == CCSFile.SECTION_CLUMP)
            {
                var tmpClump = SelectedPreviewItemTag.File.GetObject<CCSClump>(SelectedPreviewItemTag.ObjectID);
                tmpClump.Render(ProjViewMtx, extraOptions);
            }
            else if (SelectedPreviewItemTag.ObjectType == CCSFile.SECTION_OBJECT)
            {
                var tmpObj = SelectedPreviewItemTag.File.GetObject<CCSObject>(SelectedPreviewItemTag.ObjectID);
                tmpObj.ParentClump.FrameForward();
                tmpObj.Render(ProjViewMtx, extraOptions);
            }
            else if (SelectedPreviewItemTag.ObjectType == CCSFile.SECTION_MODEL)
            {
                int subNode = -1;
                if (SelectedPreviewItemTag.Type == TreeNodeTag.NodeType.SubNode)
                {
                    subNode = SelectedPreviewItemTag.SubID;
                }

                var tmpModel = SelectedPreviewItemTag.File.GetObject<CCSModel>(SelectedPreviewItemTag.ObjectID);
                tmpModel.ClumpRef.FrameForward();
                tmpModel.Render(ProjViewMtx, extraOptions, subNode);
            }
            else if (SelectedPreviewItemTag.ObjectType == CCSFile.SECTION_TEXTURE)
            {
                CCSTexture tex = SelectedPreviewItemTag.File.GetObject<CCSTexture>(SelectedPreviewItemTag.ObjectID);
                if (tex != null)
                {
                    float texW = (float)tex.Width;
                    float texH = (float)tex.Height;
                    TexturePreview.Render(ProjViewMtx, tex.TextureID, texW, texH);
                }
            }
            else if (SelectedPreviewItemTag.ObjectType == CCSFile.SECTION_ANIME)
            {
                CCSAnime tmpAnime = SelectedPreviewItemTag.File.GetObject<CCSAnime>(SelectedPreviewItemTag.ObjectID);
                if (tmpAnime != null)
                {
                    tmpAnime.Render(ProjViewMtx, extraOptions);
                }
            }
        }

    }

    private static void RenderAllCCS(Matrix4 ProjViewMtx)
    {
        int extraOptions = GetRenderMode();
        foreach (var tmpCCS in CCSFileList)
        {
            List<CCSClump> clumpList = tmpCCS.ClumpList;
            foreach (var tmpClump in clumpList)
            {
                tmpClump.Render(ProjViewMtx, extraOptions);
            }

            if (DrawCollisionMeshes)
            {
                List<CCSHitMesh> hitList = tmpCCS.HitList;
                foreach (var tmpHit in hitList)
                {
                    tmpHit.RenderAll(ProjViewMtx);
                    //tmpHit.RenderOne(ProjViewMtx, 0);
                }
            }

            if (DrawBoundingBoxes)
            {
                List<CCSBoundingBox> bboxList = tmpCCS.BBoxList;
                foreach (var tmpBBox in bboxList)
                {
                    tmpBBox.Render(ProjViewMtx);
                }
            }

            if (DrawDummyHelpers)
            {
                List<CCSDummy> dummyList = tmpCCS.DummyList;
                foreach (var tmpDummy in dummyList)
                {
                    WireHelper.RenderDummyHelper(ProjViewMtx, tmpDummy);
                }
            }
        }
    }

    public static void RenderViewAxisGizmo(float size, Matrix4 ProjMtx, bool disableDepth = false)
    {
        //SetAxisViewport();
        if (disableDepth)
        {
            GL.Disable(EnableCap.DepthTest);
        }

        ArcBallCamera curCam = CurrentCamera();
        Matrix4 ProjViewMtx = curCam.GetMatrixDistanced(size) * ProjMtx * Matrix4.Identity;
        AxisMarker.Render(Matrix4.CreateTranslation(0.0f, 0.0f, 0.0f) * ProjViewMtx, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        //SetMainViewport();
    }

    private static void SetMainViewport()
    {
        GL.Viewport(0, 0, ViewWidth, ViewHeight);
        ProjectionMtx = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI * (45.0f / 180.0f)), ViewWidth / (float)ViewHeight, 0.1f, 100000.0f);
    }

    private static void SetAxisViewport()
    {
        GL.Viewport(ViewWidth - AxisViewSize, ViewHeight - AxisViewSize, AxisViewSize, AxisViewSize);
        AxisProjectionMtx = Matrix4.CreatePerspectiveFieldOfView((float)(Math.PI * (45.0f / 180.0f)), 1.0f, 0.01f, 100000.0f);
    }

    public static void MouseMove(float mX, float mY, bool rightButton)
    {
        float dX = mX - LastMouseX;
        float dY = mY - LastMouseY;

        //ArcBallCamera CurrentCam = (SceneDisplay == SceneMode.Preview) ? PreviewCamera : SceneCamera;
        var curCam = CurrentCamera();
        Vector3 camRot = curCam.Rotation;
        Vector3 camTarget = curCam.Target;
        if (rightButton)
        {
            float dXm = MouseSensitivity * dX;
            float dYm = MouseSensitivity * dY;
            //if(ShiftModifier == KeyStatus.Up)
            //{
            // Pitch (Y) is inverted from the original: dragging up tilts the
            // far end of the scene down, and dragging down tilts it up.
            curCam.Rotation = new Vector3(camRot.X + dXm, camRot.Y - dYm, 0.0f);
            //}

            //else
            //{
            /*

            float cmx = camRot.X * dego;
            float cmy = camRot.Y * dego;
            float cmz = camRot.Z * dego;
            CurrentCam.Target = new Vector3(camTarget.X - ((float)Math.Sin(cmy) * dXm), camTarget.Y + ((float)Math.Sin(cmx) * dYm), camTarget.Z + ((float)Math.Sin(cmy) * dXm));
            */
            //float dego = 180.0f / 3.14592654f;
            //Vector3 forward = new Vector3((float)Math.Sin(camRot.X * dego), 0.0f, (float)Math.Cos(camRot.X * dego));
            // Vector3 forward = CurrentCam.Position - CurrentCam.Target;
            // forward.Normalize();
            // Vector3 right = Vector3.Cross(forward, Vector3.UnitY);
            //CurrentCam.Target -= forward * (dYm * 0.01f);
            // CurrentCam.Target += right * (dXm * 0.1f);
            //CurrentCam.Target.Z += dYm;
            //}
        }

        LastMouseX = mX;
        LastMouseY = mY;
    }

    public static void MouseWheel(float delta)
    {
        //delta is expressed in WinForms-style units (~120 per notch) by the caller.
        //ArcBallCamera CurrentCam = (SceneDisplay == SceneMode.Preview) ? PreviewCamera : SceneCamera;
        var curCam = CurrentCamera();
        float distToZoom = ((delta * MouseWheelSensitivity) * DeltaTime);
        if (ShiftModifier != KeyStatus.Up)
        {
            distToZoom *= 0.25f;
        }

        curCam.Distance += distToZoom;

    }

    public static void KeyPress(CameraKey key, bool shift, bool control)
    {
        if (key == CameraKey.Forward)
        {
            MoveForward = (MoveForward == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.Backward)
        {
            MoveBackward = (MoveBackward == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.Left)
        {
            MoveLeft = (MoveLeft == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.Right)
        {
            MoveRight = (MoveRight == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.Up)
        {
            MoveUp = (MoveUp == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.Down)
        {
            MoveDown = (MoveDown == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.ZoomIn)
        {
            ZoomIn = (ZoomIn == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
        else if (key == CameraKey.ZoomOut)
        {
            ZoomOut = (ZoomOut == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }

        if (shift)
        {
            ShiftModifier = (ShiftModifier == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }

        if (control)
        {
            ControlModifier = (ControlModifier == KeyStatus.Pressed) ? KeyStatus.Repeated : KeyStatus.Pressed;
        }
    }

    public static void KeyRelease(CameraKey key, bool shift, bool control)
    {
        if (key == CameraKey.Forward)
        {
            MoveForward = KeyStatus.Up;
        }
        else if (key == CameraKey.Backward)
        {
            MoveBackward = KeyStatus.Up;
        }
        else if (key == CameraKey.Left)
        {
            MoveLeft = KeyStatus.Up;
        }
        else if (key == CameraKey.Right)
        {
            MoveRight = KeyStatus.Up;
        }
        else if (key == CameraKey.Up)
        {
            MoveUp = KeyStatus.Up;
        }
        else if (key == CameraKey.Down)
        {
            MoveDown = KeyStatus.Up;
        }
        else if (key == CameraKey.ZoomIn)
        {
            ZoomIn = KeyStatus.Up;
        }
        else if (key == CameraKey.ZoomOut)
        {
            ZoomOut = KeyStatus.Up;
        }

        if (!shift)
        {
            ShiftModifier = KeyStatus.Up;
        }

        if (!control)
        {
            ControlModifier = KeyStatus.Up;
        }
    }


    public static ArcBallCamera CurrentCamera()
    {
        //ArcBallCamera CurrentCam = (SceneDisplay == SceneMode.Preview) ? PreviewCamera : SceneCamera;
        if (SceneDisplay == SceneMode.Preview)
        {
            return PreviewCamera;
        }

        if (SceneDisplay == SceneMode.Scene)
        {
            return SceneCamera;
        }

        return AllCamera;
    }

    public static void DumpToObj(string outputPath, bool collision, bool splitSubModels, bool splitCollision, bool withNormals, bool dummies, bool animes)
    {
        foreach (var tmpCCS in CCSFileList)
        {
            tmpCCS.DumpToObj(outputPath, collision, splitSubModels, splitCollision, withNormals, dummies);
            if (animes)
            {
                tmpCCS.DumpAnimationsToText(outputPath);
            }
        }
    }

    public static void DumpToSMD(string outputPath, bool withNormals)
    {
        foreach (var tmpCCS in CCSFileList)
        {
            tmpCCS.DumpToSMD(outputPath, withNormals);
        }
    }

    public static void DumpPreviewToSMD(string outputPath, bool withNormals)
    {
        if (Scene.SelectedPreviewItemTag.ObjectType == CCSFile.SECTION_ANIME)
        {
            CCSAnime tmpAnime = Scene.SelectedPreviewItemTag.File.GetObject<CCSAnime>(Scene.SelectedPreviewItemTag.ObjectID);
            if (tmpAnime != null)
            {
                string filename = Scene.SelectedPreviewItemTag.File.GetSubObjectName(Scene.SelectedPreviewItemTag.ObjectID);
                string fullPath = Path.Combine(outputPath, filename);

                if (!Directory.Exists(fullPath))
                {
                    if (File.Exists(fullPath))
                    {
                        Log.Error(string.Format("Error, Cannot dump CCS File, {0} exists as file", filename));
                        return;
                    }
                    else
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    tmpAnime.DumpPreviewToSMD(fullPath, withNormals);
                }
            }
        }
    }

    public static void AddAnime(CCSAnime anime)
    {
        for (int i = 0; i < ActiveAnimes.Count; i++)
        {
            var tmpAnime = ActiveAnimes[i];
            if (tmpAnime == anime)
            {
                return;
            }
        }
        ActiveAnimes.Add(anime);
    }

    public static void RemoveAnime(CCSAnime anime)
    {
        ActiveAnimes.RemoveAll(item => item == anime);
    }

}
