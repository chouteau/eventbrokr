﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBrokR
{
	public abstract class DynamicConsumerBase : IConsumer<System.Dynamic.ExpandoObject>
	{
		public void Handle(System.Dynamic.ExpandoObject eventMessage)
		{
			if (!eventMessage.IsDynamicPropertyExists("Name"))
			{
				return;
			}

			var d = eventMessage as dynamic;
			HandleMessage(d.Name as string, d);
		}

		protected abstract void HandleMessage(string name, dynamic message);
	}
}
