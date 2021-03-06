﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Revo.Domain.Entities;
using Revo.Domain.Entities.EventSourcing;
using Revo.Infrastructure.EventSourcing;

namespace Revo.Infrastructure.Repositories
{
    public class EventSourcedAggregateStore : IAggregateStore
    {
        private readonly IEventSourcedAggregateRepository eventSourcedRepository;

        public EventSourcedAggregateStore(IEventSourcedAggregateRepository eventSourcedRepository)
        {
            this.eventSourcedRepository = eventSourcedRepository;
        }

        public virtual bool NeedsSave => eventSourcedRepository.IsChanged;

        public void Add<T>(T aggregate) where T : class, IAggregateRoot
        {
            ((dynamic)eventSourcedRepository).Add((dynamic)aggregate);
        }

        public T Find<T>(Guid id) where T : class, IAggregateRoot
        {
            return ((dynamic)eventSourcedRepository).Find<T>(id);
        }

        public Task<T> FindAsync<T>(Guid id) where T : class, IAggregateRoot
        {
            return ((dynamic)eventSourcedRepository).FindAsync<T>(id);
        }

        public T Get<T>(Guid id) where T : class, IAggregateRoot
        {
            return ((dynamic)eventSourcedRepository).Get<T>(id);
        }

        public Task<T> GetAsync<T>(Guid id) where T : class, IAggregateRoot
        {
            return ((dynamic)eventSourcedRepository).GetAsync<T>(id);
        }

        public IEnumerable<IAggregateRoot> GetTrackedAggregates()
        {
            return eventSourcedRepository.GetLoadedAggregates();
        }

        public bool CanHandleAggregateType(Type aggregateType)
        {
            return typeof(IEventSourcedAggregateRoot).IsAssignableFrom(aggregateType);
        }

        public virtual Task SaveChangesAsync()
        {
            return eventSourcedRepository.SaveChangesAsync();
        }

        public void Remove<T>(T aggregate) where T : class, IAggregateRoot
        {
            eventSourcedRepository.Remove((dynamic)aggregate);
        }
    }
}
