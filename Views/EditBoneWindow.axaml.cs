using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using OpenTK.Mathematics;
using StudioCCS.libCCS;

namespace StudioCCS.Views
{
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

            // Build the bone hierarchy as CcsTreeNodes (Tag = the bone) and bind it;
            // the shared CcsNodeTemplate renders it. Each node is parented under its
            // bone's parent, mirroring the clump's skeleton.
            var roots = new List<CcsTreeNode>();
            var nodes = new List<CcsTreeNode>();
            for (int i = 0; i < OperatingClump.NodeCount; i++)
            {
                var tmpBone = OperatingClump.GetObject(i);
                var tmpNode = new CcsTreeNode(OperatingFile.GetSubObjectName(tmpBone.ObjectID)) { Tag = tmpBone };
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

            Debug.WriteLine(string.Format("BoneTree has {0} root nodes...", roots.Count));
            treeBones.ItemsSource = roots;

            Title = string.Format("Edit Bones for {0}...", OperatingFile.GetSubObjectName(OperatingClump.ObjectID));
        }

        private void OnUpdateClick(object sender, RoutedEventArgs e)
        {
            float radTo = 0.0174533f;
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
                Vector3 tmpRot = new Vector3(vals[3] * radTo, vals[4] * radTo, vals[5] * radTo);
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
        }

        private void OnBoneSelected(object sender, SelectionChangedEventArgs e)
        {
            var bone = (treeBones.SelectedItem as CcsTreeNode)?.Tag as CCSObject;
            if (bone == null) return;

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

            float pi = 3.14159265f;
            txtRotX.Text = (tmpRot.X * 180.0f / pi).ToString();
            txtRotY.Text = (tmpRot.Y * 180.0f / pi).ToString();
            txtRotZ.Text = (tmpRot.Z * 180.0f / pi).ToString();

            txtScaleX.Text = tmpScale.X.ToString();
            txtScaleY.Text = tmpScale.Y.ToString();
            txtScaleZ.Text = tmpScale.Z.ToString();

            lblBoneName.Text = OperatingFile.GetSubObjectName(OperatingObject.ObjectID);
        }

        private void OnClearRotation(object sender, RoutedEventArgs e)
        {
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
        }

        private void OnClearScale(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < OperatingClump.NodeCount; i++)
            {
                OperatingClump.BindScales[i] = Vector3.One;
                OperatingClump.PoseScales[i] = Vector3.One;
            }
        }

        private async void OnSavePose(object sender, RoutedEventArgs e)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Clump Pose",
                FileTypeChoices = PoseFileTypes(),
                DefaultExtension = "bin",
            });
            if (file == null) return;

            OperatingClump.SavePose(file.Path.LocalPath);
            Logger.LogInfo(string.Format("Saved pose for {0} to {1}.\n", OperatingFile.GetSubObjectName(OperatingClump.ObjectID), file.Path.LocalPath));
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
            if (file == null) return;

            OperatingClump.LoadPose(file.Path.LocalPath);
            Logger.LogInfo(string.Format("Loaded pose for {0} from {1}.\n", OperatingFile.GetSubObjectName(OperatingClump.ObjectID), file.Path.LocalPath));
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
            }
        }
    }
}
