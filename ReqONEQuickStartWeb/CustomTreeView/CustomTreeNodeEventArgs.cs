#region Assembly System.Web.dll, v4.0.0.0
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.Web.dll
#endregion

using System;
using System.Runtime;

namespace RequirementONEQuickStartWeb
{
    // Summary:
    //     Provides data for the System.Web.UI.WebControls.TreeView.TreeNodeCheckChanged,
    //     System.Web.UI.WebControls.TreeView.TreeNodeCollapsed, System.Web.UI.WebControls.TreeView.TreeNodeDataBound,
    //     System.Web.UI.WebControls.TreeView.TreeNodeExpanded, and System.Web.UI.WebControls.TreeView.TreeNodePopulate
    //     events of the System.Web.UI.WebControls.TreeView control. This class cannot
    //     be inherited.
    public sealed class CustomTreeNodeEventArgs : EventArgs
    {
        // Summary:
        //     Initializes a new instance of the System.Web.UI.WebControls.TreeNodeEventArgs
        //     class using the specified System.Web.UI.WebControls.TreeNode object.
        //
        // Parameters:
        //   node:
        //     A System.Web.UI.WebControls.TreeNode that represents the current node when
        //     the event is raised.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public CustomTreeNodeEventArgs(CustomTreeNode node) {
            node = Node;
        }

        // Summary:
        //     Gets the node that raised the event.
        //
        // Returns:
        //     A System.Web.UI.WebControls.TreeNode that represents the node that raised
        //     the event.
        public CustomTreeNode Node { 
                get{
                    return Node;
                }
                set {
                    this.Node = Node;
                }
            }
        }
}

