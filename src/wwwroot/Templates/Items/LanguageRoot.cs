﻿using System.Globalization;
using System.Web.UI.WebControls;
using N2.Engine.Globalization;
using N2.Details;
using N2.Serialization;
using N2.Web.UI;
using N2.Integrity;

namespace N2.Templates.Items
{
    [Definition("Language root", "LanguageRoot", "A starting point for translations of the start page.", "", 450)]
    [TabPanel(LanguageRoot.SiteArea, "Site", 70, AuthorizedUsers = new[] { "admin" }, AuthorizedRoles = new[] { "Administrators" })]
    [RestrictParents(typeof(StartPage))]
    [FieldSet(StartPage.MiscArea, "Miscellaneous", 80, ContainerName = LanguageRoot.SiteArea)]
    [FieldSet(StartPage.LayoutArea, "Layout", 75, ContainerName = LanguageRoot.SiteArea)]
    public class LanguageRoot : AbstractContentPage, IStructuralPage, ILanguage
	{
        public LanguageRoot()
        {
            Visible = false;
            SortOrder = 10000;
        }

        public const string SiteArea = "siteArea";
        public const string MiscArea = "miscArea";

        #region ILanguage Members

        public string FlagUrl
        {
            get
            {
                if (string.IsNullOrEmpty(LanguageCode))
                    return "";
                else
                {
                    string[] parts = LanguageCode.Split('-');
                    return string.Format("~/Edit/Globalization/flags/{0}.png", parts[parts.Length - 1]);
                }
            }
        }

        [EditableLanguagesDropDown("Language", 100, ContainerName = MiscArea)]
        public string LanguageCode
        {
            get { return (string)GetDetail("LanguageCode"); }
            set { SetDetail("LanguageCode", value); }
        }

        public string LanguageTitle
        {
            get
            {
                if (string.IsNullOrEmpty(LanguageCode))
                    return "";
                else
                    return new CultureInfo(LanguageCode).DisplayName;
            }
        }

        #endregion


        [FileAttachment, EditableImage("Top Image", 88, ContainerName = Tabs.Content, CssClass = "main")]
        public virtual string TopImage
        {
            get { return (string)(GetDetail("TopImage") ?? string.Empty); }
            set { SetDetail("TopImage", value, string.Empty); }
        }

        [FileAttachment, EditableImage("Content Image", 90, ContainerName = Tabs.Content, CssClass = "main")]
        public virtual string Image
        {
            get { return (string)(GetDetail("Image") ?? string.Empty); }
            set { SetDetail("Image", value, string.Empty); }
        }

        [EditableTextBox("Footer Text", 80, ContainerName = MiscArea, TextMode = TextBoxMode.MultiLine, Rows = 3)]
        public virtual string FooterText
        {
            get { return (string)(GetDetail("FooterText") ?? string.Empty); }
            set { SetDetail("FooterText", value, string.Empty); }
        }

        [EditableItem("Header", 100, ContainerName = SiteArea)]
        public virtual Top Header
        {
            get { return (Top)GetDetail("Header"); }
            set { SetDetail("Header", value); }
        }

        protected override string IconName
        {
            get { return "page_world"; }
        }

        public override string TemplateUrl
        {
            get { return "~/Default.aspx"; }
        }
	}
}