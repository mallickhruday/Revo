﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTRevo.Core.Events;
using GTRevo.Infrastructure.Globalization;
using GTRevo.Infrastructure.Globalization.Messages;
using NSubstitute;
using Xunit;

namespace GTRevo.Infrastructure.Tests.Globalization
{
    public class MessageRepositoryTests
    {
        private readonly IEventBus eventBus;
        private readonly ILocaleMessageSourceFactory[] localeMessageSourceFactories;
        private MessageRepository sut;

        public MessageRepositoryTests()
        {
            localeMessageSourceFactories = new[]
            {
                Substitute.For<ILocaleMessageSourceFactory>(),
                Substitute.For<ILocaleMessageSourceFactory>(),
                Substitute.For<ILocaleMessageSourceFactory>()
            };

            localeMessageSourceFactories[0].LocaleCode.Returns("cs-CZ");
            localeMessageSourceFactories[0].Priority.Returns(0);
            localeMessageSourceFactories[0].MessageSource.Returns(Substitute.For<IMessageSource>());

            localeMessageSourceFactories[1].LocaleCode.Returns("sk-SK");
            localeMessageSourceFactories[1].MessageSource.Returns(Substitute.For<IMessageSource>());

            localeMessageSourceFactories[2].LocaleCode.Returns("cs-CZ");
            localeMessageSourceFactories[2].Priority.Returns(1);
            localeMessageSourceFactories[2].MessageSource.Returns(Substitute.For<IMessageSource>());

            eventBus = Substitute.For<IEventBus>();
        }

        [Fact]
        public void GetMessagesForLocale_GetsMessages()
        {
            localeMessageSourceFactories[0].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new[]
                {
                    new KeyValuePair<string, string>("hello", "ahoj"),
                    new KeyValuePair<string, string>("coffee", "kafe")
                }));
            localeMessageSourceFactories[1].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new[]
                {
                    new KeyValuePair<string, string>("squirrel", "drevokocur")
                }));
            localeMessageSourceFactories[2].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new[]
                {
                    new KeyValuePair<string, string>("squirrel", "veverka"),
                    new KeyValuePair<string, string>("coffee", "kava"),
                }));

            sut = new MessageRepository(localeMessageSourceFactories, eventBus);

            var csCZMessages = sut.GetMessagesForLocale(new Locale("cs-CZ")).Messages;
            var skSKMessages = sut.GetMessagesForLocale(new Locale("sk-SK")).Messages;

            var expectedCSCZMessages = new[]
            {
                new KeyValuePair<string, string>("hello", "ahoj"),
                new KeyValuePair<string, string>("coffee", "kava"),
                new KeyValuePair<string, string>("squirrel", "veverka")
            };

            var expectedSKSKMessages = new[]
            {
                new KeyValuePair<string, string>("squirrel", "drevokocur")
            };

            Assert.Equal(expectedCSCZMessages.Length, csCZMessages.Count);
            Assert.True(csCZMessages.All(x => expectedCSCZMessages.Contains(x)));
            Assert.Equal(expectedSKSKMessages.Length, skSKMessages.Count);
            Assert.True(skSKMessages.All(x => expectedSKSKMessages.Contains(x)));
        }

        [Fact]
        public void GetMessagesForLocale_NullForUnknownLocale()
        {
            sut = new MessageRepository(new ILocaleMessageSourceFactory[] {}, eventBus);
            Assert.Null(sut.GetMessagesForLocale(new Locale("cs-CZ")));
        }

        [Fact]
        public void GetMessagesForLocale_Messages_CachesResult()
        {
            localeMessageSourceFactories[0].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new KeyValuePair<string, string>[]
                {
                }));
            localeMessageSourceFactories[1].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new KeyValuePair<string, string>[]
                {
                }));

            sut = new MessageRepository(localeMessageSourceFactories.Take(2).ToArray(), eventBus);
            var messages = sut.GetMessagesForLocale(new Locale("cs-CZ")).Messages;
            Assert.Equal(messages, sut.GetMessagesForLocale(new Locale("cs-CZ")).Messages);
        }

        [Fact]
        public void Reload_ReloadsMessages()
        {
            localeMessageSourceFactories[0].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new[]
                {
                    new KeyValuePair<string, string>("coffee", "kafe"),
                }));

            sut = new MessageRepository(new []{ localeMessageSourceFactories[0] }, eventBus);

            localeMessageSourceFactories[0].MessageSource.Messages.Returns(
                ImmutableDictionary.CreateRange<string, string>(new[]
                {
                    new KeyValuePair<string, string>("coffee", "kava"),
                }));
            sut.Reload();

            Assert.True(sut.GetMessagesForLocale(new Locale("cs-CZ")).TryGetMessage("coffee", out string message));
            Assert.Equal("kava", message);
        }

        [Fact]
        public void Reload_PublishesEvent()
        {
            sut = new MessageRepository(new ILocaleMessageSourceFactory[] {}, eventBus);
            eventBus.Received(0).Publish(Arg.Any<MessageRepositoryReloadedEvent>());
            sut.Reload();
            eventBus.Received(1).Publish(Arg.Any<MessageRepositoryReloadedEvent>());
        }
    }
}