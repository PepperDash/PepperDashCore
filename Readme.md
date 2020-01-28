# Pepperdash Core

## Overview

PepperDash.Core is a utility library used by PepperDash Essentials Framework and by standalone Simpl+ modules.

## Constituent Elements

-JSON Configuration File reading writing
-PortalConfigReader
-Generic config classes
-Communications 
    -TCP/IP client and server
    -Secure TCP/IP clinet and server
    -UDP server
    -SSH client
    -HTTP SSE client
    -HTTP (RESTful client)
-Debugging
-Console debugging
-Logging both to Crestron error log as well as a custom log file
-System Info
-Reports system and Ethernet information to SIMPL via S+
-Device Class
-Base level device class that most classes derive from
-Password Manager

#### Workflow process

- Create a Jira issue for the feature/bugfix.  If you know you're targeting this update to coincide with a new release of Core, specify that release (or create a new one) as the Fix Version for the Jira issue
- Branch from development using the syntax [feature/bugfix]/[pdc-x] (where x is the Jira issue number)
- Modify code to suit and test.  Make commits to the branch as you work.
- Log a Pull Request on www.bitbucket.org and tag Heath and Neil as reviewers

#### Pull Request process

- Check out the branch for the PR and review.
- If necessary, merge the latest Development branch into the PR branch.  Resolve any conflicts.
- Increment the appropriate Assembly version number to match the next release in Jira
- Build the project
- Copy PepperDash_Core.cpz and PepperDash_Core.dll from the bin folder to the CLZ Builds folder in the repo root.  Commit.
- Merge the PR in Bitbucket
