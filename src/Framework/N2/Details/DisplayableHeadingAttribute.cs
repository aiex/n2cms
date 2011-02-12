using System;
using N2.Web.UI.WebControls;

namespace N2.Details
{
	/// <summary>Associate a property/detail with a literal used for presentation.</summary>
	public class DisplayableHeadingAttribute : DisplayableAttribute, IWritingDisplayable
	{
		public DisplayableHeadingAttribute(int headingLevel) : base (typeof(Hn), "Text")
		{
			HeadingLevel = headingLevel;
		}

		/// <summary>The heading level for the display (1-6)</summary>
		public int HeadingLevel { get; set; }

		public override System.Web.UI.Control AddTo(ContentItem item, string detailName, System.Web.UI.Control container)
		{
			var heading = (Hn)base.AddTo(item, detailName, container);
			heading.Level = HeadingLevel;
			return heading;
		}

		#region IWritingDisplayable Members

		public void Write(ContentItem item, string propertyName, System.IO.TextWriter writer)
		{
			var value = item[propertyName] as string;
			if (value != null)
				writer.Write("<h" + HeadingLevel + ">" + value + "</h" + HeadingLevel + ">");
		}

		#endregion
	}
}