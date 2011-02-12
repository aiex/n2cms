﻿using System;
using System.Configuration;
using N2.Configuration;
using N2.Definitions;
using N2.Details;
using N2.Engine;
using N2.Persistence;
using N2.Persistence.NH;
using N2.Persistence.NH.Finder;
using N2.Tests.Fakes;
using NHibernate.Tool.hbm2ddl;
using N2.Edit;
using N2.Persistence.Finder;
using N2.Security;
using N2.Web;
using N2.Edit.Workflow;
using N2.Persistence.Proxying;
using NHibernate;
using N2.Definitions.Static;

namespace N2.Tests
{
    public static class TestSupport
    {
        public static void Setup(out IDefinitionManager definitions, out ContentActivator activator, out IItemNotifier notifier, out FakeSessionProvider sessionProvider, out ItemFinder finder, out SchemaExport schemaCreator, out InterceptingProxyFactory proxyFactory, params Type[] itemTypes)
        {
			Setup(out definitions, out activator, out notifier, out proxyFactory, itemTypes);

            DatabaseSection config = (DatabaseSection)ConfigurationManager.GetSection("n2/database");
            ConnectionStringsSection connectionStrings = (ConnectionStringsSection)ConfigurationManager.GetSection("connectionStrings");
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder(definitions, new ClassMappingGenerator(), new ThreadContext(), config, connectionStrings);

            FakeWebContextWrapper context = new Fakes.FakeWebContextWrapper();

			sessionProvider = new FakeSessionProvider(new ConfigurationSource(configurationBuilder), new NHInterceptor(proxyFactory, configurationBuilder, notifier), context);

            finder = new ItemFinder(sessionProvider, definitions);

            schemaCreator = new SchemaExport(configurationBuilder.BuildConfiguration());
        }

		public static IDefinitionManager SetupDefinitions(params Type[] itemTypes)
		{
			IItemNotifier notifier;
			IDefinitionManager definitions;
			InterceptingProxyFactory proxyFactory;
			ContentActivator activator;
			Setup(out definitions, out activator, out notifier, out proxyFactory, itemTypes);
			return definitions;
		}

		public static void Setup(out IDefinitionManager definitions, out ContentActivator activator, out IItemNotifier notifier, out InterceptingProxyFactory proxyFactory, params Type[] itemTypes)
        {
            ITypeFinder typeFinder = new Fakes.FakeTypeFinder(itemTypes[0].Assembly, itemTypes);

			DefinitionBuilder definitionBuilder = new DefinitionBuilder(typeFinder, new EngineSection());
			notifier = new ItemNotifier();
			proxyFactory = new InterceptingProxyFactory();
			activator = new ContentActivator(new N2.Edit.Workflow.StateChanger(), notifier, proxyFactory);
			definitions = new DefinitionManager(new [] { new DefinitionProvider(definitionBuilder) }, activator);
			((DefinitionManager)definitions).Start();
		}

		public static T Stub<T>()
			where T: class
		{
			return Rhino.Mocks.MockRepository.GenerateStub<T>();
		}

        public static void Setup(out N2.Edit.IEditManager editor, out IVersionManager versions, IDefinitionManager definitions, IPersister persister, IItemFinder finder)
        {
            var changer = new N2.Edit.Workflow.StateChanger();
            versions = new VersionManager(persister.Repository, finder, changer);
            editor = new EditManager(definitions, persister, versions, new SecurityManager(new ThreadContext(), new EditSection()), null, null, null, changer, null);
        }

        public static void Setup(out ContentPersister persister, ISessionProvider sessionProvider, N2.Persistence.IRepository<int, ContentItem> itemRepository, INHRepository<int, ContentDetail> linkRepository, ItemFinder finder, SchemaExport schemaCreator)
        {
            persister = new ContentPersister(itemRepository, linkRepository, finder);

#if NH2_1
            schemaCreator.Execute(false, true, false, sessionProvider.OpenSession.Session.Connection, null);
#else
			schemaCreator.Execute(false, true, false, false, sessionProvider.OpenSession.Session.Connection, null);
#endif
        }

        internal static void Setup(out ContentPersister persister, FakeSessionProvider sessionProvider, ItemFinder finder, SchemaExport schemaCreator)
        {
            IRepository<int, ContentItem> itemRepository = new ContentItemRepository(sessionProvider);
            INHRepository<int, ContentDetail> linkRepository = new NHRepository<int, ContentDetail>(sessionProvider);

            Setup(out persister, sessionProvider, itemRepository, linkRepository, finder, schemaCreator);
        }
    }
}