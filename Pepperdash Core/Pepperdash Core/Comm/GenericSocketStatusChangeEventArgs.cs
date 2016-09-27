using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;


namespace PepperDash.Core
{
	public delegate void GenericSocketStatusChangeEventDelegate(ISocketStatus client);
}