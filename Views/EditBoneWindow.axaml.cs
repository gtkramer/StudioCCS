using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using OpenTK.Mathematics;

using StudioCCS.FileFormat;
using StudioCCS.FileFormat.Geometry;
using StudioCCS.Logging;
using StudioCCS.Rendering;

namespace StudioCCS.Views;

public partial class EditBoneWindow : Window
{
    public CCSFile OperatingFile = null;
    public CCSClump OperatingClump = null;
    public CCSObject OperatingObject = null;

    private readonly List<TextBox> _textBoxes;

    public EditBoneWindow()
    {
        InitializeComponent();

        _textBoxes = new List<TextBox>
        {
            txtPosX, txtPosY, txtPosZ,
            txtRotX, txtRotY, txtRotZ,
            txtScaleX, txtScaleY, txtScaleZ,
        };

        // Let a whole tree row toggle expand/collapse, not just the chevron.
        treeBones.Tapped += TreeViewExpand.ToggleOnTap;
    }

    public void SetClump(CCSClump clump)
    {
        OperatingClump = clump;
        OperatingFile = clump.ParentFile;
        OperatingClump.RenderBones = true;

        // Build the bone hierarchy as CCSTreeNodes (Tag = the bone) and bind it;
        // the shared CCSNodeTemplate renders it. Each node is parented under its
        // bone's parent, mirroring the clump's skeleton.
        List<CCSTreeNode> roots = new List<CCSTreeNode>();
        List<CCSTreeNode> nodes = new List<CCSTreeNode>();
        for (int i = 0; i < OperatingClump.NodeCount; i++)
        {
            var tmpBone = OperatingClump.GetObject(i);
            CCSTreeNode tmpNode = new CCSTreeNode(OperatingFile.GetSubObjectName(tmpBone.ObjectID)) { Tag = tmpBone };
            nodes.Add(tmpNode);

            int parentObjectID = tmpBone.ParentObjectID;
            if (parentObjectID != 0)
            {
                int parentNodeID = OperatingClump.SearchNodeID(parentObjectID);
                if (parentNodeID == -1)
                {
                    roots.Add(tmpNode);
                }
                else
                {
                    nodes[parentNodeID].Nodes.Add(tmpNode);
                }
            }
            else
            {
                roots.Add(tmpNode);
            }
        }

        treeBones.ItemsSource = roots;

        Title = string.Format("Edit Bones for {0}...", OperatingFile.GetSubObjectName(OperatingClump.ObjectID));
        Scene.RequestRedraw();
    }

    // Bone edits mutate the pose/bind arrays that a background scene export reads.
    // The export's modal blocks MainWindow but not this separate, unowned window,
    // so refuse edits while one runs - that's what actually keeps the two from
    // racing. (See Scene.ExportInProgress.)
    private static bool ExportBlocksEdit()
    {
        if (Scene.ExportInProgress)
        {
            Log.Warning("Ignored bone edit: a scene export is in progress.\n");
            return true;
        }

        return false;
    }

    private void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        if (ExportBlocksEdit())
        {
            return;
        }

        bool result = true;
        float[] vals = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < _textBoxes.Count; i++)
        {
            var tmpTextBox = _textBoxes[i];
            float tmpVal;
            if (!float.TryParse(tmpTextBox.Text, out tmpVal))
            {
                tmpTextBox.Background = Brushes.IndianRed;
                result = false;
                continue;
            }
            tmpTextBox.ClearValue(TextBox.BackgroundProperty);
            vals[i] = tmpVal;
        }

        if (result)
        {
            Vector3 tmpPos = new Vector3(vals[0], vals[1], vals[2]);
            Vector3 tmpRot = new Vector3(MathHelper.DegreesToRadians(vals[3]), MathHelper.DegreesToRadians(vals[4]), MathHelper.DegreesToRadians(vals[5]));
            Vector3 tmpScale = new Vector3(vals[6], vals[7], vals[8]);

            if (OperatingObject != null)
            {
                if (OperatingFile.GetVersion() == CCSFileHeader.CCSVersion.Gen1)
                {
                    OperatingClump.PosePositions[OperatingObject.NodeID] = tmpPos;
                    OperatingClump.PoseRotations[OperatingObject.NodeID] = tmpRot;
                    OperatingClump.PoseScales[OperatingObject.NodeID] = tmpScale;
                }
                else
                {
                    OperatingClump.BindPositions[OperatingObject.NodeID] = tmpPos;
                    OperatingClump.BindRotations[OperatingObject.NodeID] = tmpRot;
                    OperatingClump.BindScales[OperatingObject.NodeID] = tmpScale;
                }
            }
        }

        Scene.RequestRedraw();
    }

    private void OnBoneSelected(object sender, SelectionChangedEventArgs e)
    {
        CCSObject bone = (treeBones.SelectedItem as CCSTreeNode)?.Tag as CCSObject;
        if (bone == null)
        {
            return;
        }

        OperatingObject = bone;
        OperatingClump.SelectedBoneID = OperatingObject.NodeID;

        Vector3 tmpPos = OperatingClump.BindPositions[OperatingObject.NodeID];
        Vector3 tmpRot = OperatingClump.BindRotations[OperatingObject.NodeID];
        Vector3 tmpScale = OperatingClump.BindScales[OperatingObject.NodeID];
        if (OperatingFile.GetVersion() == CCSFileHeader.CCSVersion.Gen1)
        {
            tmpPos = OperatingClump.PosePositions[OperatingObject.NodeID];
            tmpRot = OperatingClump.PoseRotations[OperatingObject.NodeID];
            tmpScale = OperatingClump.PoseScales[OperatingObject.NodeID];
        }

        txtPosX.Text = tmpPos.X.ToString();
        txtPosY.Text = tmpPos.Y.ToString();
        txtPosZ.Text = tmpPos.Z.ToString();

        txtRotX.Text = MathHelper.RadiansToDegrees(tmpRot.X).ToString();
        txtRotY.Text = MathHelper.RadiansToDegrees(tmpRot.Y).ToString();
        txtRotZ.Text = MathHelper.RadiansToDegrees(tmpRot.Z).ToString();

        txtScaleX.Text = tmpScale.X.ToString();
        txtScaleY.Text = tmpScale.Y.ToString();
        txtScaleZ.Text = tmpScale.Z.ToString();

        lblBoneName.Text = OperatingFile.GetSubObjectName(OperatingObject.ObjectID);
        Scene.RequestRedraw();
    }

    private void OnClearRotation(object sender, RoutedEventArgs e)
    {
        if (ExportBlocksEdit())
        {
            return;
        }

        for (int i = 0; i < OperatingClump.NodeCount; i++)
        {
            if (OperatingFile.GetVersion() == CCSFileHeader.CCSVersion.Gen1)
            {
                OperatingClump.PoseRotations[i] = Vector3.Zero;
            }
            else
            {
                OperatingClump.BindRotations[i] = Vector3.Zero;
                OperatingClump.PoseQuats[i] = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);
            }
        }

        Scene.RequestRedraw();
    }

    private void OnClearScale(object sender, RoutedEventArgs e)
    {
        if (ExportBlocksEdit())
        {
            return;
        }

        for (int i = 0; i < OperatingClump.NodeCount; i++)
        {
            OperatingClump.BindScales[i] = Vector3.One;
            OperatingClump.PoseScales[i] = Vector3.One;
        }

        Scene.RequestRedraw();
    }

    private async void OnSavePose(object sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Clump Pose",
            FileTypeChoices = PoseFileTypes(),
            DefaultExtension = "bin",
        });
        if (file == null)
        {
            return;
        }

        try
        {
            OperatingClump.SavePose(file.Path.LocalPath);
            Log.Info(string.Format("Saved pose for {0} to {1}.\n", OperatingFile.GetSubObjectName(OperatingClump.ObjectID), file.Path.LocalPath));
        }
        catch (Exception ex)
        {
            Log.Error(string.Format("Failed to save pose to {0}: {1}\n", file.Path.LocalPath, ex.Message));
        }
    }

    private async void OnLoadPose(object sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Clump Pose",
            AllowMultiple = false,
            FileTypeFilter = PoseFileTypes(),
        });
        var file = files.FirstOrDefault();
        if (file == null)
        {
            return;
        }

        if (ExportBlocksEdit())
        {
            return;
        }

        try
        {
            OperatingClump.LoadPose(file.Path.LocalPath);
            Log.Info(string.Format("Loaded pose for {0} from {1}.\n", OperatingFile.GetSubObjectName(OperatingClump.ObjectID), file.Path.LocalPath));
            Scene.RequestRedraw();
        }
        catch (Exception ex)
        {
            Log.Error(string.Format("Failed to load pose from {0}: {1}\n", file.Path.LocalPath, ex.Message));
        }
    }

    private static IReadOnlyList<FilePickerFileType> PoseFileTypes()
    {
        return new[] { FileFilters.Bin, FileFilters.All };
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        if (OperatingClump != null)
        {
            OperatingClump.RenderBones = false;
            OperatingClump.SelectedBoneID = -1;
            Scene.RequestRedraw();
        }
    }
}
