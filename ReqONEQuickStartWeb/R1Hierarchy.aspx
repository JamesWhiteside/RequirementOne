<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="R1Hierarchy.aspx.cs" Inherits="RequirementONEQuickStartWeb.R1Hierarchy" Title="ReqOne Quick Start Processing" %>

<!DOCTYPE html>
<script src="https://ajax.googleapis.com/ajax/libs/jquery/2.2.0/jquery.min.js"></script>
<script src="https://ajax.googleapis.com/ajax/libs/jqueryui/1.11.4/jquery-ui.min.js"></script>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link  href="Style.css" rel="stylesheet"/>
</head>
<body>

    <form id="form1" runat="server">

      <h3>List requirements or issues</h3>

    <div class="searchCriteria">

      <table>
        <tr>
          <td class="auto-style1">Search for text</td>
          <td><asp:TextBox ID="tbText" runat="server" TextMode="MultiLine" Rows="3" /></td>
        </tr>
        <tr>
          <td class="auto-style1">Project</td>
          <td><asp:DropDownList ID="ddlProjects" runat="server" OnSelectedIndexChanged=
            "ddlProjects_SelectedIndexChanged" AutoPostBack="true" /></td>
        </tr>
        <tr>
          <td class="auto-style1">Specification</td>
          <td><asp:DropDownList ID="ddlSpecifications" runat="server" 
              OnSelectedIndexChanged="generateResults" AutoPostBack="true"/></td>
          <td class="auto-style2">&nbsp;</td>
        </tr>
        <tr>
          <td class="auto-style1">Review</td>
          <td><asp:DropDownList ID="ddlReviews" runat="server" /></td>
          <td class="auto-style2">&nbsp;</td>
        </tr>
        <tr>
            <td class="auto-style1"><asp:Button ID="Clear" runat="server" Text ="Clear Results" 
                OnClick="btnclearResults"/>
                </td>
            <td><asp:Button class="searchButton" ID="ButtonGo" runat="server" Text ="Search Files"
                OnClick="btnSearchRequirements_Click"/>
            </td>       
        </tr>
      </table>
    </div>
    <div class="stats">
        <asp:Label ID="lblStatistics" runat="server" style="font-style:italic;" />
    
    <div class="TreeTable">
        <!--THIS IS CODE FOR THE TREE TABLE -->
      <asp:TreeView ID="treeHierarchy" runat="server" ImageSet="BulletedList" NodeIndent="15" ShowCheckBoxes="All" 
          OnTreeNodeCheckChanged="nodeCheckChanged" RootNodeStyle-CssClass="searchCriteria" ShowLines="True">
        </asp:TreeView>
        
        <!--THIS IS THE END OF THE TREE TABLE -->
        <br />
    </div>
    </div>

    <div style="clear:both;"></div>
    
        <div class="results" style="width:75%">
      <br />
      <asp:ListView ID="lvSearchResults" runat="server">
        <ItemTemplate>
          <h4>
            <asp:Label ID="Label1" Text='<%# Eval("CustomId") %>' runat="server" /> - 
            <asp:HyperLink NavigateUrl='<%# Eval("Link") %>' Text='<%# Eval("Name") %>'
               runat="server" />             
          </h4>
          <asp:Label Text='<%# Eval("Details") %>' runat="server" /><br /><br /><br />
        </ItemTemplate>
      </asp:ListView>
      
    </div>
    </form>
</body>   
</html>
