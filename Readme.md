# PepperDash Core (c) 2020

## [Latest Release](https://github.com/PepperDash/PepperDashCore/releases/latest)

## License
Provided under MIT license

## Overview
PepperDash Core is an open source Crestron SIMPL# library that can be used in SIMPL# Pro applications such as Essentials or as a standalone library with SIMPL+ wrappers to expose functionality in SIMPL Windows programs.

## Constituent Elements

- JSON Configuration File reading/writing
- PortalConfigReader
- Generic config classes
- Communications 
	 - TCP/IP client and server
	 - Secure TCP/IP client and server
	 - UDP server
	 - SSH client
	 - HTTP SSE client
	 - HTTP (RESTful client)
- Debugging
	 - Console debugging
	 - Logging both to Crestron error log as well as a custom log file
- System Info
- Reports system and Ethernet information to SIMPL via SIMPL+
- Device Class, IKeyed and IKeyName Interfaces
	 - Base level device class that most classes derive from
- Password Manager

## Minimum Requirements
- PepperDash Core runs on any Crestron 3-series processor or Crestron's VC-4 platform.
- To edit and compile the source, Microsoft Visual Studio 2008 Professional with SP1 is required.
- Crestron's Simpl# Plugin is also required (must be obtained from Crestron).

## Dependencies

None

## Utilization
PepperDash Core has two main applications:

 1. As a utility library for SIMPL# Pro applications like [Essentials]([Essentials](https://github.com/PepperDash/Essentials))
 2. As a library referenced by SIMPL+ wrapper modules in a SIMPL Windows application

 ## Documentation
 For detailed documentation, follow this [LINK](https://github.com/PepperDash/PepperDashCore/wiki) to the Wiki.


