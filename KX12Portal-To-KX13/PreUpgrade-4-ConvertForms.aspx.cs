using CMS.Base;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.EventLog;
using CMS.Helpers;
using CMS.Membership;
using CMS.OnlineForms;
using CMS.SiteProvider;
using CMS.UIControls;
using CMS.WorkflowEngine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using TreeNode = CMS.DocumentEngine.TreeNode;

public partial class CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_ConvertForms : CMSPage
{
    public CMSPages_UpgradeToKX13Operations_PreUpgradeOperations_ConvertForms()
    {

    }


    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);

        var formControls = new List<string>();

        foreach(var classObj in DataClassInfoProvider.GetClasses()
            .Where("ClassID in (select FormClassid from CMS_Form)")
            .TypedResult)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(classObj.ClassFormDefinition);
            foreach(XmlNode controlName in doc.SelectNodes("//controlname"))
            {
                formControls.Add(controlName.InnerText.ToLower());
            }
        }
        formControls = formControls.Distinct().OrderBy(x => x).ToList();



        string config = string.Empty;
        foreach(string control in formControls)
        {
            switch(control)
            {
                case "checkboxcontrol":
                    config += $"{control}=CheckBoxComponent\n";
                    break;
                case "dropdownlistcontrol":
                    config += $"{control}=DropDownComponent\n";
                    break;
                case "emailinput":
                    config += $"{control}=EmailInputComponent\n";
                    break;
                case "htmlareacontrol":
                case "textareacontrol":
                    config += $"{control}=TextAreaComponent\n";
                    break;
                case "radiobuttonscontrol":
                    config += $"{control}=RadioButtonsComponent\n";
                    break;
                case "multiplechoicecontrol":
                    config += $"{control}=MultipleChoiceComponent\n";
                    break;
                case "simplecaptcha":
                    config += $"{control}=RecaptchaComponent\n";
                    break;
                case "textboxcontrol":
                    config += $"{control}=TextInputComponent\n";
                    break;
                case "uploadcontrol":
                    config += $"{control}=FileUploaderComponent\n";
                    break;
                case "usphone":
                    config += $"{control}=USPhoneComponent\n";
                    break;
                case "uszipcode":
                    config += $"{control}=TextInputComponent\n";
                    break;
                default:
                    config += $"{control}=\n";
                    break;
            }
        }
        txtConfig.Rows = formControls.Count();
        txtConfig.Text += config;
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }



    protected void btnConvert_Click(object sender, EventArgs e)
    {
        Dictionary<string, string> oldToNew = new Dictionary<string, string>();
        foreach(string configLine in txtConfig.Text.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
        {
            if (!configLine.Contains('=') || configLine.Split('=')[1].Length == 0)
            {
                throw new Exception("Must have every configuration for each control");
            }
            oldToNew.Add(configLine.Split('=')[0], configLine.Split('=')[1]);
        }

        foreach(var form in BizFormInfoProvider.GetBizForms())
        {
            var classObj = DataClassInfoProvider.GetDataClassInfo(form.FormClassID);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(classObj.ClassFormDefinition);


            // Loop through Forms and generate Form Builder json
            string formBuilderJson = $"{{    \"editableAreas\": [      {{        \"identifier\": \"{txtDefaultSectionIdentifier.Text}\",        \"sections\": [          {{            \"identifier\": \"{Guid.NewGuid()}\",            \"type\": \"{txtDefaultSectionType.Text}\",            \"zones\": [              {{                \"identifier\": \"{Guid.NewGuid()}\",                \"formComponents\": [                  ";
            List<string> formElements = new List<string>();
            foreach(XmlNode fieldNode in xmlDoc.SelectNodes("//field[@visible='true']"))
            {
                var identifier = fieldNode.Attributes["guid"].Value;
                formElements.Add($"{{                    \"identifier\": \"{identifier}\"                  }}");

                // now update the Field Settings to set the controlname to unknown and add the componentidentifier
                var controlNode = fieldNode.SelectSingleNode("./settings/controlname");
                string oldName = controlNode.InnerText;
                controlNode.InnerText = "unknown";
                var settingsNode = fieldNode.SelectSingleNode("./settings");
                var componentidentifierNode = xmlDoc.CreateNode(XmlNodeType.Element, "componentidentifier", xmlDoc.NamespaceURI);
                componentidentifierNode.InnerText = oldToNew[oldName.ToLower()];
                settingsNode.AppendChild(componentidentifierNode);
            }
            formBuilderJson += string.Join(",", formElements);
            formBuilderJson += $"                ]              }}            ]          }}        ]      }}    ]  }}";

            // Save the form as an MVC
            form.FormBuilderLayout = formBuilderJson;
            form.FormDevelopmentModel = 1;
            BizFormInfoProvider.SetBizFormInfo(form);

            // Update the class to have the proper identifier info
            classObj.ClassFormDefinition = xmlDoc.OuterXml;
            DataClassInfoProvider.SetDataClassInfo(classObj);
        }
        CacheHelper.ClearCache();
        SystemHelper.RestartApplication();
        ltrResult.Text = "<div class='alert alert-info'>Operations successful</div>";
    }
}
