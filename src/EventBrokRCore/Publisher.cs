﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Threading.Tasks;

namespace EventBrokR
{
	/// <summary>
	/// Inspired by :
	/// http://elegantcode.com/2010/01/06/event-driven-architecture-publishing-events-using-an-ioc-container/
	/// </summary>
	public class Publisher : IPublisher
	{
		private IServiceProvider m_ServiceProvider;

		public Publisher(ILogger<Publisher> logger,
			IServiceProvider serviceProvider)
		{
			this.Container = new Container();
			this.Logger = logger;
			this.m_ServiceProvider = serviceProvider;
		}

		protected Container Container { get; }
		protected ILogger<Publisher> Logger { get; private set; }

		public void RegisterConsumer<TConsumer>()
		{
			var t = typeof(TConsumer);
			if (!Container.Registrations.Contains(t))
			{
				Container.Registrations.Add(t);
			}
		}

		public void Subscribe<TMessage>(Action<TMessage> predicate)
		{
			var consumer = new AnonymousConsumer<TMessage>(predicate);
			Container.Subscriptions.Add(consumer);
		}

		public virtual Task PublishAsync<T>(T eventMessage, int delay = 0)
		{
			// For tests
			var result = Task.Run(() =>
			{
				Publish(eventMessage);
			});
			return result;
		}

		public virtual dynamic CreateDynamicMessage(string messageName)
		{
			if (string.IsNullOrWhiteSpace(messageName))
			{
				throw new ArgumentException("messageName does not null or empty");
			}
			dynamic result = new System.Dynamic.ExpandoObject();
			result.Name = messageName;
			return result;
		}

		protected virtual void Publish<T>(T eventMessage)
		{
			var subscriptions = GetSubscriptions<T>();
			if (subscriptions == null || subscriptions.Count() == 0)
			{
				return;
			}

			foreach (var subscription in subscriptions)
			{
				PublishToConsumer(subscription, eventMessage);
			}
		}

		private void PublishToConsumer<T>(Consumer<T> x, T eventMessage)
		{
			try
			{
				x.Instance.Handle(eventMessage);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, ex.Message);
			}
			finally
			{
				var instance = x as IDisposable;
				if (instance != null)
				{
					instance.Dispose();
				}
			}
		}

		internal IEnumerable<Consumer<T>> GetSubscriptions<T>()
		{
			var result = new List<Consumer<T>>();

			var consumerInterfaceName = typeof(IConsumer<T>).FullName;
			var consumerTypes = from svc in Container.Registrations
								from itf in svc.GetInterfaces()
								where itf.FullName != null
									&& itf.FullName.Equals(consumerInterfaceName, StringComparison.InvariantCultureIgnoreCase)
								select svc;

			foreach (var svc in consumerTypes)
			{
				var consumer = Resolve<T>(svc);
				result.Add(consumer);
			}

			var anonymousSubscriptions = from subscription in Container.Subscriptions
										 select new Consumer<T>((IConsumer<T>)subscription);

			result.AddRange(anonymousSubscriptions);

			return result;
		}

		private Consumer<T> Resolve<T>(Type t)
		{
			var instance = ActivatorUtilities.CreateInstance(m_ServiceProvider, t);
			return new Consumer<T>((IConsumer<T>)instance);
		}
	}
}
