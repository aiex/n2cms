﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using N2.Edit.Workflow;
using N2.Security;
using N2.Tests.Fakes;
using N2.Configuration;
using N2.Edit.Workflow.Commands;
using N2.Definitions;
using N2.Edit;
using Rhino.Mocks;
using N2.Persistence;
using N2.Persistence.NH;
using N2.Tests.Workflow.Items;

namespace N2.Tests.Workflow
{
    public abstract class CommandFactoryTestsBase : ItemPersistenceMockingBase
    {
        protected CommandFactory commands;
        protected CommandDispatcher dispatcher;
        protected IDefinitionManager definitions;
        protected FakeVersionManager versions;
        protected ContentItem item, child;
		protected IBinder<CommandContext> nullBinder = new NullBinder<CommandContext>();
		protected IValidator<CommandContext> nullValidator = new NullValidator<CommandContext>();
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var changer = new StateChanger();
            versions = new FakeVersionManager(repository, changer);
            var builder = new DefinitionBuilder(new FakeTypeFinder(typeof(StatefulItem)), new EngineSection());
            definitions = new DefinitionManager(builder, changer, new NotifyingInterceptor());
            var editManager = new EditManager(definitions, persister, versions, null, null, null, changer, new EditSection());
            var security = new SecurityManager(new FakeWebContextWrapper(), new EditSection());
            commands = new CommandFactory(persister, security, versions, editManager, changer);
            dispatcher = new CommandDispatcher(commands);
			item = CreateOneItem<StatefulItem>(1, "first", null);
			child = CreateOneItem<StatefulItem>(2, "child", item);
		}

        protected abstract CommandBase<CommandContext> CreateCommand(CommandContext context);

        [TestCase(Interfaces.Editing, false)]
        [TestCase(Interfaces.Editing, true)]
        public void IsCheckedForSecurity(string userInterface, bool useVersion)
        {
            DynamicPermissionMap.SetRoles(item, Permission.Read, "None");
            if (useVersion)
                item = MakeVersion(item);
			var context = new CommandContext(item, userInterface, CreatePrincipal("someone"), new NullBinder<CommandContext>(), new NullValidator<CommandContext>());

            var command = CreateCommand(context);
            dispatcher.Execute(command, context);

            Assert.That(context.ValidationErrors.Count, Is.EqualTo(1));
            Assert.That(context.ValidationErrors.First().Name, Is.EqualTo("Unauthorized"));
        }

        [TestCase(Interfaces.Editing, false)]
        [TestCase(Interfaces.Editing, true)]
        public void IsValidated_WhenInterface_IsEditing(string userInterface, bool useVersion)
        {
            if (useVersion)
                item = MakeVersion(item);

			var validator = mocks.Stub<IValidator<CommandContext>>();
            mocks.ReplayAll();

            var context = new CommandContext(item, userInterface, CreatePrincipal("admin"), nullBinder, validator);

            var command = CreateCommand(context);
            dispatcher.Execute(command, context);

            validator.AssertWasCalled(b => b.Validate(context));
        }

        [Test]
        public void DoesntMakeVersion_OfUnsavedItem()
        {
            var context = new CommandContext(new StatefulItem(), Interfaces.Editing, CreatePrincipal("admin"), nullBinder, nullValidator);

            var command = CreateCommand(context);
            dispatcher.Execute(command, context);

            Assert.That(repository.database.Values.Count(v => v.VersionOf == item), Is.EqualTo(0));
        }

        [Test]
        public void PreviouslyPublishedVersion_CausesNewVersion()
        {
            var version = MakeVersion(item);
            version.State = ContentState.Unpublished;

            var context = new CommandContext(version, Interfaces.Editing, CreatePrincipal("admin"), nullBinder, nullValidator);

            var command = CreateCommand(context);
            dispatcher.Execute(command, context);

            Assert.That(versions.GetVersionsOf(item).Count, Is.EqualTo(3));
        }


        protected ContentItem MakeVersion(ContentItem master)
        {
            var version = CreateOneItem<StatefulItem>(2, "version", null);
            version.VersionOf = master;
            return version;
        }
    }
}