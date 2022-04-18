<%@ Page Language="C#" AutoEventWireup="true" CodeFile="PreUpgrade-3-ConvertPageTypesForMVC.aspx.cs" Inherits="CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_ConvertPageTypesForMVC" Theme="Default" MasterPageFile="~/CMSMasterPages/LiveSite/SimplePage.master" %>

<asp:Content ID="cnt" ContentPlaceHolderID="plcContent" runat="server">
    <style>
        select, input[type='text'] {
            display: inline-block !important;
            width: 200px !important;
        }
        ul {
            padding-left: 0;
            list-style: none;
        }
    </style>
    <div>
        <asp:Literal ID="ltrResult" runat="server" />
    </div>

    <div>
        <h1>Pre Upgrade Operations #3: Page Type Conversion for MVC</h1>
        <p>
            During the upgrade from 12 to 13, the upgrade tool enables / disables certain features (such as URL or page builder) depending on how these page types are configured.
        </p>
        <p>This operation will make sure your pages will be properly set and converted to MVC format.</p>
        <p>These operations are done purely in the database, and should only be done when you are going to upgrade as these WILL break the site.  Backup before and during each operation!</p>

        <asp:Panel runat="server" ID="pnlFirstRun">
        <h2>Enable Page Builder and set Default Template</h2>
        <p>
            The below page types that need to be converted to MVC.  Please select from the drop down the operation you wish, and optionally if Page Builder is to be enabled, a default MVC Page Template Identifier (ex "My.PageType_Default").  Leave empty if you do not plan on using page templates for that page type.
        </p>

        <asp:Panel runat="server" ID="pnlEnablePageBuilder">
        </asp:Panel>

        <asp:Button runat="server" ID="btnAdjustClasses" OnClick="btnAdjustClasses_Click" Text="Adjust Classes"  CssClass="btn btn-primary"/>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlSecondRun">
        <h2>Resolve Url-less Parents of Url Enabled Pages</h2>
        <p>
            Pages without URL representation by default will not be calculated in the Url of any child pages that do have Url Enabled.  This deviates from standard behavior of the NodeAliasPath sourced Url.
        </p>
        <p>
            Additionally, Page Types that are containers cannot have URL enabled.  
        </p>
            <p>
            Below are page types (both containered and content only) that have Url feature disabled but have children that have the Url Feature Enabled.  You can enable Url Feature for these page types to resolve url mismatch issues when upgrading.
        </p>
             <asp:Panel runat="server" ID="pnlEnableUrlForParents">
        </asp:Panel>


        <asp:Button runat="server" ID="btnEnableUrlFeature" OnClick="btnEnableUrlFeature_Click" Text="Adjust Classes"  CssClass="btn btn-primary"/>
        </asp:Panel>
    </div>
</asp:Content>
