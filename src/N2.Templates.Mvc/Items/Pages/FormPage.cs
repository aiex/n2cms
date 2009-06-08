using N2.Details;
using N2.Templates.Mvc.Items.Items;
using N2.Web.Mvc;
using N2.Web.UI;

namespace N2.Templates.Mvc.Items.Pages
{
	[PageDefinition("Form page",
		Description = "A page with a form that can be sumitted and sent to an email address.",
		SortOrder = 240,
		IconUrl = "~/Content/Img/report.png")]
	[TabContainer(FormPage.FormTab, "Form", Tabs.ContentIndex + 2)]
	[MvcConventionTemplate("Form")]
	public class FormPage : AbstractContentPage
	{
		public const string FormTab = "formPanel";

		[EditableItem("Form", 60, ContainerName = FormTab)]
		public virtual Form Form
		{
			get { return (Form) GetChild("Form"); }
			set
			{
				if (value != null)
				{
					value.Name = "Form";
					value.AddTo(this);
				}
			}
		}
	}
}