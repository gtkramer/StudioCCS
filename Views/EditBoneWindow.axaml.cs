using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using OpenTK.Mathematics;
using StudioCCS.libCCS;

namespace StudioCCS.Views
{
    public partial class EditBoneWindow : Window
    {
        public class BoneNodeTag
        {
            public CCSObject Bone;
        }

        public CCSFile OperatingFile = null;
        public CCSClump OperatingClump = null;
        public CCSObject OperatingObject = null;

        private TreeView _treeBones;
        private TextBlock _lblBoneName;
        private List<TextBox> _textBoxes;
        private TextBox _txtPosX, _txtPosY, _txtPosZ;
        private TextBox _txtRotX, _txtRotY, _txtRotZ;
        private TextBox _txtScaleX, _txtScaleY, _txtScaleZ;

        public EditBoneWindow()
        {
            InitializeComponent();

            _treeBones = this.FindControl<TreeView>("treeBones");
            _lblBoneName = this.FindControl<TextBlock>("lblBoneName");
            _txtPosX = this.FindControl<TextBox>("txtPosX");
            _txtPosY = this.FindControl<TextBox>("txtPosY");
            _txtPosZ = this.FindControl<TextBox>("txtPosZ");
            _txtRotX = this.FindControl<TextBox>("txtRotX");
            _txtRotY = this.FindControl<TextBox>("txtRotY");
            _txtRotZ = this.FindControl<TextBox>("txtRotZ");
            _txtScaleX = this.FindControl<TextBox>("txtScaleX");
            _txtScaleY = this.FindControl<TextBox>("txtScaleY");
            _txtScaleZ = this.FindControl<TextBox>("txtScaleZ");

            _textBoxes = new List<TextBox>
            {
                _txtPosX, _txtPosY, _txtPosZ,
                _txtRotX, _txtRotY, _txtRotZ,
                _txtScaleX, _txtScaleY, _txtScaleZ,
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetClump(CCSClump clump)
        {
            OperatingClump = clump;
            OperatingFile = clump.ParentFile;
            OperatingClump.RenderBones = true;

            string clumpName = OperatingFile.GetSubObjectName(OperatingClump.ObjectID);

            var mainNodes = new List<TreeViewItem>();
            var nodes = new List<TreeViewItem>();
            for (int i = 0; i < OperatingClump.NodeCount; i++)
            {
                var tmpBone = OperatingClump.GetObject(i);
                var tmpNode = new TreeViewItem
                {
                    Header = OperatingFile.GetSubObjectName(tmpBone.ObjectID),
                    Tag = new BoneNodeTag { Bone = tmpBone },
                };
                nodes.Add(tmpNode);

                int parentObjectID = tmpBone.ParentObjectID;
                if (parentObjectID != 0)
                {
                    int parentNodeID = OperatingClump.SearchNodeID(parentObjectID);
                    if (parentNodeID == -1)
                    {
                        mainNodes.Add(tmpNode);
                    }
                    else
                    {
                        ((IList)nodes[parentNodeID].Items).Add(tmpNode);
                    }
                }
                else
                {
                    mainNodes.Add(tmpNode);
                }
            }

            Debug.WriteLine(string.Format("BoneTree has {0} Nodes...", mainNodes.Count));
            foreach (var tmpNode in mainNodes)
            {
                ((IList)_treeBones.Items).Add(tmpNode);
            }

            Title = string.Format("Edit Bones for {0}...", clumpName);
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
            var tmpNode = _treeBones.SelectedItem as TreeViewItem;
            if (tmpNode == null) return;
            var tmpTag = tmpNode.Tag as BoneNodeTag;
            if (tmpTag != null)
            {
                OperatingObject = tmpTag.Bone;
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

                _txtPosX.Text = tmpPos.X.ToString();
                _txtPosY.Text = tmpPos.Y.ToString();
                _txtPosZ.Text = tmpPos.Z.ToString();

                float pi = 3.14159265f;
                _txtRotX.Text = (tmpRot.X * 180.0f / pi).ToString();
                _txtRotY.Text = (tmpRot.Y * 180.0f / pi).ToString();
                _txtRotZ.Text = (tmpRot.Z * 180.0f / pi).ToString();

                _txtScaleX.Text = tmpScale.X.ToString();
                _txtScaleY.Text = tmpScale.Y.ToString();
                _txtScaleZ.Text = tmpScale.Z.ToString();

                _lblBoneName.Text = OperatingFile.GetSubObjectName(OperatingObject.ObjectID);
            }
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
            return new[]
            {
                new FilePickerFileType("Bin Files") { Patterns = new[] { "*.bin" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
            };
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
