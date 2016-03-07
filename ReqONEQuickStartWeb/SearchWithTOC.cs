using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.SessionState;
using ReqOneApiReference.ReqOneApi;
using ReqOneUI;
using TreeNode = ReqOneApiReference.ReqOneApi.TreeNode;

/// <summary>
/// Summary description for Class1
/// </summary>
public class SpecificationTOC : IHttpHandler, IReadOnlySessionState
{
    //static List<RequirementDetails> RList = new List<RequirementDetails>();
    private NameValueCollection allValues;
    private bool canAdmin = false;
    

    public void ProcessRequest(HttpContext context) 
    {
        
        context.Response.ContentType = "text/plain";
        context.Response.ContentEncoding = System.Text.Encoding.UTF8;
        DataContractJsonSerializer json = null;
            
        try
        {
                      
        }
        catch
        {
            
        }
    }
    

    protected List<object> GetTableOfContents(TreeNode topNode)
    {
            var list = new List<object>();

            if(topNode != null)
            {
                
            }



            return list;
    }

        private void AddTopic(TreeNode node, int dialogId, int deleteDialogId, int newDialogId, TreeNode prevNode, TreeNode nextNode, ref List<object> list)
        {
        }

        private object GetTopicOutput(TreeNode node, int dialogId, int deleteDialogId, int createDialogId, TreeNode prevNode, TreeNode nextNode)
        {
            string nullString = null;

            var item = new
            {
                id = node.Tree_ID.ToString(),
                dialog = dialogId,
                dialogcreateaschild = createDialogId,
                canadmin = canAdmin ? "1" : "0",
                nextid = (nextNode == null ? nullString : nextNode.Tree_ID.ToString()),
                previd = (prevNode == null ? nullString : prevNode.Tree_ID.ToString()),
                hasmoveup = prevNode != null,
                hasmovedown = nextNode != null,
                parent = (node.ParentId.HasValue ? node.ParentId.Value.ToString() : nullString),
                isparent = (node.Children == null ? false : (node.Children.Length == 0 ? false : true)),
                deletedialog = deleteDialogId,
                param1 = node.Tree_ID.ToString(),
                param2 = (prevNode == null ? nullString : prevNode.Tree_ID.ToString()),
                param3 = (nextNode == null ? nullString : nextNode.Tree_ID.ToString()),
                row = new[]
                    {
                        new { caption = (string.IsNullOrEmpty(node.Name) ? "-" : (node.Name == "_root" ? General.Translate("Table of contents").ToString() : node.Name)), link = nullString },
                        new { caption = (string.IsNullOrEmpty(node.Details) ? "" : node.Details), link = nullString }
                    }
            };
            return item;
        }
        

}
    


