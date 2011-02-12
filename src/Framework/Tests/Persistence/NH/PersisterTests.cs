using System;
using System.Linq;
using N2.Tests.Persistence.Definitions;
using NUnit.Framework;
using System.Diagnostics;
using N2.Definitions;
using N2.Persistence;
using NHibernate.Tool.hbm2ddl;
using N2.Persistence.NH.Finder;
using N2.Tests.Fakes;
using N2.Persistence.NH;
using N2.Collections;
using N2.Details;
using System.Collections;
using NHibernate.Engine;
using N2.Edit.Workflow;

namespace N2.Tests.Persistence.NH
{
	[TestFixture]
	public class PersisterTests : PersisterTestsBase
	{
		[Test]
		public void CanSave()
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "saveableRoot", null);
			persister.Save(item);
			Assert.AreNotEqual(0, item.ID);
		}

		[Test, Ignore]
		public void Get_Children_AreEagerlyFetched()
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "gettableRoot", null);
			ContentItem child = CreateOneItem<Definitions.PersistableItem1>(0, "gettableChild", item);
			using (persister)
			{
				persister.Save(item);
			}

			ContentItem storedItem = persister.Get(item.ID);
			persister.Dispose();

			Assert.That(storedItem.Children.Count, Is.EqualTo(1));
		}

		[Test]
		public void SavingItemWithEmptyName_NameIsSetToNull()
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "", null);

			persister.Save(item);

			Assert.AreEqual(item.ID.ToString(), item.Name);
		}

		[Test]
		public void CanUpdate()
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "updatableRoot", null);

			using (persister)
			{
				item["someproperty"] = "hello";
				persister.Save(item);

				item["someproperty"] = "world";
				persister.Save(item);
			}
			using (persister)
			{
				item = persister.Get(item.ID);
				Assert.AreEqual("world", item["someproperty"]);
			}
		}

		[Test]
		public void CanDelete()
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);

			using (persister)
			{
				persister.Save(item);
				persister.Delete(item);
			}
			using (persister)
			{
				item = persister.Get(item.ID);
				Assert.IsNull(item, "Item should have been null.");
			}
		}

		[Test]
		public void CanMove()
		{
			ContentItem root = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);
			ContentItem item1 = CreateOneItem<Definitions.PersistableItem1>(0, "item1", root);
			ContentItem item2 = CreateOneItem<Definitions.PersistableItem1>(0, "item2", root);

			using (persister)
			{
				persister.Save(root);
				persister.Save(item1);
				persister.Save(item2);
			}

			using (persister)
			{
				root = persister.Get(root.ID);
				item1 = persister.Get(item1.ID);
				item2 = persister.Get(item2.ID);

				persister.Move(item2, item1);
			}

			using (persister)
			{
				root = persister.Get(root.ID);
				item1 = persister.Get(item1.ID);
				item2 = persister.Get(item2.ID);

				Assert.AreEqual(1, root.Children.Count);
				Assert.AreEqual(1, item1.Children.Count);
				Assert.AreEqual(item1, item2.Parent);
			}
		}

		[Test]
		public void CanCopy()
		{
			ContentItem root = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);
			ContentItem item1 = CreateOneItem<Definitions.PersistableItem1>(0, "item1", root);
			ContentItem item2 = CreateOneItem<Definitions.PersistableItem1>(0, "item2", root);

			using (persister)
			{
				persister.Save(root);
				persister.Save(item1);
				persister.Save(item2);
			}

			using (persister)
			{
				root = persister.Get(root.ID);
				item1 = persister.Get(item1.ID);
				item2 = persister.Get(item2.ID);

				persister.Copy(item2, item1);
			}

			using (persister)
			{
				root = persister.Get(root.ID);
				item1 = persister.Get(item1.ID);
				item2 = persister.Get(item2.ID);

				Assert.AreEqual(2, root.Children.Count);
				Assert.AreEqual(1, item1.Children.Count);
				Assert.AreNotEqual(root, item1.Children[0]);
				Assert.AreNotEqual(item1, item1.Children[0]);
				Assert.AreNotEqual(item2, item1.Children[0]);
			}
		}



		[Test]
		public void CanChange_SaveAction()
		{
			ContentItem itemToSave = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);

			using (persister)
			{
				ContentItem invokedItem = null;
				EventHandler<CancellableItemEventArgs> handler = delegate(object sender, CancellableItemEventArgs e)
				{
					e.FinalAction = delegate(ContentItem item) { invokedItem = item; };
				};
				persister.ItemSaving += handler;
				persister.Save(itemToSave);
				persister.ItemSaving -= handler;

				Assert.That(invokedItem, Is.EqualTo(itemToSave));
			}
		}

		[Test]
		public void CanChange_DeleteAction()
		{
			ContentItem itemToDelete = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);

			using (persister)
			{
				ContentItem invokedItem = null;
				EventHandler<CancellableItemEventArgs> handler = delegate(object sender, CancellableItemEventArgs e)
				{
					e.FinalAction = delegate(ContentItem item) { invokedItem = item; };
				};
				persister.ItemDeleting += handler;
				persister.Delete(itemToDelete);
				persister.ItemDeleting -= handler;

				Assert.That(invokedItem, Is.EqualTo(itemToDelete));
			}
		}

		[Test]
		public void CanChange_MoveAction()
		{
			ContentItem source = CreateOneItem<Definitions.PersistableItem1>(0, "source", null);
			ContentItem destination = CreateOneItem<Definitions.PersistableItem1>(0, "destination", null);

			using (persister)
			{
				ContentItem invokedFrom = null;
				ContentItem invokedTo = null;
				EventHandler<CancellableDestinationEventArgs> handler = delegate(object sender, CancellableDestinationEventArgs e)
				{
					e.FinalAction = delegate(ContentItem from, ContentItem to)
					{
						invokedFrom = from;
						invokedTo = to;
						return null;
					};
				};
				persister.ItemMoving += handler;
				persister.Move(source, destination);
				persister.ItemMoving -= handler;

				Assert.That(invokedFrom, Is.EqualTo(source));
				Assert.That(invokedTo, Is.EqualTo(destination));
			}
		}

		[Test]
		public void CanChange_CopyAction()
		{
			ContentItem source = CreateOneItem<Definitions.PersistableItem1>(0, "source", null);
			ContentItem destination = CreateOneItem<Definitions.PersistableItem1>(0, "destination", null);

			using (persister)
			{
				ContentItem invokedFrom = null;
				ContentItem invokedTo = null;
				ContentItem copyToReturn = CreateOneItem<Definitions.PersistableItem1>(0, "copied", null);
				EventHandler<CancellableDestinationEventArgs> handler = delegate(object sender, CancellableDestinationEventArgs e)
				{
					e.FinalAction = delegate(ContentItem from, ContentItem to)
					{
						invokedFrom = from;
						invokedTo = to;
						return copyToReturn;
					};
				};
				persister.ItemCopying += handler;
				ContentItem copy = persister.Copy(source, destination);
				persister.ItemCopying -= handler;

				Assert.That(copy, Is.SameAs(copyToReturn));
				Assert.That(invokedFrom, Is.EqualTo(source));
				Assert.That(invokedTo, Is.EqualTo(destination));
			}
		}

		[Test]
		public void CanSave_Guid()
		{
			PersistableItem1 item = CreateOneItem<PersistableItem1>(0, "root", null);
			PersistableItem1 fromDB = null;

			item.GuidProperty = Guid.NewGuid();
			using (persister)
			{
				persister.Save(item);
			}

			fromDB = persister.Get<PersistableItem1>(item.ID);

			Assert.That(fromDB.GuidProperty, Is.EqualTo(item.GuidProperty));
		}

		[Test]
		public void CanSave_ReadOnlyGuid()
		{
			PersistableItem1 item = CreateOneItem<PersistableItem1>(0, "root", null);
			PersistableItem1 fromDB = null;
			string guid = item.ReadOnlyGuid;

			using (persister)
			{
				persister.Save(item);
			}

			fromDB = persister.Get<PersistableItem1>(item.ID);

			Assert.That(fromDB.ReadOnlyGuid, Is.EqualTo(guid));
		}

		[Test]
		public void CanSave_WritableGuid()
		{
			PersistableItem1 item = CreateOneItem<PersistableItem1>(0, "root", null);
			PersistableItem1 fromDB = null;

			string guid = item.WritableGuid;
			item.WritableGuid = guid;
			using (persister)
			{
				persister.Save(item);
			}

			fromDB = persister.Get<PersistableItem1>(item.ID);

			Assert.That(fromDB.WritableGuid, Is.EqualTo(guid));
		}

		[Test]
		public void Laziness()
		{
			ContentItem root = CreateOneItem<PersistableItem1>(0, "root", null);
			ContentItem root2 = CreateOneItem<PersistableItem1>(0, "root2", null);
			for (int i = 0; i < 30; i++)
			{
				PersistableItem1 item = CreateOneItem<PersistableItem1>(0, "item", root);
			}
			using (persister)
			{
				persister.Save(root);
				persister.Save(root2);
			}
			using (persister)
			{
				root = persister.Get(root.ID);
				Debug.WriteLine("Got: " + root + " with Children.Count: " + root.Children.Count);
				foreach (var child in root.Children)
				{
				}
				root2 = persister.Get(root2.ID);
				Debug.WriteLine("Got: " + root2 + " with Children.Count: " + root2.Children.Count);
				foreach (var child in root2.Children)
				{
				}
			}
		}

		[Test]
		public void Save_CausesSortOrder_ToBeUpdated()
		{
			ContentItem parent = CreateOneItem<Definitions.PersistableItem1>(0, "parent", null);
			persister.Save(parent);

			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "child1", parent);
			persister.Save(child1);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "child2", parent);
			persister.Save(child2);
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "child3", parent);
			persister.Save(child3);

			Assert.That(child1.SortOrder, Is.LessThan(child2.SortOrder));
			Assert.That(child2.SortOrder, Is.LessThan(child3.SortOrder));
		}

		[Test]
		public void Save_OnParentWith_SortChildrenByUnordered_CausesSortOrder_NotToBeUpdated()
		{
			ContentItem parent = CreateOneItem<Definitions.NonVirtualItem>(0, "parent", null);
			persister.Save(parent);

			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "child1", parent);
			persister.Save(child1);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "child2", parent);
			persister.Save(child2);
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "child3", parent);
			persister.Save(child3);

			Assert.That(child1.SortOrder, Is.EqualTo(0));
			Assert.That(child2.SortOrder, Is.EqualTo(0));
			Assert.That(child3.SortOrder, Is.EqualTo(0));
		}

		[Test]
		public void Save_OnParentWith_SortChildren_ByExpression_NameDesc_CausesChildrenToBeReordered()
		{
			ContentItem parent = CreateOneItem<Definitions.PersistableItem2>(0, "parent", null);
			persister.Save(parent);

			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "child1", parent);
			persister.Save(child1);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "child2", parent);
			persister.Save(child2);
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "child3", parent);
			persister.Save(child3);

			Assert.That(child1.SortOrder, Is.GreaterThan(child2.SortOrder));
			Assert.That(child2.SortOrder, Is.GreaterThan(child3.SortOrder));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Children_CanBePaged(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "gettableRoot", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "three", item);
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var first = item.Children.FindRange(0, 1);
				var second = item.Children.FindRange(1, 1);
				var third = item.Children.FindRange(2, 1);
				var none = item.Children.FindRange(3, 1);
				var beginning = item.Children.FindRange(0, 2);
				var ending = item.Children.FindRange(2, 2);

				Assert.That(first.Single(), Is.EqualTo(child1));
				Assert.That(second.Single(), Is.EqualTo(child2));
				Assert.That(third.Single(), Is.EqualTo(child3));
				Assert.That(none.Any(), Is.False);
				Assert.That(beginning.Count(), Is.EqualTo(2));
				Assert.That(beginning.First(), Is.EqualTo(child1));
				Assert.That(beginning.Last(), Is.EqualTo(child2));
				Assert.That(ending.Count(), Is.EqualTo(1));
				Assert.That(ending.First(), Is.EqualTo(child3));

				Assert.That(item.Children.Count, Is.EqualTo(3));
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Children_CanBe_FoundByZone(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "gettableRoot", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			child1.ZoneName = "First";
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			child2.ZoneName = "Second";
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "three", item);
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var nozone = item.Children.FindParts(null);
				var emptyzone = item.Children.FindParts("");
				var first = item.Children.FindParts("First");
				var second = item.Children.FindParts("Second");
				var third = item.Children.FindParts("Third");

				Assert.That(nozone.Single(), Is.EqualTo(child3));
				Assert.That(emptyzone.Count, Is.EqualTo(0));
				Assert.That(first.Single(), Is.EqualTo(child1));
				Assert.That(second.Single(), Is.EqualTo(child2));
				Assert.That(third.Count, Is.EqualTo(0));
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void ZoneNames_CanBeFound(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "gettableRoot", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			child1.ZoneName = "TheZone";
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			child2.ZoneName = "TheZone";
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "three", item);
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var zones = item.Children.FindZoneNames();

				Assert.That(zones.Count, Is.EqualTo(2));
				Assert.That(zones.Contains(null));
				Assert.That(zones.Contains("TheZone"));
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Children_WhichArePages_CanBeFound(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "gettableRoot", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			child2.ZoneName = "Zone";
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var pages = item.Children.FindPages();

				Assert.That(pages.Single(), Is.EqualTo(child1));
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Children_WhichAreParts_CanBeFound(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "gettableRoot", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			child2.ZoneName = "Zone";
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var pages = item.Children.FindParts();

				Assert.That(pages.Single(), Is.EqualTo(child2));
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Children_CanBe_FoundByName(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "three", item);
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var nullname = item.Children.FindNamed(null);
				var emptyname = item.Children.FindNamed("");
				var rootname = item.Children.FindNamed("root");
				var first = item.Children.FindNamed("one");
				var second = item.Children.FindNamed("two");
				var third = item.Children.FindNamed("three");
				
				Assert.That(nullname, Is.Null);
				Assert.That(emptyname, Is.Null);
				Assert.That(rootname, Is.Null);
				Assert.That(first, Is.EqualTo(child1));
				Assert.That(second, Is.EqualTo(child2));
				Assert.That(third, Is.EqualTo(child3));
			}
		}

		[TestCase(true)]
		[TestCase(false)]
		public void Children_CanBeQueried(bool forceInitialize)
		{
			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);
			ContentItem child1 = CreateOneItem<Definitions.PersistableItem1>(0, "one", item);
			ContentItem child2 = CreateOneItem<Definitions.PersistableItem1>(0, "two", item);
			ContentItem child3 = CreateOneItem<Definitions.PersistableItem1>(0, "three", item);
			using (persister)
			{
				persister.Save(item);
			}

			using (persister)
			{
				item = persister.Get(item.ID);

				if (forceInitialize)
				{
					var temp = item.Children[0]; // initilze
				}

				var one = item.Children.Query().Where(i => i.Name == "one").ToList();
				var notone = item.Children.Query().Where(i => i.Name != "one").ToList();
				var containso = item.Children.Query().Where(i => i.Name.Contains("o")).ToList();

				Assert.That(one.Single(), Is.EqualTo(child1));
				Assert.That(notone.Count(), Is.EqualTo(2));
				Assert.That(notone.Any(i => i == child1), Is.False);
				Assert.That(containso.Count(), Is.EqualTo(2));
				Assert.That(containso.Contains(child1), Is.True);
				Assert.That(containso.Contains(child2), Is.True);
			}
		}

		[Test]
		public void NHibernateSearch_OnTitle()
		{
			var s = NHibernate.Search.Search.CreateFullTextSession(sessionProvider.OpenSession.Session);

			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);
			item.Title = "hello world";
			persister.Save(item);

			var results = s.CreateFullTextQuery<ContentItem>("Title:hello").List();
			Assert.That(results.Count, Is.GreaterThanOrEqualTo(1));
			Assert.That(results.Contains(item));
		}

		[Test]
		public void NHibernateSearch_OnDetail()
		{
			var s = NHibernate.Search.Search.CreateFullTextSession(sessionProvider.OpenSession.Session);

			ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "root", null);
			item.Title = "hello world";
			item["Hej"] = "V�rlden";
			persister.Save(item);

			var results = s.CreateFullTextQuery<ContentItem>("Details.StringValue:V�rlden").List();
			Assert.That(results.Count, Is.GreaterThanOrEqualTo(1));
			Assert.That(results.Contains(item));
		}

		[Test]
		public void NHibernateSearch_X()
		{
			//var s = NHibernate.Search.Search.CreateFullTextSession(sessionProvider.OpenSession.Session);

			//ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "saveableRoot", null);
			//item.Title = "hello world";
			//item.Name = "hello-world";
			//item.Published = DateTime.Now;
			//item.Expires = DateTime.Now.AddDays(1);
			//item.SavedBy = "admin";
			//item.State = ContentState.New;
			//item["Hej"] = "V�rlden";
			//persister.Save(item);

			//var results = s.CreateFullTextQuery<ContentItem>("Title:hello").List();
			//Assert.That(results.Count, Is.GreaterThanOrEqualTo(1));
			//Assert.That(results.Contains(item));
		}

		//[Test]
		//public void EagerDetails()
		//{
		//    ContentItem item = CreateOneItem<Definitions.PersistableItem1>(0, "item", null);
		//    item["Hello"] = "world";
		//    item.GetDetailCollection("World", true).Add("Hello");
		//    using(persister)
		//    {
		//        persister.Save(item);
		//    }
		//    using(persister)
		//    {
		//        var mq = sessionProvider.OpenSession.Session.CreateMultiQuery()
		//            .Add("item", sessionProvider.OpenSession.Session.CreateQuery("from ContentItem where ID=:id").SetParameter("id", item.ID))
		//            .Add("details", sessionProvider.OpenSession.Session.CreateQuery("select ci.Details from ContentItem ci where ci.ID=:id").SetParameter("id", item.ID))
		//            .Add("collections", sessionProvider.OpenSession.Session.CreateQuery("select dc from DetailCollection dc where dc.EnclosingItem.ID=:id join fetch dc.Details").SetParameter("id", item.ID))
		//            .SetCacheable(true);
		//        item = (ContentItem)((IList)mq.GetResult("item"))[0];
		//        item.Details = new PersistentContentList<ContentDetail>((ISessionImplementor)sessionProvider.OpenSession.Session, ((IList)mq.GetResult("details")).Cast<ContentDetail>().ToList()) { Owner = item };
		//        item.DetailCollections = new PersistentContentList<DetailCollection>((ISessionImplementor)sessionProvider.OpenSession.Session, ((IList)mq.GetResult("collections")).Cast<DetailCollection>().ToList()) { Owner = item };

		//        //item["Added"] = "B there";
		//        //item.DetailCollections["World"].Add("B there 2");
		//        //item.GetDetailCollection("AddedCollection", true).Add("B there 3");
		//        persister.Save(item);
		//    }
		//}
	}
}