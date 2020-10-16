using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
	/// <summary>
	/// Main <c>BoolChangeEventArgs</c> class	
	/// </summary>
	/// <remarks>
	/// Bool change event args are accessible by all classes and are used to pass boolean values from S# to S+.  Constants can be created
	/// in the classes using the event args.  Constants, when used, associate returned values with specific S+ properties.
	/// <example>
	/// Implement the following when 
	/// <code>
	/// public event EventHandler<BoolChangeEventArgs> BoolChange;
	/// </code>
	/// <code>
	/// protected void OnBoolChange(bool state, ushort index, ushort type)
	///	{
	///		var handler = BoolChange;
	///		if (handler == null) return;
	///		var args = new BoolChangeEventArgs(state, type) {Index = index};
	///		BoolChange(this, args);
	///	}
	/// </code>
	/// When referencing events in S+ you must create a event handler callback method and register the event.		
	/// <code>
	///	EventHandler BoolChanged(SampleSimplSharpClass sender, BoolChangeEventArgs args)
	///	{
	///		If(DebugEnable) Trace("BoolChanged: args[%u].Type[%u] = %u\r", args.Index, args.Type, args.IntValue);
	/// 
	///		Switch(args.Type)
	///		{
	///			Case(SampleSimplSharpClass.BoolValueChange):
	///			{
	///				// specific bool value changed
	///			}
	///			Default:
	///			{
	///				// generic bool value changed
	///			}
	///		}
	///	}
	/// </code>
	/// Register the event
	/// <code>
	/// Function Main()
	/// {
	///		//RegisterEveent([S+ class name], [S# event], [S+ event handler]);
	///		RegisterEveent(mySplusClass, BoolChange, BoolChanged);
	///	}
	/// </code>	
	/// </example>
	/// </remarks>
	public class BoolChangeEventArgs : EventArgs
	{
		/// <summary>
		/// State property
		/// </summary>
		/// <value>Boolean</value>
		public bool State { get; set; }
		
		/// <summary>
		/// Integer value property
		/// </summary>
		/// <value>Ushort</value>
		public ushort IntValue { get { return (ushort)(State ? 1 : 0); } }
		
		/// <summary>
		/// Type property
		/// </summary>
		/// <value>Ushort</value>
		public ushort Type { get; set; }
		
		/// <summary>
		/// Boolean change event args index
		/// </summary>
		public ushort Index { get; set; }
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>
		/// S+ requires an empty contructor, otherwise the class will not be avialable in S+.
		/// </remarks>
		public BoolChangeEventArgs()
		{

		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>
		/// Accepts State as a boolean and Type as a ushort.
		/// </remarks>
		/// <param name="state">a boolean value</param>
		/// <param name="type">a ushort number</param>
		public BoolChangeEventArgs(bool state, ushort type)
		{
			State = state;
			Type = type;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>
		/// Accepts State as a boolean, type as a ushort and index as ushort.
		/// </remarks>
		/// <param name="state">a boolean value</param>
		/// <param name="type">a ushort number</param>
		/// <param name="index">a ushort number</param>
		public BoolChangeEventArgs(bool state, ushort type, ushort index)
		{
			State = state;
			Type = type;
			Index = index;
		}
	}

	/// <summary>
	/// Main <c>UshrtChangeEventArgs</c> class	
	/// </summary>
	/// <remarks>
	/// Ushort change event args are accessible by all classes and are used to pass ushort values from S# to S+.  Constants can be created
	/// in the classes using the event args.  Constants, when used, associate returned values with specific S+ properties.
	/// <example>
	/// When using UshrtChangeEventArgs in a class you will need to include a change handler method.
	/// <code>
	/// public event EventHandler<UshrtChangeEventArgs> UshortChange;
	/// </code>
	/// <code>
	/// protected void OnUshortChange(ushort value, ushort index, ushort type)
	///	{
	///		var handler = UshortChange;
	///		if (handler == null) return;
	///		var args = new UshrtChangeEventArgs(value, type) {Index = index};
	///		UshortChange(this, args);
	///	}
	/// </code>
	/// When referencing events in S+ you must create a event handler callback method and register the event.		
	/// <code>
	///	EventHandler UshortChanged(SampleSimplSharpClass sender, UshrtChangeEventArgs args)
	///	{
	///		If(DebugEnable) Trace("UshortChanged: args[%u].Type[%u] = %u\r", args.Index, args.Type, args.IntValue);
	/// 
	///		Switch(args.Type)
	///		{
	///			Case(SampleSimplSharpClass.UshortValueChange):
	///			{
	///				// specific ushort value changed
	///			}
	///			Default:
	///			{
	///				// generic ushort value changed
	///			}
	///		}
	///	}
	/// </code>
	/// Register the event
	/// <code>
	/// Function Main()
	/// {
	///		//RegisterEveent([S+ class name], [S# event], [S+ event handler]);
	///		RegisterEveent(mySplusClass, UshortChange, UshortChanged);
	///	}
	/// </code>	
	/// </example>
	/// </remarks>
	public class UshrtChangeEventArgs : EventArgs
	{
		/// <summary>
		/// Ushort change event args integer value
		/// </summary>
		public ushort IntValue { get; set; }
		
		/// <summary>
		/// Ushort change event args type
		/// </summary>
		public ushort Type { get; set; }
		
		/// <summary>
		/// Ushort change event args index
		/// </summary>
		public ushort Index { get; set; }
		
		/// <summary>
		/// Constructor
		/// </summary>
		public UshrtChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="intValue">a ushort number</param>
		/// <param name="type">a ushort number</param>
		public UshrtChangeEventArgs(ushort intValue, ushort type)
		{
			IntValue = intValue;
			Type = type;
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="intValue">a ushort number</param>
		/// <param name="type">a ushort number</param>
		/// <param name="index">a ushort number</param>
		public UshrtChangeEventArgs(ushort intValue, ushort type, ushort index)
		{
			IntValue = intValue;
			Type = type;
			Index = index;
		}
	}

	/// <summary>
	/// Main <c>StringChangeEventArgs</c> class	
	/// </summary>
	/// <remarks>
	/// String change event args are accessible by all classes and are used to pass ushort values from S# to S+.  Constants can be created
	/// in the classes using the event args.  Constants, when used, associate returned values with specific S+ properties.
	/// <example>
	/// When using StringChangeEventArgs in a class you will need to include a change handler method.
	/// <code>
	/// public event EventHandler<StringChangeEventArgs> StringChange;
	/// </code>
	/// <code>
	/// protected void OnStringChange(string stringValue, ushort index, ushort type)
	///	{
	///		var handler = StringChange;
	///		if (handler == null) return;
	///		var args = new StringChangeEventArgs(stringValue, type) {Index = index};
	///		StringChange(this, args);
	///	}
	/// </code>
	/// When referencing events in S+ you must create a event handler callback method and register the event.		
	/// <code>
	///	EventHandler StringChanged(SampleSimplSharpClass sender, StringChangeEventArgs args)
	///	{
	///		If(DebugEnable) Trace("StringChanged: args[%u].Type[%u] = %u\r", args.Index, args.Type, args.StringValue);
	/// 
	///		Switch(args.Type)
	///		{
	///			Case(SampleSimplSharpClass.StringValueChange):
	///			{
	///				// specific ushort value changed
	///			}
	///			Default:
	///			{
	///				// generic ushort value changed
	///			}
	///		}
	///	}
	/// </code>
	/// Register the event
	/// <code>
	/// Function Main()
	/// {
	///		//RegisterEveent([S+ class name], [S# event], [S+ event handler]);
	///		RegisterEveent(mySplusClass, StringChange, StringChanged);
	///	}
	/// </code>	
	/// </example>
	/// </remarks>
	public class StringChangeEventArgs : EventArgs
	{
		/// <summary>
		/// String change event args value
		/// </summary>
		public string StringValue { get; set; }

		/// <summary>
		/// String change event args type
		/// </summary>
		public ushort Type { get; set; }

		/// <summary>
		/// string change event args index
		/// </summary>
		public ushort Index { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public StringChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="stringValue">a string</param>
		/// <param name="type">a ushort number</param>
		public StringChangeEventArgs(string stringValue, ushort type)
		{
			StringValue = stringValue;
			Type = type;
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="stringValue">a string</param>
		/// <param name="type">a ushort number</param>
		/// <param name="index">a ushort number</param>
		public StringChangeEventArgs(string stringValue, ushort type, ushort index)
		{
			StringValue = stringValue;
			Type = type;
			Index = index;
		}
	}	
}