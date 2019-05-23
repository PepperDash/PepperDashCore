# Pepperdash Core

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
