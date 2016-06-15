using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
	public interface IKeyed
	{
		string Key { get; }
	}
}