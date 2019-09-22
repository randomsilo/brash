
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brash.Model;

namespace BrashTest.Mock.Model
{
	public class Person : IAskId
	{
		// IdPattern
		public int? PersonId { get; set; }

		// Fields
		public string LastName { get; set; }
		public string FirstName { get; set; }
		public string MiddleName { get; set; }

        // Interface Implementations
		public string GetAskIdPropertyName()
		{
			return "PersonId";
		}
    }
}