<!--
SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
SPDX-License-Identifier: Apache License 2.0
-->
- [Introduction](#introduction)
  - [Communicate with us](#communicate-with-us)
- [The contribution process](#the-contribution-process)
  - [Preparing pull requests](#preparing-pull-requests)
  - [Submitting pull requests](#submitting-pull-requests)
  - [Release a new version](#release-a-new-version)


## Introduction

Welcome to Project MONAI Deploy Informatics Gateway! We're excited you're here and want to contribute. This documentation is intended for individuals and institutions interested in contributing to the MONAI Deploy Informatics Gateway. MONAI Deploy Informatics Gateway is an open-source project. As such, its success relies on its community of contributors willing to keep improving it. Therefore, your contribution will be a valued addition to the code base; we ask that you read this page and understand our contribution process, whether you are a seasoned open-source contributor or a first-time contributor.

### Communicate with us

We are happy to talk with you about your MONAI Deploy Informatics Gateway needs and your ideas for contributing to the project. One way to do this is to create an [issue](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/issues/new/choose) by discussing your thoughts. It might be that a very similar feature is under development or already exists, so an issue is a great starting point. If you are looking for an issue to resolve that will help Project MONAI Deploy Informatics Gateway, see the [*good first issue*](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/labels/good%20first%20issue) and [*help wanted*](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/labels/help%20wanted) labels.

## The contribution process

_Pull request early_

We encourage you to create pull requests early. It helps us track the contributions under development, whether they are ready to be merged or not. Change your pull request's title to begin with `[WIP]` or [create a draft pull request](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/about-pull-requests#draft-pull-requests) until it is ready for formal review.

### Preparing pull requests

This section highlights all the necessary preparation steps required before sending a pull request.
To collaborate efficiently, please read through this section and follow them.

* [Checking the coding style](#checking-the-coding-style)
* [Test Projects](#test-projects)
* [Building documentation](#building-the-documentation)

#### Checking the coding style

##### C# Coding Style

We follow the same [coding style](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md) as described by [dotnet](https://github.com/dotnet)/[runtime](https://github.com/dotnet/runtime) project.


The general rule we follow is "use Visual Studio defaults" or simply to [CodeMaid](https://marketplace.visualstudio.com/items?itemName=SteveCadwallader.CodeMaid) extension.

1. We use [Allman style](http://en.wikipedia.org/wiki/Indent_style#Allman_style) braces, where each brace begins on a new line. A single line statement block can go without braces, but the block must be properly indented on its line and must not be nested in other statement blocks that use braces (See rule 17 for more details). One exception is that a `using` statement is permitted to be nested within another `using` statement by starting on the following line at the same indentation level, even if the nested `using` contains a controlled block.
2. We use four spaces of indentation (no tabs).
3. We use `_camelCase` for internal and private fields and use `readonly` where possible. Prefix internal and private instance fields with `_`, static fields with `s_`, and thread static fields with `t_`. When used on static fields, `readonly` should come after `static` (e.g. `static readonly` not `readonly static`). Public fields should be used sparingly and use PascalCasing with no prefix when used.
4. We avoid `this.` unless necessary.
5. We always specify the visibility, even if it's the default (e.g.
   `private string _foo' not `string _foo'). Visibility should be the first modifier (e.g.
   `public abstract` not `abstract public`).
6. Namespace imports should be specified at the top of the file, *outside* of `namespace` declarations should be sorted alphabetically.
7. Avoid more than one empty line at any time. For example, do not have two
   blank lines between members of a type.
8. Avoid spurious-free spaces.
   For example, avoid `if (someVar == 0)...`, where the dots mark the spurious-free spaces.
   Consider enabling "View White Space (Ctrl+R, Ctrl+W)" or "Edit -> Advanced -> View White Space" if using Visual Studio to aid detection.
9. If a file happens to differ in style from these guidelines (e.g. private members are named `m_member`
   rather than `_member`), the existing style in that file takes precedence.
10. We only use `var` when it's obvious what the variable type is (e.g. `var stream = new FileStream(...)` not `var stream = OpenStandardInput()`).
11. We use language keywords instead of BCL types (e.g. `int, string, float` instead of `Int32, String, Single`, etc) for both type references as well as method calls (e.g. `int.Parse` instead of `Int32.Parse`). See issue [#13976](https://github.com/dotnet/runtime/issues/13976) for examples.
12. We use PascalCasing to name all our constant local variables and fields. The only exception is for interop code where the constant value should exactly match the name and value of the code you are calling via interop.
13. We use "`nameof(...)` "instead of ``` "..." ``` whenever possible and relevant.
14. Fields should be specified at the top within type declarations.
15. When including non-ASCII characters in the source code, use Unicode escape sequences (\uXXXX) instead of literal characters. Literal non-ASCII characters occasionally get garbled by a tool or editor.
16. When using labels (for goto), indent the label one less than the current indentation.
17. When using a single-statement if, we follow these conventions:
    - Never use single-line form (for example: `if (source is null) throw new ArgumentNullException("source");`)
    - Using braces is always accepted and required if any block of an `if`/`else if`/.../`else` compound statement uses braces or if a single statement body spans multiple lines.
    - Braces may be omitted only if the body of *every* block associated with an `if`/`else if`/.../`else` compound statement is placed on a single line.

An [EditorConfig](https://editorconfig.org "EditorConfig homepage") file (`.editorconfig`) has been provided at the root of the runtime repository, enabling C# auto-formatting conforming to the above guidelines.


##### License information

All source code files should start with this paragraph:

```
// Copyright <YEAR FROM-YEAR TO> MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
```

#### Test Projects

All C# projects reside in their directory, including a `Tests/` subdirectory.
Test projects are also linked in the main solution file [Monai.Deploy.InformaticsGateway.sln](src/Monai.Deploy.InformaticsGateway.sln) and can be executed either using Visual Studio's *Test Explorer* or with the `dotnet test` command line.


_If it's not tested, it's broken_

An appropriate set of tests should accompany all new functionality.
MONAI Deploy Informatics Gateway functionality has plenty of unit tests from which you can draw inspiration, and you can reach out to us if you are unsure how to proceed with testing.


#### Building the documentation

Documentation for MONAI Deploy Informatics Gateway is located at `docs/` and requires [DocFX](https://dotnet.github.io/docfx/) to build.

Please follow the [instructions](https://dotnet.github.io/docfx/tutorial/docfx_getting_started.html#2-use-docfx-as-a-command-line-tool) to install Mono and download the DocFX command-line tool to build the documentation.

```bash
[path-to]/docfx.exe docs/docfx.json
```

#### Automatic code formatting

Install [CodeMaid](https://marketplace.visualstudio.com/items?itemName=SteveCadwallader.CodeMaid) extension for Visual Studio or
**SHIFT+ALT+F** in Visual Studio Code.

#### Signing your work

MONAI enforces the [Developer Certificate of Origin](https://developercertificate.org/) (DCO) on all pull requests.
All commit messages should contain the `Signed-off-by` line with an email address. The [GitHub DCO app](https://github.com/apps/dco) is deployed on MONAI. The pull request's status will be `failed` if commits do not contain a valid `Signed-off-by` line.

Git has a `-s' (or `--signoff`) command-line option to append this automatically to your commit message:

```bash
git commit -s -m 'a new commit'
```

The commit message will be:

```text
    a new commit

    Signed-off-by: Your Name <yourname@example.org>
```

Full text of the DCO:

```text
Developer Certificate of Origin
Version 1.1

Copyright (C) 2004, 2006 The Linux Foundation and its contributors.
1 Letterman Drive
Suite D4700
San Francisco, CA, 94129

Everyone is permitted to copy and distribute verbatim copies of this
license document, but changing it is not allowed.


Developer's Certificate of Origin 1.1

By making a contribution to this project, I certify that:

(a) The contribution was created in whole or in part by me and I
    have the right to submit it under the open source license
    indicated in the file; or

(b) The contribution is based upon previous work that, to the best
    of my knowledge, is covered under an appropriate open source
    license and I have the right under that license to submit that
    work with modifications, whether created in whole or in part
    by me, under the same open source license (unless I am
    permitted to submit under a different license), as indicated
    in the file; or

(c) The contribution was provided directly to me by some other
    person who certified (a), (b) or (c) and I have not modified
    it.

(d) I understand and agree that this project and the contribution
    are public and that a record of the contribution (including all
    personal information I submit with it, including my sign-off) is
    maintained indefinitely and may be redistributed consistent with
    this project or the open source license(s) involved.
```

### Submitting pull requests

#### Branching

- `main`: the `main` branch is **always ready** with only completed, tested, and verified features. The CI automatically triggers an official build when a PR is merged into the branch.
- `develop`: the `develop` branch is the active development branch and is for features that are ready for testing. Releases made in this branch are prefixed with `beta`.
- `release/`: `release` branches are created when a new official release is imminent and does not accept new features except bug fixes. Releases made in this branch are prefixed with `rc`. A pull request shall be created targeting `main` and `develop`.
- `feature/`: `feature` branches are created for a specific branch. Releases made in this branch are prefixed with `alpha.{branchName}`. A pull request shall be created when the feature is ready, targeting `develop` branch.

#### Begin with Your Contribution Journey with a Pull Request

All code changes must be done via [pull requests](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/proposing-changes-to-your-work-with-pull-requests).

1. Create a new ticket or take a known ticket from [the issue list][issue list].
1. Check if there's already a branch dedicated to the task.
1. If the task has not been taken, [create a new branch in your fork](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork) of the codebase named `[ticket_id]-[task_name]`.
For example, branch name `{username}/19-ci-pipeline-setup` corresponds to issue #19.
1. Ideally, the new branch should be based on the latest `develop` branch.
1. Make changes to the branch ([use detailed commit messages if possible](https://chris.beams.io/posts/git-commit/)).
1. Make sure that new tests cover the changes and the changed codebase [passes all tests locally](#test-projects).


##### When You Are Ready to Merge
1. [Create a new pull request](https://help.github.com/en/desktop/contributing-to-projects/creating-a-pull-request) from the task branch to the `develop` branch, with detailed descriptions of the purpose of this pull request by filling out the [template](./.github/pull_request_template.md).
1. Make sure all checks are successful
1. Complete tasks listed in the template as much as possible
1. Wait for reviews; if there are reviews, make point-to-point responses, make further code changes if needed.
1. If there are conflicts between the pull request branch and the target branch, pull the changes from the target branch and resolve the conflicts locally.
1. Reviewer and contributor may have discussions back and forth until all comments are addressed.
1. Wait for the pull request to be merged.



### Release a new version

A PR is made from a `release/` branch to the `main` branch when a new official release is ready. The CI process validates & builds all components required, composes the release notes, and publishes the build in the [Releases](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases) section and any Docker images in the [Packages](https://github.com/orgs/Project-MONAI/packages?repo_name=monai-deploy-informatics-gateway) section.

- [Actions](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions)
- [Issues](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/issues)
- [Milestones](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/milestones)
- [Releases](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/releases)
- [Packages](https://github.com/orgs/Project-MONAI/packages?repo_name=monai-deploy-informatics-gateway)
