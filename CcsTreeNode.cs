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
    public class CcsTreeNode
    {
        public string Text { get; set; }
        public object Tag { get; set; }
        public Color ForeColor { get; set; } = Color.Empty;
        public CcsTreeNodeCollection Nodes { get; } = new CcsTreeNodeCollection();

        public CcsTreeNode()
        {
            Text = string.Empty;
        }

        public CcsTreeNode(string text)
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
    public class CcsTreeNodeCollection : ObservableCollection<CcsTreeNode>
    {
        public CcsTreeNode Add(string text)
        {
            var node = new CcsTreeNode(text);
            Add(node);
            return node;
        }
    }
}
