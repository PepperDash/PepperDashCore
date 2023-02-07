﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
    /// <summary>
    /// Unique key interface to require a unique key for the class
    /// </summary>
	public interface IKeyed
	{
        /// <summary>
        /// Unique Key
        /// </summary>
		string Key { get; }
	}

    /// <summary>
    /// Named Keyed device interface. Forces the devie to have a Unique Key and a name. 
    /// </summary>
	public interface IKeyName : IKeyed
	{
        /// <summary>
        /// Isn't it obvious :)
        /// </summary>
		string Name { get; }
	}
}