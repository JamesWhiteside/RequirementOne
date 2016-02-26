<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="R1EditData.aspx.cs" Inherits="RequirementONEQuickStartWeb.R1EditData" Title="ReqOne Quick Start Processing" %>

<!DOCTYPE html>

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
          <td>Search for text</td>
          <td><asp:TextBox ID="tbText" runat="server" TextMode="MultiLine" Rows="3" /></td>
        </tr>
        <tr>
          <td>Project</td>
          <td><asp:DropDownList ID="ddlProjects" runat="server" OnSelectedIndexChanged=
            "ddlProjects_SelectedIndexChanged" AutoPostBack="true" /></td>
        </tr>
        <tr>
          <td>Specification</td>
          <td><asp:DropDownList ID="ddlSpecifications" runat="server" /></td>
          <td><asp:Button ID="btnSearchRequirements" runat="server" Text="Search"
            OnClick="btnSearchRequirements_Click" /></td>
        </tr>
        <tr>
          <td>Review</td>
          <td><asp:DropDownList ID="ddlReviews" runat="server" /></td>
          <td><asp:Button ID="btnSearchIssues" runat="server" Text="Search"
            OnClick="btnSearchIssues_Click" /></td>
        </tr>
      </table>
    </div>
    <td></td>
    <div class="stats">
      <asp:Label ID="lblStatistics" runat="server" style="font-style:italic;" />
    </div>

    <div><asp:Button ID="LoadButton" runat="server" Text="Load Data"
            OnClick="LoadData_Click" /></div>
    
    <div class="results">
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
