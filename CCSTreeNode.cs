using System.Collections.ObjectModel;
using System.Drawing;
namespace StudioCCS
{
    /// <summary>
    /// Portable stand-in for System.Windows.Forms.TreeNode. It mirrors the
    /// small slice of the TreeNode API the CCS object model relies on (Text,
    /// Tag, ForeColor, and a Nodes collection) so the original ToNode() builders
    /// port over with nothing more than a type-name change. The Avalonia UI
    /// consumes these and maps them into TreeView items.
    /// </summary>
    public class CCSTreeNode
    {
        public string Text { get; set; }
        public object Tag { get; set; }
        public Color ForeColor { get; set; } = Color.Empty;
        public CCSTreeNodeCollection Nodes { get; } = new CCSTreeNodeCollection();

        public CCSTreeNode()
        {
            Text = string.Empty;
        }

        public CCSTreeNode(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return Text ?? string.Empty;
        }
    }

    /// <summary>
    /// Observable list of child nodes that also accepts a bare string (creating a
    /// node), mirroring System.Windows.Forms.TreeNodeCollection.Add(string). It is
    /// observable so that TreeViews bound to it update when nodes are added/removed
    /// after the initial build (e.g. animations added to the scene tree).
    /// </summary>
    public class CCSTreeNodeCollection : ObservableCollection<CCSTreeNode>
    {
        public CCSTreeNode Add(string text)
        {
            var node = new CCSTreeNode(text);
            Add(node);
            return node;
        }
    }
}
