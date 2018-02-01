﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using GTRevo.Core.Commands;
using GTRevo.Core.Transactions;
using NSubstitute;
using Xunit;

namespace GTRevo.Core.Tests.Commands
{
    public class CommandContextStackTests
    {
        private readonly CommandContextStack sut;

        public CommandContextStackTests()
        {
            sut = new CommandContextStack();
        }

        [Fact]
        public void DefaultIsNull()
        {
            sut.PeekOrDefault.Should().BeNull();
            sut.CurrentCommand.Should().BeNull();
            sut.UnitOfWork.Should().BeNull();
        }

        [Fact]
        public void PushPush_ThenReturnsLast()
        {
            var commandContext1 = CreateContext();
            var commandContext2 = CreateContext();

            sut.Push(commandContext1);
            sut.Push(commandContext2);

            sut.PeekOrDefault.Should().Be(commandContext2);
            sut.CurrentCommand.Should().Be(commandContext2.CurrentCommand);
            sut.UnitOfWork.Should().Be(commandContext2.UnitOfWork);
        }

        [Fact]
        public void PushPushPop_ThenReturnsFirst()
        {
            var commandContext1 = CreateContext();
            var commandContext2 = CreateContext();

            sut.Push(commandContext1);
            sut.Push(commandContext2);
            sut.Pop();

            sut.PeekOrDefault.Should().Be(commandContext1);
            sut.CurrentCommand.Should().Be(commandContext1.CurrentCommand);
            sut.UnitOfWork.Should().Be(commandContext1.UnitOfWork);
        }

        [Fact]
        public void PushPop_ThenReturnsNull()
        {
            var commandContext = CreateContext();

            sut.Push(commandContext);
            sut.Pop();

            sut.PeekOrDefault.Should().BeNull();
            sut.CurrentCommand.Should().BeNull();
            sut.UnitOfWork.Should().BeNull();
        }

        private ICommandContext CreateContext()
        {
            var commandContext = Substitute.For<ICommandContext>();
            commandContext.CurrentCommand.Returns(Substitute.For<ICommandBase>());
            commandContext.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
            return commandContext;
        }
    }
}
