﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBrokR
{
	class AnonymousConsumer<T> : IConsumer<T>
	{
		private Action<T> m_Predicate;

		public AnonymousConsumer(Action<T> predicate)
		{
			m_Predicate = predicate;
		}

		public async Task HandleAsync(T eventMessage)
		{
			await Task.Run(() => m_Predicate.Invoke(eventMessage));
		}
	}
}
